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
            // CEREALS
            {
                "maize", new CropRequirements
                {
                    ScientificName = "Zea mays",
                    Type = "Cereal",
                    PlantingSeason = "Spring/Summer",
                    GrowthDurationDays = 120,
                    ExpectedYieldKgPerHectare = 8000,
                    PreferredSoil = "Fertile Loamy",
                    MinTemperature = "15°C",
                    CommonPestsDiseases = "Fall armyworm, Stem borer, Maize streak virus",
                    Notes = "South Africa's most important crop. Needs consistent rainfall or irrigation.",
                    Source = "TrifleAPI"
                }
            },
            {
                "wheat", new CropRequirements
                {
                    ScientificName = "Triticum aestivum",
                    Type = "Cereal",
                    PlantingSeason = "Winter",
                    GrowthDurationDays = 120,
                    ExpectedYieldKgPerHectare = 4500,
                    PreferredSoil = "Well-drained Clay Loam",
                    MinTemperature = "10°C",
                    CommonPestsDiseases = "Rust, Aphids, Russian wheat aphid",
                    Notes = "Winter crop, mainly grown in Western Cape and Free State.",
                    Source = "TrifleAPI"
                }
            },
            {
                "sorghum", new CropRequirements
                {
                    ScientificName = "Sorghum bicolor",
                    Type = "Cereal",
                    PlantingSeason = "Spring/Summer",
                    GrowthDurationDays = 110,
                    ExpectedYieldKgPerHectare = 3000,
                    PreferredSoil = "Sandy Loam",
                    MinTemperature = "18°C",
                    CommonPestsDiseases = "Sorghum midge, Stalk borer, Head smut",
                    Notes = "Drought-tolerant grain crop, important in semi-arid regions.",
                    Source = "TrifleAPI"
                }
            },
            {
                "oats", new CropRequirements
                {
                    ScientificName = "Avena sativa",
                    Type = "Cereal",
                    PlantingSeason = "Winter",
                    GrowthDurationDays = 95,
                    ExpectedYieldKgPerHectare = 3500,
                    PreferredSoil = "Well-drained Loam",
                    MinTemperature = "8°C",
                    CommonPestsDiseases = "Crown rust, Barley yellow dwarf virus",
                    Notes = "Cool-season crop, often used for pasture and hay.",
                    Source = "TrifleAPI"
                }
            },
            {
                "barley", new CropRequirements
                {
                    ScientificName = "Hordeum vulgare",
                    Type = "Cereal",
                    PlantingSeason = "Winter",
                    GrowthDurationDays = 100,
                    ExpectedYieldKgPerHectare = 4000,
                    PreferredSoil = "Well-drained Loam",
                    MinTemperature = "8°C",
                    CommonPestsDiseases = "Net blotch, Powdery mildew, Aphids",
                    Notes = "Mainly for malting and animal feed. Grown in winter rainfall areas.",
                    Source = "TrifleAPI"
                }
            },

            // LEGUMES
            {
                "beans", new CropRequirements
                {
                    ScientificName = "Phaseolus vulgaris",
                    Type = "Legume",
                    PlantingSeason = "Spring/Summer",
                    GrowthDurationDays = 85,
                    ExpectedYieldKgPerHectare = 2000,
                    PreferredSoil = "Sandy Loam",
                    MinTemperature = "18°C",
                    CommonPestsDiseases = "Bean beetle, Anthracnose, Bean rust",
                    Notes = "Fixes nitrogen into soil. Popular dry bean varieties include sugar beans.",
                    Source = "TrifleAPI"
                }
            },
            {
                "cowpeas", new CropRequirements
                {
                    ScientificName = "Vigna unguiculata",
                    Type = "Legume",
                    PlantingSeason = "Summer",
                    GrowthDurationDays = 80,
                    ExpectedYieldKgPerHectare = 1500,
                    PreferredSoil = "Sandy Loam",
                    MinTemperature = "20°C",
                    CommonPestsDiseases = "Cowpea aphid, Pod borer, Bacterial blight",
                    Notes = "Drought-tolerant legume, important protein source in rural areas.",
                    Source = "TrifleAPI"
                }
            },
            {
                "groundnuts", new CropRequirements
                {
                    ScientificName = "Arachis hypogaea",
                    Type = "Legume",
                    PlantingSeason = "Spring/Summer",
                    GrowthDurationDays = 120,
                    ExpectedYieldKgPerHectare = 2500,
                    PreferredSoil = "Sandy Loam",
                    MinTemperature = "20°C",
                    CommonPestsDiseases = "Leaf spot, Aphids, Thrips",
                    Notes = "Also called peanuts. Important oilseed and protein crop.",
                    Source = "TrifleAPI"
                }
            },
            {
                "soybeans", new CropRequirements
                {
                    ScientificName = "Glycine max",
                    Type = "Legume",
                    PlantingSeason = "Spring/Summer",
                    GrowthDurationDays = 115,
                    ExpectedYieldKgPerHectare = 2800,
                    PreferredSoil = "Well-drained Loamy",
                    MinTemperature = "18°C",
                    CommonPestsDiseases = "Soybean rust, Stink bugs, Aphids",
                    Notes = "Important protein and oil crop. Growing in popularity.",
                    Source = "TrifleAPI"
                }
            },

            // ROOT VEGETABLES
            {
                "potato", new CropRequirements
                {
                    ScientificName = "Solanum tuberosum",
                    Type = "Root Vegetable",
                    PlantingSeason = "Year-round (varies by region)",
                    GrowthDurationDays = 90,
                    ExpectedYieldKgPerHectare = 35000,
                    PreferredSoil = "Well-drained Sandy Loam",
                    MinTemperature = "15°C",
                    CommonPestsDiseases = "Late blight, Potato tuber moth, Aphids",
                    Notes = "Major commercial vegetable crop in SA. Multiple plantings per year.",
                    Source = "TrifleAPI"
                }
            },
            {
                "sweet_potato", new CropRequirements
                {
                    ScientificName = "Ipomoea batatas",
                    Type = "Root Vegetable",
                    PlantingSeason = "Spring/Summer",
                    GrowthDurationDays = 120,
                    ExpectedYieldKgPerHectare = 25000,
                    PreferredSoil = "Sandy Loam",
                    MinTemperature = "18°C",
                    CommonPestsDiseases = "Sweet potato weevil, Wireworms, Leaf spot",
                    Notes = "Drought-tolerant crop, important food security crop.",
                    Source = "TrifleAPI"
                }
            },
            {
                "carrot", new CropRequirements
                {
                    ScientificName = "Daucus carota subsp. sativus",
                    Type = "Root Vegetable",
                    PlantingSeason = "Autumn/Winter",
                    GrowthDurationDays = 75,
                    ExpectedYieldKgPerHectare = 40000,
                    PreferredSoil = "Sandy Loam",
                    MinTemperature = "16°C",
                    CommonPestsDiseases = "Carrot fly, Aphids, Leaf blight",
                    Notes = "Avoid stony soils. Cool-season crop.",
                    Source = "TrifleAPI"
                }
            },
            {
                "beetroot", new CropRequirements
                {
                    ScientificName = "Beta vulgaris",
                    Type = "Root Vegetable",
                    PlantingSeason = "Autumn/Winter",
                    GrowthDurationDays = 65,
                    ExpectedYieldKgPerHectare = 30000,
                    PreferredSoil = "Well-drained Loamy",
                    MinTemperature = "15°C",
                    CommonPestsDiseases = "Leaf spot, Aphids, Cutworms",
                    Notes = "Cool-season crop, both roots and leaves are edible.",
                    Source = "TrifleAPI"
                }
            },

            // BULB VEGETABLES
            {
                "onion", new CropRequirements
                {
                    ScientificName = "Allium cepa",
                    Type = "Bulb",
                    PlantingSeason = "Winter/Spring",
                    GrowthDurationDays = 110,
                    ExpectedYieldKgPerHectare = 30000,
                    PreferredSoil = "Well-drained Loamy",
                    MinTemperature = "12°C",
                    CommonPestsDiseases = "Onion maggot, Downy mildew, Thrips",
                    Notes = "Requires good sun exposure. Long day varieties for SA conditions.",
                    Source = "TrifleAPI"
                }
            },
            {
                "garlic", new CropRequirements
                {
                    ScientificName = "Allium sativum",
                    Type = "Bulb",
                    PlantingSeason = "Autumn",
                    GrowthDurationDays = 150,
                    ExpectedYieldKgPerHectare = 8000,
                    PreferredSoil = "Well-drained Loamy",
                    MinTemperature = "10°C",
                    CommonPestsDiseases = "White rot, Thrips, Onion fly",
                    Notes = "Requires cool period for bulb development. Plant in autumn.",
                    Source = "TrifleAPI"
                }
            },

            // FRUIT VEGETABLES
            {
                "tomato", new CropRequirements
                {
                    ScientificName = "Solanum lycopersicum",
                    Type = "Fruit Vegetable",
                    PlantingSeason = "Spring/Summer",
                    GrowthDurationDays = 90,
                    ExpectedYieldKgPerHectare = 50000,
                    PreferredSoil = "Loamy",
                    MinTemperature = "18°C",
                    CommonPestsDiseases = "Late blight, Early blight, Aphids, Whiteflies",
                    Notes = "Support staking recommended. Major commercial crop in SA.",
                    Source = "TrifleAPI"
                }
            },
            {
                "pepper", new CropRequirements
                {
                    ScientificName = "Capsicum annuum",
                    Type = "Fruit Vegetable",
                    PlantingSeason = "Spring/Summer",
                    GrowthDurationDays = 85,
                    ExpectedYieldKgPerHectare = 25000,
                    PreferredSoil = "Well-drained Loamy",
                    MinTemperature = "20°C",
                    CommonPestsDiseases = "Aphids, Thrips, Bacterial wilt",
                    Notes = "Warm season crop. Both sweet and hot varieties grown.",
                    Source = "TrifleAPI"
                }
            },
            {
                "eggplant", new CropRequirements
                {
                    ScientificName = "Solanum melongena",
                    Type = "Fruit Vegetable",
                    PlantingSeason = "Spring/Summer",
                    GrowthDurationDays = 100,
                    ExpectedYieldKgPerHectare = 20000,
                    PreferredSoil = "Well-drained Loamy",
                    MinTemperature = "20°C",
                    CommonPestsDiseases = "Flea beetle, Aphids, Verticillium wilt",
                    Notes = "Heat-loving crop. Popular in Indian communities.",
                    Source = "TrifleAPI"
                }
            },

            // LEAFY VEGETABLES
            {
                "cabbage", new CropRequirements
                {
                    ScientificName = "Brassica oleracea var. capitata",
                    Type = "Leafy Vegetable",
                    PlantingSeason = "Autumn/Winter",
                    GrowthDurationDays = 80,
                    ExpectedYieldKgPerHectare = 40000,
                    PreferredSoil = "Fertile Loamy",
                    MinTemperature = "15°C",
                    CommonPestsDiseases = "Cabbage worm, Aphids, Clubroot",
                    Notes = "Cool-season crop. Important staple vegetable.",
                    Source = "TrifleAPI"
                }
            },
            {
                "spinach", new CropRequirements
                {
                    ScientificName = "Spinacia oleracea",
                    Type = "Leafy Vegetable",
                    PlantingSeason = "Autumn/Winter",
                    GrowthDurationDays = 45,
                    ExpectedYieldKgPerHectare = 15000,
                    PreferredSoil = "Well-drained Loamy",
                    MinTemperature = "10°C",
                    CommonPestsDiseases = "Leaf miner, Aphids, Downy mildew",
                    Notes = "Fast-growing cool-season crop. Multiple harvests possible.",
                    Source = "TrifleAPI"
                }
            },
            {
                "lettuce", new CropRequirements
                {
                    ScientificName = "Lactuca sativa",
                    Type = "Leafy Vegetable",
                    PlantingSeason = "Autumn/Winter/Spring",
                    GrowthDurationDays = 60,
                    ExpectedYieldKgPerHectare = 25000,
                    PreferredSoil = "Well-drained Loamy",
                    MinTemperature = "12°C",
                    CommonPestsDiseases = "Aphids, Downy mildew, Tip burn",
                    Notes = "Cool-season crop. Popular in salads and fast food industry.",
                    Source = "TrifleAPI"
                }
            },

            // VINE CROPS
            {
                "watermelon", new CropRequirements
                {
                    ScientificName = "Citrullus lanatus",
                    Type = "Vine Crop",
                    PlantingSeason = "Spring/Summer",
                    GrowthDurationDays = 90,
                    ExpectedYieldKgPerHectare = 30000,
                    PreferredSoil = "Sandy Loam",
                    MinTemperature = "20°C",
                    CommonPestsDiseases = "Cucumber beetle, Anthracnose, Powdery mildew",
                    Notes = "Warm season crop. Important commercial fruit in SA.",
                    Source = "TrifleAPI"
                }
            },
            {
                "pumpkin", new CropRequirements
                {
                    ScientificName = "Cucurbita pepo",
                    Type = "Vine Crop",
                    PlantingSeason = "Spring/Summer",
                    GrowthDurationDays = 100,
                    ExpectedYieldKgPerHectare = 25000,
                    PreferredSoil = "Fertile Loamy",
                    MinTemperature = "18°C",
                    CommonPestsDiseases = "Squash bug, Powdery mildew, Cucumber beetle",
                    Notes = "Traditional African crop. Both leaves and fruits are consumed.",
                    Source = "TrifleAPI"
                }
            },
            {
                "butternut", new CropRequirements
                {
                    ScientificName = "Cucurbita moschata",
                    Type = "Vine Crop",
                    PlantingSeason = "Spring/Summer",
                    GrowthDurationDays = 110,
                    ExpectedYieldKgPerHectare = 20000,
                    PreferredSoil = "Well-drained Loamy",
                    MinTemperature = "20°C",
                    CommonPestsDiseases = "Squash borer, Powdery mildew, Aphids",
                    Notes = "Popular winter squash variety. Good storage life.",
                    Source = "TrifleAPI"
                }
            },
            {
                "cucumber", new CropRequirements
                {
                    ScientificName = "Cucumis sativus",
                    Type = "Vine Crop",
                    PlantingSeason = "Spring/Summer",
                    GrowthDurationDays = 60,
                    ExpectedYieldKgPerHectare = 35000,
                    PreferredSoil = "Well-drained Loamy",
                    MinTemperature = "18°C",
                    CommonPestsDiseases = "Cucumber beetle, Downy mildew, Aphids",
                    Notes = "Warm season crop. Popular fresh market vegetable.",
                    Source = "TrifleAPI"
                }
            },

            // INDUSTRIAL CROPS
            {
                "sunflower", new CropRequirements
                {
                    ScientificName = "Helianthus annuus",
                    Type = "Oilseed",
                    PlantingSeason = "Spring/Summer",
                    GrowthDurationDays = 100,
                    ExpectedYieldKgPerHectare = 2000,
                    PreferredSoil = "Well-drained Loamy",
                    MinTemperature = "15°C",
                    CommonPestsDiseases = "Sunflower head moth, Rust, Downy mildew",
                    Notes = "Major oilseed crop in SA. Important for cooking oil production.",
                    Source = "TrifleAPI"
                }
            },
            {
                "canola", new CropRequirements
                {
                    ScientificName = "Brassica napus",
                    Type = "Oilseed",
                    PlantingSeason = "Winter",
                    GrowthDurationDays = 150,
                    ExpectedYieldKgPerHectare = 2500,
                    PreferredSoil = "Well-drained Clay Loam",
                    MinTemperature = "8°C",
                    CommonPestsDiseases = "Aphids, Flea beetle, Blackleg",
                    Notes = "Winter oilseed crop. Growing in importance for oil production.",
                    Source = "TrifleAPI"
                }
            },
            {
                "cotton", new CropRequirements
                {
                    ScientificName = "Gossypium hirsutum",
                    Type = "Fiber Crop",
                    PlantingSeason = "Spring/Summer",
                    GrowthDurationDays = 180,
                    ExpectedYieldKgPerHectare = 1500,
                    PreferredSoil = "Deep Loamy",
                    MinTemperature = "20°C",
                    CommonPestsDiseases = "Bollworm, Aphids, Bacterial blight",
                    Notes = "Important fiber crop. Requires warm temperatures and irrigation.",
                    Source = "TrifleAPI"
                }
            },
            {
                "tobacco", new CropRequirements
                {
                    ScientificName = "Nicotiana tabacum",
                    Type = "Cash Crop",
                    PlantingSeason = "Spring",
                    GrowthDurationDays = 120,
                    ExpectedYieldKgPerHectare = 2500,
                    PreferredSoil = "Sandy Loam",
                    MinTemperature = "18°C",
                    CommonPestsDiseases = "Tobacco hornworm, Blue mold, Aphids",
                    Notes = "Labor-intensive cash crop. Grown mainly in Limpopo and Mpumalanga.",
                    Source = "TrifleAPI"
                }
            },

            // PASTURE CROPS
            {
                "lucerne", new CropRequirements
                {
                    ScientificName = "Medicago sativa",
                    Type = "Pasture/Fodder",
                    PlantingSeason = "Autumn/Spring",
                    GrowthDurationDays = 365, // Perennial
                    ExpectedYieldKgPerHectare = 12000,
                    PreferredSoil = "Well-drained Alkaline",
                    MinTemperature = "10°C",
                    CommonPestsDiseases = "Aphids, Leaf spot, Root rot",
                    Notes = "Also called alfalfa. Important perennial fodder crop.",
                    Source = "TrifleAPI"
                }
            },
            {
                "kikuyu_grass", new CropRequirements
                {
                    ScientificName = "Pennisetum clandestinum",
                    Type = "Pasture Grass",
                    PlantingSeason = "Spring/Summer",
                    GrowthDurationDays = 365, // Perennial
                    ExpectedYieldKgPerHectare = 15000,
                    PreferredSoil = "Fertile Loamy",
                    MinTemperature = "15°C",
                    CommonPestsDiseases = "Army worm, Rust, Leaf blight",
                    Notes = "Aggressive perennial grass. Excellent for dairy pastures.",
                    Source = "TrifleAPI"
                }
            },

            // FRUIT TREES (Young plantings)
            {
                "citrus", new CropRequirements
                {
                    ScientificName = "Citrus spp.",
                    Type = "Fruit Tree",
                    PlantingSeason = "Spring",
                    GrowthDurationDays = 1095, // 3 years to first fruit
                    ExpectedYieldKgPerHectare = 40000,
                    PreferredSoil = "Well-drained Sandy Loam",
                    MinTemperature = "12°C",
                    CommonPestsDiseases = "Citrus psyllid, Scale insects, Citrus canker",
                    Notes = "Major export crop. Includes oranges, lemons, grapefruit.",
                    Source = "TrifleAPI"
                }
            },
            {
                "avocado", new CropRequirements
                {
                    ScientificName = "Persea americana",
                    Type = "Fruit Tree",
                    PlantingSeason = "Spring",
                    GrowthDurationDays = 1460, // 4 years to first fruit
                    ExpectedYieldKgPerHectare = 15000,
                    PreferredSoil = "Well-drained Sandy Loam",
                    MinTemperature = "18°C",
                    CommonPestsDiseases = "Thrips, Scale insects, Root rot",
                    Notes = "Growing export industry. Requires frost protection.",
                    Source = "TrifleAPI"
                }
            },

            // INDIGENOUS CROPS
            {
                "sorghum_beer", new CropRequirements
                {
                    ScientificName = "Sorghum bicolor var. caffrorum",
                    Type = "Indigenous Cereal",
                    PlantingSeason = "Summer",
                    GrowthDurationDays = 120,
                    ExpectedYieldKgPerHectare = 2500,
                    PreferredSoil = "Sandy Loam",
                    MinTemperature = "18°C",
                    CommonPestsDiseases = "Sorghum midge, Head smut, Stalk borer",
                    Notes = "Traditional variety used for beer brewing. Drought tolerant.",
                    Source = "TrifleAPI"
                }
            },
            {
                "african_potato", new CropRequirements
                {
                    ScientificName = "Hypoxis hemerocallidea",
                    Type = "Indigenous Medicinal",
                    PlantingSeason = "Spring",
                    GrowthDurationDays = 180,
                    ExpectedYieldKgPerHectare = 5000,
                    PreferredSoil = "Sandy Grassland",
                    MinTemperature = "15°C",
                    CommonPestsDiseases = "Root rot, Nematodes",
                    Notes = "Indigenous medicinal plant. Used in traditional medicine.",
                    Source = "TrifleAPI"
                }
            }
        };

        // Accepts both singular and plural forms, and common alternative names
        public static CropRequirements GetRequirementsByCropName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;

            name = name.Trim().ToLowerInvariant();

            // Direct match first
            if (_cropData.TryGetValue(name, out var match))
                return match;

            // Handle common alternative names
            var alternativeNames = new Dictionary<string, string>
            {
                { "mealies", "maize" },
                { "mealie", "maize" },
                { "corn", "maize" },
                { "peanuts", "groundnuts" },
                { "peanut", "groundnuts" },
                { "groundnut", "groundnuts" },
                { "soya", "soybeans" },
                { "soybean", "soybeans" },
                { "soya_beans", "soybeans" },
                { "sweet_potatoes", "sweet_potato" },
                { "sweetpotato", "sweet_potato" },
                { "potatoes", "potato" },
                { "tomatoes", "tomato" },
                { "carrots", "carrot" },
                { "onions", "onion" },
                { "peppers", "pepper" },
                { "chilli", "pepper" },
                { "chillies", "pepper" },
                { "capsicum", "pepper" },
                { "brinjal", "eggplant" },
                { "aubergine", "eggplant" },
                { "alfalfa", "lucerne" },
                { "medics", "lucerne" },
                { "kikuyu", "kikuyu_grass" },
                { "oranges", "citrus" },
                { "lemons", "citrus" },
                { "grapefruit", "citrus" },
                { "naartjies", "citrus" },
                { "avos", "avocado" },
                { "avocados", "avocado" },
                { "butternut_squash", "butternut" },
                { "gem_squash", "pumpkin" },
                { "hubbard_squash", "pumpkin" },
                { "baby_marrow", "cucumber" },
                { "courgette", "cucumber" },
                { "zucchini", "cucumber" }
            };

            if (alternativeNames.TryGetValue(name, out var alternativeName))
            {
                if (_cropData.TryGetValue(alternativeName, out var alternativeMatch))
                    return alternativeMatch;
            }

            // Handle plural → singular (naive rule)
            if (name.EndsWith("ies") && name.Length > 4)
            {
                var singular = name.Substring(0, name.Length - 3) + "y";
                if (_cropData.ContainsKey(singular))
                    return _cropData[singular];
            }

            if (name.EndsWith("es") && name.Length > 3)
            {
                var singular = name.Substring(0, name.Length - 2);
                if (_cropData.ContainsKey(singular))
                    return _cropData[singular];
            }

            if (name.EndsWith("s") && name.Length > 2)
            {
                var singular = name.Substring(0, name.Length - 1);
                if (_cropData.ContainsKey(singular))
                    return _cropData[singular];
            }

            return null;
        }

        // New method to get crops by category
        public static List<CropRequirements> GetCropsByType(string type)
        {
            return _cropData.Values.Where(c => c.Type.Equals(type, System.StringComparison.OrdinalIgnoreCase)).ToList();
        }

        // New method to get all crop types
        public static List<string> GetAllCropTypes()
        {
            return _cropData.Values.Select(c => c.Type).Distinct().OrderBy(t => t).ToList();
        }

        // New method to get crops by season
        public static List<CropRequirements> GetCropsBySeason(string season)
        {
            return _cropData.Values.Where(c => c.PlantingSeason.Contains(season)).ToList();
        }

        // New method to search crops by name pattern
        public static List<string> SearchCropNames(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm)) return new List<string>();

            searchTerm = searchTerm.ToLowerInvariant();
            return _cropData.Keys
                .Where(key => key.Contains(searchTerm))
                .OrderBy(key => key)
                .ToList();
        }
    }
}