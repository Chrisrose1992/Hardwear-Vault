using System.Management;
using System.Runtime.Versioning;
using HardwareVault.Core.Models;

namespace HardwareVault.Core.Services
{
    [SupportedOSPlatform("windows")]
    public class DetailedMemoryProvider
    {
        private readonly HardwareDatasetService _datasetService;

        public DetailedMemoryProvider()
        {
            _datasetService = new HardwareDatasetService();
        }

        public async Task<DetailedMemoryInfo> GetDetailedMemoryInfoAsync()
        {
            return await Task.FromResult(GetDetailedMemoryInfo()).ConfigureAwait(false);
        }

        public DetailedMemoryInfo GetDetailedMemoryInfo()
        {
            var memoryInfo = new DetailedMemoryInfo
            {
                MemoryModules = new List<MemoryModule>()
            };

            try
            {
                GetBasicMemoryInfo(memoryInfo);
                GetMemoryModules(memoryInfo);
                CalculateMemoryStatistics(memoryInfo);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve detailed memory information: {ex.Message}", ex);
            }

            return memoryInfo;
        }

        private void GetBasicMemoryInfo(DetailedMemoryInfo memoryInfo)
        {
            try
            {
                // Get total physical memory
                using (var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem"))
                {
                    foreach (ManagementObject system in searcher.Get())
                    {
                        if (ulong.TryParse(system["TotalPhysicalMemory"]?.ToString(), out ulong totalMemory))
                        {
                            memoryInfo.InstalledMemoryMB = (uint)(totalMemory / (1024 * 1024));
                        }
                        break;
                    }
                }

                // Get memory array information
                using (var searcher = new ManagementObjectSearcher("SELECT MaxCapacity, MemoryDevices FROM Win32_PhysicalMemoryArray"))
                {
                    foreach (ManagementObject memArray in searcher.Get())
                    {
                        if (uint.TryParse(memArray["MaxCapacity"]?.ToString(), out uint maxCapacity))
                        {
                            memoryInfo.MaxMemoryCapacityMB = maxCapacity / 1024; // Convert KB to MB
                        }

                        if (int.TryParse(memArray["MemoryDevices"]?.ToString(), out int totalSlots))
                        {
                            memoryInfo.TotalMemorySlots = totalSlots;
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting basic memory info: {ex.Message}");
            }
        }

        private void GetMemoryModules(DetailedMemoryInfo memoryInfo)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT DeviceLocator, BankLabel, Capacity, MemoryType, FormFactor, Speed, Manufacturer, PartNumber, SerialNumber, ConfiguredClockSpeed, ConfiguredVoltage, MinVoltage, MaxVoltage, TypeDetail FROM Win32_PhysicalMemory"))
                {
                    foreach (ManagementObject memory in searcher.Get())
                    {
                        var module = new MemoryModule
                        {
                            DeviceLocator = memory["DeviceLocator"]?.ToString(),
                            BankLabel = memory["BankLabel"]?.ToString(),
                            Manufacturer = memory["Manufacturer"]?.ToString(),
                            PartNumber = memory["PartNumber"]?.ToString(),
                            SerialNumber = memory["SerialNumber"]?.ToString()
                        };

                        // Capacity
                        if (ulong.TryParse(memory["Capacity"]?.ToString(), out ulong capacity))
                        {
                            module.Capacity = (uint)(capacity / (1024 * 1024)); // Convert bytes to MB
                        }

                        // Enhanced Memory Type Detection
                        if (uint.TryParse(memory["MemoryType"]?.ToString(), out uint memoryType))
                        {
                            module.MemoryType = GetEnhancedMemoryType(memoryType, module.DeviceLocator);
                        }
                        else
                        {
                            // Fallback: derive from device locator
                            module.MemoryType = DeriveMemoryTypeFromLocator(module.DeviceLocator);
                        }

                        // Form Factor
                        if (uint.TryParse(memory["FormFactor"]?.ToString(), out uint formFactor))
                        {
                            module.FormFactor = _datasetService.GetFormFactorName(formFactor) ?? $"Form Factor {formFactor}";
                        }

                        // Speed
                        if (uint.TryParse(memory["Speed"]?.ToString(), out uint speed))
                        {
                            module.Speed = speed;
                        }

                        // Configured Speed
                        if (uint.TryParse(memory["ConfiguredClockSpeed"]?.ToString(), out uint configuredSpeed))
                        {
                            module.ConfiguredSpeed = configuredSpeed;
                        }

                        // Voltages
                        if (uint.TryParse(memory["ConfiguredVoltage"]?.ToString(), out uint configuredVoltage))
                        {
                            module.ConfiguredVoltage = configuredVoltage;
                        }

                        if (uint.TryParse(memory["MinVoltage"]?.ToString(), out uint minVoltage))
                        {
                            module.MinVoltage = minVoltage;
                        }

                        if (uint.TryParse(memory["MaxVoltage"]?.ToString(), out uint maxVoltage))
                        {
                            module.MaxVoltage = maxVoltage;
                        }

                        // Enhanced manufacturer name using dataset
                        if (!string.IsNullOrWhiteSpace(module.Manufacturer))
                        {
                            var enhancedManufacturer = _datasetService.GetManufacturerName(module.Manufacturer);
                            if (!string.IsNullOrWhiteSpace(enhancedManufacturer))
                            {
                                module.Manufacturer = enhancedManufacturer;
                            }
                        }

                        memoryInfo.MemoryModules?.Add(module);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting memory modules: {ex.Message}");
            }
        }

        private string GetEnhancedMemoryType(uint memoryTypeCode, string? deviceLocator)
        {
            // First try dataset lookup
            var datasetType = _datasetService.GetMemoryTypeName(memoryTypeCode);
            if (!string.IsNullOrWhiteSpace(datasetType) && datasetType != "Unknown")
            {
                return datasetType;
            }

            // Enhanced detection based on device locator and common patterns
            if (!string.IsNullOrWhiteSpace(deviceLocator))
            {
                var locator = deviceLocator.ToUpperInvariant();
                
                if (locator.StartsWith("DDR5"))
                    return "DDR5";
                else if (locator.StartsWith("DDR4"))
                    return "DDR4";
                else if (locator.StartsWith("DDR3"))
                    return "DDR3";
                else if (locator.StartsWith("DDR2"))
                    return "DDR2";
                else if (locator.StartsWith("DDR"))
                    return "DDR";
            }

            // Fallback based on memory type code
            return memoryTypeCode switch
            {
                20 => "DDR",
                21 => "DDR2", 
                24 => "DDR3",
                26 => "DDR4",
                30 => "DDR5",
                _ => $"Type {memoryTypeCode}"
            };
        }

        private string DeriveMemoryTypeFromLocator(string? deviceLocator)
        {
            if (string.IsNullOrWhiteSpace(deviceLocator))
                return "Unknown";

            var locator = deviceLocator.ToUpperInvariant();
            
            if (locator.StartsWith("DDR5"))
                return "DDR5";
            else if (locator.StartsWith("DDR4"))
                return "DDR4";
            else if (locator.StartsWith("DDR3"))
                return "DDR3";
            else if (locator.StartsWith("DDR2"))
                return "DDR2";
            else if (locator.StartsWith("DDR"))
                return "DDR";
            else
                return "Unknown";
        }

        private void CalculateMemoryStatistics(DetailedMemoryInfo memoryInfo)
        {
            if (memoryInfo.MemoryModules?.Any() == true)
            {
                memoryInfo.UsedMemorySlots = memoryInfo.MemoryModules.Count;

                // Calculate memory speed (use the slowest module speed)
                var speeds = memoryInfo.MemoryModules
                    .Where(m => m.Speed.HasValue && m.Speed > 0)
                    .Select(m => m.Speed!.Value)
                    .ToList();

                if (speeds.Any())
                {
                    memoryInfo.MemorySpeed = speeds.Min();
                }

                // Determine memory architecture based on memory types
                var memoryTypes = memoryInfo.MemoryModules
                    .Where(m => !string.IsNullOrWhiteSpace(m.MemoryType))
                    .Select(m => m.MemoryType!)
                    .Distinct()
                    .ToList();

                if (memoryTypes.Any())
                {
                    memoryInfo.MemoryArchitecture = string.Join(", ", memoryTypes);
                }

                // Estimate total slots if not detected
                if (memoryInfo.TotalMemorySlots == null || memoryInfo.TotalMemorySlots == 0)
                {
                    var usedSlots = memoryInfo.UsedMemorySlots;
                    if (usedSlots <= 2) memoryInfo.TotalMemorySlots = 4;
                    else if (usedSlots <= 4) memoryInfo.TotalMemorySlots = 4;
                    else if (usedSlots <= 6) memoryInfo.TotalMemorySlots = 8;
                    else memoryInfo.TotalMemorySlots = 8;
                }
            }
        }

        public ManufacturerInfo GetManufacturerInfo()
        {
            var manufacturerInfo = new ManufacturerInfo();

            try
            {
                // Get system manufacturer
                using (var searcher = new ManagementObjectSearcher("SELECT Manufacturer FROM Win32_ComputerSystem"))
                {
                    foreach (ManagementObject system in searcher.Get())
                    {
                        manufacturerInfo.SystemManufacturer = system["Manufacturer"]?.ToString();
                        break;
                    }
                }

                // Get baseboard manufacturer
                using (var searcher = new ManagementObjectSearcher("SELECT Manufacturer FROM Win32_BaseBoard"))
                {
                    foreach (ManagementObject baseboard in searcher.Get())
                    {
                        manufacturerInfo.BaseboardManufacturer = baseboard["Manufacturer"]?.ToString();
                        break;
                    }
                }

                // Get chassis manufacturer
                using (var searcher = new ManagementObjectSearcher("SELECT Manufacturer FROM Win32_SystemEnclosure"))
                {
                    foreach (ManagementObject chassis in searcher.Get())
                    {
                        var manufacturer = chassis["Manufacturer"]?.ToString();
                        // Filter out placeholder values
                        if (!string.IsNullOrWhiteSpace(manufacturer) && 
                            !manufacturer.Equals("Default string", StringComparison.OrdinalIgnoreCase))
                        {
                            manufacturerInfo.ChassisManufacturer = manufacturer;
                        }
                        else
                        {
                            manufacturerInfo.ChassisManufacturer = "Unknown";
                        }
                        break;
                    }
                }

                // Enhanced memory manufacturers with proper mapping
                var memoryManufacturers = new List<string>();
                using (var searcher = new ManagementObjectSearcher("SELECT Manufacturer FROM Win32_PhysicalMemory"))
                {
                    foreach (ManagementObject memory in searcher.Get())
                    {
                        var manufacturer = memory["Manufacturer"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(manufacturer))
                        {
                            // Try to get enhanced name from dataset
                            var enhancedName = _datasetService.GetManufacturerName(manufacturer);
                            var finalName = enhancedName ?? manufacturer;
                            
                            if (!memoryManufacturers.Contains(finalName))
                            {
                                memoryManufacturers.Add(finalName);
                            }
                        }
                    }
                }
                manufacturerInfo.MemoryManufacturers = string.Join(", ", memoryManufacturers);

                // Determine if it's OEM or custom build
                var systemManufacturer = manufacturerInfo.SystemManufacturer?.ToLowerInvariant();
                var oemManufacturers = new[] { "dell", "hp", "lenovo", "asus", "acer", "msi", "sony", "toshiba", "samsung", "apple" };
                manufacturerInfo.IsOEM = !string.IsNullOrWhiteSpace(systemManufacturer) && 
                                       oemManufacturers.Any(oem => systemManufacturer.Contains(oem));
                
                manufacturerInfo.IsCustomBuild = !manufacturerInfo.IsOEM;

                if (manufacturerInfo.IsCustomBuild == true)
                {
                    manufacturerInfo.SystemIntegrator = "Custom Build";
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting manufacturer info: {ex.Message}");
            }

            return manufacturerInfo;
        }
    }
}