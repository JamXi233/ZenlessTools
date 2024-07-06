using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenlessTools.Depend
{
    class SharedData
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
}
