using ShareFile.Api.Client.Exceptions;
using System.Management.Automation;

namespace ShareFile.Api.Powershell
{
    [Cmdlet(VerbsCommon.Get, Noun)]
    public class GetSfClient : BaseCmdlet
    {
        private const string Noun = "SfClient";

        [Parameter(Position=0)]
        public string Name { get; set; }

        protected override void ProcessRecord()
        {
            if (Name.IndexOf('.') < 0) Name += ".sfps";
            var psc = new PSShareFileClient(Name);
            psc.Load();
            try
            {
                psc.Client.Sessions.Get().Execute();
            }
            catch (WebAuthenticationException)
            {
                psc = new PSShareFileClient(Name);
                psc.Load();
                psc.Client.Sessions.Get().Execute();
            }
            WriteObject(psc);
        }
    }
}
