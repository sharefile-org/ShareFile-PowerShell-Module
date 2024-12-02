using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShareFile.Api.Powershell;

namespace Test_ShareFileSnapIn
{
    [TestClass]
    public class SyncSFItemsTests
    {
        private PSObject sfLogin = null;
        Runspace runspace = null;

        [TestInitialize]
        public void InitializeTests()
        {
            RunspaceConfiguration config = RunspaceConfiguration.Create();

            PSSnapInException warning;

            config.AddPSSnapIn("ShareFile", out warning);

            runspace = RunspaceFactory.CreateRunspace(config);
            runspace.Open();

            // do login first to start tests
            using (Pipeline pipeline = runspace.CreatePipeline())
            {

                Command command = new Command("Get-SfClient");
                command.Parameters.Add(new CommandParameter("Name", Utils.LoginFilePath));

                pipeline.Commands.Add(command);

                Collection<PSObject> psObjects = pipeline.Invoke();
                Assert.AreEqual<int>(1, psObjects.Count);
                sfLogin = psObjects[0];
            }

            using (Pipeline pipeline = runspace.CreatePipeline())
            {
                Command command = new Command("New-PSDrive");
                command.Parameters.Add("Name", Utils.ShareFileDriveLetter);
                command.Parameters.Add("PSProvider", "ShareFile");
                command.Parameters.Add("Root", "/");
                command.Parameters.Add("Client", sfLogin);

                pipeline.Commands.Add(command);

                Collection<PSObject> psObjects = pipeline.Invoke();

                // Drive is successfully mapped to root folder
                Assert.AreEqual<int>(1, psObjects.Count);
            }
        }


        [TestMethod]
        public void TM1_SyncDownloadFileTest()
        {
            using (Pipeline pipeline = runspace.CreatePipeline())
            {
                Utils.DeleteLocalFile(Utils.LocalFileDownloaded);

                Command command = new Command("Sync-SfItem");
                command.Parameters.Add("ShareFilePath", Utils.ShareFileFile);
                command.Parameters.Add("LocalPath", Utils.LocalBaseFolder);
                command.Parameters.Add("Download", true);

                pipeline.Commands.Add(command);

                Collection<PSObject> psObjects = pipeline.Invoke();
                Assert.AreEqual<int>(1, psObjects.Count);

                Assert.IsTrue(Utils.IsPathExist(Utils.LocalFileDownloaded));
            }
        }

        [TestMethod]
        public void TM2_SyncDownloadFolderTest()
        {
            using (Pipeline pipeline = runspace.CreatePipeline())
            {
                Utils.DeleteLocalFolder(Utils.LocalFolderDownloaded);

                Command command = new Command("Sync-SfItem");
                command.Parameters.Add("ShareFilePath", Utils.ShareFileFolder);
                command.Parameters.Add("LocalPath", Utils.LocalBaseFolder);
                command.Parameters.Add("Download", true);

                pipeline.Commands.Add(command);

                Collection<PSObject> psObjects = pipeline.Invoke();
                Assert.AreEqual<int>(1, psObjects.Count);

                Assert.IsTrue(Utils.IsPathExist(Utils.LocalFolderDownloaded));
            }
        }

        [TestMethod]
        public void TM3_SyncDownloadRecursiveTest()
        {
            using (Pipeline pipeline = runspace.CreatePipeline())
            {
                Utils.DeleteLocalFolder(Utils.LocalFolderDownloaded);

                Command command = new Command("Sync-SfItem");
                command.Parameters.Add("ShareFilePath", Utils.ShareFileFolder);
                command.Parameters.Add("LocalPath", Utils.LocalBaseFolder);
                command.Parameters.Add("Download", true);
                command.Parameters.Add("Recursive", true);

                pipeline.Commands.Add(command);

                Collection<PSObject> psObjects = pipeline.Invoke();
                Assert.AreEqual<int>(1, psObjects.Count);

                Assert.IsTrue(Utils.IsPathExist(Utils.LocalFolderDownloaded));
                Assert.IsTrue(Utils.IsContainsSubDirectory(Utils.LocalFolderDownloaded));
            }
        }

        [TestMethod]
        public void TM4_SyncDownloadOverwriteTest()
        {
            using (Pipeline pipeline = runspace.CreatePipeline())
            {
                Command command = new Command("Sync-SfItem");
                command.Parameters.Add("ShareFilePath", Utils.ShareFileFolder);
                command.Parameters.Add("LocalPath", Utils.LocalBaseFolder);
                command.Parameters.Add("Download", true);
                command.Parameters.Add("OverWrite", true);

                pipeline.Commands.Add(command);

                Collection<PSObject> psObjects = pipeline.Invoke();
                
                Assert.AreEqual<int>(1, psObjects.Count);
                Assert.IsTrue(Utils.IsPathExist(Utils.LocalFolderDownloaded));
                Assert.IsTrue(Utils.IsContainsSubDirectory(Utils.LocalFolderDownloaded));
            }
        }

        [TestMethod]
        public void TM5_SyncDownloadMoveTest()
        {
            using (Pipeline pipeline = runspace.CreatePipeline())
            {
                Utils.DeleteLocalFolder(Utils.LocalFolderDownloaded);

                Command command = new Command("Sync-SfItem");
                command.Parameters.Add("ShareFilePath", Utils.ShareFileFolder);
                command.Parameters.Add("LocalPath", Utils.LocalBaseFolder);
                command.Parameters.Add("Download", true);
                command.Parameters.Add("Move", true);

                pipeline.Commands.Add(command);

                Collection<PSObject> psObjects = pipeline.Invoke();
                Assert.IsTrue(Utils.IsPathExist(Utils.LocalFolderDownloaded));
                Assert.IsTrue(Utils.IsContainsSubDirectory(Utils.LocalFolderDownloaded));
            }
        }
                
        [TestMethod]
        public void TM11_SyncUploadFileTest()
        {
            using (Pipeline pipeline = runspace.CreatePipeline())
            {
                Command command = new Command("Sync-SfItem");
                command.Parameters.Add("ShareFilePath", Utils.ShareFileDrivePath);
                command.Parameters.Add("LocalPath", Utils.LocalFile);
                command.Parameters.Add("Upload", true);

                // remove line after adding cleanup script to sharefile server
                command.Parameters.Add("Overwrite", true);

                pipeline.Commands.Add(command);

                Collection<PSObject> psObjects = pipeline.Invoke();
                Assert.AreEqual<int>(1, psObjects.Count);

            }

            using (Pipeline pipeline = runspace.CreatePipeline())
            {
                Command command = new Command("Get-ChildItem");

                pipeline.Commands.Add(command);

                pipeline.Input.Write(Utils.ShareFileFileUploaded);
                Collection<PSObject> psObjects = pipeline.Invoke();
                
                Assert.AreEqual<int>(1, psObjects.Count);
                Assert.AreEqual("ShareFile.Api.Models.File", psObjects[0].BaseObject.ToString());
            }
        }

        [TestMethod]
        public void TM12_SyncUploadFolderTest()
        {
            using (Pipeline pipeline = runspace.CreatePipeline())
            {
                Command command = new Command("Sync-SfItem");
                command.Parameters.Add("ShareFilePath", Utils.ShareFileDrivePath);
                command.Parameters.Add("LocalPath", Utils.LocalFolder);
                command.Parameters.Add("Upload", true);

                // remove line after adding cleanup script to sharefile server
                command.Parameters.Add("Overwrite", true);

                pipeline.Commands.Add(command);

                Collection<PSObject> psObjects = pipeline.Invoke();
                Assert.AreEqual<int>(1, psObjects.Count);
            }

            using (Pipeline pipeline = runspace.CreatePipeline())
            {
                Command command = new Command("Get-ChildItem");

                pipeline.Commands.Add(command);

                pipeline.Input.Write(Utils.ShareFileFolderUploaded);
                Collection<PSObject> psObjects = pipeline.Invoke();

                Assert.AreNotEqual<int>(0, psObjects.Count);
            }
        }

        [TestMethod]
        public void TM13_SyncUploadFolderRecursiveTest()
        {
            using (Pipeline pipeline = runspace.CreatePipeline())
            {
                Command command = new Command("Sync-SfItem");
                command.Parameters.Add("ShareFilePath", Utils.ShareFileDrivePath);
                command.Parameters.Add("LocalPath", Utils.LocalFolder);
                command.Parameters.Add("Upload", true);
                command.Parameters.Add("Recursive", true);

                // remove line after adding cleanup script to sharefile server
                command.Parameters.Add("Overwrite", true);

                pipeline.Commands.Add(command);

                Collection<PSObject> psObjects = pipeline.Invoke();
                Assert.AreEqual<int>(1, psObjects.Count);
            }

            using (Pipeline pipeline = runspace.CreatePipeline())
            {
                Command command = new Command("Get-ChildItem");

                pipeline.Commands.Add(command);

                pipeline.Input.Write(Utils.ShareFileFolderUploaded);
                Collection<PSObject> psObjects = pipeline.Invoke();

                Assert.AreNotEqual<int>(0, psObjects.Count);
            }
        }

        [TestMethod]
        public void TM14_SyncUploadFolderOverwriteTest()
        {
            using (Pipeline pipeline = runspace.CreatePipeline())
            {
                Command command = new Command("Sync-SfItem");
                command.Parameters.Add("ShareFilePath", Utils.ShareFileDrivePath);
                command.Parameters.Add("LocalPath", Utils.LocalFolder);
                command.Parameters.Add("Upload", true);
                command.Parameters.Add("Overwrite", true);

                pipeline.Commands.Add(command);

                Collection<PSObject> psObjects = pipeline.Invoke();
                Assert.AreEqual<int>(1, psObjects.Count);
            }

            using (Pipeline pipeline = runspace.CreatePipeline())
            {
                Command command = new Command("Get-ChildItem");

                pipeline.Commands.Add(command);

                pipeline.Input.Write(Utils.ShareFileFolderUploaded);
                Collection<PSObject> psObjects = pipeline.Invoke();

                Assert.AreNotEqual<int>(0, psObjects.Count);
            }
        }

        [TestMethod]
        public void TM15_SyncUploadFolderMoveTest()
        {
            using (Pipeline pipeline = runspace.CreatePipeline())
            {
                Command command = new Command("Sync-SfItem");
                command.Parameters.Add("ShareFilePath", Utils.ShareFileDrivePath);
                command.Parameters.Add("LocalPath", Utils.LocalFile);
                command.Parameters.Add("Upload", true);
                command.Parameters.Add("Move", true);

                // remove line after adding cleanup script to sharefile server
                command.Parameters.Add("Overwrite", true);

                pipeline.Commands.Add(command);

                Collection<PSObject> psObjects = pipeline.Invoke();
                Assert.AreEqual<int>(1, psObjects.Count);
            }

            using (Pipeline pipeline = runspace.CreatePipeline())
            {
                Command command = new Command("Get-ChildItem");

                pipeline.Commands.Add(command);

                pipeline.Input.Write(Utils.ShareFileFileUploaded);
                Collection<PSObject> psObjects = pipeline.Invoke();

                Assert.AreEqual<int>(1, psObjects.Count);
                Assert.IsFalse(Utils.IsPathExist(Utils.ShareFileFileUploaded));
            }
        }

        [TestMethod]
        [ExpectedException(typeof(CmdletInvocationException), "Provide only one switch to Upload or Download the files")]
        public void TM21_SyncWithUploadDownloadFlagsTest()
        {
            using (Pipeline pipeline = runspace.CreatePipeline())
            {
                Command command = new Command("Sync-SfItem");
                command.Parameters.Add("ShareFilePath", Utils.ShareFileDrivePath);
                command.Parameters.Add("LocalPath", Utils.LocalFile);
                command.Parameters.Add("Upload", true);
                command.Parameters.Add("Download", true);

                pipeline.Commands.Add(command);

                pipeline.Invoke();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(CmdletInvocationException), "Upload or Download switch must be specified")]
        public void TM22_SyncWithoutUploadDownloadFlagsTest()
        {
            using (Pipeline pipeline = runspace.CreatePipeline())
            {
                Command command = new Command("Sync-SfItem");
                command.Parameters.Add("ShareFilePath", Utils.ShareFileDrivePath);
                command.Parameters.Add("LocalPath", Utils.LocalFile);
                
                pipeline.Commands.Add(command);

                pipeline.Invoke();
            }
        }

        [TestMethod]
        public void TM23_SyncWithoutDownloadPathTest()
        {
            using (Pipeline pipeline = runspace.CreatePipeline())
            {
                Command command = new Command("Sync-SfItem");
                command.Parameters.Add("ShareFilePath", Utils.ShareFileFile);
                command.Parameters.Add("Download", true);
                command.Parameters.Add("Overwrite", true);

                pipeline.Commands.Add(command);

                Collection<PSObject> psObjects = pipeline.Invoke();
                Assert.AreEqual<int>(1, psObjects.Count);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ParameterBindingException), "Cannot process command because of one or more missing mandatory parameters: ShareFilePath.")]
        public void TM24_SyncWithoutShareFilePathTest()
        {
            using (Pipeline pipeline = runspace.CreatePipeline())
            {
                Command command = new Command("Sync-SfItem");
                command.Parameters.Add("LocalPath", Utils.ShareFileFolder);
                command.Parameters.Add("Download", true);

                pipeline.Commands.Add(command);

                pipeline.Invoke();
            }
        }

        [TestMethod]
        public void TM25_SyncItemVersionTest()
        {
            using (Pipeline pipeline = runspace.CreatePipeline())
            {
                Command command = new Command("Sync-SfItem");
                command.Parameters.Add("Help", true);

                pipeline.Commands.Add(command);

                Collection<PSObject> psObjects = pipeline.Invoke();

                Assert.AreEqual<int>(1, psObjects.Count);
                Assert.IsTrue(psObjects[0].BaseObject.ToString().StartsWith("SFCLI version"));
            }
        }
    }
}
