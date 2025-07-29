using System.Management;
using System.Globalization;
using System.Runtime.Versioning;
using HardwareVault.Core.Models;
using HardwareVault.Core.Helpers;

namespace HardwareVault.Core.Services
{
    [SupportedOSPlatform("windows")]
    public class OSInfoProvider
    {
        public async Task<OSInfo> GetOSInfoAsync()
        {
            return await Task.FromResult(GetOSInfo()).ConfigureAwait(false);
        }

        public OSInfo GetOSInfo()
        {
            var osInfo = new OSInfo
            {
                Build = new BuildInfo(),
                Installation = new InstallationInfo(),
                Processes = new ProcessInfo()
            };

            try
            {
                // Get comprehensive OS Information using your helper methods
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem"))
                {
                    foreach (ManagementObject os in searcher.Get())
                    {
                        // Basic OS info (keeping your existing structure)
                        osInfo.Name = os["Caption"]?.ToString();
                        osInfo.Version = os["Version"]?.ToString();
                        osInfo.Build.Number = os["BuildNumber"]?.ToString();
                        osInfo.Build.Architecture = os["OSArchitecture"]?.ToString();
                        osInfo.Build.ServicePack = GetServicePackInfo(os);
                        osInfo.RegisteredUser = os["RegisteredUser"]?.ToString();
                        osInfo.Organization = FilterDefaultValue(os["Organization"]?.ToString());
                        osInfo.SerialNumber = os["SerialNumber"]?.ToString();
                        osInfo.SystemDirectory = os["SystemDirectory"]?.ToString();
                        osInfo.WindowsDirectory = os["WindowsDirectory"]?.ToString();

                        // Enhanced OS info using your helpers
                        var versionInfo = OsDataHelper.GetOsVersion(os);
                        // Get basic OS information using helper methods
                        var identityInfo = OsDataHelper.GetOsIdentity(os);
                        var localeInfo = OsDataHelper.GetLocaleInfo(os);
                        var protectionInfo = OsDataHelper.GetProtectionInfo(os);
                        
                        // Set properties that exist in OSInfo model
                        osInfo.Locale = localeInfo["locale"]?.ToString();
                        osInfo.TimeZone = localeInfo["timeZone"]?.ToString();
                        osInfo.IsHypervisorPresent = Convert.ToBoolean(protectionInfo["isHypervisorPresent"]);

                        // Time info (enhanced)
                        ProcessInstallationDates(os, osInfo);
                        ProcessProcessInfo(os, osInfo);

                        break; // Should only be one OS
                    }
                }

                // Get timezone information (enhanced)
                osInfo.TimeZone = TimeZoneInfo.Local.DisplayName;

                // Check for hypervisor
                osInfo.IsHypervisorPresent = IsHypervisorPresent();

            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve OS information: {ex.Message}", ex);
            }

            return osInfo;
        }

        private void ProcessInstallationDates(ManagementObject os, OSInfo osInfo)
        {
            try
            {
                if (os["InstallDate"] != null)
                {
                    osInfo.Installation.InstallDate = ManagementDateTimeConverter.ToDateTime(os["InstallDate"].ToString());
                }

                if (os["LastBootUpTime"] != null)
                {
                    var bootTime = ManagementDateTimeConverter.ToDateTime(os["LastBootUpTime"].ToString());
                    osInfo.Installation.LastBootUpTime = bootTime;
                    osInfo.Installation.Uptime = DateTime.UtcNow - bootTime;
                }
            }
            catch
            {
                // Handle conversion errors gracefully
            }
        }

        private void ProcessProcessInfo(ManagementObject os, OSInfo osInfo)
        {
            try
            {
                if (uint.TryParse(os["NumberOfProcesses"]?.ToString(), out uint processes))
                {
                    osInfo.Processes.NumberOfProcesses = processes;
                }

                if (uint.TryParse(os["NumberOfUsers"]?.ToString(), out uint users))
                {
                    osInfo.Processes.NumberOfUsers = users;
                }
            }
            catch
            {
                // Handle parsing errors gracefully
            }
        }

        private string GetServicePackInfo(ManagementObject os)
        {
            try
            {
                var spMajor = os["ServicePackMajorVersion"]?.ToString();
                var spMinor = os["ServicePackMinorVersion"]?.ToString();
                
                if (!string.IsNullOrEmpty(spMajor) && spMajor != "0")
                {
                    return string.IsNullOrEmpty(spMinor) || spMinor == "0" 
                        ? $"Service Pack {spMajor}" 
                        : $"Service Pack {spMajor}.{spMinor}";
                }
                
                return "0";
            }
            catch
            {
                return "0";
            }
        }

        private bool IsHypervisorPresent()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem"))
                {
                    foreach (ManagementObject system in searcher.Get())
                    {
                        var hypervisorPresent = system["HypervisorPresent"];
                        if (hypervisorPresent != null && bool.TryParse(hypervisorPresent.ToString(), out bool result))
                        {
                            return result;
                        }
                        break;
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private string? FilterDefaultValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            var filtered = value.Trim();
            var invalidValues = new[] {
                "default string", "default", "unknown", "not available", "n/a", "na", "none"
            };

            return invalidValues.Contains(filtered.ToLowerInvariant()) ? null : filtered;
        }
    }
}