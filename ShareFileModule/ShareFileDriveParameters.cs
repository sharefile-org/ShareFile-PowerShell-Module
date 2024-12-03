using System;
using System.Management.Automation;

namespace ShareFile.Api.Powershell
{
    public class ShareFileDriveParameters
    {
        public ShareFileDriveParameters()
        {
            Client = null;
            RootUri = null;
        }

        [Parameter(Mandatory=true)]
        public PSShareFileClient Client { get; set; }

        [Parameter(Mandatory = false)]
        public Uri RootUri { get; set; }
    }
}
