using ShareFile.Api.Client;
using ShareFile.Api.Client.Models;
using ShareFile.Api.Powershell.Log;
using ShareFile.Api.Powershell.Parallel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;

namespace ShareFile.Api.Powershell
{
    delegate void FileSupport(String Name);

    [Cmdlet(VerbsCommon.Copy, Noun, DefaultParameterSetName = ParmamSetPath, SupportsShouldProcess = true)]
    public class CopySfItem : BaseCmdlet
    {
        private const string Noun = "SfItem";
        private const string ParamSetLiteral = "Literal";
        private const string ParmamSetPath = "Path";
        private string[] _paths;
        private bool _shouldExpandWildcards;
        private Resume.ResumeSupport ResumeSupport { get; set; }
        private FileSupport FileSupport;

        [Parameter(
            Position = 0,
            Mandatory = true,
            ValueFromPipeline = false,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = ParamSetLiteral)]
        [Alias("PSPath")]
        [ValidateNotNullOrEmpty]
        public string[] LiteralPath
        {
            get { return _paths; }
            set { _paths = value; }
        }

        [Parameter(
            Position = 0,
            Mandatory = true,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = ParmamSetPath)
        ]
        [ValidateNotNullOrEmpty]
        public string[] Path
        {
            get { return _paths; }
            set
            {
                _shouldExpandWildcards = true;
                _paths = value;
            }
        }

        [Parameter(Position = 1, Mandatory = false)]
        public string Destination { get; set; }

        [Parameter(Position = 2, Mandatory = false)]
        public string Details { get; set; }

        [Parameter(Mandatory = false)]
        public bool Force { get; set; }

        /// <summary>
        /// Resume flag, if 'true' then it will keep track of command status
        /// if command is unsuccessful due to disconnection then it will prompt user next time to complete it first
        /// </summary>
        [Parameter()]
        [Alias("ResumeFlag")]
        public SwitchParameter Resume { get; set; }

        protected override void ProcessRecord()
        {
            ResumeSupport = new Resume.ResumeSupport();
            FileSupport = new FileSupport(MarkFileStatus);

            if (Resume)
            {
                if (ResumeSupport.IsPending)
                {
                    Logger.Instance.Info("Last command wasn't successful due to discconnection.");

                    WriteObject("Last command wasn't successful due to discconnection, enter 'y' to complete or any other key to ignore: ");
                    Collection<PSObject> result = InvokeCommand.InvokeScript("Read-Host");
                    string userOption = result != null && result.Count > 0 ? result[0].ToString() : string.Empty;

                    if (userOption.ToLower().Equals("y"))
                    {
                        Logger.Instance.Info("Copying those files which were missed due to discconnection with following parameters. Source Path:{0} Destination:{1} Forced:{2} Details:{3}",
                            String.Join(",", ResumeSupport.GetPath), ResumeSupport.GetDestination, ResumeSupport.GetForce, ResumeSupport.GetDetails);

                        StartCopying(ResumeSupport.GetPath, ResumeSupport.GetDestination, ResumeSupport.GetForce, ResumeSupport.GetDetails);

                        Logger.Instance.Info("Copying operation completed for missing files");
                        ResumeSupport.End();
                        WriteObject("Last command completed, starting current operation");
                    }
                }
            }
            else
            {
                ResumeSupport.UnmarkResumeFlag();
            }

            Logger.Instance.Info(String.Format("Starting Copy Operation with following parameters. Source Path:{0} Destination:{1} Forced:{2} Details:{3}",
                String.Join(",", Path), Destination, Force, Details));

            if (Resume) ResumeSupport.Start(Path, Destination, Force, Details);
            
            StartCopying(Path, Destination.Trim(), Force, Details);
            Thread.Sleep(100);

            if (Resume) ResumeSupport.End();

            WriteObject("Files copied successfully");
        }

        private void StartCopying(String[] paramPath, String paramDestination, bool paramForce, String paramDetails)
        {
            foreach (string path in paramPath)
            {
                // Handle the source Paths. They may be provided as a wildcard, or literal paths
                ProviderInfo sourceProvider;
                PSDriveInfo sourceDrive;
                List<string> filePaths = new List<string>();

                if (_shouldExpandWildcards)
                {
                    // Turn *.txt into foo.txt,foo2.txt etc.
                    // if path is just "foo.txt," it will return unchanged.
                    var expandedPaths = this.GetResolvedProviderPathFromPSPath(path.Trim(), out sourceProvider);
                    // Not sure how to get the sourceDrive from this expansion
                    // I will get from the first element on this collection
                    // Is it possible to enumerate from multiple drives?? Is that why the method won't return the drive info?
                    if (expandedPaths.Count == 0) continue;
                    var firstPath = expandedPaths.FirstOrDefault();
                    this.SessionState.Path.GetUnresolvedProviderPathFromPSPath(path.Trim(), out sourceProvider, out sourceDrive);
                    filePaths.AddRange(expandedPaths);
                }
                else
                {
                    // no wildcards, so don't try to expand any * or ? symbols.                    
                    filePaths.Add(this.SessionState.Path.GetUnresolvedProviderPathFromPSPath(path.Trim(), out sourceProvider, out sourceDrive));
                }
                bool isSourceLocal = sourceProvider.ImplementingType == typeof(Microsoft.PowerShell.Commands.FileSystemProvider);
                bool isSourceSF = sourceProvider.ImplementingType == typeof(ShareFileProvider);

                // Handle the target Path.
                if (paramDestination == null) paramDestination = this.SessionState.Path.CurrentFileSystemLocation.ProviderPath;
                ProviderInfo targetProvider = null;
                PSDriveInfo targetDrive = null;
                var targetProviderPath = this.SessionState.Path.GetUnresolvedProviderPathFromPSPath(paramDestination, out targetProvider, out targetDrive);
                bool isTargetLocal = targetProvider.ImplementingType == typeof(Microsoft.PowerShell.Commands.FileSystemProvider);
                bool isTargetSF = targetProvider.ImplementingType == typeof(ShareFileProvider);
                Item targetItem = null;
                if (isTargetSF)
                {
                    targetItem = ShareFileProvider.GetShareFileItem((ShareFileDriveInfo)targetDrive, targetProviderPath);
                    //targetItem = Utility.ResolveShareFilePath(sourceDrive, targetProviderPath);
                }

                // Process each input path
                foreach (string filePath in filePaths)
                {
                    if (isSourceSF && isTargetLocal)
                    {
                        // ShareFile to local: perform download
                        var client = ((ShareFileDriveInfo)sourceDrive).Client;
                        var item = Utility.ResolveShareFilePath(sourceDrive, filePath); //ShareFileProvider.GetShareFileItem((ShareFileDriveInfo)sourceDrive, filePath, null, null);
                        if (item == null)
                        {
                            throw new Exception(string.Format("Source path '{0}' not found on ShareFile server.", filePath));
                        }
                        var target = new DirectoryInfo(paramDestination);
                        if (!target.Exists)
                        {
                            throw new Exception(string.Format("Destination path '{0}' not found on local drive.", paramDestination));
                        }
                        RecursiveDownload(client, new Random((int)DateTime.Now.Ticks).Next(), item, target);
                    }
                    else if (isSourceSF && isTargetSF)
                    {
                        // ShareFile to ShareFile: perform API copy
                        var sourceClient = ((ShareFileDriveInfo)sourceDrive).Client;
                        var targetClient = ((ShareFileDriveInfo)targetDrive).Client;
                        // TODO: verify that source and target drives are on the same account
                        var sourceItem = ShareFileProvider.GetShareFileItem((ShareFileDriveInfo)sourceDrive, filePath);
                        try
                        {
                            sourceClient.Items.Copy(sourceItem.url, targetItem.Id, paramForce).Execute();
                        }
                        catch(TimeoutException e)
                        {
                            if (e.InnerException is TaskCanceledException)
                            {
                                var copiedItem = ShareFileProvider.GetShareFileItem((ShareFileDriveInfo)targetDrive, targetProviderPath + "/" + sourceItem.Name);
                                int count = 0;
                                while (copiedItem.FileSizeBytes < sourceItem.FileSizeBytes && count < 250)
                                {
                                    Thread.Sleep(2000);
                                    copiedItem = ShareFileProvider.GetShareFileItem((ShareFileDriveInfo)targetDrive, targetProviderPath + "/" + sourceItem.Name);
                                    count++;
                                }
                                if (count == 250)
                                {
                                    throw;
                                }
                            }
                            else
                            {
                                throw;
                            }                                                              
                        }
                        catch(Exception e)
                        {
                            string t = e.GetType().ToString();
                            throw;
                        }
                    }
                    else if (isSourceLocal && isTargetSF)
                    {
                        // Local to Sharefile: perform upload
                        var client = ((ShareFileDriveInfo)targetDrive).Client;
                        FileSystemInfo source = null;
                        FileAttributes attr = System.IO.File.GetAttributes(filePath);
                        if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                        {
                            source = new DirectoryInfo(filePath);
                        }
                        else
                        {
                            source = new FileInfo(filePath);
                        }

                        if (targetItem == null)
                        {
                            throw new Exception(string.Format("Destination path '{0}' not found on ShareFile server.", targetProviderPath));
                        }
                        if (!source.Exists)
                        {
                            throw new Exception(string.Format("Local path '{0}' not found on local drive.", filePath));
                        }

                        RecursiveUpload(client, new Random((int)DateTime.Now.Ticks).Next(), source, targetItem);
                    }
                    else if (isSourceLocal && isTargetLocal)
                    {
                        // Local copy...
                        FileSystemInfo source = null;
                        FileAttributes attr = System.IO.File.GetAttributes(filePath);
                        if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                        {
                            source = new DirectoryInfo(filePath);
                        }
                        else
                        {
                            source = new FileInfo(filePath);
                        }
                        RecursiveCopy(source, new DirectoryInfo(paramDestination));
                    }
                }
            }
        }

        private void RecursiveUpload(ShareFileClient client, int uploadId, FileSystemInfo source, Item target)
        {
            if (source is DirectoryInfo)
            {
                var newFolder = new Folder() { Name = source.Name };
                newFolder = client.Items.CreateFolder(target.url, newFolder, Force || ResumeSupport.IsPending, false).Execute();

                ActionManager actionManager = new ActionManager(this, source.Name);
                ActionType actionType = Force ? ActionType.Force : ActionType.None;

                foreach (var fsInfo in ((DirectoryInfo)source).EnumerateFileSystemInfos())
                {
                    if (fsInfo is DirectoryInfo)
                    {
                        RecursiveUpload(client, uploadId, fsInfo, newFolder);
                    }
                    else if (fsInfo is FileInfo)
                    {
                        if (!ResumeSupport.IsPending || !ResumeSupport.CheckFileStatus(fsInfo.Name))
                        {
                            IAction uploadAction = new UploadAction(FileSupport, client, fsInfo, newFolder, Details, actionType);
                            actionManager.AddAction(uploadAction);
                        }
                    }
                }

                actionManager.Execute();
            }
            else if (source is FileInfo)
            {
                ActionManager actionManager = new ActionManager(this, source.Name);
                if (!ResumeSupport.IsPending || !ResumeSupport.CheckFileStatus(source.Name))
                {
                    ActionType actionType = Force || ResumeSupport.IsPending ? ActionType.Force : ActionType.None;
                    IAction uploadAction = new UploadAction(FileSupport, client, source, target, Details, actionType);
                    actionManager.AddAction(uploadAction);
                }
                actionManager.Execute();
            }
        }

        private void RecursiveDownload(ShareFileClient client, int downloadId, Item source, DirectoryInfo target)
        {
            if (source is Folder)
            {
                var children = client.Items.GetChildren(source.url).Execute();
                var subdirCheck = new DirectoryInfo(System.IO.Path.Combine(target.FullName, source.FileName));
                if (subdirCheck.Exists && !Force && !ResumeSupport.IsPending) throw new IOException("Path " + subdirCheck.FullName + " already exists. Use -Force to ignore");
                var subdir = target.CreateSubdirectory(source.FileName);
                if (children != null)
                {
                    ActionManager actionManager = new ActionManager(this, source.FileName);

                    foreach (var child in children.Feed)
                    {

                        if (child is Folder)
                        {
                            RecursiveDownload(client, downloadId, child, subdir);
                        }
                        else if (child is Client.Models.File)
                        {
                            if (!ResumeSupport.IsPending || !ResumeSupport.CheckFileStatus(child.FileName))
                            {
                                ActionType actionType = Force || ResumeSupport.IsPending ? ActionType.Force : ActionType.None;
                                DownloadAction downloadAction = new DownloadAction(FileSupport, client, downloadId, (Client.Models.File)child, subdir, actionType);
                                actionManager.AddAction(downloadAction);
                            }
                        }
                    }

                    actionManager.Execute();
                }
            }
            else if (source is Client.Models.File)
            {
                ActionManager actionManager = new ActionManager(this, source.FileName);
                if (!ResumeSupport.IsPending || !ResumeSupport.CheckFileStatus(source.FileName))
                {
                    ActionType actionType = Force || ResumeSupport.IsPending ? ActionType.Force : ActionType.None;
                    DownloadAction downloadAction = new DownloadAction(FileSupport, client, downloadId, (Client.Models.File)source, target, actionType);
                    actionManager.AddAction(downloadAction);
                }
                actionManager.Execute();
            }
        }

        private void MarkFileStatus(String fileName)
        {
            if (Resume)
            {
                ResumeSupport.MarkFileStatus(fileName);
            }
        }

        #region Previous Implementations (commented) > Upload/Download

        //protected void RecursiveUpload(ShareFileClient client, int uploadId, FileSystemInfo source, Models.Item target)
        //{
        //    if (source is DirectoryInfo)
        //    {
        //        var newFolder = new Models.Folder() { Name = source.Name };
        //        newFolder = client.Items.CreateFolder(target.url, newFolder, Force, false).Execute();

        //        foreach (var fsInfo in ((DirectoryInfo)source).EnumerateFileSystemInfos())
        //        {
        //            RecursiveUpload(client, uploadId, fsInfo, newFolder);
        //        }
        //    }
        //    else if (source is FileInfo)
        //    {
        //        var fileInfo = (FileInfo)source;
        //        try
        //        {
        //            var uploadSpec = new UploadSpecificationRequest
        //            {
        //                CanResume = false,
        //                Details = Details,
        //                FileName = fileInfo.Name,
        //                FileSize = fileInfo.Length,
        //                Method = UploadMethod.Threaded,
        //                Parent = target.url,
        //                ThreadCount = 4,
        //                Raw = true
        //            };
        //            var uploader = client.GetAsyncFileUploader(uploadSpec, new PlatformFileInfo(fileInfo));

        //            var progressBar = new ProgressBar(uploadId, "Uploading...", this);
        //            progressBar.SetProgress(fileInfo.Name, 0);
        //            uploader.OnTransferProgress =
        //                (sender, args) =>
        //                {
        //                    int pct = 100;
        //                    if (args.Progress.TotalBytes > 0)
        //                        pct = (int)(((float)args.Progress.BytesTransferred / args.Progress.TotalBytes) * 100);
        //                    progressBar.SetProgress(fileInfo.Name, pct);
        //                };
        //            Task.Run(() => uploader.UploadAsync()).ContinueWith(t => progressBar.Finish());
        //            progressBar.UpdateLoop();
        //        }
        //        catch (Exception e)
        //        {
        //            WriteError(new ErrorRecord(e, "ShareFile", ErrorCategory.NotSpecified, source));
        //        }
        //    }
        //}

        //protected void RecursiveDownload(ShareFileClient client, int downloadId, Models.Item source, DirectoryInfo target)
        //{
        //    if (source is Models.Folder)
        //    {
        //        var children = client.Items.GetChildren(source.url).Execute();
        //        var subdirCheck = new DirectoryInfo(System.IO.Path.Combine(target.FullName, source.FileName));
        //        if (subdirCheck.Exists && !Force) throw new IOException("Path " + subdirCheck.FullName + " already exists. Use -Force to ignore");
        //        var subdir = target.CreateSubdirectory(source.FileName);
        //        if (children != null)
        //        {
        //            foreach (var child in children.Feed)
        //            {
        //                RecursiveDownload(client, downloadId, child, subdir);
        //            }
        //        }
        //    }
        //    else if (source is Models.File)
        //    {
        //        using (var fileStream = new FileStream(System.IO.Path.Combine(target.FullName, source.FileName), Force ? FileMode.Create : FileMode.CreateNew))
        //        {
        //            var progressBar = new ProgressBar(downloadId, "Downloading...", this);
        //            progressBar.SetProgress(source.FileName, 0);
        //            var downloader = client.GetAsyncFileDownloader(source);
        //            downloader.OnTransferProgress =
        //                (sender, args) =>
        //                {
        //                    if (args.Progress.TotalBytes > 0)
        //                    {
        //                        var pct = (int)(((double)args.Progress.BytesTransferred / (double)args.Progress.TotalBytes) * 100);
        //                        progressBar.SetProgress(source.FileName, pct);
        //                    }
        //                };
        //            Task.Run(() => downloader.DownloadToAsync(fileStream)).ContinueWith(t => progressBar.Finish());
        //            progressBar.UpdateLoop();

        //            fileStream.Close();
        //        }
        //    }
        //}

        #endregion

        protected void RecursiveCopy(FileSystemInfo source, DirectoryInfo target)
        {
            if (source is DirectoryInfo)
            {
                var subdirCheck = new DirectoryInfo(System.IO.Path.Combine(target.FullName, source.Name));
                if (subdirCheck.Exists && !Force) throw new IOException("Path " + subdirCheck.FullName + " already exists. Use -Force to ignore");
                var subdir = target.CreateSubdirectory(source.Name);
                foreach (var fsInfo in ((DirectoryInfo)source).EnumerateFileSystemInfos())
                {
                    RecursiveCopy(fsInfo, subdir);
                }
            }
            else if (source is FileInfo)
            {
                var fileInfo = (FileInfo)source;
                fileInfo.CopyTo(System.IO.Path.Combine(target.FullName, source.Name), Force);
            }
        }
    }

    #region ProgressBar Class (commented)
    //class ProgressBar
    //{
    //    private int Id { get; set; }

    //    private string CurrentFile { get; set; }

    //    private int CurrentPct { get; set; }

    //    private Cmdlet Cmdlet { get; set; }

    //    private bool Finished { get; set; }

    //    private string Title { get; set; }

    //    private AutoResetEvent WaitHandle = new AutoResetEvent(true); 

    //    public ProgressBar(int id, string title, Cmdlet cmdlet)
    //    {
    //        Id = id;
    //        Cmdlet = cmdlet;
    //        Finished = false;
    //        CurrentFile = null;
    //        CurrentPct = 0;
    //        Title = title;
    //    }

    //    public void SetProgress(string filename, int percentage)
    //    {
    //        CurrentFile = filename;
    //        CurrentPct = percentage;
    //        WaitHandle.Set();
    //    }

    //    public void WriteProgress()
    //    {
    //        if (CurrentFile != null)
    //        {
    //            var pr = new ProgressRecord(Id, Title, string.Format("{0} - {1}%", CurrentFile, CurrentPct));
    //            Cmdlet.WriteProgress(pr);
    //        }
    //    }

    //    public void Finish()
    //    {
    //        CurrentPct = 100;
    //        Finished = true;
    //    }

    //    public void UpdateLoop()
    //    {
    //        while (!Finished)
    //        {
    //            WriteProgress();
    //            WaitHandle.Reset();
    //            WaitHandle.WaitOne(5000);
    //        }
    //    }
    //}
    #endregion
}
