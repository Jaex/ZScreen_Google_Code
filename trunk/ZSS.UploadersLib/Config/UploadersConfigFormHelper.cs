﻿#region License Information (GPL v2)

/*
    ZScreen - A program that allows you to upload screenshots in one keystroke.
    Copyright (C) 2008-2011 ZScreen Developers

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
using System.ComponentModel;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using HelpersLib;
using UploadersLib.FileUploaders;
using UploadersLib.HelperClasses;
using UploadersLib.ImageUploaders;
using UploadersLib.TextUploaders;

namespace UploadersLib
{
    public partial class UploadersConfigForm : Form
    {
        #region Imgur

        public void ImgurAuthOpen()
        {
            try
            {
                OAuthInfo oauth = new OAuthInfo(APIKeys.ImgurConsumerKey, APIKeys.ImgurConsumerSecret);

                string url = new Imgur(oauth).GetAuthorizationURL();

                if (!string.IsNullOrEmpty(url))
                {
                    Config.ImgurOAuthInfo = oauth;
                    Process.Start(url);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void ImgurAuthComplete()
        {
            try
            {
                string verification = txtImgurVerificationCode.Text;

                if (!string.IsNullOrEmpty(verification) && Config.ImgurOAuthInfo != null &&
                    !string.IsNullOrEmpty(Config.ImgurOAuthInfo.AuthToken) && !string.IsNullOrEmpty(Config.ImgurOAuthInfo.AuthSecret))
                {
                    bool result = new Imgur(Config.ImgurOAuthInfo).GetAccessToken(verification);

                    if (result)
                    {
                        lblImgurAccountStatus.Text = "Login successful: " + Config.ImgurOAuthInfo.UserToken;
                        MessageBox.Show("Login successful.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        lblImgurAccountStatus.Text = "Login failed.";
                        MessageBox.Show("Login failed.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion Imgur

        #region Flickr

        public void FlickrAuthOpen()
        {
            try
            {
                FlickrUploader flickr = new FlickrUploader(APIKeys.FlickrKey, APIKeys.FlickrSecret);
                btnFlickrOpenAuthorize.Tag = flickr.GetFrob();
                string url = flickr.GetAuthLink(FlickrPermission.Write);
                if (!string.IsNullOrEmpty(url))
                {
                    Process.Start(url);
                    btnFlickrCompleteAuth.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void FlickrAuthComplete()
        {
            try
            {
                string token = btnFlickrOpenAuthorize.Tag as string;
                if (!string.IsNullOrEmpty(token))
                {
                    FlickrUploader flickr = new FlickrUploader(APIKeys.FlickrKey, APIKeys.FlickrSecret);
                    Config.FlickrAuthInfo = flickr.GetToken(token);
                    pgFlickrAuthInfo.SelectedObject = Config.FlickrAuthInfo;
                    // btnFlickrOpenImages.Text = string.Format("{0}'s photostream", Engine.conf.FlickrAuthInfo.Username);
                    MessageBox.Show("Login successful.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void FlickrCheckToken()
        {
            try
            {
                if (Config.FlickrAuthInfo != null)
                {
                    string token = Config.FlickrAuthInfo.Token;
                    if (!string.IsNullOrEmpty(token))
                    {
                        FlickrUploader flickr = new FlickrUploader(APIKeys.FlickrKey, APIKeys.FlickrSecret);
                        Config.FlickrAuthInfo = flickr.CheckToken(token);
                        pgFlickrAuthInfo.SelectedObject = Config.FlickrAuthInfo;
                        MessageBox.Show("Login successful.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void FlickrOpenImages()
        {
            if (Config.FlickrAuthInfo != null)
            {
                string userID = Config.FlickrAuthInfo.UserID;
                if (!string.IsNullOrEmpty(userID))
                {
                    FlickrUploader flickr = new FlickrUploader(APIKeys.FlickrKey, APIKeys.FlickrSecret);
                    string url = flickr.GetPhotosLink(userID);
                    if (!string.IsNullOrEmpty(url))
                    {
                        Process.Start(url);
                    }
                }
            }
        }

        #endregion Flickr

        #region MediaWiki

        private MediaWikiAccount GetSelectedMediaWiki()
        {
            MediaWikiAccount account = null;

            if (Config.MediaWikiAccountList.CheckSelected(Config.MediaWikiAccountSelected))
            {
                account = Config.MediaWikiAccountList[Config.MediaWikiAccountSelected];
            }

            return account;
        }

        private void TestMediaWikiAccount(MediaWikiAccount account, Action success, Action<string> failure)
        {
            var timeoutTimer = new System.Windows.Forms.Timer();
            Thread thread = new Thread(new ThreadStart(delegate
            {
                TestMediaWikiAccountThread(account,
                    delegate()
                    {
                        timeoutTimer.Stop(); success();
                    },
                    delegate(string msg)
                    {
                        timeoutTimer.Stop(); failure(msg);
                    });
            }));
            thread.Start();
            timeoutTimer.Interval = 10000;
            timeoutTimer.Tick += new EventHandler(delegate(object sender, EventArgs e)
            {
                thread.Interrupt();
                timeoutTimer.Stop();
                failure("The website at the URL you specified doesn't answer");
            });
            timeoutTimer.Start();
        }

        private void TestMediaWikiAccountThread(MediaWikiAccount acc, Action success, Action<string> failure)
        {
            try
            {
                MediaWiki connector = new MediaWiki(new MediaWikiOptions(acc, null)); // TODO: CheckProxySettings().GetWebProxy
                connector.Login();
                success();
            }
            catch (Exception ex)
            {
                // ignore ThreadInterruptedException : the request timed out and the thread was interrupted
                if (!(ex.InnerException is ThreadInterruptedException))
                    failure(ex.Message);
            }
        }

        private void MediaWikiSetup(IEnumerable<MediaWikiAccount> accs)
        {
            if (accs != null)
            {
                ucMediaWikiAccounts.AccountsList.Items.Clear();
                Config.MediaWikiAccountList = new List<MediaWikiAccount>();
                Config.MediaWikiAccountList.AddRange(accs);
                foreach (MediaWikiAccount acc in Config.MediaWikiAccountList)
                {
                    ucMediaWikiAccounts.AccountsList.Items.Add(acc);
                }
            }
        }

        #endregion MediaWiki

        #region Dropbox

        public void DropboxAuthOpen()
        {
            try
            {
                OAuthInfo oauth = new OAuthInfo(APIKeys.DropboxConsumerKey, APIKeys.DropboxConsumerSecret);

                string url = new Dropbox(oauth).GetAuthorizationURL();

                if (!string.IsNullOrEmpty(url))
                {
                    Config.DropboxOAuthInfo = oauth;
                    Process.Start(url);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void DropboxAuthComplete()
        {
            if (Config.DropboxOAuthInfo != null && !string.IsNullOrEmpty(Config.DropboxOAuthInfo.AuthToken) &&
                !string.IsNullOrEmpty(Config.DropboxOAuthInfo.AuthSecret))
            {
                Dropbox dropbox = new Dropbox(Config.DropboxOAuthInfo);
                bool result = dropbox.GetAccessToken();

                if (result)
                {
                    DropboxAccountInfo account = dropbox.GetAccountInfo();

                    if (account != null)
                    {
                        Config.DropboxEmail = account.Email;
                        Config.DropboxName = account.Display_name;
                        Config.DropboxUserID = account.Uid.ToString();
                        Config.DropboxUploadPath = txtDropboxPath.Text;
                        UpdateDropboxStatus();
                        MessageBox.Show("Login successful.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    else
                    {
                        MessageBox.Show("GetAccountInfo failed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Login failed.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("You must give access to ZScreen from Authorize page first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            Config.DropboxOAuthInfo = null;
            UpdateDropboxStatus();
        }

        private void UpdateDropboxStatus()
        {
            if (Config.DropboxOAuthInfo != null && !string.IsNullOrEmpty(Config.DropboxOAuthInfo.UserToken) &&
                !string.IsNullOrEmpty(Config.DropboxOAuthInfo.UserSecret))
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Login status: Success");
                sb.AppendLine("Email: " + Config.DropboxEmail);
                sb.AppendLine("Name: " + Config.DropboxName);
                sb.AppendLine("User ID: " + Config.DropboxUserID);
                string uploadPath = new NameParser { IsFolderPath = true }.Convert(Dropbox.TidyUploadPath(Config.DropboxUploadPath));
                if (!string.IsNullOrEmpty(uploadPath))
                {
                    sb.AppendLine("Upload path: " + uploadPath);
                    sb.AppendLine("Download path: " + Dropbox.GetDropboxURL(Config.DropboxUserID, uploadPath, "{Filename}"));
                }
                lblDropboxStatus.Text = sb.ToString();
            }
            else
            {
                lblDropboxStatus.Text = "Login status: Authorize required";
            }
        }

        #endregion Dropbox

        #region FTP

        public bool CheckFTPAccounts()
        {
            return Config.FTPAccountList.CheckSelected(Config.FTPSelectedImage);
        }

        public void TestFTPAccountAsync(FTPAccount acc)
        {
            if (acc != null)
            {
                ucFTPAccounts.btnTest.Enabled = false;
                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += new DoWorkEventHandler(bw_DoWorkTestFTPAccount);
                bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompletedTestFTPAccount);
                bw.RunWorkerAsync(acc);
            }
        }

        private void bw_DoWorkTestFTPAccount(object sender, DoWorkEventArgs e)
        {
            TestFTPAccount((FTPAccount)e.Argument, false);
        }

        private void bw_RunWorkerCompletedTestFTPAccount(object sender, RunWorkerCompletedEventArgs e)
        {
            ucFTPAccounts.btnTest.Enabled = true;
        }

        private void FTPAccountsExport()
        {
            if (Config.FTPAccountList != null)
            {
                SaveFileDialog dlg = new SaveFileDialog
                {
                    FileName = string.Format("{0}-{1}-accounts", Application.ProductName, DateTime.Now.ToString("yyyyMMdd")),
                    Filter = StaticHelper.FilterFTPAccounts
                };
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    FTPAccountManager fam = new FTPAccountManager(Config.FTPAccountList);
                    fam.Save(dlg.FileName);
                }
            }
        }

        private void FTPAccountsImport()
        {
            OpenFileDialog dlg = new OpenFileDialog { Filter = StaticHelper.FilterFTPAccounts };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                FTPAccountManager fam = FTPAccountManager.Read(dlg.FileName);
                FTPSetup(fam.FTPAccounts);
            }
        }

        public static void TestFTPAccount(FTPAccount account, bool silent)
        {
            string msg;
            string sfp = account.GetSubFolderPath();
            using (FTP ftpClient = new FTP(account))
            {
                try
                {
                    DateTime time = DateTime.Now;
                    ftpClient.Test(sfp);
                    msg = "Success!";
                }
                catch (Exception e)
                {
                    if (e.Message.StartsWith("Could not change working directory to"))
                    {
                        try
                        {
                            ftpClient.MakeMultiDirectory(sfp);
                            ftpClient.Test(sfp);
                            msg = "Success!\nAuto created folders: " + sfp;
                        }
                        catch (Exception e2)
                        {
                            msg = e2.Message;
                        }
                    }
                    else
                    {
                        msg = e.Message;
                    }
                }
            }

            if (!string.IsNullOrEmpty(msg))
            {
                string ping = SendPing(account.Host, 3);
                if (!string.IsNullOrEmpty(ping))
                {
                    msg += "\n\nPing results:\n" + ping;
                }
                if (silent)
                {
                    // Engine.MyLogger.WriteLine(string.Format("Tested {0} sub-folder path in {1}", sfp, account.ToString()));
                }
                else
                {
                    MessageBox.Show(msg, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        public static string SendPing(string host)
        {
            return SendPing(host, 1);
        }

        public static string SendPing(string host, int count)
        {
            string[] status = new string[count];

            using (Ping ping = new Ping())
            {
                PingReply reply;
                //byte[] buffer = Encoding.ASCII.GetBytes(new string('a', 32));
                for (int i = 0; i < count; i++)
                {
                    reply = ping.Send(host, 3000);
                    if (reply.Status == IPStatus.Success)
                    {
                        status[i] = reply.RoundtripTime.ToString() + " ms";
                    }
                    else
                    {
                        status[i] = "Timeout";
                    }
                    Thread.Sleep(100);
                }
            }

            return string.Join(", ", status);
        }

        public FTPAccount GetFtpAcctActive()
        {
            FTPAccount acc = null;
            if (CheckFTPAccounts())
            {
                acc = Config.FTPAccountList[Config.FTPSelectedImage];
            }
            return acc;
        }

        #endregion FTP

        #region SendSpace

        public UserPassBox SendSpaceRegister()
        {
            UserPassBox upb = new UserPassBox("SendSpace Registration...", "John Doe", "john.doe@gmail.com", "JohnDoe", "");
            upb.ShowDialog();
            if (upb.DialogResult == DialogResult.OK)
            {
                SendSpace sendSpace = new SendSpace(APIKeys.SendSpaceKey);
                upb.Success = sendSpace.AuthRegister(upb.UserName, upb.FullName, upb.Email, upb.Password);
                if (!upb.Success && sendSpace.Errors.Count > 0)
                {
                    MessageBox.Show(sendSpace.ToErrorString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            return upb;
        }

        #endregion SendSpace

        #region Pastebin

        public void PastebinLogin()
        {
            if (Config.PastebinSettings != null)
            {
                try
                {
                    PastebinUploader pastebin = new PastebinUploader(APIKeys.PastebinKey, Config.PastebinSettings);

                    if (pastebin.Login())
                    {
                        pgPastebinSettings.SelectedObject = Config.PastebinSettings;
                        MessageBox.Show("Login successful.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Login failed.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        #endregion Pastebin
    }
}