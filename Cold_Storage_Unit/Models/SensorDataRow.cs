

namespace Cold_Storage_Unit.Models
{
    public class ColdStorageUnit
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public int RipenessStage { get; set; }
        public string PowerStatus { get; set; }
        public string DoorStatus { get; set; }
        public double Co2Level { get; set; }
        public double EthyleneLevel { get; set; }
        public int FanSpeed { get; set; }
        public string LastUpdated { get; set; }
        public string Timestamp { get; set; }
        public string HistoricalData { get; set; }
        public string LastAlertAcknowledged { get; set; }
        public string AlertStatus { get; set; }
    }

}