using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenlessTools.Depend
{
    public static class SharedDatas
    {
        public static class Gacha
        {
            private static double _fiveStarPity = 0;
            public static double FiveStarPity
            {
                get
                {
                    return _fiveStarPity;
                }
                set
                {
                    _fiveStarPity = value;
                }
            }
        }

        public static class ScreenShotData
        {
            private static string _screenShotPath = null;
            public static string ScreenShotPath
            {
                get
                {
                    return _screenShotPath;
                }
                set
                {
                    _screenShotPath = value;
                }
            }
        }

    }
}
