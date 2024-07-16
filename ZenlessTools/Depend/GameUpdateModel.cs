using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenlessTools.Depend
{
    public static class GameUpdateModel
    {
        public static string LatestVersion { get; set; }
        public static List<string> LatestPaths { get; set; }
        public static List<string> LatestSizes { get; set; }
        public static List<string> LatestMD5s { get; set; }
        public static string LatestVoicePacksPath { get; set; }
        public static string LatestVoicePacksSize { get; set; }
        public static string LatestVoicePacksMD5 { get; set; }
        public static string DiffsPath { get; set; }
        public static string DiffsSize { get; set; }
        public static string DiffsMD5 { get; set; }
        public static string DiffsVoicePacksPath { get; set; }
        public static string DiffsVoicePacksSize { get; set; }
        public static string DiffsVoicePacksMD5 { get; set; }
        public static bool isGetUpdateInfo { get; set; }
    }

}
