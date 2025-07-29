using System.Management;
using System.Globalization;
using HardwareVault.Core.Mapping;

namespace HardwareVault.Core.Helpers
{
    public static class OsDataHelper
    {
        public static Dictionary<string, object> GetOsIdentity(ManagementObject os)
        {
            return new Dictionary<string, object>
            {
                ["hostName"] = os["CSName"],
                ["registeredUser"] = os["RegisteredUser"],
                ["serialNumber"] = os["SerialNumber"]
            };
        }

        public static Dictionary<string, object> GetOsVersion(ManagementObject os)
        {
            return new Dictionary<string, object>
            {
                ["caption"] = os["Caption"],
                ["version"] = os["Version"],
                ["buildNumber"] = os["BuildNumber"],
                ["productType"] = OsMappings.GetProductTypeName(Convert.ToInt32(os["ProductType"] ?? 0)),
                ["osSku"] = os["OperatingSystemSKU"],
                ["osSkuName"] = OsMappings.GetOsSkuName(Convert.ToInt32(os["OperatingSystemSKU"] ?? 0)),
                ["architecture"] = os["OSArchitecture"]
            };
        }

        public static Dictionary<string, object> GetOsTime(ManagementObject os)
        {
            var installDate = ManagementDateTimeConverter.ToDateTime(os["InstallDate"].ToString());
            var lastBoot = ManagementDateTimeConverter.ToDateTime(os["LastBootUpTime"].ToString());
            var uptime = DateTime.UtcNow - lastBoot;

            return new Dictionary<string, object>
            {
                ["installDate"] = installDate.ToString("yyyy-MM-dd HH:mm:ss"),
                ["lastBootUpTime"] = lastBoot.ToString("yyyy-MM-dd HH:mm:ss"),
                ["uptime"] = $"{(int)uptime.TotalDays} days {uptime.Hours} hours {uptime.Minutes} minutes"
            };
        }

        public static Dictionary<string, object> GetLocaleInfo(ManagementObject os)
        {
            return new Dictionary<string, object>
            {
                ["systemDirectory"] = os["SystemDirectory"],
                ["systemDrive"] = os["SystemDrive"],
                ["windowsDirectory"] = os["WindowsDirectory"],
                ["bootDevice"] = os["BootDevice"]
            };
        }

        public static Dictionary<string, object> GetProtectionInfo(ManagementObject os)
        {
            return new Dictionary<string, object>
            {
                ["dataExecutionPrevention"] = new
                {
                    available = os["DataExecutionPrevention_Available"],
                    drivers = os["DataExecutionPrevention_Drivers"],
                    supportPolicy = os["DataExecutionPrevention_SupportPolicy"]
                },
                ["encryptionLevel"] = os["EncryptionLevel"]
            };
        }
    }

    public static class HotfixHelper
    {
        public static List<object> GetInstalledHotfixes()
        {
            var hotfixes = new List<object>();
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_QuickFixEngineering");
                foreach (ManagementObject fix in searcher.Get())
                {
                    hotfixes.Add(new
                    {
                        id = fix["HotFixID"],
                        description = fix["Description"],
                        installedOn = fix["InstalledOn"]
                    });
                }
            }
            catch { }
            return hotfixes;
        }
    }

    public static class RegionalHelper
    {
        public static object GetRegionalSettings()
        {
            return new
            {
                timeZone = TimeZoneInfo.Local.StandardName,
                locale = CultureInfo.CurrentCulture.Name,
                uiCulture = CultureInfo.CurrentUICulture.Name
            };
        }
    }
}