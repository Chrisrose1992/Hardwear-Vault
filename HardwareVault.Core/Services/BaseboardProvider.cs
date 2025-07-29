using System.Management;
using System.Runtime.Versioning;
using HardwareVault.Core.Models;

namespace HardwareVault.Core.Services
{
    [SupportedOSPlatform("windows")]
    public class BaseboardProvider
    {
        private readonly ChipsetDatasetService _chipsetService;
        private readonly UsbDeviceProvider _usbProvider;

        public BaseboardProvider()
        {
            _chipsetService = new ChipsetDatasetService();
            _usbProvider = new UsbDeviceProvider();
        }
        public async Task<BaseboardInfo> GetBaseboardInfoAsync()
        {
            return await Task.FromResult(GetBaseboardInfo()).ConfigureAwait(false);
        }

        public BaseboardInfo GetBaseboardInfo()
        {
            var baseboardInfo = new BaseboardInfo
            {
                PciSlotInfo = new PciSlotInfo()
            };

            try
            {
                // Get baseboard information
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard"))
                {
                    foreach (ManagementObject baseboard in searcher.Get())
                    {
                        baseboardInfo.Manufacturer = FilterDefaultValue(baseboard["Manufacturer"]?.ToString());
                        baseboardInfo.Product = FilterDefaultValue(baseboard["Product"]?.ToString());
                        baseboardInfo.SerialNumber = FilterDefaultValue(baseboard["SerialNumber"]?.ToString());
                        baseboardInfo.Version = FilterDefaultValue(baseboard["Version"]?.ToString());
                        baseboardInfo.Model = FilterDefaultValue(baseboard["Model"]?.ToString());
                        break;
                    }
                }            

                // Get PCI and USB info
                var (pciModel, pciVersion, availableSlots, releaseYear) = GetPciSlotInfo(baseboardInfo.Manufacturer, baseboardInfo.Product);
                baseboardInfo.PciSlotInfo.Model = pciModel;
                baseboardInfo.PciSlotInfo.Version = pciVersion;
                baseboardInfo.PciSlotInfo.AvailableSlots = availableSlots;
                baseboardInfo.PciSlotInfo.ReleaseYear = releaseYear;
                baseboardInfo.UsbVersion = _usbProvider.GetUsbVersionFromSystem();

            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve baseboard information: {ex.Message}", ex);
            }

            return baseboardInfo;
        }

        private (string Model, string Version, List<PciSlot>? AvailableSlots, int? ReleaseYear) GetPciSlotInfo(string? manufacturer, string? product)
        {
            try
            {
                var (model, chipset) = _chipsetService.ExtractModelAndChipset(product);
                var version = _chipsetService.GetPcieVersionFromDataset(chipset ?? product);
                var availableSlots = _chipsetService.GetAvailableSlots(chipset ?? product);
                var releaseYear = _chipsetService.GetReleaseYear(chipset ?? product);
                
                return (chipset ?? "Unknown", version, availableSlots, releaseYear);
            }
            catch
            {
                return ("Unknown", "PCIe 3.0+", null, null);
            }
        }





        private string? FilterDefaultValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            var filtered = value.Trim();
            var invalidValues = new[] {
                "default string", "default", "unknown", "not available", "n/a", "na", "none",
                "to be filled by o.e.m.", "system manufacturer", "system product name",
                "system version", "type1productconfigid", "sku", "system sku", "x.x"
            };

            return invalidValues.Contains(filtered.ToLowerInvariant()) ? null : filtered;
        }
    }
}