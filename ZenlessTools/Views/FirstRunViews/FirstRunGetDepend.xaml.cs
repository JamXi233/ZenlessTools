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

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ZenlessTools.Depend;
using System.Diagnostics;
using System;
using Windows.Storage;
using System.IO;
using System.IO.Compression;
using Microsoft.UI.Xaml.Media;

namespace ZenlessTools.Views.FirstRunViews
{
    public sealed partial class FirstRunGetDepend : Page
    {
        private MainWindow mainWindow;
        string fileUrl;
        private GetNetData _getNetData;
        private readonly GetGithubLatest _getGithubLatest = new GetGithubLatest();
        private readonly GetJSGLatest _getJSGLatest = new GetJSGLatest();

        public FirstRunGetDepend()
        {
            this.InitializeComponent();
            Logging.Write("Switch to FirstRunGetDepend", 0);
            AppDataController.SetFirstRunStatus(4);
            OnGetDependLatestReleaseInfo();
        }

        //依赖下载开始

        private const string FileFolder = "\\JSG-LLC\\ZenlessTools\\Depends";
        private const string ZipFileName = "ZenlessToolsHelper.zip";
        private const string ExtractedFolder = "ZenlessToolsHelper";

        private async void DependDownload_Click(object sender, RoutedEventArgs e)
        {
            depend_Progress_Text.Text = "正在下载...";
            _getNetData = new GetNetData();
            string userDocumentsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string localFilePath = Path.Combine(userDocumentsFolderPath + FileFolder, ZipFileName);
            Trace.WriteLine(fileUrl);
            ToggleDependGridVisibility(false);
            depend_Download.IsEnabled = false;
            var progress = new Progress<double>(DependReportProgress);
            bool downloadResult = false;
            try
            {
                downloadResult = await _getNetData.DownloadFileWithProgressAsync(fileUrl, localFilePath, progress);
            }
            catch (Exception ex)
            {
                Logging.Write($"Download error: {ex.Message}", 0);
                throw new Exception(ex.Message);
            }

            if (downloadResult)
            {
                string extractionPath = Path.Combine(userDocumentsFolderPath + FileFolder, ExtractedFolder);
                if (File.Exists(extractionPath + "\\ZenlessToolsHelper.exe"))
                {
                    mainWindow?.KillFirstUI();
                    Frame parentFrame = GetParentFrame(this);
                    if (parentFrame != null)
                    {
                        parentFrame.Navigate(typeof(FirstRunExtra));
                    }
                }
                else
                {
                    Trace.WriteLine(userDocumentsFolderPath);
                    Trace.WriteLine(extractionPath);
                    ZipFile.ExtractToDirectory(localFilePath, extractionPath);
                    Frame parentFrame = GetParentFrame(this);
                    if (parentFrame != null)
                    {
                        parentFrame.Navigate(typeof(FirstRunExtra));
                    }
                }
            }
            else
            {
                // 使用Dispatcher在UI线程上执行ToggleUpdateGridVisibility函数
                ToggleDependGridVisibility(true, false);
            }
        }

        private void ToggleDependGridVisibility(bool updateGridVisible, bool downloadSuccess = false)
        {
            depend_Grid.Visibility = updateGridVisible ? Visibility.Visible : Visibility.Collapsed;
            depend_Progress_Grid.Visibility = updateGridVisible ? Visibility.Collapsed : Visibility.Visible;
            depend_Btn_Text.Text = downloadSuccess ? "下载完成" : "下载失败";
        }

        private async void OnGetDependLatestReleaseInfo()
        {
            depend_Progress_Text.Text = "正在获取...";
            depend_Grid.Visibility = Visibility.Collapsed;
            depend_Progress_Grid.Visibility = Visibility.Visible;
            var dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            try
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                var latestReleaseInfo = await _getJSGLatest.GetLatestReleaseInfoAsync("cn.jamsg.ZenlessToolshelper");
                switch (localSettings.Values["Config_UpdateService"])
                {
                    case 0:
                        latestReleaseInfo = await _getGithubLatest.GetLatestDependReleaseInfoAsync("JamXi233", "Releases", "ZenlessToolsHelper");
                        break;
                    case 2:
                        latestReleaseInfo = await _getJSGLatest.GetLatestReleaseInfoAsync("cn.jamsg.ZenlessToolshelper");
                        break;
                    default:
                        throw new InvalidOperationException($"Invalid update service value: {localSettings.Values["Config_UpdateService"]}");
                }

                fileUrl = latestReleaseInfo.DownloadUrl;
                dispatcher.TryEnqueue(() =>
                {
                    depend_Latest_Name.Text = $"依赖项: {latestReleaseInfo.Name}";
                    depend_Latest_Version.Text = $"版本号: {latestReleaseInfo.Version}";
                    depend_Download.IsEnabled = true;
                    depend_Grid.Visibility = Visibility.Visible;
                    depend_Progress_Grid.Visibility = Visibility.Collapsed;
                });
            }
            catch (Exception ex)
            {
                dispatcher.TryEnqueue(() =>
                {
                    depend_Grid.Visibility = Visibility.Visible;
                    depend_Progress_Grid.Visibility = Visibility.Collapsed;
                    depend_Btn_Text.Text = "获取失败";
                    depend_Latest_Version.Text = ex.Message;
                    depend_Btn_Bar.Visibility = Visibility.Collapsed;
                });
                Logging.Write($"Error fetching latest release info: {ex.Message}", 0);
            }
        }

        private void DependReportProgress(double progressPercentage)
        {
            depend_Btn_Bar.Value = progressPercentage;
        }

        private Frame GetParentFrame(FrameworkElement child)
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);

            while (parent != null && !(parent is Frame))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            return parent as Frame;
        }
    }
}
