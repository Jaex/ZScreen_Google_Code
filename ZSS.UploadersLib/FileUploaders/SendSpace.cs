﻿#region License Information (GPL v2)
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
using System.Xml.Linq;

namespace UploadersLib.FileUploaders
{
    public class SendSpace : FileUploader
    {
        private const string SENDSPACE_API_KEY = "LV6OS1R0Q3";
        private const string SENDSPACE_API_URL = "http://api.sendspace.com/rest/";
        private const string SENDSPACE_API_VERSION = "1.0";

        private string appVersion = "1.0";

        public SendSpace()
        {

        }

        public override string Name
        {
            get { return "SendSpace"; }
        }

        #region Helpers

        public class ResponsePacket
        {
            public string Method { get; set; }
            public string Status { get; set; }
            public bool Error { get; set; }
            public string ErrorCode { get; set; }
            public string ErrorText { get; set; }
            public XElement Result { get; set; }
        }

        public ResponsePacket ParseResponse(string response)
        {
            ResponsePacket packet = new ResponsePacket();

            XDocument xml = XDocument.Parse(response);
            packet.Result = xml.Element("result");
            packet.Method = packet.Result.Attribute("method").Value;
            packet.Status = packet.Result.Attribute("status").Value;
            packet.Error = packet.Status == "fail";

            if (packet.Error)
            {
                XElement error = packet.Result.Element("error");
                packet.ErrorCode = error.Attribute("code").Value;
                packet.ErrorText = error.Attribute("text").Value;

                base.Errors.Add(string.Format("( {0} - {1} ) {2}", packet.ErrorCode, packet.Method, packet.ErrorText));
            }

            return packet;
        }

        public class LoginInfo
        {
            /// <summary>
            /// Session key to be sent with all method calls, user information, including the user account's capabilities
            /// </summary>
            public string SessionKey { get; set; }
            public string Username { get; set; }
            public string EMail { get; set; }
            public string MembershipType { get; set; }
            public string MembershipEnds { get; set; }
            public bool CapableUpload { get; set; }
            public bool CapableDownload { get; set; }
            public bool CapableFolders { get; set; }
            public bool CapableFiles { get; set; }
            public bool CapableHTTPS { get; set; }
            public bool CapableAddressBook { get; set; }
            public string BandwidthLeft { get; set; }
            public string DiskSpaceLeft { get; set; }
            public string DiskSpaceUsed { get; set; }
            public string Points { get; set; }

            public LoginInfo() { }

            public LoginInfo(XElement element)
            {
                SessionKey = element.ElementValue("session_key");
                Username = element.ElementValue("user_name");
                EMail = element.ElementValue("email");
                MembershipType = element.ElementValue("membership_type");
                MembershipEnds = element.ElementValue("membership_ends");
                CapableUpload = element.ElementValue("capable_upload") != "0";
                CapableDownload = element.ElementValue("capable_download") != "0";
                CapableFolders = element.ElementValue("capable_folders") != "0";
                CapableFiles = element.ElementValue("capable_files") != "0";
                CapableHTTPS = element.ElementValue("capable_https") != "0";
                CapableAddressBook = element.ElementValue("capable_addressbook") != "0";
                BandwidthLeft = element.ElementValue("bandwidth_left");
                DiskSpaceLeft = element.ElementValue("diskspace_left");
                DiskSpaceUsed = element.ElementValue("diskspace_used");
                Points = element.ElementValue("points");
            }
        }

        #endregion

        #region Authentication

        /// <summary>
        /// Creates a new user account. An activation/validation email will be sent automatically to the user.
        /// http://www.sendspace.com/dev_method.html?method=auth.register
        /// </summary>
        /// <returns>true = success, false = error</returns>
        public bool AuthRegister(string username, string fullname, string email, string password)
        {
            Dictionary<string, string> args = new Dictionary<string, string>();
            args.Add("method", "auth.register");
            args.Add("api_key", SENDSPACE_API_KEY); // Received from sendspace
            args.Add("user_name", username); // a-z/A-Z/0-9, 3-20 chars
            args.Add("full_name", fullname); // a-z/A-Z/space, 3-20 chars
            args.Add("email", email); // Valid email address required
            args.Add("password", password); // Can be left empty and the API will create a unique password or enter one with 4-20 chars

            string response = GetResponse(SENDSPACE_API_URL, args);

            return !ParseResponse(response).Error;
        }

        /// <summary>
        /// Obtains a new and random token per session. Required for login.
        /// http://www.sendspace.com/dev_method.html?method=auth.createToken
        /// </summary>
        /// <returns>A token to be used with the auth.login method</returns>
        public string AuthCreateToken()
        {
            Dictionary<string, string> args = new Dictionary<string, string>();
            args.Add("method", "auth.createToken");
            args.Add("api_key", SENDSPACE_API_KEY); // Received from sendspace
            args.Add("api_version", SENDSPACE_API_VERSION); // Value must be: 1.0
            args.Add("app_version", appVersion); // Application specific, formatting / style is up to you
            args.Add("response_format", "xml"); // Value must be: XML

            string response = GetResponse(SENDSPACE_API_URL, args);

            ResponsePacket packet = ParseResponse(response);

            if (!packet.Error)
            {
                return packet.Result.ElementValue("token");
            }

            return "";
        }

        /// <summary>
        /// Starts a session and returns user API method capabilities -- which features the given user can and cannot use.
        /// http://www.sendspace.com/dev_method.html?method=auth.login
        /// </summary>
        /// <param name="token">Received on create token</param>
        /// <param name="username">Registered user name</param>
        /// <param name="password">Registered password</param>
        /// <returns>Account informations including session key</returns>
        public LoginInfo AuthLogin(string token, string username, string password)
        {
            Dictionary<string, string> args = new Dictionary<string, string>();
            args.Add("method", "auth.login");
            args.Add("token", token); // Received on create token
            args.Add("user_name", username); // Registered user name
            args.Add("tokened_password", GetMD5(token + GetMD5(password))); // lowercase(md5(token+lowercase(md5(password)))) - md5 values should always be lowercase.

            string response = GetResponse(SENDSPACE_API_URL, args);

            ResponsePacket packet = ParseResponse(response);

            if (!packet.Error)
            {
                LoginInfo info = new LoginInfo(packet.Result);
                return info;
            }

            return null;
        }

        /// <summary>
        /// Checks if a session is valid or not.
        /// http://www.sendspace.com/dev_method.html?method=auth.checksession
        /// </summary>
        /// <param name="sessionKey">Received from auth.login</param>
        /// <returns>true = success, false = error</returns>
        public bool AuthCheckSession(string sessionKey)
        {
            Dictionary<string, string> args = new Dictionary<string, string>();
            args.Add("method", "auth.checkSession");
            args.Add("session_key", sessionKey); // Received from auth.login

            string response = GetResponse(SENDSPACE_API_URL, args);

            ResponsePacket packet = ParseResponse(response);

            if (!packet.Error)
            {
                string session = packet.Result.ElementValue("session");

                if (!string.IsNullOrEmpty(session))
                {
                    return session == "ok";
                }
            }

            return false;
        }

        /// <summary>
        /// Logs out from a session.
        /// http://www.sendspace.com/dev_method.html?method=auth.logout
        /// </summary>
        /// <param name="sessionKey">Received from auth.login</param>
        /// <returns>true = success, false = error</returns>
        public bool AuthLogout(string sessionKey)
        {
            Dictionary<string, string> args = new Dictionary<string, string>();
            args.Add("method", "auth.logout");
            args.Add("session_key", sessionKey); // Received from auth.login

            string response = GetResponse(SENDSPACE_API_URL, args);

            return !ParseResponse(response).Error;
        }

        #endregion

        #region Upload

        /// <summary>
        /// Obtains the information needed to perform an upload.
        /// http://www.sendspace.com/dev_method.html?method=upload.getInfo
        /// </summary>
        /// <param name="sessionKey">Received from auth.login</param>
        /// <param name="speedLimit">Upload speed limit in kilobytes, 0 for unlimited</param>
        /// <returns>URL to upload the file to, progress_url for real-time progress information, max_file_size for max size current user can upload, upload_identifier & extra_info to be passed with the upload form</returns>
        public string UploadGetInfo(string sessionKey, string speedLimit)
        {
            Dictionary<string, string> args = new Dictionary<string, string>();
            args.Add("method", "upload.getInfo");
            args.Add("session_key", sessionKey); // Received from auth.login
            args.Add("speed_limit", speedLimit); // Upload speed limit in kilobytes, 0 for unlimited

            string response = GetResponse(SENDSPACE_API_URL, args);

            return "";
        }

        /// <summary>
        /// http://www.sendspace.com/dev_method.html?method=upload.getInfo
        /// </summary>
        /// <param name="max_file_size">max_file_size value received in UploadGetInfo response</param>
        /// <param name="upload_identifier">upload_identifier value received in UploadGetInfo response</param>
        /// <param name="extra_info">extra_info value received in UploadGetInfo response</param>
        /// <param name="description"></param>
        /// <param name="password"></param>
        /// <param name="folder_id"></param>
        /// <param name="recipient_email">an email (or emails separated with ,) of recipient/s to receive information about the upload</param>
        /// <param name="notify_uploader">0/1 - should the uploader be notified?</param>
        /// <param name="redirect_url">page to redirect after upload will be attached upload_status=ok/fail&file_id=XXXX</param>
        /// <returns></returns>
        public Dictionary<string, string> PrepareArguments(string max_file_size, string upload_identifier, string extra_info,
            string description, string password, string folder_id, string recipient_email, string notify_uploader, string redirect_url)
        {
            Dictionary<string, string> args = new Dictionary<string, string>();
            args.Add("MAX_FILE_SIZE", max_file_size);
            args.Add("UPLOAD_IDENTIFIER", upload_identifier);
            args.Add("extra_info", extra_info);

            // Optional fields

            if (!string.IsNullOrEmpty(description)) args.Add("description", description);
            if (!string.IsNullOrEmpty(password)) args.Add("password", password);
            if (!string.IsNullOrEmpty(folder_id)) args.Add("folder_id", folder_id);
            if (!string.IsNullOrEmpty(recipient_email)) args.Add("recipient_email", recipient_email);
            if (!string.IsNullOrEmpty(notify_uploader)) args.Add("notify_uploader", notify_uploader);
            if (!string.IsNullOrEmpty(redirect_url)) args.Add("redirect_url", redirect_url);

            return args;
        }

        public Dictionary<string, string> PrepareArguments(string max_file_size, string upload_identifier, string extra_info)
        {
            return PrepareArguments(max_file_size, upload_identifier, extra_info, null, null, null, null, null, null);
        }

        public override string Upload(byte[] data, string fileName)
        {
            return "";
        }

        #endregion
    }
}