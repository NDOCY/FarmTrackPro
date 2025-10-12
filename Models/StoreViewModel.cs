using FarmTrack.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FarmTrack.Models
{
    public class StoreViewModel
    {
        public string TypeFilter { get; set; }
        public string CategoryFilter { get; set; }

        public List<StoreCategoryViewModel> Categories { get; set; }
    }

    public class StoreCategoryViewModel
    {
        public string Category { get; set; }
        public List<Product> Items { get; set; }
    }

}