using FarmPro.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace FarmTrack.Models
{
    public class Livestock
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LivestockId { get; set; }

        [Required]
        public string Type { get; set; }

        [Required]
        public string Breed { get; set; }

        public string TagNumber { get; set; }

        public string Notes { get; set; }

        public int? UserId { get; set; }

        public DateTime DateRegistered { get; set; } = DateTime.Now;

        public string ImagePath { get; set; }

        public string QrCodePath { get; set; }

        public string Sex { get; set; }

        public string Status { get; set; } = "In-House";

        [Range(0, 100)]
        public double Age { get; set; }

        [Range(0, 2000)]
        public double Weight { get; set; }

        [Range(0, 2000)]
        public double? InitialWeight { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        public bool IsBreedingStock { get; set; } = false;

        [NotMapped]
        public int? AgeInMonths
        {
            get
            {
                if (!DateOfBirth.HasValue) return 0;
                var today = DateTime.Today;
                return ((today.Year - DateOfBirth.Value.Year) * 12) + today.Month - DateOfBirth.Value.Month;
            }
        }

        public int? ParentId { get; set; }
        public int? ReproductionRecordId { get; set; }

        [ForeignKey("ReproductionRecordId")]
        public virtual ReproductionRecord ReproductionRecord { get; set; }

        public virtual Livestock Parent { get; set; }
        public virtual User User { get; set; }

        public virtual ICollection<HealthRecord> HealthRecords { get; set; } = new List<HealthRecord>();
        public virtual ICollection<Livestock> Offspring { get; set; } = new List<Livestock>();
        public virtual ICollection<ReproductionRecord> FemaleReproductions { get; set; }
        public virtual ICollection<ReproductionRecord> MaleReproductions { get; set; }
        public virtual ICollection<WeightRecord> WeightRecords { get; set; } = new List<WeightRecord>();


        public static class LivestockBreedingHelper
        {
            private static readonly Dictionary<string, (int MinAgeMonths, double MinWeightKg)> Standards =
                new Dictionary<string, (int MinAgeMonths, double MinWeightKg)>(StringComparer.OrdinalIgnoreCase)
                {
                    { "Cow", (18, 300) },
                    { "Goat", (12, 30) },
                    { "Sheep", (12, 35) },
                    { "Pig", (8, 130) },
                    { "Horse", (36, 400) },
                    { "Rabbit", (6, 2.5) }
                };


            public static bool MeetsBreedingRequirements(Livestock animal)
            {
                if (!animal.IsBreedingStock || string.IsNullOrWhiteSpace(animal.Type))
                    return false;

                string type = animal.Type.Trim().ToLowerInvariant();
                int age = animal.AgeInMonths ?? 0;
                double latestWeight = animal.WeightRecords != null && animal.WeightRecords.Any()
                    ? animal.WeightRecords.OrderByDescending(w => w.RecordedAt).First().Weight
                    : animal.Weight;

                if (type == "cow") return age >= 18 && latestWeight >= 300;
                if (type == "goat") return age >= 12 && latestWeight >= 30;
                if (type == "sheep") return age >= 12 && latestWeight >= 35;
                if (type == "pig") return age >= 8 && latestWeight >= 130;
                if (type == "horse") return age >= 36 && latestWeight >= 400;
                if (type == "rabbit") return age >= 6 && latestWeight >= 2.5;
                if (type == "chicken") return age >= 5; // no weight requirement for chicken

                return false; // type not supported
            }



            public static string GetEligibilityMessage(Livestock animal)
            {
                if (!animal.IsBreedingStock)
                    return "Not marked as breeding stock.";

                if (string.IsNullOrWhiteSpace(animal.Type))
                    return "Animal type is missing.";

                string type = animal.Type.Trim().ToLowerInvariant();
                int age = animal.AgeInMonths ?? 0;
                double latestWeight = animal.WeightRecords != null && animal.WeightRecords.Any()
                    ? animal.WeightRecords.OrderByDescending(w => w.RecordedAt).First().Weight
                    : animal.Weight;

                if (type == "cow")
                    return $"Cow: needs 18+ months & 300+ kg. This one is {age} months, {latestWeight} kg.";
                if (type == "goat")
                    return $"Goat: needs 12+ months & 30+ kg. This one is {age} months, {latestWeight} kg.";
                if (type == "sheep")
                    return $"Sheep: needs 12+ months & 35+ kg. This one is {age} months, {latestWeight} kg.";
                if (type == "pig")
                    return $"Pig: needs 8+ months & 130+ kg. This one is {age} months, {latestWeight} kg.";
                if (type == "horse")
                    return $"Horse: needs 36+ months & 400+ kg. This one is {age} months, {latestWeight} kg.";
                if (type == "rabbit")
                    return $"Rabbit: needs 6+ months & 2.5+ kg. This one is {age} months, {latestWeight} kg.";
                if (type == "chicken")
                    return $"Chicken: needs 5+ months. This one is {age} months.";

                return "No breeding criteria defined for this animal type.";
            }

        }

        public string GetBreedingEligibilityMessage()
        {
            if (!IsBreedingStock)
                return string.Empty;

            switch (Type?.ToLowerInvariant())
            {
                case "cow":
                    return GetCowEligibility();
                case "sheep":
                    return GetSheepEligibility();
                case "goat":
                    return GetGoatEligibility();
                case "pig":
                    return GetPigEligibility();
                case "chicken":
                    return GetChickenEligibility();
                case "horse":
                    return GetHorseEligibility();
                default:
                    return "No breeding criteria defined for this livestock type.";
            }
        }


        private string GetCowEligibility()
        {
            if (AgeInMonths < 18)
                return "Cattle must be at least 18 months old to breed.";
            if (Weight < 250)
                return "Cattle must weigh at least 250 kg to breed.";
            return "Cattle is eligible for breeding.";
        }

        private string GetSheepEligibility()
        {
            if (AgeInMonths < 6)
                return "Sheep must be at least 6 months old to breed.";
            if (Weight < 40)
                return "Sheep must weigh at least 40 kg to breed.";
            return "Sheep is eligible for breeding.";
        }

        private string GetGoatEligibility()
        {
            if (AgeInMonths < 8)
                return "Goat must be at least 8 months old to breed.";
            if (Weight < 35)
                return "Goat must weigh at least 35 kg to breed.";
            return "Goat is eligible for breeding.";
        }

        private string GetPigEligibility()
        {
            if (AgeInMonths < 7)
                return "Pig must be at least 7 months old to breed.";
            if (Weight < 90)
                return "Pig must weigh at least 90 kg to breed.";
            return "Pig is eligible for breeding.";
        }

        private string GetChickenEligibility()
        {
            if (AgeInMonths < 5)
                return "Chicken must be at least 5 months old to breed.";
            return "Chicken is eligible for breeding.";
        }

        private string GetHorseEligibility()
        {
            if (AgeInMonths < 24)
                return "Horse must be at least 24 months old to breed.";
            if (Weight < 400)
                return "Horse must weigh at least 400 kg to breed.";
            return "Horse is eligible for breeding.";
        }


    }
}
