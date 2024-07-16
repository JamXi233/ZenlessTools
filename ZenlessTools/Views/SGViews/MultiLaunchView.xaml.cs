using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ZenlessTools.Depend;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;

namespace ZenlessTools.Views.SGViews
{
    public sealed partial class MultiLaunchView : Page
    {
        private ObservableCollection<Container> _containers;

        public MultiLaunchView()
        {
            this.InitializeComponent();
            Logging.Write("Switch to MultiLaunchView", 0);
            _containers = new ObservableCollection<Container>();
            ContainerListView.ItemsSource = _containers;
            InitData();
        }

        private async Task InitData(string mode = null)
        {
            if (AppDataController.GetMultiLaunchMode() == 1)
            {
                LoadContainers();
            }
        }

        private void LoadContainers()
        {
            var basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "JSG-LLC", "ZenlessTools", "MultiLaunch");
            if (Directory.Exists(basePath))
            {
                _containers.Clear();
                var directories = Directory.GetDirectories(basePath, "ZenlessZoneZero_*");
                var containerList = new List<Container>();

                foreach (var dir in directories)
                {
                    var idPart = dir.Split('_').Last();
                    if (int.TryParse(idPart, out int id))
                    {
                        var status = IsContainerRunning(dir) ? "已启动" : "未启动";
                        containerList.Add(new Container { ID = id, Status = status });
                    }
                }
                containerList = containerList.OrderBy(c => c.ID).ToList();
                foreach (var container in containerList)
                {
                    _containers.Add(container);
                }
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadContainers();
        }

        private void AddContainer_Click(object sender, RoutedEventArgs e)
        {
            var basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "JSG-LLC", "ZenlessTools", "MultiLaunch");
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }

            var newId = _containers.Count > 0 ? _containers.Max(c => c.ID) + 1 : 1;
            var newDir = Path.Combine(basePath, $"ZenlessZoneZero_Multi_{newId}");
            var gamePath = AppDataController.GetGamePathWithoutGameName();

            var linkCommand = $"/C mklink /D \"{newDir}\" \"{gamePath}\"";
            var processInfo = new ProcessStartInfo("cmd.exe", linkCommand)
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(processInfo))
            {
                process.WaitForExit();
                var output = process.StandardOutput.ReadToEnd();
            }

            _containers.Add(new Container { ID = newId, Status = "未启动" });
        }

        private void StartContainer_Click(object sender, RoutedEventArgs e)
        {
            var container = (sender as Button)?.DataContext as Container;
            if (container != null)
            {
                StartContainer(container.ID);
                container.Status = "已启动";
            }
        }

        private void DeleteContainer_Click(object sender, RoutedEventArgs e)
        {
            var container = (sender as Button)?.DataContext as Container;
            if (container != null)
            {
                var dirToDelete = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "JSG-LLC", "ZenlessTools", "MultiLaunch", $"ZenlessZoneZero_Multi_{container.ID}");
                if (Directory.Exists(dirToDelete))
                {
                    Directory.Delete(dirToDelete, true);
                }
                _containers.Remove(container);
            }
        }

        private void StartContainer(int id)
        {
            var exePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "JSG-LLC", "ZenlessTools", "MultiLaunch", $"ZenlessZoneZero_Multi_{id}", "ZenlessZoneZero.exe");
            if (File.Exists(exePath))
            {

                var processInfo = new ProcessStartInfo(exePath)
                {
                    UseShellExecute = true,
                    Verb = "runas",
                };
                Process.Start(processInfo);
            }
        }

        private bool IsContainerRunning(string dir)
        {
            var exePath = Path.Combine(dir, "ZenlessZoneZero.exe");
            var runningProcesses = Process.GetProcessesByName("ZenlessZoneZero");

            foreach (var process in runningProcesses)
            {
                try
                {
                    if (!process.HasExited && Path.GetDirectoryName(process.MainModule.FileName).Equals(Path.GetDirectoryName(exePath), StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                catch (Exception)
                {
                    // 有时会引发异常，可忽略这些异常
                }
            }

            return false;
        }


        public class Container : INotifyPropertyChanged
        {
            private string _status;

            public int ID { get; set; }

            public string Status
            {
                get { return _status; }
                set
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
