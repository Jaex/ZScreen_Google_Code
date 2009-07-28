#region License Information (GPL v2)
/*
    ZScreen - A program that allows you to upload screenshots in one keystroke.
    Copyright (C) 2008-2009  Brandon Zimmerman

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
    
    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using ZSS.Forms;

namespace ZSS.Global
{
    public static class FormsMgr
    {
        public static void ShowLicense()
        {
            string lic = FileSystem.GetTextFromFile(Path.Combine(Application.StartupPath, "License.txt"));
            lic = lic != string.Empty ? lic : FileSystem.GetText("License.txt");
            if (lic != string.Empty)
            {
                frmTextViewer v = new frmTextViewer(string.Format("{0} - {1}",
                    Application.ProductName, "License"), lic) { Icon = Properties.Resources.zss_main };
                v.ShowDialog();
            }
        }

        public static void ShowVersionHistory()
        {
            string h = FileSystem.GetTextFromFile(Path.Combine(Application.StartupPath, "VersionHistory.txt"));
            if (h == string.Empty)
            {
                h = FileSystem.GetText("VersionHistory.txt");
            }
            if (h != string.Empty)
            {
                frmTextViewer v = new frmTextViewer(string.Format("{0} - {1}",
                    Application.ProductName, "Version History"), h) { Icon = Properties.Resources.zss_main };
                v.ShowDialog();
            }
        }

        public static void ShowAboutWindow()
        {
            AboutBox ab = new AboutBox();
            ab.ShowDialog();
        }

    }
}
