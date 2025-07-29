using System.Management;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using HardwareVault.Core.Models;
using HardwareVault.Core.Utilities;

namespace HardwareVault.Core.Services
{
    [SupportedOSPlatform("windows")]
    public class UsbDeviceProvider
    {
        private readonly IHardwareDatasetService _datasetService;

        public UsbDeviceProvider()
        {
            _datasetService = new HardwareDatasetService();
        }

        public async Task<List<UsbDeviceInfo>> GetUsbDevicesAsync()
        {
            return await Task.FromResult(GetUsbDevices()).ConfigureAwait(false);
        }

        public List<UsbDeviceInfo> GetUsbDevices()
        {
            var usbDevices = new List<UsbDeviceInfo>();

            try
            {
                GetPnPDevices(usbDevices);
                GetUsbControllerDevices(usbDevices);
                EnhanceDeviceInformation(usbDevices);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting USB devices: {ex.Message}");
            }

            return usbDevices.DistinctBy(d => d.DeviceId).ToList();
        }

        private void GetPnPDevices(List<UsbDeviceInfo> usbDevices)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT DeviceID, Name, Description, Manufacturer, Service, Status, Present, HardwareID FROM Win32_PnPEntity WHERE DeviceID LIKE 'USB%'"))
                {
                    foreach (ManagementObject device in searcher.Get())
                    {
                        var usbDevice = new UsbDeviceInfo
                        {
                            DeviceId = device["DeviceID"]?.ToString(),
                            Name = NullHandler.FilterPlaceholders(device["Name"]?.ToString()),
                            Description = NullHandler.FilterPlaceholders(device["Description"]?.ToString()),
                            Manufacturer = NullHandler.FilterPlaceholders(device["Manufacturer"]?.ToString()),
                            IsConnected = ParseBooleanStatus(device["Present"]?.ToString())
                        };

                        // Extract VID and PID from device ID
                        ExtractVidPid(usbDevice);

                        // Try to determine device class from name/description
                        usbDevice.DeviceClass = _datasetService.GetUsbDeviceClassFromName(usbDevice.Name) ??
                                              _datasetService.GetUsbDeviceClassFromName(usbDevice.Description);

                        if (!string.IsNullOrWhiteSpace(usbDevice.DeviceClass))
                        {
                            usbDevice.DeviceClassDescription = _datasetService.GetUsbDeviceClassName(usbDevice.DeviceClass);
                        }

                        usbDevices.Add(usbDevice);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting PnP USB devices: {ex.Message}");
            }
        }

        private void GetUsbControllerDevices(List<UsbDeviceInfo> usbDevices)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Name, Description, DeviceID, Manufacturer, Status FROM Win32_USBController"))
                {
                    foreach (ManagementObject controller in searcher.Get())
                    {
                        var usbDevice = new UsbDeviceInfo
                        {
                            DeviceId = controller["DeviceID"]?.ToString(),
                            Name = NullHandler.FilterPlaceholders(controller["Name"]?.ToString()),
                            Description = NullHandler.FilterPlaceholders(controller["Description"]?.ToString()),
                            Manufacturer = NullHandler.FilterPlaceholders(controller["Manufacturer"]?.ToString()),
                            IsConnected = true, // Controllers are always connected if detected
                            DeviceClass = "09", // Hub class
                            DeviceClassDescription = "Hub"
                        };

                        usbDevices.Add(usbDevice);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting USB controllers: {ex.Message}");
            }
        }

        private void ExtractVidPid(UsbDeviceInfo usbDevice)
        {
            if (string.IsNullOrWhiteSpace(usbDevice.DeviceId))
                return;

            try
            {
                // Pattern to match VID and PID in USB device IDs
                var vidPattern = @"VID_([0-9A-F]{4})";
                var pidPattern = @"PID_([0-9A-F]{4})";

                var vidMatch = Regex.Match(usbDevice.DeviceId, vidPattern, RegexOptions.IgnoreCase);
                var pidMatch = Regex.Match(usbDevice.DeviceId, pidPattern, RegexOptions.IgnoreCase);

                if (vidMatch.Success)
                {
                    usbDevice.VendorId = "0x" + vidMatch.Groups[1].Value;
                }

                if (pidMatch.Success)
                {
                    usbDevice.ProductId = "0x" + pidMatch.Groups[1].Value;
                }

                // Try to get enhanced manufacturer name from vendor ID
                if (!string.IsNullOrWhiteSpace(usbDevice.VendorId))
                {
                    var enhancedManufacturer = _datasetService.GetManufacturerName(usbDevice.VendorId);
                    if (!string.IsNullOrWhiteSpace(enhancedManufacturer) && 
                        (string.IsNullOrWhiteSpace(usbDevice.Manufacturer) || usbDevice.Manufacturer == "Unknown"))
                    {
                        usbDevice.Manufacturer = enhancedManufacturer;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting VID/PID from {usbDevice.DeviceId}: {ex.Message}");
            }
        }

        private void EnhanceDeviceInformation(List<UsbDeviceInfo> usbDevices)
        {
            foreach (var device in usbDevices)
            {
                try
                {
                    // Enhanced device classification based on name patterns
                    if (string.IsNullOrWhiteSpace(device.DeviceClass))
                    {
                        device.DeviceClass = ClassifyDeviceByName(device.Name ?? device.Description);
                        if (!string.IsNullOrWhiteSpace(device.DeviceClass))
                        {
                            device.DeviceClassDescription = _datasetService.GetUsbDeviceClassName(device.DeviceClass);
                        }
                    }

                    // Set version information if available from device ID
                    if (!string.IsNullOrWhiteSpace(device.DeviceId))
                    {
                        if (device.DeviceId.Contains("USB\\VID_", StringComparison.OrdinalIgnoreCase))
                        {
                            if (device.DeviceId.Contains("\\MI_", StringComparison.OrdinalIgnoreCase))
                            {
                                device.Version = "USB 2.0+"; // Composite devices usually USB 2.0+
                            }
                            else
                            {
                                device.Version = "USB 1.1+"; // Basic assumption
                            }
                        }
                    }

                    // Set last connected time for connected devices using UTC
                    if (device.IsConnected == true)
                    {
                        device.LastConnected = DateTime.UtcNow;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error enhancing device {device.DeviceId}: {ex.Message}");
                }
            }
        }

        private string? ClassifyDeviceByName(string? deviceName)
        {
            if (string.IsNullOrWhiteSpace(deviceName))
                return null;

            var name = deviceName.ToLowerInvariant();

            // Mass storage devices
            if (name.Contains("storage") || name.Contains("disk") || name.Contains("drive") || 
                name.Contains("flash") || name.Contains("usb stick"))
                return "08";

            // HID devices
            if (name.Contains("keyboard") || name.Contains("mouse") || name.Contains("hid"))
                return "03";

            // Audio devices
            if (name.Contains("audio") || name.Contains("speaker") || name.Contains("microphone") || 
                name.Contains("headset") || name.Contains("sound"))
                return "01";

            // Video devices
            if (name.Contains("camera") || name.Contains("webcam") || name.Contains("video"))
                return "0E";

            // Printers
            if (name.Contains("printer") || name.Contains("print"))
                return "07";

            // Communication devices
            if (name.Contains("modem") || name.Contains("bluetooth") || name.Contains("wireless") || 
                name.Contains("network") || name.Contains("wifi"))
                return "02";

            // Hubs
            if (name.Contains("hub") || name.Contains("root hub"))
                return "09";

            return null;
        }

        private bool? ParseBooleanStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return null;

            return status.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                   status.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                   status.Equals("1", StringComparison.OrdinalIgnoreCase);
        }

        public string GetUsbVersionFromSystem()
        {
            try
            {
                var usbVersions = new HashSet<string>();
                
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_USBController"))
                {
                    foreach (ManagementObject controller in searcher.Get())
                    {
                        var name = controller["Name"]?.ToString()?.ToLower() ?? "";
                        
                        if (name.Contains("usb 4") || name.Contains("4.0") || name.Contains("thunderbolt"))
                            usbVersions.Add("USB 4.0");
                        else if (name.Contains("usb 3") || name.Contains("3.0") || name.Contains("xhci"))
                            usbVersions.Add("USB 3.0+");
                        else if (name.Contains("usb 2") || name.Contains("2.0") || name.Contains("ehci"))
                            usbVersions.Add("USB 2.0");
                        else if (name.Contains("usb 1") || name.Contains("1.1") || name.Contains("ohci") || name.Contains("uhci"))
                            usbVersions.Add("USB 1.1");
                    }
                }
                
                if (usbVersions.Contains("USB 4.0"))
                    return "USB 4.0";
                else if (usbVersions.Contains("USB 3.0+"))
                    return "USB 3.0+";
                else if (usbVersions.Contains("USB 2.0"))
                    return "USB 2.0";
                else if (usbVersions.Contains("USB 1.1"))
                    return "USB 1.1";
                else
                    return "USB 2.0+";
            }
            catch
            {
                return "USB 2.0+";
            }
        }
    }
}