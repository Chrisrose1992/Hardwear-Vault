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

                        // Enhanced device classification
                        ClassifyDevice(usbDevice);

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

                        // Determine USB version from controller name
                        DetermineUsbVersionFromController(usbDevice);

                        usbDevices.Add(usbDevice);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting USB controllers: {ex.Message}");
            }
        }

        private void ClassifyDevice(UsbDeviceInfo usbDevice)
        {
            // First try dataset classification
            usbDevice.DeviceClass = _datasetService.GetUsbDeviceClassFromName(usbDevice.Name) ??
                                  _datasetService.GetUsbDeviceClassFromName(usbDevice.Description);

            if (!string.IsNullOrWhiteSpace(usbDevice.DeviceClass))
            {
                usbDevice.DeviceClassDescription = _datasetService.GetUsbDeviceClassName(usbDevice.DeviceClass);
            }
            else
            {
                // Enhanced classification based on VID/PID and device patterns
                ClassifyByVidPidAndName(usbDevice);
            }
        }

        private void ClassifyByVidPidAndName(UsbDeviceInfo usbDevice)
        {
            var name = (usbDevice.Name ?? usbDevice.Description ?? "").ToLowerInvariant();
            var deviceId = (usbDevice.DeviceId ?? "").ToLowerInvariant();

            // Webcam detection
            if (name.Contains("webcam") || name.Contains("camera") || name.Contains("c960") ||
                deviceId.Contains("mi_00") && name.Contains("hd"))
            {
                usbDevice.DeviceClass = "0E";
                usbDevice.DeviceClassDescription = "Video";
                return;
            }

            // Audio device detection
            if (name.Contains("audio") || deviceId.Contains("mi_02"))
            {
                usbDevice.DeviceClass = "01";
                usbDevice.DeviceClassDescription = "Audio";
                return;
            }

            // HID device detection
            if (name.Contains("input") || name.Contains("keyboard") || name.Contains("mouse") ||
                usbDevice.VendorId == "0x04F3" || usbDevice.VendorId == "0x30FA" || usbDevice.VendorId == "0x048D")
            {
                usbDevice.DeviceClass = "03";
                usbDevice.DeviceClassDescription = "HID (Human Interface Device)";
                return;
            }

            // Bluetooth detection
            if (name.Contains("bluetooth") || usbDevice.VendorId == "0x8087")
            {
                usbDevice.DeviceClass = "02";
                usbDevice.DeviceClassDescription = "Communications and CDC Control";
                return;
            }

            // Hub detection
            if (name.Contains("hub") || name.Contains("root hub") || usbDevice.VendorId == "0x05E3" || usbDevice.VendorId == "0x0BDA")
            {
                usbDevice.DeviceClass = "09";
                usbDevice.DeviceClassDescription = "Hub";
                return;
            }

            // Composite device
            if (name.Contains("composite"))
            {
                usbDevice.DeviceClass = "00";
                usbDevice.DeviceClassDescription = "Use class information in the Interface Descriptors";
                return;
            }

            // Unknown classification
            usbDevice.DeviceClass = "FF";
            usbDevice.DeviceClassDescription = "Vendor Specific";
        }

        private void DetermineUsbVersionFromController(UsbDeviceInfo usbDevice)
        {
            var name = usbDevice.Name?.ToLowerInvariant() ?? "";

            if (name.Contains("usb 3.2") || name.Contains("3.20"))
                usbDevice.Version = "USB 3.2";
            else if (name.Contains("usb 3.1") || name.Contains("3.10"))
                usbDevice.Version = "USB 3.1";
            else if (name.Contains("usb 3.0") || name.Contains("3.0") || name.Contains("xhci"))
                usbDevice.Version = "USB 3.0";
            else if (name.Contains("usb 2.0") || name.Contains("2.0") || name.Contains("ehci"))
                usbDevice.Version = "USB 2.0";
            else if (name.Contains("usb 1.1") || name.Contains("1.1") || name.Contains("ohci") || name.Contains("uhci"))
                usbDevice.Version = "USB 1.1";
            else
                usbDevice.Version = "USB 2.0+";
        }
    }
}