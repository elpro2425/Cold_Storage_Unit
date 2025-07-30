using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Cold_Storage_Unit.Models
{
    public class DoorStatus
    {
        public int Id { get; set; }
        public string Unitname { get; set; }
        public string Status { get; set; }
        public string Hardwaredate { get; set; }
    }
}