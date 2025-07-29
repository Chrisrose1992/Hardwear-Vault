using System.Management;
using System.Runtime.Versioning;
using HardwareVault.Core.Models;
using HardwareVault.Core.Utilities;

namespace HardwareVault.Core.Services
{
    [SupportedOSPlatform("windows")]
    public class UuidProvider
    {
        public async Task<UuidInfo> GetUuidsAsync()
        {
            return await Task.FromResult(GetUuids()).ConfigureAwait(false);
        }

        public async Task<UuidInfo> GetUuidInfoAsync()
        {
            return await GetUuidsAsync();
        }

        public UuidInfo GetUuids()
        {
            return GetUuidInfo();
        }

        public UuidInfo GetUuidInfo()
        {
            var uuidInfo = new UuidInfo();

            try
            {
                // Get System UUID
                try
                {
                    using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystemProduct"))
                    {
                        foreach (ManagementObject product in searcher.Get())
                        {
                            var uuid = product["UUID"]?.ToString();
                            if (!string.IsNullOrEmpty(uuid) && uuid != "FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF")
                            {
                                uuidInfo.SystemUuid = uuid;
                            }
                            else
                            {
                                uuidInfo.SystemUuid = "Unknown";
                            }
                            break;
                        }
                    }
                }
                catch 
                { 
                    uuidInfo.SystemUuid = "Unknown";
                }

                // Get Baseboard UUID/Serial
                try
                {
                    using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard"))
                    {
                        foreach (ManagementObject baseboard in searcher.Get())
                        {
                            var serial = baseboard["SerialNumber"]?.ToString();
                            uuidInfo.BaseboardUuid = NullHandler.FilterPlaceholders(serial);
                            break;
                        }
                    }
                }
                catch 
                { 
                    uuidInfo.BaseboardUuid = "Unknown";
                }

                // Get Chassis UUID/Serial
                try
                {
                    using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_SystemEnclosure"))
                    {
                        foreach (ManagementObject chassis in searcher.Get())
                        {
                            var serial = chassis["SerialNumber"]?.ToString();
                            uuidInfo.ChassisUuid = NullHandler.FilterPlaceholders(serial);
                            break;
                        }
                    }
                }
                catch 
                { 
                    uuidInfo.ChassisUuid = "Unknown";
                }

                // Final fallback - ensure no nulls
                uuidInfo.SystemUuid ??= "Unknown";
                uuidInfo.BaseboardUuid ??= "Unknown";
                uuidInfo.ChassisUuid ??= "Unknown";

            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve UUID information: {ex.Message}", ex);
            }

            return uuidInfo;
        }
    }
}