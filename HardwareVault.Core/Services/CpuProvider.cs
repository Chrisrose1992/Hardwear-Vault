using System.Management;
using System.Runtime.Versioning;
using HardwareVault.Core.Models;

namespace HardwareVault.Core.Services
{
    [SupportedOSPlatform("windows")]
    public class CPUProvider
    {
        public async Task<CPUInfo> GetCPUInfoAsync()
        {
            return await Task.FromResult(GetCPUInfo()).ConfigureAwait(false);
        }

        public CPUInfo GetCPUInfo()
        {
            var cpuInfo = new CPUInfo();

            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
                {
                    foreach (ManagementObject processor in searcher.Get())
                    {
                        cpuInfo.Name = processor["Name"]?.ToString()?.Trim();
                        cpuInfo.Manufacturer = processor["Manufacturer"]?.ToString();
                        cpuInfo.MaxClockSpeed = uint.TryParse(processor["MaxClockSpeed"]?.ToString(), out uint maxClock) ? maxClock : null;
                        cpuInfo.CurrentClockSpeed = uint.TryParse(processor["CurrentClockSpeed"]?.ToString(), out uint currentClock) ? currentClock : null;
                        cpuInfo.NumberOfCores = uint.TryParse(processor["NumberOfCores"]?.ToString(), out uint cores) ? cores : null;
                        cpuInfo.NumberOfLogicalProcessors = uint.TryParse(processor["NumberOfLogicalProcessors"]?.ToString(), out uint logical) ? logical : null;
                        cpuInfo.Architecture = processor["Architecture"]?.ToString();
                        cpuInfo.Family = processor["Family"]?.ToString();
                        cpuInfo.Model = processor["Model"]?.ToString();
                        cpuInfo.Stepping = processor["Stepping"]?.ToString();
                        cpuInfo.ProcessorId = processor["ProcessorId"]?.ToString();
                        cpuInfo.L2CacheSize = uint.TryParse(processor["L2CacheSize"]?.ToString(), out uint l2Cache) ? l2Cache : null;
                        cpuInfo.L3CacheSize = uint.TryParse(processor["L3CacheSize"]?.ToString(), out uint l3Cache) ? l3Cache : null;
                        
                        // Determine architecture
                        if (uint.TryParse(processor["Architecture"]?.ToString(), out uint archCode))
                        {
                            cpuInfo.Architecture = GetArchitectureName(archCode);
                        }

                        // Clean up CPU name
                        if (!string.IsNullOrWhiteSpace(cpuInfo.Name))
                        {
                            cpuInfo.Name = cpuInfo.Name.Replace("(R)", "®").Replace("(TM)", "™").Trim();
                            while (cpuInfo.Name.Contains("  "))
                            {
                                cpuInfo.Name = cpuInfo.Name.Replace("  ", " ");
                            }
                        }

                        break; // Usually only one CPU
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve CPU information: {ex.Message}", ex);
            }

            return cpuInfo;
        }

        private string GetArchitectureName(uint architecture)
        {
            return architecture switch
            {
                0 => "x86",
                1 => "MIPS",
                2 => "Alpha",
                3 => "PowerPC",
                5 => "ARM",
                6 => "Itanium",
                9 => "x64",
                12 => "ARM64",
                _ => $"Architecture {architecture}"
            };
        }
    }

    public class CPUInfo
    {
        public string? Name { get; set; }
        public string? Manufacturer { get; set; }
        public uint? MaxClockSpeed { get; set; }
        public uint? CurrentClockSpeed { get; set; }
        public uint? NumberOfCores { get; set; }
        public uint? NumberOfLogicalProcessors { get; set; }
        public string? Architecture { get; set; }
        public string? Family { get; set; }
        public string? Model { get; set; }
        public string? Stepping { get; set; }
        public string? ProcessorId { get; set; }
        public uint? L2CacheSize { get; set; }
        public uint? L3CacheSize { get; set; }
    }
}