using System.Text.Json;
using HardwareVault.Core.Models;

namespace HardwareVault.Core.Services
{
    public class ChipsetDatasetService
    {
        private readonly List<ChipsetDataset> _datasets;
        private readonly string _datasetPath;

        public ChipsetDatasetService()
        {
            _datasetPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dataset");
            _datasets = new List<ChipsetDataset>();
            LoadDatasets();
        }

        private void LoadDatasets()
        {
            try
            {
                var amdDatasetPath = Path.Combine(_datasetPath, "amd_chipsets.json");
                var intelDatasetPath = Path.Combine(_datasetPath, "intel_chipsets.json");

                if (File.Exists(amdDatasetPath))
                {
                    var amdJson = File.ReadAllText(amdDatasetPath);
                    var amdDataset = JsonSerializer.Deserialize<ChipsetDataset>(amdJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    if (amdDataset != null)
                        _datasets.Add(amdDataset);
                }

                if (File.Exists(intelDatasetPath))
                {
                    var intelJson = File.ReadAllText(intelDatasetPath);
                    var intelDataset = JsonSerializer.Deserialize<ChipsetDataset>(intelJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    if (intelDataset != null)
                        _datasets.Add(intelDataset);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw - fallback to hardcoded values
                Console.WriteLine($"Warning: Failed to load chipset datasets: {ex.Message}");
            }
        }

        public ChipsetInfo? FindChipsetByModel(string model)
        {
            if (string.IsNullOrWhiteSpace(model))
                return null;

            foreach (var dataset in _datasets)
            {
                if (dataset.Chipsets != null)
                {
                    var chipset = dataset.Chipsets.FirstOrDefault(c => 
                        !string.IsNullOrEmpty(c.Model) && 
                        model.Contains(c.Model, StringComparison.OrdinalIgnoreCase));
                    
                    if (chipset != null)
                        return chipset;
                }
            }

            return null;
        }

        public (string? model, string? chipset) ExtractModelAndChipset(string? product)
        {
            if (string.IsNullOrWhiteSpace(product))
                return (null, null);

            // Try to find chipset from dataset first
            foreach (var dataset in _datasets)
            {
                if (dataset.Chipsets != null)
                {
                    foreach (var chipset in dataset.Chipsets)
                    {
                        if (!string.IsNullOrEmpty(chipset.Model) && 
                            product.Contains(chipset.Model, StringComparison.OrdinalIgnoreCase))
                        {
                            var index = product.IndexOf(chipset.Model, StringComparison.OrdinalIgnoreCase);
                            var model = index > 0 ? product.Substring(0, index).Trim() : product.Trim();
                            return (model, chipset.Model);
                        }
                    }
                }
            }

            return (product.Trim(), null);
        }

        public string GetPcieVersionFromDataset(string? chipsetModel)
        {
            if (string.IsNullOrWhiteSpace(chipsetModel))
                return "PCIe 3.0+";

            var chipset = FindChipsetByModel(chipsetModel);
            return chipset?.PciVersion ?? "PCIe 3.0+";
        }

        public List<PciSlot>? GetAvailableSlots(string? chipsetModel)
        {
            if (string.IsNullOrWhiteSpace(chipsetModel))
                return null;

            var chipset = FindChipsetByModel(chipsetModel);
            return chipset?.Slots;
        }

        public int? GetReleaseYear(string? chipsetModel)
        {
            if (string.IsNullOrWhiteSpace(chipsetModel))
                return null;

            var chipset = FindChipsetByModel(chipsetModel);
            return chipset?.ReleaseYear;
        }

        public bool IsDatasetLoaded => _datasets.Any();
    }
} 