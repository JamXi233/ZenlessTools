﻿// Copyright (c) 2021-2024, JamXi JSG-LLC.
// All rights reserved.

// This file is part of ZenlessTools.

// ZenlessTools is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// ZenlessTools is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with ZenlessTools.  If not, see <http://www.gnu.org/licenses/>.

// For more information, please refer to <https://www.gnu.org/licenses/gpl-3.0.html>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using static ZenlessTools.App;
using System.IO;
using static ZenlessTools.Depend.GachaModel;

namespace ZenlessTools.Depend
{
    public class GachaRecords
    {
        public async static Task GetGachaAsync(string url)
        {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string zenlessToolsFolderPath = Path.Combine(documentsPath, "JSG-LLC", "ZenlessTools", "GachaRecords");

            Directory.CreateDirectory(zenlessToolsFolderPath);

            var charRecords = await GetAllGachaRecordsAsync(url, "2001", "2");
            var lightRecords = await GetAllGachaRecordsAsync(url, "3001", "3");
            var regularRecords = await GetAllGachaRecordsAsync(url, "1001", "1");
            var bangbooRecords = await GetAllGachaRecordsAsync(url, "5001", "5");

            if (charRecords.Count == 0 && lightRecords.Count == 0 && regularRecords.Count == 0)
            {
                Logging.Write("未能获取任何调频记录，请检查链接或重试", 1);
                return;
            }

            string uid = charRecords.FirstOrDefault()?.Uid ?? lightRecords.FirstOrDefault()?.Uid ?? regularRecords.FirstOrDefault()?.Uid;

            if (string.IsNullOrEmpty(uid) || uid.Length != 8)
            {
                Logging.Write("抽卡链接UID无效", 1);
                return;
            }

            string filePath = Path.Combine(zenlessToolsFolderPath, $"{uid}.json");
            var existingData = new GachaData { info = new GachaInfo { uid = uid }, list = new List<GachaPool>() };

            if (File.Exists(filePath))
            {
                var jsonData = await File.ReadAllTextAsync(filePath);
                existingData = JsonConvert.DeserializeObject<GachaData>(jsonData) ?? existingData;

                // 对现有数据的时间格式进行修正
                foreach (var pool in existingData.list)
                {
                    foreach (var record in pool.records)
                    {
                        record.time = CorrectTimeFormat(record.time);
                    }
                }
            }

            MergeRecords(existingData, charRecords, 2);
            MergeRecords(existingData, lightRecords, 3);
            MergeRecords(existingData, regularRecords, 1);
            MergeRecords(existingData, bangbooRecords, 5);

            foreach (var pool in existingData.list)
            {
                pool.records = pool.records.OrderByDescending(r => r.id).ToList();
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping // 保留特殊字符
            };
            var serializedData = System.Text.Json.JsonSerializer.Serialize(existingData, options);
            await File.WriteAllTextAsync(filePath, serializedData);

            Logging.Write($"调频记录已保存到 {filePath}", 0);
        }

        private static void MergeRecords(GachaData existingData, List<GachaRecords> newRecords, int cardPoolId)
        {
            var pool = existingData.list.FirstOrDefault(p => p.cardPoolId == cardPoolId) ?? new GachaPool
            {
                cardPoolId = cardPoolId,
                cardPoolType = GetCardPoolType(cardPoolId),
                records = new List<GachaRecord>()
            };

            if (!existingData.list.Contains(pool))
            {
                existingData.list.Add(pool);
            }

            foreach (var newRecord in newRecords)
            {
                // 转换新记录的时间格式
                newRecord.Time = CorrectTimeFormat(newRecord.Time);

                // 查找并更新记录
                var existingRecord = pool.records.FirstOrDefault(r => r.id == newRecord.Id);
                if (existingRecord != null)
                {
                    existingRecord.gachaType = newRecord.GachaType;
                    existingRecord.itemId = newRecord.ItemId;
                    existingRecord.count = newRecord.Count;
                    existingRecord.time = newRecord.Time;
                    existingRecord.name = newRecord.Name;
                    existingRecord.lang = newRecord.Lang;
                    existingRecord.itemType = newRecord.ItemType;
                    existingRecord.rankType = newRecord.RankType;
                }
                else
                {
                    pool.records.Add(new GachaRecord
                    {
                        gachaType = newRecord.GachaType,
                        itemId = newRecord.ItemId,
                        count = newRecord.Count,
                        time = newRecord.Time,
                        name = newRecord.Name,
                        lang = newRecord.Lang,
                        itemType = newRecord.ItemType,
                        rankType = newRecord.RankType,
                        id = newRecord.Id
                    });
                }
            }
        }

        private static string CorrectTimeFormat(string time)
        {
            if (DateTime.TryParse(time, out DateTime parsedTime))
            {
                return parsedTime.ToString("yyyy-MM-dd HH:mm:ss");
            }
            return time;
        }

        private static string GetCardPoolType(int cardPoolId)
        {
            return cardPoolId switch
            {
                2 => "独家频段",
                3 => "音擎频段",
                1 => "常驻频段",
                5 => "邦布频段",
                _ => "未知频段"
            };
        }

        public async static Task<List<GachaRecords>> GetAllGachaRecordsAsync(string url, string gachaType, string realGachaType)
        {
            var client = new HttpClient();
            var records = new List<GachaRecords>();
            var count = 0;
            int urlIndex = url.IndexOf("&gacha_type=");
            var endId = "0";

            while (true)
            {
                try
                {
                    string newUrl = $"{url.Substring(0, urlIndex)}&gacha_type={gachaType}&real_gacha_type={realGachaType}&end_id={endId}";
                    var response = await client.GetAsync(newUrl);
                    var json = await response.Content.ReadAsStringAsync();
                    var jsonObj = JObject.Parse(json);

                    if (jsonObj["message"].ToString() == "authkey timeout")
                    {
                        records.Add(new GachaRecords { Uid = "authkey timeout" });
                        return records;
                    }
                    if (jsonObj["message"].ToString() == "visit too frequently" && jsonObj["retcode"].ToString() == "-110")
                    {
                        Logging.Write("等待...", 1);
                        WaitOverlayManager.RaiseWaitOverlay(true, "正在获取API信息,请不要退出", "等待...", true, 0);
                        await Task.Delay(TimeSpan.FromSeconds(0.05));
                        continue;
                    }

                    var data = jsonObj["data"];
                    if (data["list"].Count() == 0) break;

                    foreach (var item in data["list"])
                    {
                        var gachaRecord = new GachaRecords
                        {
                            Uid = item["uid"].ToString(),
                            GachaId = item["gacha_id"].ToString(),
                            GachaType = item["gacha_type"].ToString(),
                            ItemId = item["item_id"].ToString(),
                            Count = item["count"].ToString(),
                            Time = CorrectTimeFormat(item["time"].ToString()), // 确保时间格式正确
                            Name = item["name"].ToString(),
                            Lang = item["lang"].ToString(),
                            ItemType = item["item_type"].ToString(),
                            RankType = item["rank_type"].ToString(),
                            Id = item["id"].ToString()
                        };

                        records.Add(gachaRecord);
                        count++;

                        string logMessage = $"{gachaRecord.Uid}|{gachaRecord.Time}|{gachaRecord.GachaId}|{gachaRecord.Name}|{gachaRecord.Id}";
                        Logging.Write(logMessage, 0);

                        string overlayMessage = $"已获取{count}条记录 {gachaRecord.Uid}|{gachaRecord.Time}|{gachaRecord.Name}";
                        WaitOverlayManager.RaiseWaitOverlay(true, "正在获取API信息,请不要退出", overlayMessage, true, 0);
                    }

                    endId = records.Last().Id;

                }
                catch (Exception ex)
                {
                    records.Add(new GachaRecords { Uid = ex.Message });
                    return records;
                }
            }
            Logging.Write("Gacha Get Finished!", 0);
            return records;
        }
        public string Uid { get; set; }
        public string GachaId { get; set; }
        public string GachaType { get; set; }
        public string ItemId { get; set; }
        public string Count { get; set; }
        public string Time { get; set; }
        public string Name { get; set; }
        public string Lang { get; set; }
        public string ItemType { get; set; }
        public string RankType { get; set; }
        public string Id { get; set; }
    }
}
