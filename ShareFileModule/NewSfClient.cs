using ShareFile.Api.Powershell.Properties;
using System.Management.Automation;

namespace ShareFile.Api.Powershell
{
    [Cmdlet(VerbsCommon.New, Noun)]
    public class NewSfClient : BaseCmdlet
    {
        private const string Noun = "SfClient";

        [Parameter(Position = 0)]
        public string Name { get; set; }

        [Parameter(Position = 1)]
        public string Account { get; set; }

        [Parameter(Position = 2)]
        public string Domain { get; set; }

        [Parameter]
        public PSCredential Credential { get; set; }

        [Parameter]
        public string ApiVersion { get; set; }

        [Parameter]
        public string Provider { get; set; }

        [Parameter]
        public string Email { get; set; }

        protected override void ProcessRecord()
        {
            if (ApiVersion == null) ApiVersion = Resources.DefaultApiVersion;
            if (Domain == null) Domain = Resources.DefaultApiDomain;
            if (Account == null) Account = Resources.DefaultGlobalApiSubdomain;
            if (Provider == null) Provider = Resources.ShareFileProvider;
            var authDomain = new AuthenticationDomain()
            {
                Account = Account,
                Domain = Domain,
                ApiVersion = ApiVersion,
                Provider = Provider,
                Username = Email
            };
            authDomain.Credential = Credential != null ? Credential.GetNetworkCredential() : null;
            PSShareFileClient psc = new PSShareFileClient(Name, authDomain);
            psc.GetSession();
            WriteObject(psc);
        }
    }
}
