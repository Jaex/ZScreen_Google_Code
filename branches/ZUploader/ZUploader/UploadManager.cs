﻿#region License Information (GPL v2)

/*
    ZUploader - A program that allows you to upload images, text or files in your clipboard
    Copyright (C) 2010 ZScreen Developers

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

#endregion License Information (GPL v2)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Windows.Forms;
using UploadersLib.Helpers;

namespace ZUploader
{
    public static class UploadManager
    {
        public static ImageDestType2 ImageUploader { get; set; }
        public static TextDestType2 TextUploader { get; set; }
        public static FileUploaderType2 FileUploader { get; set; }
        public static ListView ListViewControl { get; set; }

        public static List<Task> Tasks = new List<Task>();

        public static void Upload(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                if (File.Exists(path))
                {
                    EDataType type;

                    if (TextUploader != TextDestType2.FILE && Helpers.IsValidTextFile(path))
                    {
                        type = EDataType.Text;
                    }
                    else if (ImageUploader != ImageDestType2.FILE && Helpers.IsValidImageFile(path))
                    {
                        type = EDataType.Image;
                    }
                    else
                    {
                        type = EDataType.File;
                    }

                    Task task = new Task(type, path);
                    StartUpload(task);
                }
                else if (Directory.Exists(path))
                {
                    string[] files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);

                    Upload(files);
                }
            }
        }

        public static void Upload(string[] files)
        {
            foreach (string file in files)
            {
                Upload(file);
            }
        }

        public static void Upload()
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    Upload(ofd.FileName);
                }
            }
        }

        public static void ClipboardUpload()
        {
            if (Clipboard.ContainsImage())
            {
                using (Image img = Clipboard.GetImage())
                {
                    MemoryStream stream = new MemoryStream();
                    img.SaveJPG100(stream);
                    string fileName = UploadHelpers.GetDateTimeString() + ".jpg";
                    EDataType type = ImageUploader == ImageDestType2.FILE ? EDataType.File : EDataType.Image;
                    Task task = new Task(type, stream, fileName);
                    StartUpload(task);
                }
            }
            else if (Clipboard.ContainsText())
            {
                byte[] byteArray = Encoding.UTF8.GetBytes(Clipboard.GetText());
                MemoryStream stream = new MemoryStream(byteArray);
                string fileName = UploadHelpers.GetDateTimeString() + ".txt";
                EDataType type = TextUploader == TextDestType2.FILE ? EDataType.File : EDataType.Text;
                Task task = new Task(type, stream, fileName);
                StartUpload(task);
            }
            else if (Clipboard.ContainsFileDropList())
            {
                string[] files = Clipboard.GetFileDropList().Cast<string>().ToArray();
                Upload(files);
            }
        }

        private static void StartUpload(Task task)
        {
            Tasks.Add(task);
            task.Info.ID = Tasks.Count - 1;
            task.UploadStarted += new Task.TaskEventHandler(task_UploadStarted);
            task.UploadProgressChanged += new Task.TaskEventHandler(task_UploadProgressChanged);
            task.UploadCompleted += new Task.TaskEventHandler(task_UploadCompleted);
            task.Start();
        }

        public static void StopUpload(int index)
        {
            if (Tasks.Count < index)
            {
                Tasks[index].Stop();
            }
        }

        private static void task_UploadStarted(UploadInfo info)
        {
            if (ListViewControl != null)
            {
                ListViewItem lvi = new ListViewItem();
                lvi.Text = info.FileName;
                lvi.SubItems.Add(info.Status);
                lvi.SubItems.Add(string.Empty);
                lvi.SubItems.Add(string.Empty);
                lvi.SubItems.Add(string.Empty);
                lvi.SubItems.Add(string.Empty);
                lvi.SubItems.Add(info.UploaderType.ToString());
                lvi.SubItems.Add(info.UploaderName);
                lvi.SubItems.Add(string.Empty);
                lvi.BackColor = info.ID % 2 == 0 ? Color.White : Color.WhiteSmoke;
                lvi.ImageIndex = 0;
                ListViewControl.Items.Add(lvi);
            }
        }

        private static void task_UploadProgressChanged(UploadInfo info)
        {
            if (ListViewControl != null)
            {
                ListViewItem lvi = ListViewControl.Items[info.ID];
                lvi.SubItems[2].Text = string.Format("{0:N0}%  {1:N0} kB / {2:N0} kB", info.Progress.Percentage,
                    info.Progress.Position / 1000, info.Progress.Length / 1000);
                lvi.SubItems[3].Text = string.Format("{0:N0} kB/s", info.Progress.Speed);
                lvi.SubItems[4].Text = string.Format("{0:00}:{1:00}", info.Progress.Elapsed.Minutes, info.Progress.Elapsed.Seconds);
                lvi.SubItems[5].Text = string.Format("{0:00}:{1:00}", info.Progress.Remaining.Minutes, info.Progress.Remaining.Seconds);
            }
        }

        private static void task_UploadCompleted(UploadInfo info)
        {
            if (ListViewControl != null)
            {
                ListViewItem lvi = ListViewControl.Items[info.ID];
                lvi.Tag = info.Result;

                if (info.Result.Errors != null && info.Result.Errors.Count > 0)
                {
                    lvi.SubItems[1].Text = "Error: " + info.Result.Errors.Last();
                    lvi.SubItems[8].Text = string.Empty;
                    lvi.ImageIndex = 1;
                }
                else
                {
                    lvi.SubItems[1].Text = info.Status;
                    lvi.SubItems[8].Text = info.Result.URL;
                    lvi.ImageIndex = 2;
                }

                lvi.EnsureVisible();

                if (Program.Settings.ClipboardAutoCopy && !string.IsNullOrEmpty(info.Result.URL))
                {
                    Clipboard.SetText(info.Result.URL);
                }

                if (Program.Settings.AutoPlaySound)
                {
                    SystemSounds.Exclamation.Play();
                }
            }
        }
    }
}