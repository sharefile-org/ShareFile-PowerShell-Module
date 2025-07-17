using System;
using System.IO;
using System.Management.Automation.Runspaces;
using System.Management.Automation;

namespace Test_ShareFileSnapIn
{
    public class Utils
    {

        private static string _localFile = "ToUpload.txt";
        private static string _localFolder = "Folder1";
        private static string _sfFile = "DeepText.txt";
        private static string _sfFolder = "Folder1Q";

        /// <summary>
        /// %LOCALAPPDATA%\ShareFile\sflogin.sfps
        /// </summary>
        public static string LoginFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ShareFile", "sflogin.sfps");

        /// <summary>
        /// sf
        /// </summary>
        public static string ShareFileDriveLetter = "sf";
        /// <summary>
        /// sf:
        /// </summary>
        public static string ShareFileDrivePath = string.Format("{0}{1}",ShareFileDriveLetter, ":");
        /// <summary>
        /// sf:/Personal Folders
        /// </summary>
        public static string ShareFileHomeFolder = string.Format("{0}{1}", ShareFileDrivePath, "/Personal Folders");

        /// <summary>
        /// sf:/Folder1
        /// </summary>
        public static string ShareFileFolder = string.Format("{0}{1}{2}", ShareFileDrivePath, "/", _localFolder);
        /// <summary>
        /// sf:/Personal Folders/Folder1Q
        /// </summary>
        public static string ShareFileFolderUploaded = string.Format("{0}{1}{2}", ShareFileHomeFolder, "/", _sfFolder);
        /// <summary>
        /// sf:/Personal Folders/Folder1
        /// </summary>
        public static string ShareFileFolderFullPath = string.Format("{0}{1}{2}", ShareFileHomeFolder, "/", _localFolder);
        /// <summary>
        /// sf:/DeepText.txt
        /// </summary>
        public static string ShareFileFile = string.Format("{0}{1}{2}", ShareFileDrivePath, "/", _sfFile);
        /// <summary>
        /// sf:/ToUpload.txt
        /// </summary>
        public static string ShareFileFileUploaded = string.Format("{0}{1}{2}", ShareFileHomeFolder, "/", _localFile);
        /// <summary>
        /// sf:/Personal Folders/DeepText.txt
        /// </summary>
        public static string ShareFileFileFullPath = string.Format("{0}{1}{2}", ShareFileHomeFolder, "/", _sfFile);

        /// <summary>
        /// %LOCALAPPDATA%\ShareFile\SFTemp
        /// </summary>
        public static string LocalBaseFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ShareFile", "SFTemp");
        /// <summary>
        /// %LOCALAPPDATA%\ShareFile\SFTemp\Folder1Q
        /// </summary>
        public static string LocalFolder = string.Format("{0}{1}{2}", LocalBaseFolder, @"\", _sfFolder);
        /// <summary>
        /// %LOCALAPPDATA%\ShareFile\SFTemp\Folder1
        /// </summary>
        public static string LocalFolderDownloaded = string.Format("{0}{1}{2}", LocalBaseFolder, @"\", _localFolder);
        /// <summary>
        /// %LOCALAPPDATA%\ShareFile\SFTemp\ToUpload.txt
        /// </summary>
        public static string LocalFile = string.Format("{0}{1}{2}", LocalBaseFolder, @"\", _localFile);
        /// <summary>
        /// %LOCALAPPDATA%\ShareFile\SFTemp\DeepText.txt
        /// </summary>
        public static string LocalFileDownloaded = string.Format("{0}{1}{2}", LocalBaseFolder, @"\", _sfFile);
        
        public static void DeleteProgressFile()
        {
            DeleteLocalFile(".progressfile");
        }

        public static bool IsPathExist(string path)
        {
            return File.Exists(path) || Directory.Exists(path);
        }

        public static bool IsContainsSubDirectory(string path)
        {
            DirectoryInfo directory = new DirectoryInfo(path);
            foreach (var child in directory.EnumerateFileSystemInfos())
            {
                if (child is DirectoryInfo)
                {
                    return true;
                }
            }
            return false;
        }

        public static void DeleteLocalFile(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public static void DeleteLocalFolder(string path)
        {
            if (Directory.Exists(path))
            {
                DirectoryInfo directory = new DirectoryInfo(path);
                DeleteLocalItemRecursive(directory);
            }
        }

        public static Runspace OpenRunspace()
        {
            const string moduleName = "ShareFile";
            InitialSessionState initial = InitialSessionState.CreateDefault();
            initial.ImportPSModule(new string[] { moduleName });
            var runspace = RunspaceFactory.CreateRunspace(initial);
            runspace.Open();
            return runspace;
        }

        private static void DeleteLocalItemRecursive(FileSystemInfo source)
        {
            if (source is DirectoryInfo)
            {
                foreach (var child in (source as DirectoryInfo).EnumerateFileSystemInfos())
                {
                    DeleteLocalItemRecursive(child);
                }
            }
           
            source.Delete();
        }
    }
}
