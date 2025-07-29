using System.Management;
using System.Runtime.Versioning;
using Microsoft.Win32;
using HardwareVault.Core.Models;

namespace HardwareVault.Core.Services
{
    [SupportedOSPlatform("windows")]
    public class SecurityProvider
    {
        public async Task<SecurityInfo> GetSecurityInfoAsync()
        {
            return await Task.FromResult(GetSecurityInfo()).ConfigureAwait(false);
        }

        public SecurityInfo GetSecurityInfo()
        {
            var securityInfo = new SecurityInfo();

            try
            {
                // Get Windows Defender status
                securityInfo.AntivirusEnabled = GetAntivirusStatus();

                // Get BitLocker status
                securityInfo.BitLockerEnabled = GetBitLockerStatus();

                // Get Firewall status
                securityInfo.FirewallEnabled = GetFirewallStatus();

                // Get UAC status
                securityInfo.UacEnabled = IsUacEnabled();

                // Set security center info
                securityInfo.SecurityCenter = "Windows Security Center";

            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve security information: {ex.Message}", ex);
            }

            return securityInfo;
        }

        private bool GetAntivirusStatus()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher(@"root\SecurityCenter2", "SELECT * FROM AntiVirusProduct"))
                {
                    foreach (ManagementObject av in searcher.Get())
                    {
                        var displayName = av["displayName"]?.ToString();
                        if (!string.IsNullOrEmpty(displayName))
                        {
                            return true; // At least one antivirus product is installed
                        }
                    }
                }
            }
            catch
            {
                // Ignore errors and return false
            }
            return false;
        }

        private bool GetBitLockerStatus()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\BitLocker"))
                {
                    return key != null;
                }
            }
            catch
            {
                return false;
            }
        }

        private bool GetFirewallStatus()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Service WHERE Name='MpsSvc'"))
                {
                    foreach (ManagementObject service in searcher.Get())
                    {
                        var status = service["State"]?.ToString();
                        return status == "Running";
                    }
                }
            }
            catch
            {
                return false;
            }
            return false;
        }

        private bool IsUacEnabled()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"))
                {
                    var value = key?.GetValue("EnableLUA");
                    return value != null && value.ToString() == "1";
                }
            }
            catch
            {
                return false;
            }
        }
    }
}