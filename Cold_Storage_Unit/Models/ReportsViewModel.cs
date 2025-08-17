using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Cold_Storage_Unit.Models
{
    public class ReportsViewModel
    {
        public List<ColdStorageUnit> ColdStorageData { get; set; }
        public List<DoorStatus> DoorStatusData { get; set; }
        public List<Alert> AlertsData { get; set; }

    }

}