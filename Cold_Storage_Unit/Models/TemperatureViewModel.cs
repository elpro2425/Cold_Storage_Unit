using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Cold_Storage_Unit.Models
{
    public class TemperatureViewModel
    {
        public string UnitName { get; set; }
        public double? TempHigh { get; set; }
        public double? TempLow { get; set; }
        public string SensitivityHigh { get; set; }
        public string SensitivityLow { get; set; }
        public bool EnabledHigh { get; set; }
        public bool EnabledLow { get; set; }
    }

}