using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FarmTrack.Models
{
    public class SendLivestockToProductsViewModel
    {
        public int LivestockId { get; set; }

        public string Type { get; set; }

        public string Breed { get; set; }

        public string TagNumber { get; set; }
    }

}