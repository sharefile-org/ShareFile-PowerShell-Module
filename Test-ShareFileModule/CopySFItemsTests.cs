using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShareFile.Api.Powershell;

namespace Test_ShareFileSnapIn
{
    [TestClass]
    public class CopySFItemsTests
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
        public void TM1_CopyItemToLocalTest()
        {
            using (Pipeline pipeline = runspace.CreatePipeline())
            {
                Utils.DeleteLocalFile(Utils.LocalFileDownloaded);
                Utils.DeleteProgressFile();

                Command command = new Command("Copy-SFItem");
                command.Parameters.Add("Path", Utils.ShareFileFileFullPath);
                command.Parameters.Add("Destination", Utils.LocalBaseFolder);
                
                pipeline.Commands.Add(command);
                
                Collection<PSObject> psObjects = pipeline.Invoke();
                
                Assert.AreEqual<int>(1, psObjects.Count);
                Assert.IsTrue(Utils.IsPathExist(Utils.LocalFileDownloaded));
            }
        }

        [TestMethod]
        public void TM2_CopyItemToLocalForceTest()
        {
            using (Pipeline pipeline = runspace.CreatePipeline())
            {
                Utils.DeleteProgressFile();

                Command command = new Command("Copy-SFItem");
                command.Parameters.Add("Path", Utils.ShareFileFileFullPath);
                command.Parameters.Add("Destination", Utils.LocalBaseFolder);
                command.Parameters.Add("Force", true);

                pipeline.Commands.Add(command);

                Collection<PSObject> psObjects = pipeline.Invoke();
                Assert.AreEqual<int>(1, psObjects.Count);
            }
        }

        [TestMethod]
        public void TM3_CopyItemToShareFileServerTest()
        {
            using (Pipeline pipeline = runspace.CreatePipeline())
            {
                Utils.DeleteProgressFile();

                Command command = new Command("Copy-SFItem");
                command.Parameters.Add("Path", Utils.LocalFile);
                command.Parameters.Add("Destination", Utils.ShareFileHomeFolder);

                // remove line after adding cleanup script to sharefile server
                command.Parameters.Add("Force", true);

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
        public void TM4_CopyItemToShareFileServerForceTest()
        {
            using (Pipeline pipeline = runspace.CreatePipeline())
            {
                Utils.DeleteProgressFile();

                Command command = new Command("Copy-SFItem");
                command.Parameters.Add("Path", Utils.LocalFile);
                command.Parameters.Add("Destination", Utils.ShareFileHomeFolder);
                command.Parameters.Add("Force", true);

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
        public void TM5_CopyItemToShareFileWildCardTest()
        {
            using (Pipeline pipeline = runspace.CreatePipeline())
            {
                Utils.DeleteProgressFile();

                Command command = new Command("Copy-SFItem");
                command.Parameters.Add("Path", Utils.LocalFolder + @"\*.*");
                command.Parameters.Add("Destination", Utils.ShareFileHomeFolder);

                // remove line after adding cleanup script to sharefile server
                command.Parameters.Add("Force", true);

                pipeline.Commands.Add(command);

                Collection<PSObject> psObjects = pipeline.Invoke();
                Assert.AreEqual<int>(1, psObjects.Count);
            }
        }
    }
}
