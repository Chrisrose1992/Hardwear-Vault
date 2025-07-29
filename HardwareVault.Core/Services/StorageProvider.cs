using System.Management;
using System.Runtime.Versioning;
using HardwareVault.Core.Models;

namespace HardwareVault.Core.Services
{
    [SupportedOSPlatform("windows")]
    public class StorageProvider
    {
        public async Task<List<StorageInfo>> GetStorageInfoAsync()
        {
            return await Task.FromResult(GetStorageInfo()).ConfigureAwait(false);
        }

        public List<StorageInfo> GetStorageInfo()
        {
            var storageDevices = new List<StorageInfo>();

            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive"))
                {
                    foreach (ManagementObject disk in searcher.Get())
                    {
                        var storage = new StorageInfo
                        {
                            Model = disk["Model"]?.ToString()?.Trim(),
                            Manufacturer = disk["Manufacturer"]?.ToString(),
                            SerialNumber = disk["SerialNumber"]?.ToString()?.Trim(),
                            InterfaceType = disk["InterfaceType"]?.ToString(),
                            MediaType = disk["MediaType"]?.ToString(),
                            FirmwareRevision = disk["FirmwareRevision"]?.ToString()?.Trim()
                        };

                        // Size
                        if (ulong.TryParse(disk["Size"]?.ToString(), out ulong size))
                        {
                            storage.SizeBytes = size;
                            storage.SizeGB = Math.Round(size / (1024.0 * 1024.0 * 1024.0), 2);
                        }

                        // Determine drive type
                        storage.DriveType = DetermineDriveType(storage.Model, storage.InterfaceType, storage.MediaType);

                        // Clean up model name
                        if (!string.IsNullOrWhiteSpace(storage.Model))
                        {
                            storage.Model = storage.Model.Replace("\\", "").Trim();
                        }

                        storageDevices.Add(storage);
                    }
                }

                // Get partition information
                foreach (var storage in storageDevices)
                {
                    storage.Partitions = GetPartitionInfo(storage.Model);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting storage information: {ex.Message}");
            }

            return storageDevices;
        }

        private string DetermineDriveType(string? model, string? interfaceType, string? mediaType)
        {
            var modelLower = model?.ToLowerInvariant() ?? "";
            var interfaceLower = interfaceType?.ToLowerInvariant() ?? "";
            var mediaLower = mediaType?.ToLowerInvariant() ?? "";

            // SSD Detection
            if (modelLower.Contains("ssd") ||
                modelLower.Contains("nvme") ||
                modelLower.Contains("solid state") ||
                interfaceLower.Contains("nvme") ||
                mediaLower.Contains("ssd"))
            {
                if (interfaceLower.Contains("nvme") || modelLower.Contains("nvme"))
                    return "NVMe SSD";
                else
                    return "SATA SSD";
            }

            // HDD Detection
            if (mediaLower.Contains("fixed hard disk") ||
                modelLower.Contains("hdd") ||
                modelLower.Contains("hard disk"))
            {
                return "HDD";
            }

            // USB/External
            if (interfaceLower.Contains("usb"))
            {
                return "USB Drive";
            }

            return "Unknown";
        }

        private List<PartitionInfo>? GetPartitionInfo(string? diskModel)
        {
            var partitions = new List<PartitionInfo>();

            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_LogicalDisk"))
                {
                    foreach (ManagementObject disk in searcher.Get())
                    {
                        var partition = new PartitionInfo
                        {
                            DriveLetter = disk["DeviceID"]?.ToString(),
                            Label = disk["VolumeName"]?.ToString(),
                            FileSystem = disk["FileSystem"]?.ToString(),
                            DriveType = GetDriveTypeName(disk["DriveType"]?.ToString())
                        };

                        if (ulong.TryParse(disk["Size"]?.ToString(), out ulong totalSize))
                        {
                            partition.TotalSizeGB = Math.Round(totalSize / (1024.0 * 1024.0 * 1024.0), 2);
                        }

                        if (ulong.TryParse(disk["FreeSpace"]?.ToString(), out ulong freeSize))
                        {
                            partition.FreeSizeGB = Math.Round(freeSize / (1024.0 * 1024.0 * 1024.0), 2);
                            if (partition.TotalSizeGB > 0)
                            {
                                partition.UsagePercentage = Math.Round(((partition.TotalSizeGB - partition.FreeSizeGB) / partition.TotalSizeGB) * 100, 1);
                            }
                        }

                        partitions.Add(partition);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting partition info: {ex.Message}");
            }

            return partitions.Any() ? partitions : null;
        }
    }
}