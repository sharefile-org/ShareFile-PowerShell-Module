using System;
using System.Linq;
using Microsoft.Win32;

namespace ShareFile.Api.Powershell
{
    public class WebpopInternetExplorerMode
    {
        private const string InternetExplorerEmulationRegistryKey = @"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION";
        private const string InternetExplorerInstalledVersionKey = @"Software\Microsoft\Internet Explorer";
        private const string InternetExplorerVersionKeyName = "svcVersion";
        private const string InternetExplorerVersionKeyNameOld = "Version";

        public static bool SetUseCurrentIERegistryKey()
        {
            var ieVersion = GetInstalledInternetExplorerVersion() ?? InternetExplorerVersion.IE9;
            return SetInternetExplorerEmulationRegistryKey(ieVersion);
        }

        public static InternetExplorerVersion? GetInstalledInternetExplorerVersion()
        {
            Func<string, InternetExplorerVersion?> getInstalledVersion = keyName => ParseInternetExplorerVersionString(GetRegistryString(Registry.LocalMachine, InternetExplorerInstalledVersionKey, keyName));
            return getInstalledVersion(InternetExplorerVersionKeyName) ?? getInstalledVersion(InternetExplorerVersionKeyNameOld);
        }

        private static InternetExplorerVersion? ParseInternetExplorerVersionString(string version)
        {
            if (String.IsNullOrEmpty(version))
            {
                return null;
            }

            int dotIndex = version.IndexOf('.');
            if (dotIndex == -1)
            {
                return null;
            }

            string majorVersionString = version.Substring(0, dotIndex);
            int majorVersion;
            if (!int.TryParse(majorVersionString, out majorVersion))
            {
                return null;
            }

            majorVersion *= 1000;

            if (Enum.GetValues(typeof(InternetExplorerVersion)).Cast<int>().Contains(majorVersion))
            {
                return (InternetExplorerVersion)majorVersion;
            }
            else
            {
                return null;
            }
        }

        private static string GetRegistryString(RegistryKey parent, string keyPath, string keyName)
        {
            try
            {
                using (var regKey = parent.OpenSubKey(keyPath))
                {
                    return regKey.GetValue(keyName) as string;
                }
            }
            catch
            {
                return null;
            }
        }

        public static bool SetInternetExplorerEmulationRegistryKey(InternetExplorerVersion ieVersion)
        {
            return SetInternetExplorerEmulationRegistryKey((ieVersion == InternetExplorerVersion.None) ? null : (int?)ieVersion);
        }

        public static bool SetInternetExplorerEmulationRegistryKey(int? ieVersion)
        {
            try
            {
                using (var regKey = Registry.CurrentUser.CreateSubKey(InternetExplorerEmulationRegistryKey, RegistryKeyPermissionCheck.ReadWriteSubTree)) //opens an existing subkey or creates it
                {
                    string appName = "powershell.exe";
                    if (ieVersion.HasValue)
                    {
                        regKey.SetValue(appName, ieVersion.Value, RegistryValueKind.DWord);
                    }
                    else
                    {
                        regKey.DeleteValue(appName);
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
