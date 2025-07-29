using System.Management;
using System.Runtime.Versioning;
using HardwareVault.Core.Models;
using HardwareVault.Core.Utilities;

namespace HardwareVault.Core.Services
{
    [SupportedOSPlatform("windows")]
    public class ChassisProvider
    {
        private readonly IHardwareDatasetService _datasetService;

        public ChassisProvider()
        {
            _datasetService = new HardwareDatasetService();
        }

        public async Task<ChassisInfo> GetChassisInfoAsync()
        {
            return await Task.FromResult(GetChassisInfo()).ConfigureAwait(false);
        }

        public ChassisInfo GetChassisInfo()
        {
            var chassisInfo = new ChassisInfo();

            try
            {
                // Use a more conservative query with only commonly available properties
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_SystemEnclosure"))
                {
                    foreach (ManagementObject chassis in searcher.Get())
                    {
                        // Basic information
                        chassisInfo.Manufacturer = NullHandler.FilterPlaceholders(chassis["Manufacturer"]?.ToString());
                        chassisInfo.SerialNumber = NullHandler.FilterPlaceholders(chassis["SerialNumber"]?.ToString());
                        chassisInfo.Model = NullHandler.FilterPlaceholders(chassis["Model"]?.ToString());
                        
                        // Try to get SMBIOSAssetTag, but handle if it doesn't exist
                        try
                        {
                            chassisInfo.AssetTag = NullHandler.FilterPlaceholders(chassis["SMBIOSAssetTag"]?.ToString());
                        }
                        catch
                        {
                            chassisInfo.AssetTag = NullHandler.FilterPlaceholders(chassis["Tag"]?.ToString());
                        }
                        
                        // Try to get SKU
                        try
                        {
                            chassisInfo.SKU = NullHandler.FilterPlaceholders(chassis["SKU"]?.ToString());
                        }
                        catch
                        {
                            chassisInfo.SKU = "Unknown";
                        }

                        // Enhanced chassis type detection using dataset
                        try
                        {
                            if (chassis["ChassisTypes"] is ushort[] chassisTypes && chassisTypes.Length > 0)
                            {
                                chassisInfo.ChassisTypeCode = (uint)chassisTypes[0];
                                chassisInfo.ChassisType = _datasetService.GetChassisType(chassisTypes[0]);
                                chassisInfo.ChassisTypeDescription = _datasetService.GetChassisTypeName((uint)chassisTypes[0]);
                            }
                            else if (chassis["ChassisTypes"] is object[] chassisTypesObj && chassisTypesObj.Length > 0)
                            {
                                // Handle different array type
                                if (ushort.TryParse(chassisTypesObj[0]?.ToString(), out ushort chassisType))
                                {
                                    chassisInfo.ChassisTypeCode = (uint)chassisType;
                                    chassisInfo.ChassisType = _datasetService.GetChassisType(chassisType);
                                    chassisInfo.ChassisTypeDescription = _datasetService.GetChassisTypeName((uint)chassisType);
                                }
                            }
                            else
                            {
                                chassisInfo.ChassisType = "Unknown";
                                chassisInfo.ChassisTypeDescription = "Unknown";
                            }
                        }
                        catch
                        {
                            chassisInfo.ChassisType = "Unknown";
                            chassisInfo.ChassisTypeDescription = "Unknown";
                        }

                        // State information - these properties may not exist on all systems
                        try
                        {
                            if (chassis["BootupState"] != null && uint.TryParse(chassis["BootupState"].ToString(), out uint bootupState))
                            {
                                chassisInfo.BootupState = bootupState == 3; // 3 = Safe
                            }
                        }
                        catch
                        {
                            chassisInfo.BootupState = null;
                        }

                        try
                        {
                            if (chassis["PowerSupplyState"] != null && uint.TryParse(chassis["PowerSupplyState"].ToString(), out uint powerSupplyState))
                            {
                                chassisInfo.PowerSupplyState = powerSupplyState == 3; // 3 = Safe
                            }
                        }
                        catch
                        {
                            chassisInfo.PowerSupplyState = null;
                        }

                        try
                        {
                            if (chassis["ThermalState"] != null && uint.TryParse(chassis["ThermalState"].ToString(), out uint thermalState))
                            {
                                chassisInfo.ThermalState = thermalState == 3; // 3 = Safe
                            }
                        }
                        catch
                        {
                            chassisInfo.ThermalState = null;
                        }

                        try
                        {
                            if (chassis["NumberOfPowerCords"] != null && uint.TryParse(chassis["NumberOfPowerCords"].ToString(), out uint powerCords))
                            {
                                chassisInfo.NumberOfPowerCords = powerCords;
                            }
                        }
                        catch
                        {
                            chassisInfo.NumberOfPowerCords = null;
                        }

                        break; // Should only be one chassis
                    }
                }

                // Ensure we have at least basic information
                if (string.IsNullOrEmpty(chassisInfo.ChassisType))
                {
                    chassisInfo.ChassisType = "Unknown";
                    chassisInfo.ChassisTypeDescription = "Unknown";
                }

                if (string.IsNullOrEmpty(chassisInfo.Manufacturer))
                {
                    chassisInfo.Manufacturer = "Unknown";
                }

                if (string.IsNullOrEmpty(chassisInfo.SerialNumber))
                {
                    chassisInfo.SerialNumber = "Unknown";
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve chassis information: {ex.Message}", ex);
            }

            return chassisInfo;
        }
    }
}