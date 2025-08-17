using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Cold_Storage_Unit.Models
{
    public class ReportSummaryViewModel
    {
        public List<ColdStorageUnit> Rows { get; set; }

        // Summary metrics
        public double MinTemp { get; set; }
        public double MaxTemp { get; set; }
        public double AvgTemp { get; set; }

        public double MinHum { get; set; }
        public double MaxHum { get; set; }
        public double AvgHum { get; set; }

        public double MinCO2 { get; set; }
        public double MaxCO2 { get; set; }
        public double AvgCO2 { get; set; }

        public double MinEth { get; set; }
        public double MaxEth { get; set; }
        public double AvgEth { get; set; }

        // Charts as base64 images
        public string LineChartBase64 { get; set; }
        public string BarChartBase64 { get; set; }
        public string PieChartBase64 { get; set; }
    }
}