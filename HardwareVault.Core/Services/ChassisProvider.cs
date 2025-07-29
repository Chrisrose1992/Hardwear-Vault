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
                using (var searcher = new ManagementObjectSearcher("SELECT Manufacturer, SerialNumber, Model, SMBIOSAssetTag, ChassisTypes, SKU, BootupState, PowerSupplyState, ThermalState, NumberOfPowerCords FROM Win32_SystemEnclosure"))
                {
                    foreach (ManagementObject chassis in searcher.Get())
                    {
                        // Basic information
                        chassisInfo.Manufacturer = NullHandler.FilterPlaceholders(chassis["Manufacturer"]?.ToString());
                        chassisInfo.SerialNumber = NullHandler.FilterPlaceholders(chassis["SerialNumber"]?.ToString());
                        chassisInfo.Model = NullHandler.FilterPlaceholders(chassis["Model"]?.ToString());
                        chassisInfo.AssetTag = NullHandler.FilterPlaceholders(chassis["SMBIOSAssetTag"]?.ToString());
                        chassisInfo.SKU = NullHandler.FilterPlaceholders(chassis["SKU"]?.ToString());

                        // Enhanced chassis type detection using dataset
                        if (chassis["ChassisTypes"] is ushort[] chassisTypes && chassisTypes.Length > 0)
                        {
                            chassisInfo.ChassisTypeCode = (uint)chassisTypes[0];
                            chassisInfo.ChassisType = _datasetService.GetChassisType(chassisTypes[0]);
                            chassisInfo.ChassisTypeDescription = _datasetService.GetChassisTypeName((uint)chassisTypes[0]);
                        }
                        else
                        {
                            chassisInfo.ChassisType = "Unknown";
                            chassisInfo.ChassisTypeDescription = "Unknown";
                        }

                        // State information
                        if (uint.TryParse(chassis["BootupState"]?.ToString(), out uint bootupState))
                        {
                            chassisInfo.BootupState = bootupState == 3; // 3 = Safe, 4 = Warning, 5 = Critical
                        }

                        if (uint.TryParse(chassis["PowerSupplyState"]?.ToString(), out uint powerSupplyState))
                        {
                            chassisInfo.PowerSupplyState = powerSupplyState == 3;
                        }

                        if (uint.TryParse(chassis["ThermalState"]?.ToString(), out uint thermalState))
                        {
                            chassisInfo.ThermalState = thermalState == 3;
                        }

                        if (uint.TryParse(chassis["NumberOfPowerCords"]?.ToString(), out uint powerCords))
                        {
                            chassisInfo.NumberOfPowerCords = powerCords;
                        }

                        break; // Should only be one chassis
                    }
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