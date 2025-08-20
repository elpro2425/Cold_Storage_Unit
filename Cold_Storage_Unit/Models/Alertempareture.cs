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

        public string Condition_Trigger { get; set; }
        public string Remarks { get; set; }

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

        // Optional: Selected Alert Code (e.g. A1H, A2L)
        [NotMapped]
        public string SelectedAlertCode { get; set; }

        // Optional: Entire list of available alert definitions for dropdowns
        [NotMapped]
        public List<AlertDefinition> AlertDefinitions { get; set; } = new List<AlertDefinition>();
    }

    // Put this class in a shared location or file if used in multiple places
    public class AlertDefinition
    {
        public string AlertName { get; set; }              // e.g. A1H
        public string MessageDisplay { get; set; }         // e.g. High CO₂ Level Alert
        public string ConditionTrigger { get; set; }       // e.g. CO₂ > 1200 ppm
        public string Severity { get; set; }               // e.g. High
        public string Remarks { get; set; }                // e.g. Risk of mold, spoilage
    }
}