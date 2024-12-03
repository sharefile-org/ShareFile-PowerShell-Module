using System;
using System.Net;

namespace ShareFile.Api.Powershell
{
    public class AuthenticationDomain
    {
        public string Id { get; set; }

        public bool IsDefault { get; set; }

        public string Provider { get; set; }

        public string Entity { get; set; }

        /// <summary>
        /// List of known ShareFile domains. These are checked to allow splitting of
        /// URLs into account + TLD; which can't be done otherwise. 
        /// </summary>
        private string[] ShareFileDomains = new string[]
        {
            "sf-api.com", "sharefile.com", "sf-apitest.com", "sharefiletest.com", 
            "sf-apidev.com", "sharefiledev.com", "citrixdata.com", "sharefile.eu",
            "sf-api.eu", "securevdr.com"
        };

        public string Uri
        {
            get
            {
                if (Domain != null && Provider != null && ApiVersion != null)
                {
                    string accountDomainRoot = !string.IsNullOrEmpty(Account) ? string.Format("{0}.{1}", Account, Domain) : Domain;
                    if (!string.IsNullOrEmpty(Root)) accountDomainRoot += "/" + Root;
                    return string.Format("https://{0}/{1}/{2}", accountDomainRoot, Provider, ApiVersion);
                }
                else return null;
            }
            set
            {
                Domain = null;
                Uri uri = new Uri(value);
                // Domain for sharefile accounts is account.domain
                // Domain for connectors do not have the account split
                foreach (var domain in ShareFileDomains)
                {
                    if (uri.Host.EndsWith(domain))
                    {
                        Domain = domain;
                        Account = uri.Host.Substring(0, uri.Host.Length - domain.Length - 1);
                    }
                }
                if (Domain == null) Domain = uri.Authority;

                // Parts are /<root>/<provider>/<version>
                // The Uri may contain extra parts, if the caller passes something like /sf/v3/Items (which contains the Entity)
                // We break-down the parts from back to front, using v<id> as the identifier for version
                string[] parts = uri.Segments;
                int idx = parts.Length - 1;
                while (idx >= 0)
                {
                    if (parts[idx].StartsWith("v"))
                    {
                        ApiVersion = parts[idx--];
                        if (ApiVersion.EndsWith("/")) ApiVersion = ApiVersion.Substring(0, ApiVersion.Length - 1);
                        if (idx < 0) throw new Exception("Invalid ShareFile URI - missing provider");
                        Provider = parts[idx].Substring(0, parts[idx].Length - 1);
                        if (idx > parts.Length - 1) Entity = parts[idx + 1];
                        idx--;
                        break;
                    }
                    idx--;
                }
                if (idx >= 0)
                {
                    for (int i = 0; i <= idx; i++)
                    {
                        if (parts[i] != "/") Root += parts[i];
                    }
                }
            }
        }

        public string Account 
        {
            get { return _account;  }
            set { _account = value != null ? value.ToLower() : null;  }
        }
        private string _account;

        public string Domain
        {
            get { return _domain; }
            set { _domain = value != null ? value.ToLower() : null; }
        }
        private string _domain;

        public string Root
        {
            get { return _root; }
            set { _root = value != null ? value.ToLower() : null; }
        }
        private string _root;

        public string ApiVersion
        {
            get { return _apiVersion; }
            set { _apiVersion = value != null ? value.ToLower() : null; }
        }
        private string _apiVersion;

        public string OAuthToken { get; set; }

        public string OAuthRefreshToken { get; set; }

        public string AuthID { get; set; }

        public NetworkCredential Credential { get; set; }

        public bool IsApiUri
        {
            get
            {
                return ApiVersion != null && Provider != null && Entity != null && Domain != null;
            }
        }

        public bool IsShareFileUri
        {
            get
            {
                return Provider.Equals("sf");
            }
        }

        public string Username { get; set; }
   }
}
