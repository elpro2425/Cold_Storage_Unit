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
           // GET api: ColdStorageUnit/GetAll
            [System.Web.Mvc.HttpGet]
            public ActionResult GetAll()
            {
                List<ColdStorageUnit> units = new List<ColdStorageUnit>();

                using (var conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString))
                {
                    conn.Open();
                    var cmd = new MySqlCommand("SELECT * FROM ColdStorageUnit ORDER BY Timestamp DESC", conn);
                    var reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        units.Add(new ColdStorageUnit
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            Name = reader["Name"].ToString(),
                            Temperature = Convert.ToDouble(reader["Temperature"]),
                            Humidity = Convert.ToDouble(reader["Humidity"]),
                            RipenessStage = Convert.ToInt32(reader["RipenessStage"]),
                            PowerStatus = reader["PowerStatus"].ToString(),
                            DoorStatus = reader["DoorStatus"].ToString(),
                            Co2Level = Convert.ToDouble(reader["Co2Level"]),
                            EthyleneLevel = Convert.ToDouble(reader["EthyleneLevel"]),
                            FanSpeed = Convert.ToInt32(reader["FanSpeed"]),
                            LastUpdated = reader["LastUpdated"].ToString(),
                            Timestamp = reader["Timestamp"].ToString(),
                            HistoricalData = reader["HistoricalData"].ToString(),
                            LastAlertAcknowledged = reader["LastAlertAcknowledged"].ToString(),
                            AlertStatus = reader["AlertStatus"].ToString()
                        });
                    }
                }
                // Return JSON result
                return Json(units, JsonRequestBehavior.AllowGet);
            }

            // POST api: ColdStorageUnit/PostUnit
            [System.Web.Mvc.HttpPost]
            public ActionResult PostUnit(ColdStorageUnit unit)
            {
                if (unit == null)
                {
                    return Json(new { success = false, message = "Invalid unit data." });
                }

                using (var conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString))
                {
                    conn.Open();

                    var cmd = new MySqlCommand(@"INSERT INTO ColdStorageUnit 
                    (Name, Temperature, Humidity, RipenessStage, PowerStatus, DoorStatus, Co2Level, EthyleneLevel, FanSpeed, LastUpdated, Timestamp, HistoricalData, LastAlertAcknowledged, AlertStatus) 
                    VALUES (@Name, @Temperature, @Humidity, @RipenessStage, @PowerStatus, @DoorStatus, @Co2Level, @EthyleneLevel, @FanSpeed, @LastUpdated, @Timestamp, @HistoricalData, @LastAlertAcknowledged, @AlertStatus)", conn);

                    cmd.Parameters.AddWithValue("@Name", unit.Name);
                    cmd.Parameters.AddWithValue("@Temperature", unit.Temperature);
                    cmd.Parameters.AddWithValue("@Humidity", unit.Humidity);
                    cmd.Parameters.AddWithValue("@RipenessStage", unit.RipenessStage);
                    cmd.Parameters.AddWithValue("@PowerStatus", unit.PowerStatus);
                    cmd.Parameters.AddWithValue("@DoorStatus", unit.DoorStatus);
                    cmd.Parameters.AddWithValue("@Co2Level", unit.Co2Level);
                    cmd.Parameters.AddWithValue("@EthyleneLevel", unit.EthyleneLevel);
                    cmd.Parameters.AddWithValue("@FanSpeed", unit.FanSpeed);
                    cmd.Parameters.AddWithValue("@LastUpdated", unit.LastUpdated);
                    cmd.Parameters.AddWithValue("@Timestamp", unit.Timestamp);
                    cmd.Parameters.AddWithValue("@HistoricalData", unit.HistoricalData);
                    cmd.Parameters.AddWithValue("@LastAlertAcknowledged", unit.LastAlertAcknowledged);
                    cmd.Parameters.AddWithValue("@AlertStatus", unit.AlertStatus);

                    cmd.ExecuteNonQuery();
                }

                return Json(new { success = true, message = "Record inserted successfully." });
            }
        }
    }