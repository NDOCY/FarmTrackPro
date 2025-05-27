using FarmTrack.Models;
using System;
using FarmTrack.ViewModels;
using System.Data.Entity;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FarmTrack.Models
{
    public class LivestockCreateViewModel
    {
        public Livestock Livestock { get; set; }
        public string InitialEventType { get; set; } // Birth or Purchased
        public DateTime InitialEventDate { get; set; } = DateTime.Now;
    }

}