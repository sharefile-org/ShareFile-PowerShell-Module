using ShareFile.Api.Client.Models;
using System;
using System.IO;
using System.Management.Automation;
using System.Security.Cryptography;
using System.Text;

namespace ShareFile.Api.Powershell.Parallel
{
    class Utility
    {
        #region Local Variables
        public const string DefaultSharefileFolder = "My Files & Folders";
        #endregion

        public static String GetMD5HashFromFile(Stream file)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] retVal = md5.ComputeHash(file);
            file.Close();
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < retVal.Length; i++)
            {
                sb.Append(retVal[i].ToString("x2"));
            }
            return sb.ToString();
        }

        public static String GetMD5HashFromFile(String fileName)
        {
            string fileHashCode = string.Empty;
            using (FileStream file = new FileStream(fileName, FileMode.Open))
            {

                fileHashCode = GetMD5HashFromFile(file);
                file.Close();
            }

            return fileHashCode;
        }

        /// <summary>
        /// Resolve ShareFile item path if ShareFile drive letter or root folder is not provided
        /// </summary>
        public static Item ResolveShareFilePath(PSDriveInfo driveInfo, string path)
        {
            var item = ShareFileProvider.GetShareFileItem((ShareFileDriveInfo)driveInfo, path, null, null);

            // if user didn't specify the Sharefile HomeFolder in path then append in path
            // e.g. if user tries sf:/Folder1 as sharefile source then resolve this path to sf:/My Files & Folders/Folder1
            if (item == null && !path.StartsWith(String.Format(@"\{0}\", DefaultSharefileFolder)))
            {
                string updatedPath = String.Format(@"\{0}\{1}", DefaultSharefileFolder, path);
                item = ShareFileProvider.GetShareFileItem((ShareFileDriveInfo)driveInfo, updatedPath, null, null);
            }

            return item;
        }
    }
}
