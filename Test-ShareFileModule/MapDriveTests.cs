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
    public class MapDriveTests
    {
        private Runspace runspace = null;
        private PSObject sfLogin = null;

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

                Collection<PSObject> objs = pipeline.Invoke();
                Assert.AreEqual<int>(1, objs.Count);
                sfLogin = objs[0];
            }
        }
        
        [TestMethod]
        public void TM1_MapDriveTest()
        {
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
                PSObject sfDrive = psObjects[0];
                Assert.IsNotNull(sfDrive);
            }
        }

        [TestMethod]
        public void TM2_CheckHomeContentsTest()
        {
            Pipeline pipeline = runspace.CreatePipeline();

            Command command = new Command("New-PSDrive");
            command.Parameters.Add("Name", Utils.ShareFileDriveLetter);
            command.Parameters.Add("PSProvider", "ShareFile");
            command.Parameters.Add("Root", "/My Files & Folders");
            command.Parameters.Add("Client", sfLogin);

            pipeline.Commands.Add(command);
            Collection<PSObject> psObjects = pipeline.Invoke();
            Assert.AreEqual<int>(1, psObjects.Count);

            pipeline = runspace.CreatePipeline();
            command = new Command("Get-ChildItem");
            
            pipeline.Commands.Add(command);
                
            pipeline.Input.Write(Utils.ShareFileDrivePath);
            psObjects = pipeline.Invoke();
            Assert.AreNotEqual<int>(0, psObjects.Count);
        }
                
    }
}
