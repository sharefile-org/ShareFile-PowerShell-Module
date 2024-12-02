using System;
using System.Management.Automation;

namespace ShareFile.Api.Powershell
{
    public class NewItemParameters
    {
        public NewItemParameters()
        {
            Uri = null;
            Details = null;
        }

        [Parameter(Mandatory = false)]
        public Uri Uri { get; set; }

        [Parameter(Mandatory = false)]
        public string Details { get; set; }
    }
}
