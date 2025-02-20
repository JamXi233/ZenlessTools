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
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using static ZenlessTools.App;
using System.Text.RegularExpressions;
using System.IO;
using ZenlessTools.Views;

namespace ZenlessTools.Depend
{
    public static class Region
    {
        public static async Task GetRegion(bool isFirst)
        {
            string gameFile = AppDataController.GetGamePath();
            string fileAssemblyPath = @gameFile.Replace("ZenlessZoneZero.exe", "GameAssembly.dll");
            Logging.Write(fileAssemblyPath);
            try
            {
                // 创建一个X509Certificate2对象，并加载文件的数字证书
                X509Certificate2 cert = new X509Certificate2(fileAssemblyPath);

                // 解析Subject字符串以获取CN部分
                string[] parts = cert.Subject.Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                string signerName = "";
                foreach (var part in parts)
                {
                    if (part.Trim().StartsWith("CN="))
                    {
                        signerName = part.Trim().Substring(3);
                        break;
                    }
                }
                if (signerName == "\"miHoYo Co.,Ltd.\"") 
                {
                    if (await isBili()) 
                    { 
                        if (isFirst) NotificationManager.RaiseNotification("游戏路径读取完成", "检测到B服\n" + signerName, InfoBarSeverity.Success, true, 3); 
                        StartGameView.GameRegion = "CN"; 
                    }
                    else 
                    {
                        if (isFirst) NotificationManager.RaiseNotification("游戏路径读取完成", "检测到国服\n" + signerName, InfoBarSeverity.Success, true, 3); 
                        StartGameView.GameRegion = "CN"; 
                    }
                }
                else if (signerName == "COGNOSPHERE PTE. LTD.") 
                {
                    if (isFirst) NotificationManager.RaiseNotification("游戏路径读取完成", "检测到国际服\n" + signerName, InfoBarSeverity.Success, true, 3);
                    StartGameView.GameRegion = "Global"; 
                }
                Logging.Write(signerName);
            }
            catch (CryptographicException ex)
            {
                if (isFirst) NotificationManager.RaiseNotification("游戏路径读取完成", "检测区服失败", InfoBarSeverity.Warning, true, 5);
                Logging.Write($"{ex.Message}", 2);
            }
        }

        private static async Task<bool> isBili()
        {
            try
            {
                var gameDirectory = AppDataController.GetGamePathWithoutGameName();
                if (gameDirectory != null)
                {
                    string configFilePath = Path.Combine(gameDirectory, "config.ini");

                    if (File.Exists(configFilePath))
                    {
                        string[] lines = await File.ReadAllLinesAsync(configFilePath);
                        bool inGeneralSection = false;
                        bool cpsIsBilibiliPC = false;

                        foreach (string line in lines)
                        {
                            if (line.Trim() == "[General]")
                            {
                                inGeneralSection = true;
                            }
                            else if (inGeneralSection)
                            {
                                if (string.IsNullOrWhiteSpace(line))
                                {
                                    // 离开[General]部分
                                    inGeneralSection = false;
                                }
                                else if (line.StartsWith("cps="))
                                {
                                    string cpsValue = line.Substring("cps=".Length).Trim();
                                    if (cpsValue == "bilibili_PC")
                                    {
                                        cpsIsBilibiliPC = true;
                                    }
                                }
                                else if (line.StartsWith("game_version="))
                                {
                                    // 使用正则表达式提取版本号
                                    Match match = Regex.Match(line, @"\d+(\.\d+)+");
                                    if (match.Success)
                                    {
                                        Logging.Write($"ZenlessZoneZero Current Version: {match.Value}");
                                        if (cpsIsBilibiliPC)
                                        {
                                            return true;
                                        }
                                        return false;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Logging.Write("config.ini 文件不存在.");
                    }
                }
                else
                {
                    Logging.Write("无法获取游戏目录.");
                }
            }
            catch (Exception ex)
            {
                Logging.Write($"获取游戏版本时出现错误: {ex.Message}");
            }
            return false;
        }
    }
}
