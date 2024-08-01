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
using static ZenlessTools.App;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO.Compression;
using System.Linq;

namespace ZenlessTools.Depend
{
    // 全局状态管理器
    public class DownloadHelpers
    {
        public static double CurrentProgress { get; set; }
        public static string CurrentSpeed { get; set; }
        public static string CurrentSize { get; set; }
        public static bool isDownloading { get; set; } = false;
        public static bool isPaused { get; set; } = true;
        public static bool isFinished { get; set; } = false;
        public static event Action<double, string, string> DownloadProgressChanged;

        public static void UpdateProgress(double progress, string speed, string size)
        {
            CurrentProgress = progress;
            CurrentSpeed = speed;
            CurrentSize = size;
            DownloadProgressChanged?.Invoke(progress, speed, size);
        }

        public static async Task DownloadAndExtractZipFileAsync(string url, string destinationPath, bool extract = false, string extractPath = null, string overlayMessage = "正在下载文件", string overlayTitle = "请耐心等待")
        {
            string zipFilePath = Path.Combine(destinationPath, "tempDownload.zip");
            await DownloadFileAsync(url, zipFilePath, overlayMessage, overlayTitle);

            if (extract)
            {
                extractPath ??= destinationPath;
                await ExtractZipFileAsync(zipFilePath, extractPath, overlayMessage, overlayTitle);
                File.Delete(zipFilePath);
            }
        }

        public static async Task DownloadFileAsync(string url, string destinationPath, string overlayMessage = "正在下载文件", string overlayTitle = "请耐心等待")
        {
            WaitOverlayManager.RaiseWaitOverlay(true, overlayMessage, overlayTitle, true, 0);

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

            using (HttpClient client = new HttpClient())
            {
                using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    long totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var buffer = new byte[8192];
                        long totalReadBytes = 0L;
                        int readBytes;

                        while ((readBytes = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, readBytes);
                            totalReadBytes += readBytes;
                            int progress = totalBytes != -1L ? (int)((totalReadBytes * 100) / totalBytes) : -1;
                            WaitOverlayManager.RaiseWaitOverlay(true, overlayMessage, overlayTitle, true, progress);
                        }
                    }
                }
            }

            WaitOverlayManager.RaiseWaitOverlay(false, "", "", false, 0);
        }

        private static async Task ExtractZipFileAsync(string zipFilePath, string extractPath, string overlayMessage, string overlayTitle)
        {
            using (FileStream zipFileStream = new FileStream(zipFilePath, FileMode.Open, FileAccess.Read))
            using (ZipArchive archive = new ZipArchive(zipFileStream, ZipArchiveMode.Read))
            {
                long totalBytes = archive.Entries.Sum(entry => entry.Length);
                long totalReadBytes = 0L;
                int progress = 0;

                foreach (var entry in archive.Entries)
                {
                    string destinationFileName = Path.Combine(extractPath, entry.FullName);
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationFileName));

                    using (var entryStream = entry.Open())
                    using (var fileStream = new FileStream(destinationFileName, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var buffer = new byte[8192];
                        int readBytes;
                        while ((readBytes = await entryStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, readBytes);
                            totalReadBytes += readBytes;
                            progress = (int)((totalReadBytes * 100) / totalBytes);
                            WaitOverlayManager.RaiseWaitOverlay(true, overlayMessage, overlayTitle, true, progress);
                        }
                    }
                }
            }

            WaitOverlayManager.RaiseWaitOverlay(false, "", "", false, 0);
        }
    }
}
