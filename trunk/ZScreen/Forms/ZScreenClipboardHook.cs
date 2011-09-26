﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using HelpersLib;
using ZScreenLib;
using System.Windows.Forms;
using System.ComponentModel;

namespace ZScreenGUI
{
    public partial class ZScreen : HotkeyForm
    {
        private static Timer tmrClipboardMonitor = new Timer() { Interval = 1000, Enabled = true };

        #region Clipboard Methods

        void tmrClipboardMonitor_Tick(object sender, EventArgs e)
        {
            try
            {
                if (IsReady && !Engine.IsClipboardUploading)
                {
                    bool uploadImage = false, uploadText = false, uploadFile = false, shortenUrl = false;
                    string cbText = string.Empty;
                    if (Engine.conf.MonitorImages)
                    {
                        uploadImage = Clipboard.ContainsImage();
                    }
                    if (Engine.conf.MonitorText && Clipboard.ContainsText())
                    {
                        cbText = Clipboard.GetText();
                        uploadText = !string.IsNullOrEmpty(cbText);
                    }
                    if (Engine.conf.MonitorFiles)
                    {
                        uploadFile = Clipboard.ContainsFileDropList();
                    }
                    if (Engine.conf.MonitorUrls && Clipboard.ContainsText())
                    {
                        cbText = Clipboard.GetText();
                        shortenUrl = !string.IsNullOrEmpty(cbText) && FileSystem.IsValidLink(cbText) && cbText.Length > Engine.conf.ShortenUrlAfterUploadAfter;
                    }

                    if ((uploadImage || uploadText || uploadFile || shortenUrl) && (cbText != Engine.zPreviousSetClipboardText))
                    {
                        UploadUsingClipboard();
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        #endregion Clipboard Methods
    }
}