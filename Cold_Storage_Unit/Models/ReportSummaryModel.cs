using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Cold_Storage_Unit.Models
{
    public class ReportSummaryModel
    {
        public List<ColdStorageUnit> Rows { get; set; }
        public List<SensorSummary> Summaries { get; set; }
        public string LineChartBase64 { get; set; }
        public string BarChartBase64 { get; set; }
        public string PieChartBase64 { get; set; }

    }
    public class SummaryRow
    {
        public string Name { get; set; }
        public double AvgTemp { get; set; }
        public double AvgHumidity { get; set; }
        public double AvgCO2 { get; set; }

        public double AvgEthylene { get; set; }
    }

    public class SensorSummary
    {
        public string Metric { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public double Avg { get; set; }
        public double Range => Max - Min;
    }
}