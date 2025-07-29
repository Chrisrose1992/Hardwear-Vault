using System.Management;
using HardwareVault.Core.Models;

namespace HardwareVault.Core.Services
{
    public class SystemCollectorService
    {
        private readonly OSInfoProvider _osProvider;
        private readonly BaseboardProvider _baseboardProvider;
        private readonly ChassisProvider _chassisProvider;
        private readonly DetailedMemoryProvider _memoryProvider;
        private readonly UsbDeviceProvider _usbProvider;
        private readonly UserProvider _userProvider;
        private readonly UuidProvider _uuidProvider;
        private readonly SecurityProvider _securityProvider;
        private readonly ActiveUserProvider _activeUserProvider;

        public SystemCollectorService()
        {
            _osProvider = new OSInfoProvider();
            _baseboardProvider = new BaseboardProvider();
            _chassisProvider = new ChassisProvider();
            _memoryProvider = new DetailedMemoryProvider();
            _usbProvider = new UsbDeviceProvider();
            _userProvider = new UserProvider();
            _uuidProvider = new UuidProvider();
            _securityProvider = new SecurityProvider();
            _activeUserProvider = new ActiveUserProvider();
        }

        public async Task<SystemInfo> GetCompleteSystemInfoAsync()
        {
            var systemInfo = new SystemInfo();

            try
            {
                // Start all async operations concurrently for better performance
                var osTask = _osProvider.GetOSInfoAsync();
                var baseboardTask = _baseboardProvider.GetBaseboardInfoAsync();
                var chassisTask = _chassisProvider.GetChassisInfoAsync();
                var usersTask = _userProvider.GetUsersAsync();
                var uuidsTask = _uuidProvider.GetUuidInfoAsync();
                var usbTask = _usbProvider.GetUsbDevicesAsync();
                var manufacturerTask = Task.Run(() => _memoryProvider.GetManufacturerInfo());

                // Wait for all tasks to complete and assign results
                systemInfo.OS = await osTask;
                systemInfo.Baseboard = await baseboardTask;
                systemInfo.Chassis = await chassisTask;
                systemInfo.Users = await usersTask;
                systemInfo.Uuids = await uuidsTask;
                systemInfo.UsbDevices = await usbTask;
                systemInfo.ManufacturerInfo = await manufacturerTask;

                // Get detailed memory information and hardware info
                var detailedMemory = await _memoryProvider.GetDetailedMemoryInfoAsync();
                var basicMemory = GetBasicMemoryInfo();
                
                systemInfo.Hardware = new HardwareInfo
                {
                    Memory = detailedMemory,
                    BasicMemory = basicMemory,
                    Manufacturer = systemInfo.ManufacturerInfo?.SystemManufacturer,
                    Model = systemInfo.Baseboard?.Product,
                    SystemType = DetermineSystemType(systemInfo.Chassis?.ChassisTypeDescription)
                };

                // Get security information if available
                try
                {
                    var securityInfo = _securityProvider.GetSecurityInfo();
                    // Add security info to system info if needed (you can extend SystemInfo model)
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to get security info: {ex.Message}");
                }

                // Add timestamp for when this information was collected
                if (systemInfo.OS?.Installation != null)
                {
                    systemInfo.OS.Installation.LastBootUpTime = systemInfo.OS.Installation.LastBootUpTime;
                }

            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to collect complete system information: {ex.Message}", ex);
            }

            return systemInfo;
        }

        public SystemInfo GetCompleteSystemInfo()
        {
            var systemInfo = new SystemInfo
            {
                OS = _osProvider.GetOSInfo(),
                Baseboard = _baseboardProvider.GetBaseboardInfo(),
                Chassis = _chassisProvider.GetChassisInfo(),
                Users = _userProvider.GetUsers(),
                Uuids = _uuidProvider.GetUuidInfo(),
                UsbDevices = _usbProvider.GetUsbDevices(),
                ManufacturerInfo = _memoryProvider.GetManufacturerInfo()
            };

            // Get detailed memory information
            var detailedMemory = _memoryProvider.GetDetailedMemoryInfo();
            var basicMemory = GetBasicMemoryInfo();
            
            systemInfo.Hardware = new HardwareInfo
            {
                Memory = detailedMemory,
                BasicMemory = basicMemory,
                Manufacturer = systemInfo.ManufacturerInfo?.SystemManufacturer,
                Model = systemInfo.Baseboard?.Product,
                SystemType = DetermineSystemType(systemInfo.Chassis?.ChassisTypeDescription)
            };

            return systemInfo;
        }

        private MemoryInfo GetBasicMemoryInfo()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize, FreePhysicalMemory, TotalVirtualMemorySize, FreeVirtualMemory FROM Win32_OperatingSystem"))
                {
                    foreach (ManagementObject os in searcher.Get())
                    {
                        var memoryInfo = new MemoryInfo();

                        if (uint.TryParse(os["TotalVisibleMemorySize"]?.ToString(), out uint totalVisible))
                            memoryInfo.TotalVisibleMemorySize = totalVisible;

                        if (uint.TryParse(os["FreePhysicalMemory"]?.ToString(), out uint freePhysical))
                            memoryInfo.FreePhysicalMemory = freePhysical;

                        if (uint.TryParse(os["TotalVirtualMemorySize"]?.ToString(), out uint totalVirtual))
                            memoryInfo.TotalVirtualMemorySize = totalVirtual;

                        if (uint.TryParse(os["FreeVirtualMemory"]?.ToString(), out uint freeVirtual))
                            memoryInfo.FreeVirtualMemory = freeVirtual;

                        // Calculate memory usage percentage
                        if (memoryInfo.TotalVisibleMemorySize.HasValue && memoryInfo.FreePhysicalMemory.HasValue)
                        {
                            var usedMemory = memoryInfo.TotalVisibleMemorySize.Value - memoryInfo.FreePhysicalMemory.Value;
                            memoryInfo.MemoryUsagePercentage = Math.Round(
                                (double)usedMemory / memoryInfo.TotalVisibleMemorySize.Value * 100, 2);
                        }

                        return memoryInfo;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting basic memory info: {ex.Message}");
            }

            return new MemoryInfo();
        }

        private string? DetermineSystemType(string? chassisType)
        {
            if (string.IsNullOrWhiteSpace(chassisType))
                return "Unknown";

            var type = chassisType.ToLowerInvariant();

            if (type.Contains("laptop") || type.Contains("notebook") || type.Contains("portable"))
                return "Laptop";
            else if (type.Contains("desktop") || type.Contains("tower") || type.Contains("mini tower"))
                return "Desktop";
            else if (type.Contains("server") || type.Contains("rack"))
                return "Server";
            else if (type.Contains("tablet"))
                return "Tablet";
            else if (type.Contains("all in one"))
                return "All-in-One";
            else if (type.Contains("workstation"))
                return "Workstation";
            else
                return chassisType;
        }

        public async Task<Dictionary<string, object>> GetSystemSummaryAsync()
        {
            var systemInfo = await GetCompleteSystemInfoAsync();
            
            return new Dictionary<string, object>
            {
                ["SystemManufacturer"] = systemInfo.ManufacturerInfo?.SystemManufacturer ?? "Unknown",
                ["SystemModel"] = systemInfo.Hardware?.Model ?? "Unknown",
                ["SystemType"] = systemInfo.Hardware?.SystemType ?? "Unknown",
                ["ChassisType"] = systemInfo.Chassis?.ChassisTypeDescription ?? "Unknown",
                ["OperatingSystem"] = $"{systemInfo.OS?.Name} {systemInfo.OS?.Version}".Trim(),
                ["TotalMemoryGB"] = Math.Round((systemInfo.Hardware?.Memory?.InstalledMemoryMB ?? 0) / 1024.0, 1),
                ["MemoryModules"] = systemInfo.Hardware?.Memory?.MemoryModules?.Count ?? 0,
                ["MemorySlots"] = $"{systemInfo.Hardware?.Memory?.UsedMemorySlots ?? 0}/{systemInfo.Hardware?.Memory?.TotalMemorySlots ?? 0}",
                ["ChipsetModel"] = systemInfo.Baseboard?.PciSlotInfo?.Model ?? "Unknown",
                ["PCIeVersion"] = systemInfo.Baseboard?.PciSlotInfo?.Version ?? "Unknown",
                ["USBVersion"] = systemInfo.Baseboard?.UsbVersion ?? "Unknown",
                ["ConnectedUSBDevices"] = systemInfo.UsbDevices?.Count(d => d.IsConnected == true) ?? 0,
                ["SystemUUID"] = systemInfo.Uuids?.SystemUuid ?? "Unknown",
                ["IsOEM"] = systemInfo.ManufacturerInfo?.IsOEM ?? false,
                ["DatasetEnhanced"] = true
            };
        }
    }
}