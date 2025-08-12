using FarmTrack.Models;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace FarmTrack.Services
{
    public static class MockCropRequirementService
    {
        private static readonly Dictionary<string, CropRequirements> _cropData = new Dictionary<string, CropRequirements>(System.StringComparer.OrdinalIgnoreCase)
            {
                {
                    "tomato", new CropRequirements
                    {
                        ScientificName = "Solanum lycopersicum",
                        Type = "Fruit Vegetable",
                        PlantingSeason = "Spring",
                        GrowthDurationDays = 90,
                        ExpectedYieldKgPerHectare = 50000,
                        PreferredSoil = "Loamy",
                        MinTemperature = "18°C",
                        CommonPestsDiseases = "Aphids, Blight",
                        Notes = "Support staking recommended.",
                        Source = "TrifleAPI"
                    }
                },
                {
                    "carrot", new CropRequirements
                    {
                        ScientificName = "Daucus carota subsp. sativus",
                        Type = "Root Vegetable",
                        PlantingSeason = "Autumn",
                        GrowthDurationDays = 75,
                        ExpectedYieldKgPerHectare = 40000,
                        PreferredSoil = "Sandy Loam",
                        MinTemperature = "16°C",
                        CommonPestsDiseases = "Carrot fly, Aphids",
                        Notes = "Avoid stony soils.",
                        Source = "TrifleAPI"                    }
                },
                {
                    "onion", new CropRequirements
                    {
                        ScientificName = "Allium cepa",
                        Type = "Bulb",
                        PlantingSeason = "Winter",
                        GrowthDurationDays = 110,
                        ExpectedYieldKgPerHectare = 30000,
                        PreferredSoil = "Well-drained Loamy",
                        MinTemperature = "12°C",
                        CommonPestsDiseases = "Onion maggot, Downy mildew",
                        Notes = "Requires good sun exposure.",
                        Source = "TrifleAPI"
                    }
                },
                {
                    "maize", new CropRequirements
                    {
                        ScientificName = "Zea mays",
                        Type = "Cereal",
                        PlantingSeason = "Spring",
                        GrowthDurationDays = 120,
                        ExpectedYieldKgPerHectare = 8000,
                        PreferredSoil = "Fertile Loamy",
                        MinTemperature = "15°C",
                        CommonPestsDiseases = "Fall armyworm, Stem borer",
                        Notes = "Needs consistent rainfall or irrigation.",
                        Source = "TrifleAPI"
                    }
                },
                {
                    "beans", new CropRequirements
                    {
                        ScientificName = "Phaseolus vulgaris",
                        Type = "Legume",
                        PlantingSeason = "Summer",
                        GrowthDurationDays = 85,
                        ExpectedYieldKgPerHectare = 2000,
                        PreferredSoil = "Sandy Loam",
                        MinTemperature = "18°C",
                        CommonPestsDiseases = "Bean beetle, Anthracnose",
                        Notes = "Fixes nitrogen into soil.",
                        Source = "TrifleAPI"
                    }
                }
            };

        // Accepts both singular and plural forms
        public static CropRequirements GetRequirementsByCropName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;

            name = name.Trim().ToLowerInvariant();

            if (_cropData.TryGetValue(name, out var match))
                return match;

            // Handle plural → singular (naive rule)
            if (name.EndsWith("es") && _cropData.ContainsKey(name.Substring(0, name.Length - 2)))
                return _cropData[name.Substring(0, name.Length - 2)];

            if (name.EndsWith("s") && _cropData.ContainsKey(name.Substring(0, name.Length - 1)))
                return _cropData[name.Substring(0, name.Length - 1)];

            return null;
        }
    }
}
