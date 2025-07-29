namespace HardwareVault.Core.Services
{
    public interface IHardwareDatasetService
    {
        // Manufacturer methods
        string? GetManufacturerName(string? manufacturerId);
        bool IsKnownManufacturer(string? manufacturerName);

        // Memory methods
        string? GetMemoryTypeName(uint? memoryTypeCode);
        string? GetFormFactorName(uint? formFactorCode);
        string? MapMemoryType(string? memoryTypeString);

        // Chassis methods
        string? GetChassisTypeName(uint? chassisTypeCode);
        string GetChassisType(ushort chassisTypeCode);

        // USB methods
        string? GetUsbDeviceClassName(string? deviceClassCode);
        string? GetUsbDeviceClassFromName(string? deviceName);

        // General utility methods
        bool IsDatasetLoaded { get; }
        Dictionary<string, int> GetDatasetStatistics();
    }
}