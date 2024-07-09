// Copyright (c) 2021-2024, JamXi JSG-LLC.
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
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Threading.Tasks;
using Fiddler;
using Microsoft.UI.Dispatching;
using Windows.ApplicationModel.DataTransfer;
using ZenlessTools.Depend;
using System.Linq;
using Windows.Storage;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Collections.Generic;
using ZenlessTools.Views.GachaViews;
using Spectre.Console;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using Microsoft.UI.Xaml.Media;
using static ZenlessTools.App;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.UI.Xaml.Data;
using System.Threading;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI;
using System.Runtime.InteropServices;
using Windows.Graphics;
using Windows.Storage.Pickers;
using WinRT.Interop;
using ZenlessTools.Depend;


namespace ZenlessTools.Views.ToolViews
{

    public sealed partial class GachaView : Page
    {

        public bool isProxyRunning;
        public static String selectedUid;
        public static int selectedCardPoolId;
        public String GachaLink_String;
        public String GachaLinkCache_String;
        public bool isClearGachaSaved;
        public static GachaModel.CardPoolInfo cardPoolInfo;

        private bool isUserInteraction = false;
        private string latestUpdatedUID = null;


        private FiddlerCoreStartupSettings startupSettings;
        private Proxy oProxy;

        BCCertMaker.BCCertMaker certProvider = new BCCertMaker.BCCertMaker();
        private DispatcherQueueTimer dispatcherTimer;

        [DllImport("User32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        public GachaView()
        {
            InitializeComponent();
            Logging.Write("Switch to GachaView", 0);
            this.Loaded += GachaView_Loaded;
            InitTimer();
        }

        private void InitTimer()
        {
            var dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            dispatcherTimer = dispatcherQueue.CreateTimer();
            dispatcherTimer.Interval = TimeSpan.FromSeconds(2);
            dispatcherTimer.Tick += CheckProcess;
        }

        private async void GachaView_Loaded(object sender, RoutedEventArgs e)
        {
            if (AppDataController.GetGamePath() == "Null") { ProxyButton.IsEnabled = false;}
            // 获取卡池信息
            cardPoolInfo = await GetCardPoolInfo();

            if (cardPoolInfo == null || cardPoolInfo.CardPools == null)
            {
                Console.WriteLine("无法获取卡池信息或卡池列表为空");
                return;
            }
            await LoadUIDs();
        }

        private async Task<GachaModel.CardPoolInfo> GetCardPoolInfo()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.GetStringAsync("https://zentools.jamsg.cn/api/cardPoolRule");
                    return JsonConvert.DeserializeObject<GachaModel.CardPoolInfo>(response);
                }
            }
            catch (Exception ex)
            {
                NotificationManager.RaiseNotification("获取卡池信息时发生错误", "", InfoBarSeverity.Error, false, 5);
                Logging.Write($"获取卡池信息时发生错误: {ex.Message}", 2);
                throw;
            }
        }

        private async void ReloadGachaView(TeachingTip sender = null, object args = null)
        {
            GachaRecordsUID.SelectionChanged -= GachaRecordsUID_SelectionChanged;
            await LoadUIDs();
            GachaRecordsUID.SelectionChanged += GachaRecordsUID_SelectionChanged;

            ReloadGachaMainView();

            if (!string.IsNullOrEmpty(latestUpdatedUID))
            {
                isUserInteraction = false;
                GachaRecordsUID.SelectedItem = latestUpdatedUID;
                latestUpdatedUID = null;
            }
            isUserInteraction = true;
        }

        private void ComboBox_Click(object sender, object e)
        {
            isUserInteraction = true;
        }

        private void ReloadGachaComboBox()
        {
            selectedUid = null;
            GachaRecordsUID.SelectionChanged -= GachaRecordsUID_SelectionChanged;
            GachaRecordsUID.ItemsSource = null;
            GachaRecordsUID.Items.Clear();

            string linkDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "JSG-LLC", "ZenlessTools", "GachaLinks");
            string recordsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "JSG-LLC", "ZenlessTools", "GachaRecords");

            if (Directory.Exists(recordsDirectory))
            {
                var uidFiles = Directory.GetFiles(recordsDirectory, "*.json");
                var uidList = uidFiles.Select(Path.GetFileNameWithoutExtension).ToList();
                GachaRecordsUID.ItemsSource = uidList;
                if (uidList.Count > 0)
                {
                    GachaRecordsUID.SelectedIndex = 0;
                    selectedUid = GachaRecordsUID.SelectedValue.ToString();
                }
            }
            GachaRecordsUID.SelectionChanged += GachaRecordsUID_SelectionChanged; // 重新启用事件处理程序
        }

        // 新增的方法，用于重新加载整个界面
        private void ReloadGachaMainView()
        {
            isUserInteraction = false;
            ReloadGachaComboBox();
            if (GachaRecordsUID.Items.Count == 0)
            {
                gachaView.Visibility = Visibility.Collapsed;
                loadGachaProgress.Visibility = Visibility.Collapsed;
                noGachaFound.Visibility = Visibility.Visible;
            }
            else
            {
                gachaView.Visibility = Visibility.Visible;
            }
            if (gachaFrame != null)
            {
                gachaFrame.Navigate(typeof(TempGachaView), selectedUid);
            }
            gachaNav.Items.Clear();
            if (!string.IsNullOrEmpty(selectedUid))
            {
                LoadGachaRecords(selectedUid);
            }
            ClearGacha.IsEnabled = !string.IsNullOrEmpty(selectedUid);

            loadGachaProgress.Visibility = Visibility.Collapsed;
            noGachaFound.Visibility = Visibility.Collapsed;
        }

        private async Task LoadUIDs()
        {
            GachaRecordsUID.ItemsSource = null;
            try
            {
                string recordsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "JSG-LLC", "ZenlessTools", "GachaRecords");

                HashSet<string> uidSet = new HashSet<string>();

                if (Directory.Exists(recordsDirectory))
                {
                    var recordFiles = Directory.GetFiles(recordsDirectory, "*.json");
                    foreach (var file in recordFiles)
                    {
                        uidSet.Add(Path.GetFileNameWithoutExtension(file));
                    }
                }

                if (uidSet.Count == 0)
                {
                    //NotificationManager.RaiseNotification($"抽卡分析", "无调频记录", InfoBarSeverity.Warning);
                    loadGachaProgress.Visibility = Visibility.Collapsed;
                    noGachaFound.Visibility = Visibility.Visible;
                    return;
                }

                GachaRecordsUID.ItemsSource = uidSet.ToList();
                if (uidSet.Count > 0)
                {
                    GachaRecordsUID.SelectedIndex = 0;
                    loadGachaProgress.Visibility = Visibility.Collapsed;
                    noGachaFound.Visibility = Visibility.Collapsed;
                    gachaView.Visibility = Visibility.Visible;
                    ClearGacha.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                NotificationManager.RaiseNotification($"抽卡分析", "加载UID时出现错误", InfoBarSeverity.Error);
            }
        }


        private void GachaRecordsUID_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GachaRecordsUID.SelectedItem != null)
            {
                selectedUid = GachaRecordsUID.SelectedItem.ToString();
                if (!string.IsNullOrEmpty(selectedUid))
                {
                    if (isUserInteraction)
                    {
                        LoadGachaRecords(selectedUid);
                    }
                    else
                    {
                        Logging.Write("GachaUID:" + selectedUid);
                        LoadGachaRecords(selectedUid);
                        isUserInteraction = true;
                    }
                }
                else
                {
                    Logging.Write("UID为空");
                }
            }
        }

        private async void LoadGachaRecords(string uid)
        {
            try
            {
                Logging.Write("Load GachaRecords...");
                string recordsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "JSG-LLC", "ZenlessTools", "GachaRecords");
                string filePath = Path.Combine(recordsDirectory, $"{uid}.json");

                if (!File.Exists(filePath))
                {
                    if (isUserInteraction)
                    {
                        return;
                    }

                    // 使用for循环遍历所有选项
                    bool recordFound = false;
                    isUserInteraction = false;
                    GachaRecordsUID.SelectionChanged -= GachaRecordsUID_SelectionChanged;
                    for (int i = 0; i < GachaRecordsUID.Items.Count; i++)
                    {
                        GachaRecordsUID.SelectedIndex = i;
                        var newUid = GachaRecordsUID.SelectedItem.ToString();
                        string newFilePath = Path.Combine(recordsDirectory, $"{newUid}.json");

                        if (File.Exists(newFilePath))
                        {
                            recordFound = true;
                            break;
                        }
                    }

                    if (!recordFound)
                    {
                        GachaRecordsUID.SelectedIndex = 0;
                        NotificationManager.RaiseNotification("无可用的调频记录", "", InfoBarSeverity.Warning);
                        gachaNav.Visibility = Visibility.Collapsed;
                        gachaFrame.Visibility = Visibility.Collapsed;
                    }
                    GachaRecordsUID.SelectionChanged += GachaRecordsUID_SelectionChanged;
                    return;
                }

                var jsonContent = await File.ReadAllTextAsync(filePath);
                var gachaData = JsonConvert.DeserializeObject<GachaModel.GachaData>(jsonContent);

                DisplayGachaData(gachaData);
            }
            catch (Exception ex)
            {
                NotificationManager.RaiseNotification("加载调频记录时发生错误", $"{ex.Message}", InfoBarSeverity.Error);
            }
        }

        private void DisplayGachaData(GachaModel.GachaData gachaData)
        {
            Logging.Write("Display GachaData...");
            gachaNav.Items.Clear();

            if (gachaData?.list == null || gachaData.list.Count == 0)
            {
                return;
            }

            SelectorBarItem firstEnabledItem = null;

            foreach (var pool in gachaData.list)
            {
                var item = new SelectorBarItem
                {
                    Text = pool.cardPoolType,
                    Tag = pool.cardPoolId.ToString(),
                    IsEnabled = pool.records != null && pool.records.Count > 0
                };

                if (item.IsEnabled && firstEnabledItem == null)
                {
                    firstEnabledItem = item;
                }

                item.Tapped += SelectorBarItem_Tapped;
                gachaNav.Items.Add(item);
            }

            if (firstEnabledItem != null)
            {
                gachaNav.Visibility = Visibility.Visible;
                gachaFrame.Visibility = Visibility.Visible;
                firstEnabledItem.IsSelected = true;
                LoadGachaPoolData(firstEnabledItem.Tag.ToString());
            }
        }

        private void SelectorBarItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is SelectorBarItem item)
            {
                string tag = item.Tag.ToString();
                Console.WriteLine($"Selected Card Pool: {tag}");
                selectedCardPoolId = int.Parse(tag);
                LoadGachaPoolData(tag);
            }
        }

        private void LoadGachaPoolData(string cardPoolId)
        {
            // 切换到新选择的池子时重新加载frame
            selectedCardPoolId = int.Parse(cardPoolId);
            if (gachaFrame != null)
            {
                gachaFrame.Navigate(typeof(TempGachaView), selectedUid);
            }
        }


        private async void ExportUIGF_Click(object sender, RoutedEventArgs e)
        {
            var window = new Window();
            string recordsBasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"JSG-LLC\ZenlessTools\GachaRecords");

            // 打开文件选择器
            var savePicker = new FileSavePicker();
            var hwnd = WindowNative.GetWindowHandle(window);
            InitializeWithWindow.Initialize(savePicker, hwnd);

            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("Uniformed Interchangeable GachaLog Format standard v4.0", new List<string>() { ".json" });
            savePicker.SuggestedFileName = $"ZenlessTools_Gacha_{selectedUid}_Export_UIGF4";

            StorageFile exportFile = await savePicker.PickSaveFileAsync();
            if (exportFile != null)
            {
                await ExportGacha.ExportAsync($"{recordsBasePath}\\{selectedUid}.json", exportFile.Path);
            }
        }
        private async void ImportUIGF_Click(object sender, RoutedEventArgs e)
        {
            var window = new Window();
            // 打开文件选择器
            var openPicker = new FileOpenPicker();
            var hwnd = WindowNative.GetWindowHandle(window);
            InitializeWithWindow.Initialize(openPicker, hwnd);

            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            openPicker.FileTypeFilter.Add(".json");

            StorageFile importFile = await openPicker.PickSingleFileAsync();
            if (importFile != null)
            {
                string recordsBasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"JSG-LLC\ZenlessTools\GachaRecords");
                await ImportGacha.Import(importFile.Path);
            }
            ReloadGachaView();
        }

        private async void ClearGacha_Click(object sender, RoutedEventArgs e)
        {
            DialogManager.RaiseDialog(XamlRoot, "清空调频记录", $"确定要清空UID:{selectedUid}的调频记录吗？", true, "确认", ClearGacha_Run);
        }

        private void ClearGacha_Run()
        {
            try
            {
                string recordsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "JSG-LLC", "ZenlessTools", "GachaRecords");
                string filePath = Path.Combine(recordsDirectory, $"{selectedUid}.json");

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Logging.Write($"Deleted Gacha Record File: {filePath}", 0);
                }

                NotificationManager.RaiseNotification("操作成功", "已成功清除调频记录", InfoBarSeverity.Success, true, 2);
                isUserInteraction = false;
                ReloadGachaMainView();
            }
            catch (Exception ex)
            {
                Logging.Write($"Error clearing Gacha records: {ex.Message}", 2);
                NotificationManager.RaiseNotification("操作失败", "清除调频记录时出现错误", InfoBarSeverity.Error, true, 3);
            }
        }


        public Border CreateDetailBorder()
        {
            return new Border
            {
                Padding = new Thickness(3),
                BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8)
            };
        }

        public async Task StartAsync()
        {
            await Task.Run(() =>
            {
                startupSettings = new FiddlerCoreStartupSettingsBuilder()
                    .ListenOnPort(8888)
                    .RegisterAsSystemProxy()
                    .DecryptSSL()
                    .Build();

                FiddlerApplication.BeforeRequest += OnBeforeRequest;
                FiddlerApplication.Startup(startupSettings);
                Logging.Write("Starting Proxy...", 0);
                oProxy = FiddlerApplication.CreateProxyEndpoint(8888, true, "127.0.0.1");
                Logging.Write("Started Proxy", 0);
            });
        }

        public void Stop()
        {
            Logging.Write("Stopping Proxy...", 0);
            FiddlerApplication.Shutdown();
            if (oProxy != null)
            {
                oProxy.Dispose();
                Logging.Write("Stopped Proxy", 0);
                gacha_status.Text = "等待操作";
                Enable_NavBtns();
            }
        }


        private async void OnBeforeRequest(Session session)
        {
            await Task.Run(() =>
            {
                if (session.fullUrl.Contains("getGachaLog"))
                {
                    GachaLink_String = session.fullUrl;
                }
            });
        }

        private async void ProxyButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProxyButton.IsChecked ?? false)
            {
                if (Process.GetProcessesByName("ZenlessZoneZero").Length > 0)
                {
                    // 进程正在运行
                    try
                    {
                        ProxyButton.IsChecked = true;
                        CertMaker.oCertProvider = certProvider;
                        string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "JSG-LLC\\ZenlessTools");
                        string filePath = Path.Combine(folderPath, "RootCertificate.p12");
                        string rootCertificatePassword = "S0m3T0pS3cr3tP4ssw0rd";
                        if (!Directory.Exists(folderPath))
                        {
                            Logging.Write("Creating Proxy Cert...", 0);
                            Directory.CreateDirectory(folderPath);
                        }
                        if (!File.Exists(filePath))
                        {
                            Logging.Write("Creating Proxy Cert...", 0);
                            certProvider.CreateRootCertificate();
                            certProvider.WriteRootCertificateAndPrivateKeyToPkcs12File(filePath, rootCertificatePassword);
                        }
                        certProvider.ReadRootCertificateAndPrivateKeyFromPkcs12File(filePath, rootCertificatePassword);
                        if (!CertMaker.rootCertIsTrusted())
                        {
                            Logging.Write("Trust Proxy Cert...", 0);
                            CertMaker.trustRootCert();
                        }
                        isProxyRunning = true;
                        await StartAsync();
                        Disable_NavBtns();
                        gacha_status.Text = "正在等待打开抽卡历史记录...";
                        WaitOverlayManager.RaiseWaitOverlay(true, "正在等待调频记录链接", "请前往游戏打开调频记录", true, 0, true, "取消获取", StopProxy);
                        ProxyButton.IsEnabled = true;
                        dispatcherTimer.Start();
                    }
                    catch (Exception ex)
                    {
                        NotificationManager.RaiseNotification("出现错误", ex.Message, InfoBarSeverity.Error);
                        AnsiConsole.WriteException(ex);
                    }
                }
                else
                {
                    // 进程未运行
                    ProxyButton.IsChecked = false;
                    NotificationManager.RaiseNotification("未运行游戏", "先到启动游戏页面启动游戏后再启动代理", InfoBarSeverity.Warning);
                }
            }
            else { Stop(); isProxyRunning = false; }
        }

        private void StopProxy()
        {
            Stop();
            ProxyButton.IsChecked = false;
            isProxyRunning = false;
            
            WaitOverlayManager.RaiseWaitOverlay(false);
        }

        // 定时器回调函数，检查进程是否正在运行
        private async void CheckProcess(DispatcherQueueTimer timer, object e)
        {
            if (isProxyRunning || GachaLinkCache_String != null)
            {
                ProxyButton.IsChecked = true;
                if (GachaLink_String != null && GachaLink_String.Contains("getGachaLog"))
                {
                    // 进程正在运行
                    gacha_status.Text = GachaLink_String;
                    ProxyButton.IsEnabled = false;
                    Stop();
                    Logging.Write("Get GachaLink Finish!", 0);
                    WaitOverlayManager.RaiseWaitOverlay(true, "正在获取API信息,请不要退出", "请稍等片刻", true, 0);
                    ProxyButton.IsChecked = false;
                    DataPackage dataPackage = new DataPackage();
                    dataPackage.SetText(GachaLink_String);
                    Logging.Write("Writing GachaLink in Clipboard...", 0);
                    Clipboard.SetContent(dataPackage);
                    dispatcherTimer.Stop();
                    Logging.Write("Loading Gacha Data...", 0);
                    await GachaRecords.GetGachaAsync(GachaLink_String);
                    NotificationManager.RaiseNotification("获取调频记录完成", "", InfoBarSeverity.Success, true, 2);
                    GachaLink_String = null;
                    WaitOverlayManager.RaiseWaitOverlay(false);
                    ProxyButton.IsEnabled = true;
                    ReloadGachaView();
                }
            }
            else
            {
                ProxyButton.IsChecked = false;
            }
        }


        private void Disable_NavBtns()
        {
            NavigationView parentNavigationView = GetParentNavigationView(this);
            if (parentNavigationView != null)
            {
                var selectedItem = parentNavigationView.SelectedItem;
                var excludeTags = new HashSet<string> { "account_status", "event", "account", "jsg_account" };  // 需要排除的标签

                foreach (var menuItem in parentNavigationView.MenuItems.Concat(parentNavigationView.FooterMenuItems))
                {
                    if (menuItem is NavigationViewItem navViewItem && navViewItem != selectedItem && !excludeTags.Contains(navViewItem.Tag as string))
                    {
                        navViewItem.IsEnabled = false;
                    }
                }
                // 特别处理设置按钮
                if (parentNavigationView.SettingsItem is NavigationViewItem settingsItem && settingsItem != selectedItem)
                {
                    settingsItem.IsEnabled = false;
                }
            }
        }

        private void Enable_NavBtns()
        {
            NavigationView parentNavigationView = GetParentNavigationView(this);
            if (parentNavigationView != null)
            {
                var selectedItem = parentNavigationView.SelectedItem;
                var excludeTags = new HashSet<string> { "account_status", "event", "account", "jsg_account" };  // 需要排除的标签

                foreach (var menuItem in parentNavigationView.MenuItems.Concat(parentNavigationView.FooterMenuItems))
                {
                    if (menuItem is NavigationViewItem navViewItem && navViewItem != selectedItem && !excludeTags.Contains(navViewItem.Tag as string))
                    {
                        navViewItem.IsEnabled = true;
                    }
                }
                // 特别处理设置按钮
                if (parentNavigationView.SettingsItem is NavigationViewItem settingsItem && settingsItem != selectedItem)
                {
                    settingsItem.IsEnabled = true;
                }
            }
        }

        private void SetButtonsEnabledState(DependencyObject parent, bool isEnabled)
        {
            var childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is Button button)
                {
                    button.IsEnabled = isEnabled;
                }
                else
                {
                    SetButtonsEnabledState(child, isEnabled);
                }
            }
        }



        private NavigationView GetParentNavigationView(FrameworkElement child)
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);

            while (parent != null && !(parent is NavigationView))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            return parent as NavigationView;
        }


        private void Window_Closed(object sender, EventArgs e)
        {
            dispatcherTimer.Stop();
            dispatcherTimer.Tick -= CheckProcess;
        }

        
    }
}