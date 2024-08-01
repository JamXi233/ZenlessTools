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
using ZenlessTools.Depend;
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;


namespace ZenlessTools
{
    public partial class App : Application
    {
        private const string MutexName = "ZenlessToolsSingletonMutex";
        private static Mutex _mutex;

        public static MainWindow MainWindow { get; private set; }
        public static ApplicationTheme CurrentTheme { get; private set; }
        public static bool SDebugMode { get; set; }
        // 导入 AllocConsole 和 FreeConsole 函数
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();
        // 导入 GetAsyncKeyState 函数
        [DllImport("User32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_RESTORE = 9;

        internal Window m_window;
        public static bool IsRequireReboot { get; set; } = false;
        public static bool IsZenlessToolsRequireUpdate { get; set; } = false;
        public static bool IsZenlessToolsHelperRequireUpdate { get; set; } = false;

        public App()
        {
            bool createdNew;
            _mutex = new Mutex(true, MutexName, out createdNew);
            if (!createdNew)
            {
                BringExistingInstanceToForeground();
                Environment.Exit(1);
            }
            else
            {
                InitializeComponent();
                Init();
                InitAppData();
                SetupTheme();
                InitAdminMode();
            }

        }

        private void BringExistingInstanceToForeground()
        {
            var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
            foreach (var process in System.Diagnostics.Process.GetProcessesByName(currentProcess.ProcessName))
            {
                if (process.Id != currentProcess.Id)
                {
                    IntPtr hWnd = process.MainWindowHandle;
                    if (IsIconic(hWnd))
                    {
                        ShowWindow(hWnd, SW_RESTORE);
                    }
                    SetForegroundWindow(hWnd);
                    break;
                }
            }
        }


        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            if (AppDataController.GetTerminalMode() == -1 || AppDataController.GetTerminalMode() == 0)
            {
                MainWindow = new MainWindow();
                MainWindow.Activate();
            }
            else await InitTerminalModeAsync(AppDataController.GetTerminalMode());
        }

        private void InitAppData() 
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values["Config_FirstRun"] == null)
            {
                AppDataController appDataController = new AppDataController();
                appDataController.FirstRunInit();
            }
        }

        private void InitAdminMode()
        {
            if (AppDataController.GetAdminMode() == 1)
            {
                if (!ProcessRun.IsRunAsAdmin()) ProcessRun.RequestAdminAndRestart();
            }
        }

        private void SetupTheme()
        {
            var dayNight = AppDataController.GetDayNight();
            try
            {
                if (dayNight == 1)
                {
                    this.RequestedTheme = ApplicationTheme.Light;
                }
                else if (dayNight == 2)
                {
                    this.RequestedTheme = ApplicationTheme.Dark;
                }
            }
            catch (Exception ex) 
            {
                Logging.Write(ex.StackTrace);
                NotificationManager.RaiseNotification("主题切换失败", ex.Message, InfoBarSeverity.Error, true, 5);
            }
            
        }

        public async Task InitTerminalModeAsync(int Mode) 
        {
            TerminalMode.ShowConsole();
            TerminalMode terminalMode = new TerminalMode();
            bool response = await terminalMode.Init(Mode);
            if (response)
            {
                m_window = new MainWindow();
                m_window.Activate();
            }
        }

        public void Init()
        {
            AllocConsole();
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;
            Console.SetWindowSize(60, 25);
            Console.SetBufferSize(60, 25);
            TerminalMode.HideConsole();
            bool isDebug = false;
            #if DEBUG
            isDebug = true;
            #else
            #endif

            if (isDebug)
            {
                Logging.Write("Debug Mode", 1);
            }
            else
            {
                Logging.Write("Release Mode", 1);
            }
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values["Config_FirstRun"] != null) { 
                switch (AppDataController.GetConsoleMode())
                {
                    case 0:
                        TerminalMode.HideConsole();
                        break;
                    case 1:
                        TerminalMode.ShowConsole();
                        break;
                    default:
                        TerminalMode.HideConsole();
                        break;
                }
            }

            if (isDebug)
            {
                Console.Title = "𝐃𝐞𝐛𝐮𝐠𝐌𝐨𝐝𝐞:ZenlessTools";
                TerminalMode.ShowConsole();
            }
            else
            {
                Console.Title = "𝐍𝐨𝐫𝐦𝐚𝐥𝐌𝐨𝐝𝐞:ZenlessTools";
            }

            if (AppDataController.GetTerminalMode() != -1)
            {
                int Mode = (int)localSettings.Values["Config_TerminalMode"];
                TerminalMode terminalMode = new TerminalMode();
            }

        }

        public static class NotificationManager
        {
            public delegate void NotificationEventHandler(string title, string message, InfoBarSeverity severity, bool isClosable = true, int TimerSec = 0, Action action = null, string actionButtonText = null);
            public static event NotificationEventHandler OnNotificationRequested;

            public static void RaiseNotification(string title, string message, InfoBarSeverity severity, bool isClosable = true, int TimerSec = 0, Action action = null, string actionButtonText = null)
            {
                OnNotificationRequested?.Invoke(title, message, severity, isClosable, TimerSec, action, actionButtonText);
            }
        }

        public static class WaitOverlayManager
        {
            public delegate void WaitOverlayEventHandler(bool status, string title = null, string subtitle = null, bool isProgress = false, int progress = 0, bool isBtnEnabled = false, string btnContent = "", Action btnAction = null);
            public static event WaitOverlayEventHandler OnWaitOverlayRequested;

            public static void RaiseWaitOverlay(bool status, string title = null, string subtitle = null, bool isProgress = false, int progress = 0, bool isBtnEnabled = false, string btnContent = "", Action btnAction = null)
            {
                OnWaitOverlayRequested?.Invoke(status, title, subtitle, isProgress, progress, isBtnEnabled, btnContent, btnAction);
            }
        }

        public static class DialogManager
        {
            public delegate void DialogEventHandler(XamlRoot xamlRoot, string title = null, object content = null, bool isPrimaryButtonEnabled = false, string primaryButtonContent = "", Action primaryButtonAction = null, bool isSecondaryButtonEnabled = false, string secondaryButtonContent = "", Action secondaryButtonAction = null);
            public static event DialogEventHandler OnDialogRequested;

            public static void RaiseDialog(XamlRoot xamlRoot, string title = null, object content = null, bool isPrimaryButtonEnabled = false, string primaryButtonContent = "", Action primaryButtonAction = null, bool isSecondaryButtonEnabled = false, string secondaryButtonContent = "", Action secondaryButtonAction = null)
            {
                // 如果content是string类型，则创建一个TextBlock来包装它
                if (content is string textContent)
                {
                    content = new TextBlock { Text = textContent };
                }

                OnDialogRequested?.Invoke(xamlRoot, title, content, isPrimaryButtonEnabled, primaryButtonContent, primaryButtonAction, isSecondaryButtonEnabled, secondaryButtonContent, secondaryButtonAction);
            }
        }

    }
}
