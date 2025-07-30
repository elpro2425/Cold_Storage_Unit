using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Cold_Storage_Unit.Models
{

    public class GroupedSummaryModel
    {
        public string Name { get; set; }
        public int Count { get; set; }
        public double AvgTemp { get; set; }
        public double MinTemp { get; set; }
        public double MaxTemp { get; set; }
        public double AvgHumidity { get; set; }
        public double MinHumidity { get; set; }
        public double MaxHumidity { get; set; }
        public double AvgEthylene { get; set; }
        public double MinEthylene { get; set; }
        public double MaxEthylene { get; set; }
    }

}