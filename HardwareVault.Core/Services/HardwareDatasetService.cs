using System.Text.Json;
using HardwareVault.Core.Models;

namespace HardwareVault.Core.Services
{
    public class HardwareDatasetService : IHardwareDatasetService
    {
        private readonly string _datasetPath;
        private Dictionary<string, string>? _manufacturers;
        private Dictionary<string, string>? _memoryTypes;
        private Dictionary<string, string>? _formFactors;
        private Dictionary<string, string>? _memoryTypeMappings;
        private Dictionary<string, string>? _chassisTypes;
        private Dictionary<string, string>? _usbDeviceClasses;
        private Dictionary<string, object>? _commonUsbDevices;

        public HardwareDatasetService()
        {
            _datasetPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dataset");
            LoadAllDatasets();
        }

        private void LoadAllDatasets()
        {
            try
            {
                LoadManufacturers();
                LoadMemoryTypes();
                LoadChassisTypes();
                LoadUsbDeviceClasses();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to load hardware datasets: {ex.Message}");
            }
        }

        private void LoadManufacturers()
        {
            var filePath = Path.Combine(_datasetPath, "manufacturers.json");
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                var data = JsonSerializer.Deserialize<JsonElement>(json);
                if (data.TryGetProperty("manufacturers", out var manufacturersElement))
                {
                    _manufacturers = new Dictionary<string, string>();
                    foreach (var manufacturer in manufacturersElement.EnumerateObject())
                    {
                        _manufacturers[manufacturer.Name] = manufacturer.Value.GetString() ?? "";
                    }
                }
            }
        }

        private void LoadMemoryTypes()
        {
            var filePath = Path.Combine(_datasetPath, "memory_types.json");
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                var data = JsonSerializer.Deserialize<JsonElement>(json);
                
                if (data.TryGetProperty("memoryTypes", out var memoryTypesElement))
                {
                    _memoryTypes = new Dictionary<string, string>();
                    foreach (var memoryType in memoryTypesElement.EnumerateObject())
                    {
                        _memoryTypes[memoryType.Name] = memoryType.Value.GetString() ?? "";
                    }
                }

                if (data.TryGetProperty("formFactors", out var formFactorsElement))
                {
                    _formFactors = new Dictionary<string, string>();
                    foreach (var formFactor in formFactorsElement.EnumerateObject())
                    {
                        _formFactors[formFactor.Name] = formFactor.Value.GetString() ?? "";
                    }
                }

                if (data.TryGetProperty("memoryTypeMappings", out var mappingsElement))
                {
                    _memoryTypeMappings = new Dictionary<string, string>();
                    foreach (var mapping in mappingsElement.EnumerateObject())
                    {
                        _memoryTypeMappings[mapping.Name] = mapping.Value.GetString() ?? "";
                    }
                }
            }
        }

        private void LoadChassisTypes()
        {
            var filePath = Path.Combine(_datasetPath, "chassis_types.json");
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                var data = JsonSerializer.Deserialize<JsonElement>(json);
                if (data.TryGetProperty("chassisTypes", out var chassisTypesElement))
                {
                    _chassisTypes = new Dictionary<string, string>();
                    foreach (var chassisType in chassisTypesElement.EnumerateObject())
                    {
                        _chassisTypes[chassisType.Name] = chassisType.Value.GetString() ?? "";
                    }
                }
            }
        }

        private void LoadUsbDeviceClasses()
        {
            var filePath = Path.Combine(_datasetPath, "usb_device_classes.json");
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                var data = JsonSerializer.Deserialize<JsonElement>(json);
                
                if (data.TryGetProperty("deviceClasses", out var deviceClassesElement))
                {
                    _usbDeviceClasses = new Dictionary<string, string>();
                    foreach (var deviceClass in deviceClassesElement.EnumerateObject())
                    {
                        _usbDeviceClasses[deviceClass.Name] = deviceClass.Value.GetString() ?? "";
                    }
                }

                if (data.TryGetProperty("commonDevices", out var commonDevicesElement))
                {
                    _commonUsbDevices = new Dictionary<string, object>();
                    foreach (var device in commonDevicesElement.EnumerateObject())
                    {
                        _commonUsbDevices[device.Name] = device.Value;
                    }
                }
            }
        }

        // Manufacturer methods
        public string? GetManufacturerName(string? manufacturerId)
        {
            if (string.IsNullOrWhiteSpace(manufacturerId) || _manufacturers == null)
                return null;

            // Try exact match first
            if (_manufacturers.TryGetValue(manufacturerId, out var exactMatch))
                return exactMatch;

            // Try partial match for IDs that might have different formats
            var normalizedId = manufacturerId.ToUpperInvariant().Replace("0X", "");
            return _manufacturers.FirstOrDefault(m => 
                m.Key.ToUpperInvariant().Replace("0X", "").Contains(normalizedId) ||
                normalizedId.Contains(m.Key.ToUpperInvariant().Replace("0X", ""))
            ).Value;
        }

        public bool IsKnownManufacturer(string? manufacturerName)
        {
            if (string.IsNullOrWhiteSpace(manufacturerName) || _manufacturers == null)
                return false;

            return _manufacturers.Values.Any(m => 
                string.Equals(m, manufacturerName, StringComparison.OrdinalIgnoreCase));
        }

        // Memory methods
        public string? GetMemoryTypeName(uint? memoryTypeCode)
        {
            if (memoryTypeCode == null || _memoryTypes == null)
                return null;

            return _memoryTypes.TryGetValue(memoryTypeCode.ToString(), out var memoryType) ? memoryType : null;
        }

        public string? GetFormFactorName(uint? formFactorCode)
        {
            if (formFactorCode == null || _formFactors == null)
                return null;

            return _formFactors.TryGetValue(formFactorCode.ToString(), out var formFactor) ? formFactor : null;
        }

        public string? MapMemoryType(string? memoryTypeString)
        {
            if (string.IsNullOrWhiteSpace(memoryTypeString) || _memoryTypeMappings == null)
                return memoryTypeString;

            return _memoryTypeMappings.TryGetValue(memoryTypeString, out var mappedType) ? mappedType : memoryTypeString;
        }

        // Chassis methods
        public string? GetChassisTypeName(uint? chassisTypeCode)
        {
            if (chassisTypeCode == null || _chassisTypes == null)
                return null;

            return _chassisTypes.TryGetValue(chassisTypeCode.ToString(), out var chassisType) ? chassisType : null;
        }

        public string GetChassisType(ushort chassisTypeCode)
        {
            return GetChassisTypeName((uint)chassisTypeCode) ?? $"Chassis Type {chassisTypeCode}";
        }

        // USB methods
        public string? GetUsbDeviceClassName(string? deviceClassCode)
        {
            if (string.IsNullOrWhiteSpace(deviceClassCode) || _usbDeviceClasses == null)
                return null;

            var normalizedCode = deviceClassCode.ToUpperInvariant().Replace("0X", "");
            return _usbDeviceClasses.TryGetValue(normalizedCode, out var deviceClass) ? deviceClass : null;
        }

        public string? GetUsbDeviceClassFromName(string? deviceName)
        {
            if (string.IsNullOrWhiteSpace(deviceName) || _commonUsbDevices == null)
                return null;

            var deviceNameLower = deviceName.ToLowerInvariant();
            foreach (var commonDevice in _commonUsbDevices)
            {
                if (deviceNameLower.Contains(commonDevice.Key))
                {
                    // Extract class from the JSON element
                    if (commonDevice.Value is JsonElement element && 
                        element.TryGetProperty("class", out var classElement))
                    {
                        return classElement.GetString();
                    }
                }
            }

            return null;
        }

        // General utility methods
        public bool IsDatasetLoaded => _manufacturers != null && _memoryTypes != null && _chassisTypes != null && _usbDeviceClasses != null;

        public Dictionary<string, int> GetDatasetStatistics()
        {
            return new Dictionary<string, int>
            {
                ["Manufacturers"] = _manufacturers?.Count ?? 0,
                ["MemoryTypes"] = _memoryTypes?.Count ?? 0,
                ["FormFactors"] = _formFactors?.Count ?? 0,
                ["ChassisTypes"] = _chassisTypes?.Count ?? 0,
                ["UsbDeviceClasses"] = _usbDeviceClasses?.Count ?? 0
            };
        }
    }
}