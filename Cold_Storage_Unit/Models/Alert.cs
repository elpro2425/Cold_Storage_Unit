using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Cold_Storage_Unit.Models
{
    public class Alert
    {
        public int ID { get; set; }
        public string Alert_Name { get; set; }
        public string Condition_Trigger { get; set; }
        public string Severity { get; set; }
        public string Remarks { get; set; }
        public string Alert_Date { get; set; }
        public string UnitName { get; set; }

        public double Actual_Value { get; set; }
    }
}