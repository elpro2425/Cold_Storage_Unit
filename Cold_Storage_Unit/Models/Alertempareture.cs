using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Cold_Storage_Unit.Models
{
    public class Alertempareture
    {
        public int Id { get; set; }
        public string UnitName { get; set; }
        public string Category { get; set; }
        public double threshold { get; set; }
        public string message_display { get; set; }
        public bool enabled { get; set; }
        public string enabled_sensitivity { get; set; }
        public string condition { get; set; }
        public TimeSpan TimeInMinutes { get; set; }

        [NotMapped]
        public string TimeInMinutesFormatted
        {
            get => TimeInMinutes.ToString(@"hh\:mm\:ss");
            set
            {
                if (TimeSpan.TryParse(value, out var ts))
                    TimeInMinutes = ts;
                else
                    TimeInMinutes = TimeSpan.Zero;
            }
        }

        public string Condition_Trigger { get; set; }
        public string Remarks { get; set; }
    }
}