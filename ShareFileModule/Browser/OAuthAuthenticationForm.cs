using Microsoft.Web.WebView2.Core;
using ShareFile.Api.Powershell.Extensions;
using ShareFile.Api.Powershell.Log;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ShareFile.Api.Powershell.Browser
{
    public partial class OAuthAuthenticationForm : Form
    {
        public delegate bool UrlEventCallback(Uri uri);
        private readonly Dictionary<string, UrlEventCallback> _urlEventHandlers = new Dictionary<string,UrlEventCallback>();

        private readonly Dictionary<ulong, string> _navigationIdToUrlMapping = new Dictionary<ulong, string>();

        private readonly CoreWebView2EnvironmentOptions webView2EnvOptions = new CoreWebView2EnvironmentOptions(
                                    additionalBrowserArguments: "--disable-features=msSmartScreenProtection",
                                    allowSingleSignOnUsingOSPrimaryAccount: true);

        public OAuthAuthenticationForm()
        {
            InitializeComponent();

            InitializeWebView2();
        }

        public void AddUrlEventHandler(string uri, UrlEventCallback handler)
        {
            _urlEventHandlers.Add(uri, handler);
        }

        public void Navigate(Uri uri)
        {
            webViewBrowser.CoreWebView2.Navigate(uri.ToString());
            ShowDialog();
        }

        private void InitializeWebView2()
        {
            // Select the cache directory 
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string cacheFolder = Path.Combine(localAppData, "ShareFile", "ShareFile PowerShell SDK");

            InitializeWebView2Env(cacheFolder);

            webViewBrowser.CoreWebView2.ProcessFailed += CoreWebView2_ProcessFailed;
            webViewBrowser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            webViewBrowser.CoreWebView2.Settings.AreDevToolsEnabled = false;
            webViewBrowser.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
            webViewBrowser.CoreWebView2.ClientCertificateRequested += CoreWebView2_ClientCertificateRequested;

            webViewBrowser.NavigationStarting += WebViewBrowser_NavigationStarting;
            webViewBrowser.NavigationCompleted += WebViewBrowser_NavigationCompleted;
        }

        private void InitializeWebView2Env(string cacheFolder)
        {
            var env = TaskHelper.RunSynchronously(CoreWebView2Environment.CreateAsync(null, cacheFolder, webView2EnvOptions));

            TaskHelper.RunSynchronously(webViewBrowser.EnsureCoreWebView2Async(env));
        }

        private void CoreWebView2_ClientCertificateRequested(object sender, CoreWebView2ClientCertificateRequestedEventArgs e)
        {
            var certificateList = e.MutuallyTrustedCertificates;

            if (certificateList.Count() == 1)
            {
                // If there is only one client certificate, then just use that one instead of prompting the user to click on it.
                e.SelectedCertificate = certificateList[0];
                e.Handled = true;
            }
        }

        private void CoreWebView2_ProcessFailed(object sender, CoreWebView2ProcessFailedEventArgs e)
        {
            LogProcessFailedEventArgs(e);
        }

        private void LogProcessFailedEventArgs(CoreWebView2ProcessFailedEventArgs e)
        {
            Logger.Instance.Error($"WebView2 process failed: Reason: {e.Reason} Exit code: {e.ExitCode} ProcessFailedKind: {e.ProcessFailedKind} Process description: {e.ProcessDescription}");
            if ((e.FrameInfosForFailedProcess?.Any()).GetValueOrDefault())
            {
                var sb = new StringBuilder("FrameInfos");
                foreach (var frameInfo in e.FrameInfosForFailedProcess)
                {
                    sb.AppendFormat("\tName: {0}, Source: {1} ({2})", frameInfo.Name, frameInfo.Source, frameInfo.ToString());
                    sb.AppendLine();
                }
                Logger.Instance.Error(sb.ToString());
            }
        }

        private void WebViewBrowser_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            _navigationIdToUrlMapping[e.NavigationId] = e.Uri;
        }

        private void WebViewBrowser_NavigationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            if (_navigationIdToUrlMapping.TryGetValue(e.NavigationId, out var url))
            {
                foreach (var handler in _urlEventHandlers)
                {
                    if (url.ToString().StartsWith(handler.Key))
                    {
                        if (handler.Value.Invoke(new Uri(url)))
                        {
                            this.Close();
                        }
                    }
                }
                _navigationIdToUrlMapping.Remove(e.NavigationId);
            }
        }

        private void ClearWebView2Cache()
        {
            var task = webViewBrowser?.CoreWebView2?.Profile?.ClearBrowsingDataAsync();
            if (task != null)
            {
                TaskHelper.RunSynchronously(task);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            ClearWebView2Cache();
            if (webViewBrowser != null)
            {
                if (webViewBrowser.CoreWebView2 != null)
                {
                    webViewBrowser.CoreWebView2.ProcessFailed -= CoreWebView2_ProcessFailed;
                    webViewBrowser.CoreWebView2.ClientCertificateRequested -= CoreWebView2_ClientCertificateRequested;
                }

                webViewBrowser.NavigationStarting -= WebViewBrowser_NavigationStarting;
                webViewBrowser.NavigationCompleted -= WebViewBrowser_NavigationCompleted;
            }
            base.OnClosed(e);
        }
    }
}
