using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Cold_Storage_Unit.Models
{
    public class WeightRecord
    {
        public int SrNo { get; set; }
        public int LineNo { get; set; }
        public double Weight { get; set; }
        public string Condition { get; set; }
        public string DateTime { get; set; }
    }
}