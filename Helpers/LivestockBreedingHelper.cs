using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FarmTrack.Models;

namespace FarmTrack.Helpers
{
    public static class LivestockBreedingHelper
    {
        public static string GetEligibilityMessage(FarmTrack.Models.Livestock animal)
        {
            if (animal.Sex == "Female" && animal.AgeInMonths >= 18 && animal.Weight >= 300)
                return "This animal meets breeding stock requirements.";
            return "This animal does not currently meet breeding stock requirements.";
        }
    }
}
