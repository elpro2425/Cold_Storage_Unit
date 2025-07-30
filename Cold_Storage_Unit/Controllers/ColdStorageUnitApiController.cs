using Cold_Storage_Unit.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web.Mvc;

namespace Cold_Storage_Unit.Controllers
{  
    public class ColdStorageUnitApiController : Controller
    {
        // GET api: ColdStorageUnit1/GetAll
        [System.Web.Mvc.HttpGet]
        public ActionResult GetAllUnit1()
        {
            List<ColdStorageUnit> units = new List<ColdStorageUnit>();

            using (var conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString))
            {
                conn.Open();
                var cmd = new MySqlCommand("SELECT * FROM ColdStorageUnit1 ORDER BY Hardwaredate DESC", conn);
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    units.Add(new ColdStorageUnit
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        Name = reader["Name"].ToString(),
                        Temperature = Convert.ToDouble(reader["Temperature"]),
                        Humidity = Convert.ToDouble(reader["Humidity"]),
                        PowerStatus = reader["PowerStatus"].ToString(),
                        DoorStatus = reader["DoorStatus"].ToString(),
                        Co2Level = Convert.ToDouble(reader["Co2Level"]),
                        EthyleneLevel = Convert.ToDouble(reader["EthyleneLevel"]),
                        FanSpeed = Convert.ToInt32(reader["FanSpeed"]),
                        Hardwaredate = reader["Hardwaredate"].ToString(),
                        AlertStatus = reader["AlertStatus"].ToString()
                    });
                }
            }
            return Json(units, JsonRequestBehavior.AllowGet);
        }

        // GET api: ColdStorageUnit2/GetAll
        [System.Web.Mvc.HttpGet]
        public ActionResult GetAllUnit2()
        {
            List<ColdStorageUnit> units = new List<ColdStorageUnit>();

            using (var conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString))
            {
                conn.Open();
                var cmd = new MySqlCommand("SELECT * FROM ColdStorageUnit2 ORDER BY Hardwaredate DESC", conn);
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    units.Add(new ColdStorageUnit
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        Name = reader["Name"].ToString(),
                        Temperature = Convert.ToDouble(reader["Temperature"]),
                        Humidity = Convert.ToDouble(reader["Humidity"]),
                        PowerStatus = reader["PowerStatus"].ToString(),
                        DoorStatus = reader["DoorStatus"].ToString(),
                        Co2Level = Convert.ToDouble(reader["Co2Level"]),
                        EthyleneLevel = Convert.ToDouble(reader["EthyleneLevel"]),
                        FanSpeed = Convert.ToInt32(reader["FanSpeed"]),
                        Hardwaredate = reader["Hardwaredate"].ToString(),
                        AlertStatus = reader["AlertStatus"].ToString()
                    });
                }
            }
            return Json(units, JsonRequestBehavior.AllowGet);
        }

        // POST api: ColdStorageUnit2/PostUnit
        [System.Web.Mvc.HttpPost]
        public ActionResult PostUnit(List<ColdStorageUnit> unit)
        {
            int count = 0;
            if (unit == null)
            {
                return Json(new { success = false, message = "Invalid unit data." });
            }
            using (var conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString))
            {
                conn.Open();

                for (int i = 0; i < unit.Count; i++)
                {
                    ColdStorageUnit localData = unit[i];
                    var cmd = new MySqlCommand(@"INSERT INTO ColdStorageUnit2 
            (Name, Temperature, Humidity, PowerStatus, DoorStatus, Co2Level, EthyleneLevel, FanSpeed, Hardwaredate, AlertStatus) 
            VALUES (@Name, @Temperature, @Humidity, @PowerStatus, @DoorStatus, @Co2Level, @EthyleneLevel, @FanSpeed, @Hardwaredate, @AlertStatus)", conn);

                    if (localData.Name=="Unit 1") 
                    {
                        cmd = new MySqlCommand(@"INSERT INTO ColdStorageUnit1 
            (Name, Temperature, Humidity, PowerStatus, DoorStatus, Co2Level, EthyleneLevel, FanSpeed, Hardwaredate, AlertStatus) 
            VALUES (@Name, @Temperature, @Humidity, @PowerStatus, @DoorStatus, @Co2Level, @EthyleneLevel, @FanSpeed, @Hardwaredate, @AlertStatus)", conn);

                    }
                    else if (localData.Name == "Unit 2")
                    {
                        cmd = new MySqlCommand(@"INSERT INTO ColdStorageUnit2 
            (Name, Temperature, Humidity, PowerStatus, DoorStatus, Co2Level, EthyleneLevel, FanSpeed, Hardwaredate, AlertStatus) 
            VALUES (@Name, @Temperature, @Humidity, @PowerStatus, @DoorStatus, @Co2Level, @EthyleneLevel, @FanSpeed, @Hardwaredate, @AlertStatus)", conn);

                    }

                    cmd.Parameters.AddWithValue("@Name", localData.Name);
                    cmd.Parameters.AddWithValue("@Temperature", localData.Temperature);
                    cmd.Parameters.AddWithValue("@Humidity", localData.Humidity);
                    cmd.Parameters.AddWithValue("@PowerStatus", localData.PowerStatus);
                    cmd.Parameters.AddWithValue("@DoorStatus", localData.DoorStatus);
                    cmd.Parameters.AddWithValue("@Co2Level", localData.Co2Level);
                    cmd.Parameters.AddWithValue("@EthyleneLevel", localData.EthyleneLevel);
                    cmd.Parameters.AddWithValue("@FanSpeed", localData.FanSpeed);
                    cmd.Parameters.AddWithValue("@Hardwaredate", localData.Hardwaredate);
                    cmd.Parameters.AddWithValue("@AlertStatus", localData.AlertStatus);

                    cmd.ExecuteNonQuery();
                    count++;
                }
            }
            return Json(new { success = true, message = count + " Record inserted successfully." });
        }

        [HttpGet]
        public ActionResult GetAlldoorstaus()
        {
            List<DoorStatus> doorStatuses = new List<DoorStatus>();

            using (var conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString))
            {
                conn.Open();
                var cmd = new MySqlCommand("SELECT * FROM door_status ORDER BY Hardwaredate DESC", conn);
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    doorStatuses.Add(new DoorStatus
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Unitname = reader["Unitname"].ToString(),
                        Status = reader["Status"].ToString(),
                        Hardwaredate = reader["Hardwaredate"].ToString()
                    });
                }
            }

            return Json(doorStatuses, JsonRequestBehavior.AllowGet);
        }

        // POST api: DoorStatus/PostDoorStatus
        [System.Web.Mvc.HttpPost]
        public ActionResult PostDoorStatus(List<DoorStatus> doorStatus)
        {
            if (doorStatus == null)
            {
                return Json(new { success = false, message = "Invalid door status data." });
            }

            using (var conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString))
            {
                conn.Open();

                for (int i = 0; i < doorStatus.Count; i++)
                {

                    DoorStatus statusdoor = doorStatus[i];
                    var cmd = new MySqlCommand(@"INSERT INTO door_status 
                    (Unitname, Status, Hardwaredate) 
                    VALUES (@Unitname, @Status, @Hardwaredate)", conn);

                    cmd.Parameters.AddWithValue("@Unitname", statusdoor.Unitname);
                    cmd.Parameters.AddWithValue("@Status", statusdoor.Status);
                    cmd.Parameters.AddWithValue("@Hardwaredate", statusdoor.Hardwaredate);

                    cmd.ExecuteNonQuery();
                }
            }

            return Json(new { success = true, message = "Record inserted successfully." });
        }

        //update code 
        [System.Web.Mvc.HttpPost]
        public ActionResult UpdateReadTimeColdStorage(ColdStorageUnit unit)
        {
            if (unit == null || string.IsNullOrWhiteSpace(unit.UnitName))
            {
                return Json(new { success = false, message = "Invalid unit data. UnitName is required." });
            }

            using (var conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString))
            {
                conn.Open();

                var cmd = new MySqlCommand(@"
            UPDATE ReadTimeColdStorage 
            SET Name = @Name,
                Temperature = @Temperature,
                Humidity = @Humidity,
                PowerStatus = @PowerStatus,
                DoorStatus = @DoorStatus,
                Co2Level = @Co2Level,
                EthyleneLevel = @EthyleneLevel,
                FanSpeed = @FanSpeed,
                Hardwaredate = @Hardwaredate
            WHERE UnitName = @UnitName", conn);

                cmd.Parameters.AddWithValue("@Name", unit.Name);
                cmd.Parameters.AddWithValue("@Temperature", unit.Temperature);
                cmd.Parameters.AddWithValue("@Humidity", unit.Humidity);
                cmd.Parameters.AddWithValue("@PowerStatus", unit.PowerStatus);
                cmd.Parameters.AddWithValue("@DoorStatus", unit.DoorStatus);
                cmd.Parameters.AddWithValue("@Co2Level", unit.Co2Level);
                cmd.Parameters.AddWithValue("@EthyleneLevel", unit.EthyleneLevel);
                cmd.Parameters.AddWithValue("@FanSpeed", unit.FanSpeed);
                cmd.Parameters.AddWithValue("@Hardwaredate", unit.Hardwaredate);
                cmd.Parameters.AddWithValue("@AlertStatus", unit.AlertStatus);
                cmd.Parameters.AddWithValue("@UnitName", unit.UnitName); 

                int rowsAffected = cmd.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    return Json(new { success = true, message = unit.Name + " Record is updated successfully." });
                }
                else
                {
                    return Json(new { success = false, message = "No record found with the given UnitName." });
                }
            }
        }
    }
}