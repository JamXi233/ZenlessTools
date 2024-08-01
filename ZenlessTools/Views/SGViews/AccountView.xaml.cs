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

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using ZenlessTools.Depend;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static ZenlessTools.App;
using System.IO;

namespace ZenlessTools.Views.SGViews
{
    public sealed partial class AccountView : Page
    {
        public AccountView()
        {
            this.InitializeComponent();
            Logging.Write("Switch to AccountView", 0);
            InitData();
        }

        private async Task InitData(string mode = null)
        {
            saveAccount.IsEnabled = false;
            renameAccount.IsEnabled = false;
            deleteAccount.IsEnabled = false;
            currentAccount.IsEnabled = false;
            updateAccount.Visibility = Visibility.Collapsed;

            var backupDataTask = ProcessRun.ZenlessToolsHelperAsync($"/GetSaveUser {StartGameView.GameRegion}");

            await LoadData(await backupDataTask, await GetCurrentLogin());

            Account_Load.Visibility = Visibility.Collapsed;
            refreshAccount.Visibility = Visibility.Visible;
            refreshAccount_Loading.Visibility = Visibility.Collapsed;
        }

        private async Task LoadData(string jsonData, string CurrentLoginUID)
        {
            var accountexist = false;
            AccountListView.SelectionChanged -= AccountListView_SelectionChanged;
            List<Account> accounts = JsonConvert.DeserializeObject<List<Account>>(jsonData);

            if (accounts == null)
            {
                accounts = new List<Account>();
            }

            AccountListView.ItemsSource = accounts;
            if (accounts.Count > 0)
            {
                foreach (Account account in accounts)
                {
                    if (account.uid == CurrentLoginUID)
                    {
                        accountexist = true;
                        AccountListView.SelectedItem = account;
                        saveAccount.IsEnabled = true;
                        renameAccount.IsEnabled = true;
                        deleteAccount.IsEnabled = true;
                        currentAccount.IsEnabled = true;
                        break;
                    }
                }
            }

            if (!accountexist)
            {
                saveAccount.IsEnabled = false;
                renameAccount.IsEnabled = false;
                deleteAccount.IsEnabled = false;
                currentAccount.IsEnabled = true;
            }

            if (!accountexist && CurrentLoginUID != "0" && CurrentLoginUID != "当前未登录账号")
            {
                Account newAccount = new Account { uid = CurrentLoginUID, name = "未保存"};
                accounts.Add(newAccount);
                AccountListView.ItemsSource = accounts;
                AccountListView.SelectedItem = newAccount;
                saveAccount.IsEnabled = true;
                renameAccount.IsEnabled = false;
                currentAccount.IsEnabled = true;
                deleteAccount.IsEnabled = false;
            }

            AccountListView.SelectionChanged += AccountListView_SelectionChanged;
            refreshAccount.Visibility = Visibility.Visible;
            refreshAccount_Loading.Visibility = Visibility.Collapsed;
        }

        private async void AccountListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AccountListView.SelectedItem != null)
            {
                Account selectedAccount = (Account)AccountListView.SelectedItem;
                string command = $"/RestoreUser {StartGameView.GameRegion} {selectedAccount.uid} {selectedAccount.name}";
                var result = await ProcessRun.ZenlessToolsHelperAsync(command);

                if (result.Contains("切换失败"))
                {
                    NotificationManager.RaiseNotification("账号切换失败", result, InfoBarSeverity.Error, false, 3);
                    await InitData();
                    return;
                }
                else
                {
                    // 更新文件访问时间
                    UpdateFileAccessTime(selectedAccount.uid);
                    NotificationManager.RaiseNotification("账号切换成功", $"成功切换到 UID: {selectedAccount.uid}", InfoBarSeverity.Success, false, 3);
                }

                // 仅获取当前UID
                var CurrentLoginUID = await GetCurrentLogin();

                saveAccount.IsEnabled = selectedAccount.uid == CurrentLoginUID;
                renameAccount.IsEnabled = selectedAccount.uid == CurrentLoginUID;
                deleteAccount.IsEnabled = selectedAccount.uid == CurrentLoginUID;
                currentAccount.IsEnabled = true;

            }
        }

        private void UpdateFileAccessTime(string uid)
        {
            string gamePath = AppDataController.GetGamePathWithoutGameName();
            string targetDirectory = Path.Combine(gamePath, "ZenlessZoneZero_Data", "Persistent", "LocalStorage");

            if (Directory.Exists(targetDirectory))
            {
                var files = Directory.GetFiles(targetDirectory, $"USD_{uid}.bin");
                if (files.Length > 0)
                {
                    var latestFile = files.OrderByDescending(f => File.GetLastAccessTime(f)).First();
                    File.SetLastAccessTime(latestFile, DateTime.Now);
                    Logging.Write($"Updated access time for file: {latestFile}", 0);
                }
                else
                {
                    Logging.Write($"No file found for UID: {uid}", 0);
                }
            }
        }

        private async Task<string> GetCurrentLogin()
        {
            var gamePath = AppDataController.GetGamePathWithoutGameName();
            string targetDirectory = Path.Combine(gamePath, "ZenlessZoneZero_Data", "Persistent", "LocalStorage");

            if (Directory.Exists(targetDirectory))
            {
                var files = Directory.GetFiles(targetDirectory, "USD_*.bin");
                if (files.Length > 0)
                {
                    var latestFile = files.OrderByDescending(f => File.GetLastAccessTime(f)).First();
                    var fileName = Path.GetFileNameWithoutExtension(latestFile);
                    var prefix = "USD_";

                    if (fileName.StartsWith(prefix))
                    {
                        var result = fileName.Substring(prefix.Length);
                        Logging.Write($"Found latest accessed file: {result}", 0);
                        if (await ProcessRun.ZenlessToolsHelperAsync($"/GetCurrentLogin {StartGameView.GameRegion}") == "false") return "当前未登录账号";
                        return result;
                    }
                }
            }
            Logging.Write("No valid files found. Returning 0", 0);
            return "0";
        }


        private async void UpdateAccount(object sender, RoutedEventArgs e)
        {
            if (AccountListView.SelectedItem != null)
            {
                Account selectedAccount = (Account)AccountListView.SelectedItem;
                string command = $"/RestoreUser {StartGameView.GameRegion} {selectedAccount.uid} {selectedAccount.name}";
                var result = await ProcessRun.ZenlessToolsHelperAsync(command);

                if (result.Contains("切换失败"))
                {
                    NotificationManager.RaiseNotification("账号更新失败", result, InfoBarSeverity.Error, false, 3);
                }
                else
                {
                    NotificationManager.RaiseNotification("账号更新成功", $"成功更新 UID: {selectedAccount.uid}", InfoBarSeverity.Success, false, 3);
                }
                await InitData();
            }
        }

        private async void SaveAccount(object sender, RoutedEventArgs e)
        {
            try
            {
                var accountsTask = ProcessRun.ZenlessToolsHelperAsync($"/GetSaveUser {StartGameView.GameRegion}");

                var CurrentLoginUID = await GetCurrentLogin();
                List<Account> accounts = JsonConvert.DeserializeObject<List<Account>>(await accountsTask);

                var currentAccount = accounts?.FirstOrDefault(account => account.uid == CurrentLoginUID);

                if (currentAccount != null)
                {
                    DialogManager.RaiseDialog(XamlRoot, "账号已经存在", $"是否要覆盖保存UID: {CurrentLoginUID}", true, "确定覆盖", SaveAccount_Override);
                }
                else
                {
                    saveAccountUID.Text = $"将要保存的UID为: {CurrentLoginUID}";
                    saveAccountName.IsOpen = true;
                }
            }
            catch (Exception ex)
            {
                NotificationManager.RaiseNotification("保存账户失败", ex.Message, InfoBarSeverity.Error, false, 3);
            }
        }

        private async void SaveAccount_Override()
        {
            try
            {
                var CurrentLoginUID = await GetCurrentLogin();

                string currentName = await GetAccountNameByUID(CurrentLoginUID);

                if (!string.IsNullOrEmpty(currentName))
                {
                    string backupResult = await ProcessRun.ZenlessToolsHelperAsync($"/BackupUser {StartGameView.GameRegion} {currentName}");
                    NotificationManager.RaiseNotification("覆盖保存成功", $"成功保存 UID: {CurrentLoginUID}", InfoBarSeverity.Success, false, 3);
                    Console.WriteLine(backupResult);
                }
                else
                {
                    NotificationManager.RaiseNotification("覆盖保存失败", "未找到当前 UID 的别名，无法进行覆盖保存。", InfoBarSeverity.Error, false, 3);
                }
            }
            catch (Exception ex)
            {
                NotificationManager.RaiseNotification("覆盖保存失败", ex.Message, InfoBarSeverity.Error, false, 3);
            }
        }

        public static async Task<string> GetAccountNameByUID(string UID)
        {
            string jsonData = await ProcessRun.ZenlessToolsHelperAsync($"/GetSaveUser {StartGameView.GameRegion}");
            List<Account> accounts = JsonConvert.DeserializeObject<List<Account>>(jsonData);
            Account account = accounts?.Find(acc => acc.uid == UID);
            return account?.name ?? "";
        }

        private async void SaveAccount_C(object sender, RoutedEventArgs e)
        {
            saveAccountName.IsOpen = false;
            var result = await ProcessRun.ZenlessToolsHelperAsync($"/BackupUser {StartGameView.GameRegion} " + await GetCurrentLogin() + " " + saveAccountNameInput.Text);
            if (result.Length > 0) NotificationManager.RaiseNotification("保存账户失败", result, InfoBarSeverity.Error, false, 3);
            else NotificationManager.RaiseNotification("保存账户成功", result, InfoBarSeverity.Success, false, 3);
            renameAccount.IsEnabled = true;
            saveAccount.IsEnabled = true;
            saveAccountNameInput.Text = "";
            await InitData();
        }

        private async void RenameAccount_C(object sender, RoutedEventArgs e)
        {
            renameAccountTip.IsOpen = false;
            Account selectedAccount = (Account)AccountListView.SelectedItem;
            var result = await ProcessRun.ZenlessToolsHelperAsync($"/RenameUser {StartGameView.GameRegion} {selectedAccount.uid} {selectedAccount.name} {renameAccountNameInput.Text}");
            NotificationManager.RaiseNotification("重命名账户成功", result, InfoBarSeverity.Success, false, 3);
            renameAccountSuccess.Subtitle = result;
            renameAccountSuccess.IsOpen = true;
            await InitData();
        }

        private async void RemoveAccount_C(TeachingTip sender, object args)
        {
            removeAccountCheck.IsOpen = false;
            Account selectedAccount = (Account)AccountListView.SelectedItem;
            var result = await ProcessRun.ZenlessToolsHelperAsync($"/RemoveUser {StartGameView.GameRegion} {selectedAccount.uid} {selectedAccount.name}");
            NotificationManager.RaiseNotification("移除账户成功", result, InfoBarSeverity.Success, false, 3);
            removeAccountSuccess.Subtitle = result;
            removeAccountSuccess.IsOpen = true;
            await InitData();
        }

        private async void GetCurrentAccount(object sender, RoutedEventArgs e)
        {
            var result = await GetCurrentLogin();
            NotificationManager.RaiseNotification("当前账户查询", "UID:"+result, InfoBarSeverity.Success, false, 3);
            currentAccountTip.IsOpen = true;
            currentAccountTip.Subtitle = "当前UID为:" + result;
        }

        private void DeleteAccount(object sender, RoutedEventArgs e)
        {
            removeAccountCheck.IsOpen = true;
        }

        private void RenameAccount(object sender, RoutedEventArgs e)
        {
            renameAccountTip.IsOpen = true;
        }

        private async void RefreshAccount(object sender, RoutedEventArgs e)
        {
            refreshAccount.Visibility = Visibility.Collapsed;
            refreshAccount_Loading.Visibility = Visibility.Visible;
            await InitData();
        }
    }

    public class Account
    {
        public string uid { get; set; }
        public string name { get; set; }
    }
}
