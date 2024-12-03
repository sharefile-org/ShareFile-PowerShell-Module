using System.IO;

namespace ShareFile.Api.Powershell.Parallel
{
    /// <summary>
    /// DownloadAction class to download files from ShareFile server
    /// </summary>
    class DownloadAction : IAction
    {
        private Client.Models.File child;
        private Client.ShareFileClient client;
        private int downloadId;
        private FileSystemInfo target;
        private ActionType actionType;
        private FileSupport fileSupportDelegate;
        private string fileName;
        public string FileName
        {
            get
            {
                return fileName;
            }
        }
        public ActionType OpActionType
        {
            get
            {
                return actionType;
            }
            set
            {
                actionType = value;
            }
        }

        public DownloadAction(FileSupport fileSupport, Client.ShareFileClient client, int downloadId, Client.Models.File child, FileSystemInfo target, ActionType type)
        {
            this.child = child;
            this.client = client;
            this.downloadId = downloadId;
            this.target = target;
            this.actionType = type;
            this.fileSupportDelegate = fileSupport;
        }

        void IAction.CopyFileItem(ProgressInfo progressInfo)
        {
            fileName = System.IO.Path.Combine(target.FullName, child.FileName);
            bool duplicateFile = File.Exists(fileName);
            bool hashcodeMatches = duplicateFile ? Utility.GetMD5HashFromFile(fileName).Equals(child.Hash) : false;

            if (duplicateFile && actionType == ActionType.None)
            {
                throw new IOException("File already exist");
            }
            else if (!duplicateFile || actionType == ActionType.Force || (actionType == ActionType.Sync && !hashcodeMatches))
            {
                using (var fileStream = new FileStream(fileName, actionType == ActionType.Force || actionType == ActionType.Sync ? FileMode.Create : FileMode.CreateNew))
                {
                    var downloader = client.GetAsyncFileDownloader(child);

                    progressInfo.ProgressTotal(progressInfo.FileIndex, child.FileSizeBytes.GetValueOrDefault());

                    downloader.OnTransferProgress +=
                        (sender, args) =>
                        {
                            if (args.Progress.TotalBytes > 0)
                            {
                                progressInfo.ProgressTransferred(progressInfo.FileIndex, args.Progress.BytesTransferred);
                            }
                        };

                    downloader.DownloadToAsync(fileStream).Wait();
                    
                    fileStream.Close();
                    fileSupportDelegate(fileName);
                }
            }
        }
    }
}
