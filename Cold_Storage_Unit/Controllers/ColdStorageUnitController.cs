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
        public ActionResult Index()
        {
            ColdStorageUnit latest1 = null;
            ColdStorageUnit latest2 = null;

            List<string> timestamps1 = new List<string>();
            List<double> temperatures1 = new List<double>();
            List<double> humidities1 = new List<double>();

            List<string> timestamps2 = new List<string>();
            List<double> temperatures2 = new List<double>();
            List<double> humidities2 = new List<double>();

            // Declare alert lists here, OUTSIDE the using block:
            List<Alert> latestAlertsUnit1 = new List<Alert>();
            List<Alert> latestAlertsUnit2 = new List<Alert>();

            using (var conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString))
            {
                conn.Open();

                // Latest for Unit 1
                var cmd1 = new MySqlCommand("SELECT * FROM ColdStorageUnit1 ORDER BY STR_TO_DATE(Hardwaredate, '%e/%c/%Y, %r') DESC LIMIT 1", conn);
                var reader1 = cmd1.ExecuteReader();
                if (reader1.Read())
                {
                    latest1 = new ColdStorageUnit
                    {
                        Id = Convert.ToInt32(reader1["id"]),
                        Name = reader1["Name"].ToString(),
                        Temperature = Convert.ToDouble(reader1["Temperature"]),
                        Humidity = Convert.ToDouble(reader1["Humidity"]),
                        PowerStatus = reader1["PowerStatus"].ToString(),
                        DoorStatus = reader1["DoorStatus"].ToString(),
                        Co2Level = Convert.ToDouble(reader1["Co2Level"]),
                        EthyleneLevel = Convert.ToDouble(reader1["EthyleneLevel"]),
                        FanSpeed = Convert.ToInt32(reader1["FanSpeed"]),
                        Hardwaredate = reader1["Hardwaredate"].ToString(),
                        AlertStatus = reader1["AlertStatus"].ToString()
                    };
                }
                reader1.Close();

                // 24 Hour History for Unit 1
                var historyCmd1 = new MySqlCommand(@"
SELECT *
FROM ColdStorageUnit1
WHERE STR_TO_DATE(Hardwaredate, '%e/%c/%Y, %r') BETWEEN (
    SELECT MAX(STR_TO_DATE(Hardwaredate, '%e/%c/%Y, %r')) - INTERVAL 1 DAY
    FROM ColdStorageUnit1
) AND (
    SELECT MAX(STR_TO_DATE(Hardwaredate, '%e/%c/%Y, %r'))
    FROM ColdStorageUnit1
)
ORDER BY STR_TO_DATE(Hardwaredate, '%e/%c/%Y, %r') ASC", conn);
                var historyReader1 = historyCmd1.ExecuteReader();
                while (historyReader1.Read())
                {
                    temperatures1.Add(Convert.ToDouble(historyReader1["Temperature"]));
                    humidities1.Add(Convert.ToDouble(historyReader1["Humidity"]));

                    var timestampObj = historyReader1["Hardwaredate"];
                    if (timestampObj != DBNull.Value && DateTime.TryParse(timestampObj.ToString(), out DateTime parsedTimestamp))
                        timestamps1.Add(parsedTimestamp.ToString("hh:mm:ss tt"));
                    else
                        timestamps1.Add("Invalid");
                }
                historyReader1.Close();

                // Latest for Unit 2
                var cmd2 = new MySqlCommand("SELECT * FROM ColdStorageUnit2 ORDER BY STR_TO_DATE(Hardwaredate, '%e/%c/%Y, %r') DESC LIMIT 1", conn);
                var reader2 = cmd2.ExecuteReader();
                if (reader2.Read())
                {
                    latest2 = new ColdStorageUnit
                    {
                        Id = Convert.ToInt32(reader2["id"]),
                        Name = reader2["Name"].ToString(),
                        Temperature = Convert.ToDouble(reader2["Temperature"]),
                        Humidity = Convert.ToDouble(reader2["Humidity"]),
                        PowerStatus = reader2["PowerStatus"].ToString(),
                        DoorStatus = reader2["DoorStatus"].ToString(),
                        Co2Level = Convert.ToDouble(reader2["Co2Level"]),
                        EthyleneLevel = Convert.ToDouble(reader2["EthyleneLevel"]),
                        FanSpeed = Convert.ToInt32(reader2["FanSpeed"]),
                        Hardwaredate = reader2["Hardwaredate"].ToString(),
                        AlertStatus = reader2["AlertStatus"].ToString()
                    };
                }
                reader2.Close();


                // Latest 5 alerts for Unit 1
                var alertsCmd1 = new MySqlCommand(@"
        SELECT * FROM Alerts
WHERE UnitName = 'Unit 1'
  AND STR_TO_DATE(Alert_Date, '%Y-%m-%d %H:%i:%s') >= NOW() - INTERVAL 1 DAY
ORDER BY STR_TO_DATE(Alert_Date, '%Y-%m-%d %H:%i:%s') DESC", conn);

                var alertsReader1 = alertsCmd1.ExecuteReader();
                while (alertsReader1.Read())
                {
                    latestAlertsUnit1.Add(new Alert
                    {
                        ID = Convert.ToInt32(alertsReader1["ID"]),
                        Alert_Name = alertsReader1["Alert_Name"].ToString(),
                        Condition_Trigger = alertsReader1["Condition_Trigger"].ToString(),
                        Severity = alertsReader1["Severity"].ToString(),
                        Remarks = alertsReader1["Remarks"].ToString(),
                        Alert_Date = alertsReader1["Alert_Date"].ToString(),
                        UnitName = alertsReader1["UnitName"].ToString(),
                        Actual_Value = Convert.ToDouble(alertsReader1["Actual_Value"])

                    });
                }
                alertsReader1.Close();

                // Latest 5 alerts for Unit 2
                var alertsCmd2 = new MySqlCommand(@"
       SELECT * FROM Alerts
WHERE UnitName = 'Unit 2'
  AND STR_TO_DATE(Alert_Date, '%Y-%m-%d %H:%i:%s') >= NOW() - INTERVAL 1 DAY
ORDER BY STR_TO_DATE(Alert_Date, '%Y-%m-%d %H:%i:%s') DESC", conn);

                var alertsReader2 = alertsCmd2.ExecuteReader();
                while (alertsReader2.Read())
                {
                    latestAlertsUnit2.Add(new Alert
                    {
                        ID = Convert.ToInt32(alertsReader2["ID"]),
                        Alert_Name = alertsReader2["Alert_Name"].ToString(),
                        Condition_Trigger = alertsReader2["Condition_Trigger"].ToString(),
                        Severity = alertsReader2["Severity"].ToString(),
                        Remarks = alertsReader2["Remarks"].ToString(),
                        Alert_Date = alertsReader2["Alert_Date"].ToString(),
                        UnitName = alertsReader2["UnitName"].ToString(),
                        Actual_Value = Convert.ToDouble(alertsReader2["Actual_Value"])

                    });
                }
                alertsReader2.Close();
            


            // 24 Hour History for Unit 2
            var historyCmd2 = new MySqlCommand(@"
SELECT *
FROM ColdStorageUnit2
WHERE STR_TO_DATE(Hardwaredate, '%e/%c/%Y, %r') BETWEEN (
    SELECT MAX(STR_TO_DATE(Hardwaredate, '%e/%c/%Y, %r')) - INTERVAL 1 DAY
    FROM ColdStorageUnit2
) AND (
    SELECT MAX(STR_TO_DATE(Hardwaredate, '%e/%c/%Y, %r'))
    FROM ColdStorageUnit2
)
ORDER BY STR_TO_DATE(Hardwaredate, '%e/%c/%Y, %r') ASC", conn);
                var historyReader2 = historyCmd2.ExecuteReader();
                while (historyReader2.Read())
                {
                    temperatures2.Add(Convert.ToDouble(historyReader2["Temperature"]));
                    humidities2.Add(Convert.ToDouble(historyReader2["Humidity"]));

                    var timestampObj = historyReader2["Hardwaredate"];
                    if (timestampObj != DBNull.Value && DateTime.TryParse(timestampObj.ToString(), out DateTime parsedTimestamp))
                        timestamps2.Add(parsedTimestamp.ToString("hh:mm:ss tt"));
                    else
                        timestamps2.Add("Invalid");
                }
                historyReader2.Close();
            }

            ViewBag.LatestAlertsUnit1 = latestAlertsUnit1;
            ViewBag.LatestAlertsUnit2 = latestAlertsUnit2;


            ViewBag.Timestamps1 = timestamps1;
            ViewBag.Temperatures1 = temperatures1;
            ViewBag.Humidities1 = humidities1;

            ViewBag.Timestamps2 = timestamps2;
            ViewBag.Temperatures2 = temperatures2;
            ViewBag.Humidities2 = humidities2;
            ViewBag.Latest2 = latest2;

            if (latest1 == null)
                latest1 = new ColdStorageUnit();

            return View(latest1);
        }


        [HttpGet]
        public JsonResult GetLatestRows()
        {
            var result = new List<object>();

            using (var conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString))
            {
                conn.Open();

                // Step 1: Get all distinct UnitNames from DB
                var unitNames = new List<string>();
                var unitCmd = new MySqlCommand("SELECT DISTINCT UnitName FROM ReadTimeColdStorage", conn);
                using (var reader = unitCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        unitNames.Add(reader["UnitName"].ToString());
                    }
                }

                // Step 2: For each unit, get latest record and time gap
                foreach (var unitName in unitNames)
                {
                    ColdStorageUnit latest = null;
                    string timeGapStr = "N/A";

                    // Get latest record
                    var cmd = new MySqlCommand(@"
                SELECT * FROM ReadTimeColdStorage 
                WHERE UnitName = @unitName 
                ORDER BY STR_TO_DATE(Hardwaredate, '%d/%m/%Y, %h:%i:%s %p') DESC 
                LIMIT 1", conn);
                    cmd.Parameters.AddWithValue("@unitName", unitName);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            latest = new ColdStorageUnit
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
                                AlertStatus = reader["AlertStatus"].ToString(),
                                UnitName = reader["UnitName"].ToString()
                            };
                        }
                    }

                    // Get time gap
                    if (latest != null)
                    {
                        var gapCmd = new MySqlCommand(@"
                    SELECT Hardwaredate FROM ReadTimeColdStorage 
                    WHERE UnitName = @unitName 
                    ORDER BY STR_TO_DATE(Hardwaredate, '%d/%m/%Y, %h:%i:%s %p') DESC 
                    LIMIT 2", conn);
                        gapCmd.Parameters.AddWithValue("@unitName", unitName);

                        var timestamps = new List<DateTime>();
                        using (var gapReader = gapCmd.ExecuteReader())
                        {
                            while (gapReader.Read())
                            {
                                if (DateTime.TryParse(gapReader["Hardwaredate"].ToString(), out DateTime dt))
                                    timestamps.Add(dt);
                            }
                        }

                        if (timestamps.Count == 2)
                        {
                            TimeSpan gap = timestamps[0] - timestamps[1];
                            timeGapStr = $"{(int)gap.TotalMinutes} min {gap.Seconds} sec";
                        }
                    }
                    result.Add(new { UnitName = unitName, Latest = latest, TimeGap = timeGapStr });
                }
            }
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        private List<T> FilterByMinuteInterval<T>(List<T> fullList, List<string> timestamps, int intervalMinutes, out List<string> filteredTimestamps)
        {
            filteredTimestamps = new List<string>();
            var filteredValues = new List<T>();

            for (int i = 0; i < timestamps.Count; i++)
            {
                if (DateTime.TryParse(timestamps[i], out DateTime parsed))
                {
                    if (parsed.Minute % intervalMinutes == 0)
                    {
                        filteredTimestamps.Add(parsed.ToString("hh:mm tt"));
                        filteredValues.Add(fullList[i]);
                    }
                }
            }

            return filteredValues;
        }

        [HttpGet]
        public JsonResult GetChartData()
        {
            var timestamps1 = new List<string>();
            var temperatures1 = new List<double>();
            var humidities1 = new List<double>();

            var timestamps2 = new List<string>();
            var temperatures2 = new List<double>();
            var humidities2 = new List<double>();

            using (var conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString))
            {
                conn.Open();

                var historyCmd1 = new MySqlCommand(@"
SELECT *
FROM ColdStorageUnit1
WHERE STR_TO_DATE(Hardwaredate, '%e/%c/%Y, %r') BETWEEN (
    SELECT MAX(STR_TO_DATE(Hardwaredate, '%e/%c/%Y, %r')) - INTERVAL 1 DAY
    FROM ColdStorageUnit1
) AND (
    SELECT MAX(STR_TO_DATE(Hardwaredate, '%e/%c/%Y, %r'))
    FROM ColdStorageUnit1
)
ORDER BY STR_TO_DATE(Hardwaredate, '%e/%c/%Y, %r') ASC", conn);

                var reader1 = historyCmd1.ExecuteReader();
                while (reader1.Read())
                {
                    temperatures1.Add(Convert.ToDouble(reader1["Temperature"]));
                    humidities1.Add(Convert.ToDouble(reader1["Humidity"]));
                    var timestampObj = reader1["Hardwaredate"];
                    if (timestampObj != DBNull.Value && DateTime.TryParse(timestampObj.ToString(), out DateTime parsedTimestamp))
                        timestamps1.Add(parsedTimestamp.ToString("hh:mm:ss tt"));
                    else
                        timestamps1.Add("Invalid");
                }
                reader1.Close();

                var historyCmd2 = new MySqlCommand(@"
SELECT *
FROM ColdStorageUnit2
WHERE STR_TO_DATE(Hardwaredate, '%e/%c/%Y, %r') BETWEEN (
    SELECT MAX(STR_TO_DATE(Hardwaredate, '%e/%c/%Y, %r')) - INTERVAL 1 DAY
    FROM ColdStorageUnit2
) AND (
    SELECT MAX(STR_TO_DATE(Hardwaredate, '%e/%c/%Y, %r'))
    FROM ColdStorageUnit2
)
ORDER BY STR_TO_DATE(Hardwaredate, '%e/%c/%Y, %r') ASC", conn);

                var reader2 = historyCmd2.ExecuteReader();
                while (reader2.Read())
                {
                    temperatures2.Add(Convert.ToDouble(reader2["Temperature"]));
                    humidities2.Add(Convert.ToDouble(reader2["Humidity"]));
                    var timestampObj = reader2["Hardwaredate"];
                    if (timestampObj != DBNull.Value && DateTime.TryParse(timestampObj.ToString(), out DateTime parsedTimestamp))
                        timestamps2.Add(parsedTimestamp.ToString("hh:mm:ss tt"));
                    else
                        timestamps2.Add("Invalid");
                }
                reader2.Close();
            }

            // Filter by 20-minute interval
            temperatures1 = FilterByMinuteInterval(temperatures1, timestamps1, 20, out timestamps1);
            humidities1 = FilterByMinuteInterval(humidities1, timestamps1, 20, out _); // timestamps1 already filtered

            temperatures2 = FilterByMinuteInterval(temperatures2, timestamps2, 20, out timestamps2);
            humidities2 = FilterByMinuteInterval(humidities2, timestamps2, 20, out _); // timestamps2 already filtered

            return Json(new
            {
                timestamps1,
                temperatures1,
                humidities1,
                timestamps2,
                temperatures2,
                humidities2
            }, JsonRequestBehavior.AllowGet);
        }
    }
}
