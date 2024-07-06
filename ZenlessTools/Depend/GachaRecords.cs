using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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

        public async static Task GetGachaAsync(string url)
        {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string zenlessToolsFolderPath = Path.Combine(documentsPath, "JSG-LLC", "ZenlessTools", "GachaRecords");

            Directory.CreateDirectory(zenlessToolsFolderPath);

            var charRecords = await new GachaRecords().GetAllGachaRecordsAsync(url, null, "2001", "2");
            var lightRecords = await new GachaRecords().GetAllGachaRecordsAsync(url, null, "3001", "3");
            var regularRecords = await new GachaRecords().GetAllGachaRecordsAsync(url, null, "1001", "1");

            if ((charRecords == null || charRecords.Count == 0) &&
                (lightRecords == null || lightRecords.Count == 0) &&
                (regularRecords == null || regularRecords.Count == 0))
            {
                Logging.Write("未能获取任何调频记录，请检查链接或重试", 1);
                return;
            }

            string uid = charRecords?.FirstOrDefault()?.Uid ??
                         lightRecords?.FirstOrDefault()?.Uid ??
                         regularRecords?.FirstOrDefault()?.Uid;

            if (string.IsNullOrEmpty(uid) || uid.Length != 8)
            {
                Logging.Write("抽卡链接UID无效", 1);
                return;
            }

            string filePath = Path.Combine(zenlessToolsFolderPath, $"{uid}.json");

            var existingData = new GachaData { info = new GachaInfo(), list = new List<GachaPool>() };

            if (File.Exists(filePath))
            {
                var jsonData = await File.ReadAllTextAsync(filePath);
                existingData = JsonConvert.DeserializeObject<GachaData>(jsonData) ?? new GachaData { info = new GachaInfo(), list = new List<GachaPool>() };
            }
            existingData.info.uid = uid;
            existingData.list = existingData.list ?? new List<GachaPool>();

            
            MergeRecords(existingData, charRecords, 2);
            MergeRecords(existingData, lightRecords, 3);
            MergeRecords(existingData, regularRecords, 1);

            foreach (var pool in existingData.list)
            {
                pool.records = pool.records.OrderByDescending(r => r.id).ToList();
            }

            var serializedData = JsonConvert.SerializeObject(existingData, Formatting.Indented);
            await File.WriteAllTextAsync(filePath, serializedData);

            Logging.Write($"调频记录已保存到 {filePath}", 0);
        }

        private static void MergeRecords(GachaData existingData, List<GachaRecords> newRecords, int cardPoolId)
        {
            var pool = existingData.list.FirstOrDefault(p => p.cardPoolId == cardPoolId);
            if (pool == null)
            {
                pool = new GachaPool
                {
                    cardPoolId = cardPoolId,
                    cardPoolType = GetCardPoolType(cardPoolId),
                    records = new List<GachaRecord>()
                };
                existingData.list.Add(pool);
            }

            foreach (var newRecord in newRecords)
            {
                if (!pool.records.Any(r => r.id == newRecord.Id))
                {
                    pool.records.Add(new GachaRecord
                    {
                        gachaType = newRecord.GachaType,
                        itemId = newRecord.ItemId,
                        count = newRecord.Count,
                        time = DateTime.Parse(newRecord.Time),
                        name = newRecord.Name,
                        lang = newRecord.Lang,
                        itemType = newRecord.ItemType,
                        rankType = newRecord.RankType,
                        id = newRecord.Id
                    });
                }
            }
        }

        private static string GetCardPoolType(int cardPoolId)
        {
            return cardPoolId switch
            {
                2 => "独家频段",
                3 => "音擎频段",
                1 => "常驻频段",
                _ => "未知频段"
            };
        }

        public async Task<List<GachaRecords>> GetAllGachaRecordsAsync(string url, string localData = null, string gachaType = "3001", string realGachaType = "3")
        {
            var records = new List<GachaRecords>();
            if (localData == null)
            {
                var page = 1;
                var count = 0;
                var endId = "0";
                int urlindex = url.IndexOf("&gacha_type=");
                while (true)
                {
                    try
                    {
                        var client = new HttpClient();
                        await Task.Delay(TimeSpan.FromSeconds(0.08));
                        Logging.Write("Wait Timeout...", 0);
                        var newurl = url.Substring(0, urlindex) + "&gacha_type=" + gachaType + "&real_gacha_type=" + realGachaType + "&end_id=" + endId;
                        var response = await client.GetAsync(newurl);
                        if (!response.IsSuccessStatusCode) break;
                        var json = await response.Content.ReadAsStringAsync();
                        var data = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(json).GetProperty("data");
                        Logging.Write(url.Substring(0, urlindex) + "&gacha_type=" + gachaType + "&real_gacha_type=" + realGachaType + "&end_id=" + endId, 0);
                        JObject jsonObj = JObject.Parse(json);
                        if (jsonObj["message"].ToString() == "authkey timeout")
                        {
                            records.Add(new GachaRecords
                            {
                                Uid = jsonObj["message"].ToString(),
                                GachaId = "",
                                GachaType = "",
                                ItemId = "",
                                Count = "",
                                Time = "",
                                Name = "",
                                Lang = "",
                                ItemType = "",
                                RankType = "",
                                Id = ""
                            });
                            return records;
                        }
                        else
                        {
                            if (data.GetProperty("list").GetArrayLength() == 0) break;
                            foreach (var item in data.GetProperty("list").EnumerateArray())
                            {
                                records.Add(new GachaRecords
                                {
                                    Uid = item.GetProperty("uid").GetString(),
                                    GachaId = item.GetProperty("gacha_id").GetString(),
                                    GachaType = item.GetProperty("gacha_type").GetString(),
                                    ItemId = item.GetProperty("item_id").GetString(),
                                    Count = item.GetProperty("count").GetString(),
                                    Time = item.GetProperty("time").GetString(),
                                    Name = item.GetProperty("name").GetString(),
                                    Lang = item.GetProperty("lang").GetString(),
                                    ItemType = item.GetProperty("item_type").GetString(),
                                    RankType = item.GetProperty("rank_type").GetString(),
                                    Id = item.GetProperty("id").GetString()
                                });
                                count++;
                                Logging.Write(item.GetProperty("uid").GetString() + "|" + item.GetProperty("time").GetString() + "|" + item.GetProperty("gacha_id").GetString() + "|" + item.GetProperty("name").GetString() + "|" + item.GetProperty("id").GetString(), 0);
                                WaitOverlayManager.RaiseWaitOverlay(true, "正在获取API信息,请不要退出", "已获取" + count + "条记录" + item.GetProperty("uid").GetString() + "|" + item.GetProperty("time").GetString() + "|" + item.GetProperty("name").GetString(), true, 0);
                            }
                            endId = records.Last().Id;
                            page++;
                        }
                    }
                    catch (Exception ex)
                    {
                        records.Add(new GachaRecords
                        {
                            Uid = ex.Message,
                            GachaId = "",
                            GachaType = "",
                            ItemId = "",
                            Count = "",
                            Time = "",
                            Name = "",
                            Lang = "",
                            ItemType = "",
                            RankType = "",
                            Id = ""
                        });
                        return records;
                    }
                }
            }
            else
            {
                records = JsonConvert.DeserializeObject<List<GachaRecords>>(localData);
            }
            Logging.Write("Gacha Get Finished!", 0);
            return records;
        }
    }
}

