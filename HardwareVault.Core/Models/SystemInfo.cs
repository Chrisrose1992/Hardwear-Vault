namespace HardwareVault.Core.Models
{
    public class SystemResponse
    {
        public SystemInfo? System { get; set; }
    }

    public class SystemInfo
    {
        public OSInfo? OS { get; set; }
        public HardwareInfo? Hardware { get; set; }
        public BaseboardInfo? Baseboard { get; set; }
        public ChassisInfo? Chassis { get; set; }
        public List<UserInfo>? Users { get; set; }
        public UuidInfo? Uuids { get; set; }
        public List<UsbDeviceInfo>? UsbDevices { get; set; }
        public ManufacturerInfo? ManufacturerInfo { get; set; }
        
        // New enhanced components
        public CPUInfo? CPU { get; set; }
        public List<GPUInfo>? GPUs { get; set; }
        public List<StorageInfo>? Storage { get; set; }
        public NetworkInfo? Network { get; set; }
        public SecurityInfo? Security { get; set; }
        public SystemPerformanceInfo? Performance { get; set; }
    }

    // Enhanced HardwareInfo to include new components
    public class HardwareInfo
    {
        public DetailedMemoryInfo? Memory { get; set; } 
        public string? Manufacturer { get; set; }
        public string? Model { get; set; }
        public string? SystemType { get; set; }
        public MemoryInfo? BasicMemory { get; set; }
        public PowerInfo? Power { get; set; }
        public TemperatureInfo? Temperature { get; set; }
    }

    // New models for enhanced system information
    public class NetworkInfo
    {
        public List<NetworkAdapterInfo>? Adapters { get; set; }
        public string? PrimaryAdapter { get; set; }
        public string? PublicIP { get; set; }
        public string? LocalIP { get; set; }
        public string? MACAddress { get; set; }
        public bool? InternetConnected { get; set; }
    }

    public class NetworkAdapterInfo
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? MACAddress { get; set; }
        public string? IPAddress { get; set; }
        public string? SubnetMask { get; set; }
        public string? DefaultGateway { get; set; }
        public string? AdapterType { get; set; }
        public bool? IsConnected { get; set; }
        public ulong? Speed { get; set; }
    }

    public class SystemPerformanceInfo
    {
        public double? CPUUsagePercentage { get; set; }
        public double? MemoryUsagePercentage { get; set; }
        public double? DiskUsagePercentage { get; set; }
        public uint? ProcessCount { get; set; }
        public uint? ThreadCount { get; set; }
        public TimeSpan? SystemUptime { get; set; }
        public DateTime? LastBootTime { get; set; }
    }

    public class PowerInfo
    {
        public bool? IsOnBattery { get; set; }
        public uint? BatteryPercentage { get; set; }
        public string? PowerScheme { get; set; }
        public bool? BatteryPresent { get; set; }
        public string? BatteryStatus { get; set; }
    }

    public class TemperatureInfo
    {
        public double? CPUTemperature { get; set; }
        public double? GPUTemperature { get; set; }
        public double? SystemTemperature { get; set; }
        public List<SensorInfo>? Sensors { get; set; }
    }

    public class SensorInfo
    {
        public string? Name { get; set; }
        public string? Type { get; set; }
        public double? Value { get; set; }
        public string? Unit { get; set; }
    }

    // Existing models (keeping for compatibility)
    public class OSInfo
    {
        public string? Name { get; set; }
        public string? Version { get; set; }
        public BuildInfo? Build { get; set; }
        public InstallationInfo? Installation { get; set; }
        public string? RegisteredUser { get; set; }
        public string? Organization { get; set; }
        public string? SerialNumber { get; set; }
        public string? ProductKey { get; set; }
        public string? SystemDirectory { get; set; }
        public string? WindowsDirectory { get; set; }
        public string? Locale { get; set; }
        public string? TimeZone { get; set; }
        public ProcessInfo? Processes { get; set; }
        public bool? IsHypervisorPresent { get; set; }
    }

    public class BuildInfo
    {
        public string? Number { get; set; }
        public string? Architecture { get; set; }
        public string? ServicePack { get; set; }
    }

    public class InstallationInfo
    {
        public DateTime? InstallDate { get; set; }
        public DateTime? LastBootUpTime { get; set; }
        public TimeSpan? Uptime { get; set; }
    }

    public class PciSlotInfo
    {
        public string? Model { get; set; }
        public string? Version { get; set; }
        public List<PciSlot>? AvailableSlots { get; set; }
        public int? ReleaseYear { get; set; }
    }

    public class MemoryInfo
    {
        public uint? TotalVisibleMemorySize { get; set; }
        public uint? FreePhysicalMemory { get; set; }
        public uint? TotalVirtualMemorySize { get; set; }
        public uint? FreeVirtualMemory { get; set; }
        public double? MemoryUsagePercentage { get; set; }
    }

    public class DetailedMemoryInfo
    {
        public uint? InstalledMemoryMB { get; set; }
        public uint? MaxMemoryCapacityMB { get; set; }
        public int? TotalMemorySlots { get; set; }
        public int UsedMemorySlots { get; set; }
        public List<MemoryModule>? MemoryModules { get; set; }
        public string? MemoryArchitecture { get; set; }
        public uint? MemorySpeed { get; set; }
    }

    public class MemoryModule
    {
        public string? DeviceLocator { get; set; }
        public string? BankLabel { get; set; }
        public uint? Capacity { get; set; }
        public string? MemoryType { get; set; }
        public string? FormFactor { get; set; }
        public uint? Speed { get; set; }
        public string? Manufacturer { get; set; }
        public string? PartNumber { get; set; }
        public string? SerialNumber { get; set; }
        public uint? ConfiguredSpeed { get; set; }
        public uint? ConfiguredVoltage { get; set; }
        public uint? MinVoltage { get; set; }
        public uint? MaxVoltage { get; set; }
    }

    public class BaseboardInfo
    {
        public string? Manufacturer { get; set; }
        public string? Product { get; set; }
        public string? SerialNumber { get; set; }
        public string? Version { get; set; }
        public string? Model { get; set; }
        public PciSlotInfo? PciSlotInfo { get; set; }
        public string? UsbVersion { get; set; }
    }

    public class ChassisInfo
    {
        public string? Manufacturer { get; set; }
        public string? SerialNumber { get; set; }
        public string? ChassisType { get; set; }
        public string? ChassisTypeDescription { get; set; }
        public uint? ChassisTypeCode { get; set; }
        public string? Model { get; set; }
        public string? AssetTag { get; set; }
        public string? SKU { get; set; }
        public bool? BootupState { get; set; }
        public bool? PowerSupplyState { get; set; }
        public bool? ThermalState { get; set; }
        public uint? NumberOfPowerCords { get; set; }
    }

    public class ActiveUserInfo
    {
        public string? Name { get; set; }
        public string? FullName { get; set; }
        public string? Domain { get; set; }
        public DateTime? LoginTime { get; set; }
        public string? SessionType { get; set; }
        public string? HomeDirectory { get; set; }
    }

    public class UserInfo
    {
        public string? Name { get; set; }
        public string? FullName { get; set; }
        public string? Description { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsLocked { get; set; }
        public DateTime? LastLogin { get; set; }
        public string? HomeDirectory { get; set; }
    }

    public class UuidInfo
    {
        public string? SystemUuid { get; set; }
        public string? BaseboardUuid { get; set; }
        public string? ChassisUuid { get; set; }
    }

    public class ProcessInfo
    {
        public uint? NumberOfProcesses { get; set; }
        public uint? NumberOfUsers { get; set; }
    }

    public class SecurityInfo
    {
        public bool? FirewallEnabled { get; set; }
        public bool? AntivirusEnabled { get; set; }
        public string? SecurityCenter { get; set; }
        public bool? BitLockerEnabled { get; set; }
        public bool? UacEnabled { get; set; }
    }

    public class UsbDeviceInfo
    {
        public string? DeviceId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Manufacturer { get; set; }
        public string? DeviceClass { get; set; }
        public string? DeviceClassDescription { get; set; }
        public string? VendorId { get; set; }
        public string? ProductId { get; set; }
        public string? Version { get; set; }
        public string? SerialNumber { get; set; }
        public bool? IsConnected { get; set; }
        public DateTime? LastConnected { get; set; }
    }

    public class ManufacturerInfo
    {
        public string? SystemManufacturer { get; set; }
        public string? BaseboardManufacturer { get; set; }
        public string? ChassisManufacturer { get; set; }
        public string? MemoryManufacturers { get; set; }
        public bool? IsOEM { get; set; }
        public bool? IsCustomBuild { get; set; }
        public string? SystemIntegrator { get; set; }
    }

    // Chipset dataset models
    public class ChipsetDataset
    {
        public string? Manufacturer { get; set; }
        public List<ChipsetInfo>? Chipsets { get; set; }
    }

    public class ChipsetInfo
    {
        public string? Model { get; set; }
        public int ReleaseYear { get; set; }
        public string? PciVersion { get; set; }
        public List<PciSlot>? Slots { get; set; }
    }

    public class PciSlot
    {
        public string? Type { get; set; }
        public string? SpeedPerLane { get; set; }
        public string? TotalBandwidth { get; set; }
    }

    // New component models
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

    public class StorageInfo
    {
        public string? Model { get; set; }
        public string? Manufacturer { get; set; }
        public string? SerialNumber { get; set; }
        public string? InterfaceType { get; set; }
        public string? MediaType { get; set; }
        public string? FirmwareRevision { get; set; }
        public string? DriveType { get; set; }
        public ulong? SizeBytes { get; set; }
        public double? SizeGB { get; set; }
        public List<PartitionInfo>? Partitions { get; set; }
    }

    public class PartitionInfo
    {
        public string? DriveLetter { get; set; }
        public string? Label { get; set; }
        public string? FileSystem { get; set; }
        public string? DriveType { get; set; }
        public double? TotalSizeGB { get; set; }
        public double? FreeSizeGB { get; set; }
        public double? UsagePercentage { get; set; }
    }
}