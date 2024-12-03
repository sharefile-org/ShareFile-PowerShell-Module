using Newtonsoft.Json;

namespace ShareFile.Api.Powershell
{
    public class OAuthToken
    {
        [JsonProperty(PropertyName = "access_token")]
        public string AccessToken { get; set; }

        [JsonProperty(PropertyName = "refresh_token")]
        public string RefreshToken { get; set; }

        [JsonProperty(PropertyName = "token_type")]
        public string TokenType { get; set; }

        [JsonProperty(PropertyName = "appcp")]
        public string AppCP { get; set; }

        [JsonProperty(PropertyName = "apicp")]
        public string ApiCP { get; set; }

        [JsonProperty(PropertyName = "subdomain")]
        public string Subdomain { get; set; }

        [JsonProperty(PropertyName = "expires_in")]
        public long ExpiresIn { get; set; }

        [JsonProperty(PropertyName = "access_files_folders")]
        public bool AccessFilesFolders { get; set; }

        [JsonProperty(PropertyName = "modify_files_folders")]
        public bool ModifyFilesFolders { get; set; }

        [JsonProperty(PropertyName = "admin_users")]
        public bool AdminUsers { get; set; }

        [JsonProperty(PropertyName = "admin_accounts")]
        public bool AdminAccounts { get; set; }

        [JsonProperty(PropertyName = "change_my_settings")]
        public bool ChangeMySettings { get; set; }

        [JsonProperty(PropertyName = "web_app_login")]
        public bool WebAppLogin { get; set; } 
    }
}
