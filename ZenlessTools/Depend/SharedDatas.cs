using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenlessTools.Depend
{
    public static class SharedDatas
    {
        public static class SharedData
        {
            private static string _savedUsers = null;
            public static string SavedUsers
            {
                get
                {
                    return _savedUsers;
                }
                set
                {
                    _savedUsers = value;
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
