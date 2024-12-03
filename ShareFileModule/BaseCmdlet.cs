using System.Management.Automation;

namespace ShareFile.Api.Powershell
{
    public abstract class BaseCmdlet : PSCmdlet
    {
        public BaseCmdlet() : base()
        {
            AssemblyBindingRedirects.Register();
        }
    }
}
