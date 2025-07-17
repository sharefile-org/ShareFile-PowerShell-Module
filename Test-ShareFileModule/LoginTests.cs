using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test_ShareFileSnapIn
{
    [TestClass]
    public class LoginTests
    {
        private Runspace runspace = null;

        [TestInitialize]
        public void InitializeTests()
        {
            runspace = Utils.OpenRunspace();
        }

        [TestCleanup]
        public void CleanupTests()
        {
            runspace?.Dispose();
            runspace = null;
        }

        [TestMethod]
        public void TM1_0_NewLoginTest()
        {
            using var ps = PowerShell.Create();
            ps.Runspace = runspace;

            ps.Commands.Clear();
            ps.AddCommand("New-SFClient");
            ps.AddParameter("Name", Utils.LoginFilePath);

            var psObjects = ps.Invoke();

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
