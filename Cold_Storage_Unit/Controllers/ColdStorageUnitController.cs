using Cold_Storage_Unit.Models;
using System.Collections.Generic;
using System.Configuration;
using System;
using System.Web.Mvc;
using MySql.Data.MySqlClient;

namespace Cold_Storage_Unit.Controllers
{
    public class ColdStorageUnitController : Controller
    {
        // GET: ColdStorageUnit
        public ActionResult Index()
        {
            ColdStorageUnit latest = null;
            List<string> timestamps = new List<string>();
            List<double> temperatures = new List<double>();
            List<double> humidities = new List<double>();

            using (var conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString))
            {
                conn.Open();

                // Get latest for the current info
                var cmd = new MySqlCommand("SELECT * FROM ColdStorageUnit ORDER BY Timestamp DESC LIMIT 1", conn);
                var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    latest = new ColdStorageUnit
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
                    };
                }
                reader.Close();

                // Get last 10 or 20 historical rows
                var historyCmd = new MySqlCommand("SELECT Temperature, Humidity, Timestamp FROM ColdStorageUnit ORDER BY Timestamp DESC LIMIT 20", conn);
                var historyReader = historyCmd.ExecuteReader();

                while (historyReader.Read())
                {
                    temperatures.Insert(0, Convert.ToDouble(historyReader["Temperature"]));
                    humidities.Insert(0, Convert.ToDouble(historyReader["Humidity"]));
                    var timestampObj = historyReader["Timestamp"];

                    if (timestampObj != DBNull.Value && DateTime.TryParse(timestampObj.ToString(), out DateTime parsedTimestamp))
                    {
                        timestamps.Insert(0, parsedTimestamp.ToString("HH:mm:ss"));
                    }
                    else
                    {
                        timestamps.Insert(0, "Invalid");
                    }
                }
            }
            ViewBag.Timestamps = timestamps;
            ViewBag.Temperatures = temperatures;
            ViewBag.Humidities = humidities;

            return View(latest);
        }

        [HttpGet]
        public JsonResult GetRecentRows()
        {
            List<ColdStorageUnit> rows = new List<ColdStorageUnit>();

            using (var conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString))
            {
                conn.Open();
                var cmd = new MySqlCommand("SELECT * FROM ColdStorageUnit ORDER BY Timestamp DESC LIMIT 20", conn);
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    rows.Add(new ColdStorageUnit
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
                        Timestamp = reader["Timestamp"].ToString()
                    });
                }
            }
            return Json(rows, JsonRequestBehavior.AllowGet);
        }
    }
}