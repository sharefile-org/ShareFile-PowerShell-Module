using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShareFile.Api.Powershell;

namespace Test_ShareFileSnapIn
{
    [TestClass]
    public class LoginTests
    {
        private Runspace runspace = null;

        [TestInitialize]
        public void InitializeTests()
        {
            RunspaceConfiguration config = RunspaceConfiguration.Create();

            PSSnapInException warning;

            config.AddPSSnapIn("ShareFile", out warning);

            runspace = RunspaceFactory.CreateRunspace(config);
            runspace.Open();
        }

        [TestMethod]
        public void TM1_0_NewLoginTest()
        {
            PowerShell ps = PowerShell.Create();

            ps.AddCommand("Add-PSSnapIn");
            ps.AddArgument("ShareFile");

            Collection<PSObject> psObjects = ps.Invoke();

            ps.Commands.Clear();
            ps.AddCommand("New-SFClient");
            ps.AddParameter("Name", Utils.LoginFilePath);

            psObjects = ps.Invoke();

            Assert.AreEqual<int>(1, psObjects.Count);
        }


        [TestMethod]
        public void TM2_GetLoginTest()
        {
            using (Pipeline pipeline = runspace.CreatePipeline())
            {
                Command command = new Command("Get-SfClient");
                command.Parameters.Add(new CommandParameter("Name", Utils.LoginFilePath));

                pipeline.Commands.Add(command);

                Collection<PSObject> psObjects = pipeline.Invoke();
                Assert.AreEqual<int>(1, psObjects.Count);
                Assert.AreEqual(psObjects[0].BaseObject.ToString(), "ShareFile.Api.Powershell.PSShareFileClient");
            }
        }

    }
}
