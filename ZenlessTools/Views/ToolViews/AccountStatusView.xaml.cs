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

using Fiddler;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Spectre.Console;
using ZenlessTools.Depend;
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace ZenlessTools.Views.ToolViews
{
    public sealed partial class AccountStatusView : Page
    {
        public AccountStatusView()
        {
            this.InitializeComponent();
        }

    }

}
