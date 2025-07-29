using System.Management;
using System.Runtime.Versioning;
using HardwareVault.Core.Models;

namespace HardwareVault.Core.Services
{
    [SupportedOSPlatform("windows")]
    public class GPUProvider
    {
        public async Task<List<GPUInfo>> GetGPUInfoAsync()
        {
            return await Task.FromResult(GetGPUInfo()).ConfigureAwait(false);
        }

        public List<GPUInfo> GetGPUInfo()
        {
            var gpuDevices = new List<GPUInfo>();

            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
                {
                    foreach (ManagementObject gpu in searcher.Get())
                    {
                        var gpuInfo = new GPUInfo
                        {
                            Name = gpu["Name"]?.ToString()?.Trim(),
                            Manufacturer = gpu["AdapterCompatibility"]?.ToString(),
                            DriverVersion = gpu["DriverVersion"]?.ToString(),
                            DriverDate = ParseDriverDate(gpu["DriverDate"]?.ToString()),
                            VideoProcessor = gpu["VideoProcessor"]?.ToString(),
                            DeviceId = gpu["DeviceID"]?.ToString(),
                            Status = gpu["Status"]?.ToString()
                        };

                        // Memory
                        if (uint.TryParse(gpu["AdapterRAM"]?.ToString(), out uint adapterRam) && adapterRam > 0)
                        {
                            gpuInfo.MemoryMB = adapterRam / (1024 * 1024);
                            gpuInfo.MemoryGB = Math.Round(gpuInfo.MemoryMB / 1024.0, 2);
                        }

                        // Resolution
                        if (uint.TryParse(gpu["CurrentHorizontalResolution"]?.ToString(), out uint width) &&
                            uint.TryParse(gpu["CurrentVerticalResolution"]?.ToString(), out uint height))
                        {
                            gpuInfo.CurrentResolution = $"{width}x{height}";
                        }

                        // Refresh rate
                        if (uint.TryParse(gpu["CurrentRefreshRate"]?.ToString(), out uint refreshRate))
                        {
                            gpuInfo.RefreshRate = refreshRate;
                        }

                        // Determine GPU type
                        gpuInfo.Type = DetermineGpuType(gpuInfo.Name, gpuInfo.Manufacturer);

                        // Clean up names
                        CleanupGpuNames(gpuInfo);

                        gpuDevices.Add(gpuInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting GPU information: {ex.Message}");
            }

            return gpuDevices;
        }

        private DateTime? ParseDriverDate(string? dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString))
                return null;

            try
            {
                // WMI date format is typically YYYYMMDDHHMMSS.MMMMMMSZZZ
                if (dateString.Length >= 8)
                {
                    var year = int.Parse(dateString.Substring(0, 4));
                    var month = int.Parse(dateString.Substring(4, 2));
                    var day = int.Parse(dateString.Substring(6, 2));
                    
                    return new DateTime(year, month, day);
                }
            }
            catch
            {
                // Ignore parsing errors
            }

            return null;
        }

        private string DetermineGpuType(string? name, string? manufacturer)
        {
            var nameLower = name?.ToLowerInvariant() ?? "";
            var manufacturerLower = manufacturer?.ToLowerInvariant() ?? "";

            // Integrated Graphics
            if (nameLower.Contains("intel") && (nameLower.Contains("uhd") || nameLower.Contains("iris") || nameLower.Contains("hd graphics")))
                return "Integrated";
            
            if (nameLower.Contains("amd") && (nameLower.Contains("vega") || nameLower.Contains("radeon") && nameLower.Contains("graphics")))
                return "Integrated";

            // Dedicated Graphics
            if (nameLower.Contains("nvidia") || nameLower.Contains("geforce") || nameLower.Contains("quadro") || nameLower.Contains("titan"))
                return "Dedicated";

            if (nameLower.Contains("radeon") && !nameLower.Contains("graphics"))
                return "Dedicated";

            if (manufacturerLower.Contains("nvidia") || manufacturerLower.Contains("amd") || manufacturerLower.Contains("ati"))
                return "Dedicated";

            return "Unknown";
        }

        private void CleanupGpuNames(GPUInfo gpuInfo)
        {
            // Clean up GPU name
            if (!string.IsNullOrWhiteSpace(gpuInfo.Name))
            {
                gpuInfo.Name = gpuInfo.Name
                    .Replace("(R)", "®")
                    .Replace("(TM)", "™")
                    .Replace("(C)", "©")
                    .Trim();

                // Remove duplicate spaces
                while (gpuInfo.Name.Contains("  "))
                {
                    gpuInfo.Name = gpuInfo.Name.Replace("  ", " ");
                }
            }

            // Clean up manufacturer
            if (!string.IsNullOrWhiteSpace(gpuInfo.Manufacturer))
            {
                gpuInfo.Manufacturer = gpuInfo.Manufacturer
                    .Replace("(R)", "®")
                    .Replace("(TM)", "™")
                    .Replace("(C)", "©")
                    .Trim();
            }
        }
    }

    public class GPUInfo
    {
        public string? Name { get; set; }
        public string? Manufacturer { get; set; }
        public string? Type { get; set; } // Integrated, Dedicated, Unknown
        public uint? MemoryMB { get; set; }
        public double? MemoryGB { get; set; }
        public string? DriverVersion { get; set; }
        public DateTime? DriverDate { get; set; }
        public string? VideoProcessor { get; set; }
        public string? DeviceId { get; set; }
        public string? Status { get; set; }
        public string? CurrentResolution { get; set; }
        public uint? RefreshRate { get; set; }
    }
}