using Cold_Storage_Unit.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
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

                    if (localData.Name == "Unit 1")
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

        [HttpPost]
        public ActionResult UpdateReadTimeColdStorage(ColdStorageUnit unit)
        {
            if (unit == null || string.IsNullOrWhiteSpace(unit.UnitName))
            {
                return Json(new { success = false, message = "Invalid unit data. UnitName is required." });
            }

            using (var conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. Update the Cold Storage record
                        var updateCmd = new MySqlCommand(@"
                    UPDATE ReadTimeColdStorage 
                    SET Name = @Name,
                        Temperature = @Temperature,
                        Humidity = @Humidity,
                        PowerStatus = @PowerStatus,
                        DoorStatus = @DoorStatus,
                        Co2Level = @Co2Level,
                        EthyleneLevel = @EthyleneLevel,
                        FanSpeed = @FanSpeed,
                        Hardwaredate = @Hardwaredate,
                        AlertStatus = @AlertStatus
                    WHERE UnitName = @UnitName", conn, transaction);

                        updateCmd.Parameters.AddWithValue("@Name", unit.Name);
                        updateCmd.Parameters.AddWithValue("@Temperature", unit.Temperature);
                        updateCmd.Parameters.AddWithValue("@Humidity", unit.Humidity);
                        updateCmd.Parameters.AddWithValue("@PowerStatus", unit.PowerStatus);
                        updateCmd.Parameters.AddWithValue("@DoorStatus", unit.DoorStatus);
                        updateCmd.Parameters.AddWithValue("@Co2Level", unit.Co2Level);
                        updateCmd.Parameters.AddWithValue("@EthyleneLevel", unit.EthyleneLevel);
                        updateCmd.Parameters.AddWithValue("@FanSpeed", unit.FanSpeed);
                        updateCmd.Parameters.AddWithValue("@Hardwaredate", unit.Hardwaredate);
                        updateCmd.Parameters.AddWithValue("@AlertStatus", unit.AlertStatus);
                        updateCmd.Parameters.AddWithValue("@UnitName", unit.UnitName);

                        int rowsAffected = updateCmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            // 2. Get all enabled Settings rows for this unit
                            var selectSettingCmd = new MySqlCommand(@"
                        SELECT * FROM Settings 
                        WHERE UnitName = @UnitName AND enabled = 1", conn, transaction);

                            selectSettingCmd.Parameters.AddWithValue("@UnitName", unit.UnitName);

                            var settingsList = new List<dynamic>();

                            using (var reader = selectSettingCmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    settingsList.Add(new
                                    {
                                        ID = Convert.ToInt32(reader["id"]),
                                        Category = reader["Category"].ToString(),
                                        Condition = reader["Condition_Trigger"].ToString(),
                                        Message = reader["message_display"].ToString(),
                                        Severity = reader["enabled_sensitivity"].ToString(),
                                        Remarks = reader["Remarks"].ToString(),
                                        TimeInMinutes = TimeSpan.TryParse(reader["TimeInMinutes"].ToString(), out var t) ? t.Minutes : 0

                                    });
                                }
                            }

                            // Loop through settings
                            foreach (var setting in settingsList)
                            {
                                double actualValue = 0;

                                switch (setting.Category.ToLower())
                                {
                                    case "temperature":
                                        actualValue = unit.Temperature;
                                        break;
                                    case "humidity":
                                        actualValue = unit.Humidity;
                                        break;
                                    case "co2":
                                    case "co2level":
                                        actualValue = unit.Co2Level;
                                        break;
                                    case "ethylene":
                                    case "ethylenelevel":
                                        actualValue = unit.EthyleneLevel;
                                        break;
                                    default:
                                        continue;
                                }

                                bool isConditionMet = EvaluateCondition(setting.Condition, actualValue);

                                if (!isConditionMet)
                                    continue;

                                // 3. Check last alert for same SettingsID
                                var lastAlertCmd = new MySqlCommand(@"
                            SELECT Alert_Date FROM Alerts 
                            WHERE SettingsID = @SettingsID 
                            ORDER BY Alert_Date DESC 
                            LIMIT 1", conn, transaction);

                                lastAlertCmd.Parameters.AddWithValue("@SettingsID", setting.ID);

                                var lastAlertDateObj = lastAlertCmd.ExecuteScalar();
                                bool shouldInsert = true;

                                if (lastAlertDateObj != null && DateTime.TryParse(lastAlertDateObj.ToString(), out DateTime lastAlertDate))
                                {
                                    double minutesSinceLast = (DateTime.Now - lastAlertDate).TotalMinutes;
                                    if (minutesSinceLast < setting.TimeInMinutes)
                                    {
                                        shouldInsert = false; // too soon
                                    }
                                }

                                if (!shouldInsert)
                                    continue;

                                // 4. Insert alert
                                var insertAlertCmd = new MySqlCommand(@"
                            INSERT INTO Alerts 
                            (Alert_Name, Condition_Trigger, Severity, Remarks, Alert_Date, UnitName, Actual_Value, SettingsID)
                            VALUES 
                            (@AlertName, @Condition, @Severity, @Remarks, @AlertDate, @UnitName, @ActualValue, @SettingsID)", conn, transaction);

                                insertAlertCmd.Parameters.AddWithValue("@AlertName", setting.Message);
                                insertAlertCmd.Parameters.AddWithValue("@Condition", setting.Condition);
                                insertAlertCmd.Parameters.AddWithValue("@Severity", setting.Severity);
                                insertAlertCmd.Parameters.AddWithValue("@Remarks", setting.Remarks);
                                insertAlertCmd.Parameters.AddWithValue("@AlertDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                                insertAlertCmd.Parameters.AddWithValue("@UnitName", unit.UnitName);
                                insertAlertCmd.Parameters.AddWithValue("@ActualValue", actualValue);
                                insertAlertCmd.Parameters.AddWithValue("@SettingsID", setting.ID);

                                insertAlertCmd.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                        return Json(new { success = true, message = "Data updated and alerts evaluated." });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return Json(new { success = false, message = "Error: " + ex.Message });
                    }
                }
            }
        }
        private double ExtractThresholdValue(string condition)
        {
            var match = System.Text.RegularExpressions.Regex.Match(condition, @"[-+]?[0-9]*\.?[0-9]+");
            return match.Success ? Convert.ToDouble(match.Value) : 0;
        }

        private bool EvaluateCondition(string condition, double actualValue)
        {
            condition = condition.Replace("°C", "").Replace("Temp", "").Trim();
            double threshold = ExtractThresholdValue(condition);

            if (condition.Contains(">="))
                return actualValue >= threshold;
            if (condition.Contains("<="))
                return actualValue <= threshold;
            if (condition.Contains(">"))
                return actualValue > threshold;
            if (condition.Contains("<"))
                return actualValue < threshold;
            if (condition.Contains("="))
                return Math.Abs(actualValue - threshold) < 0.0001;

            return false;
        }

        [HttpGet]
        public ActionResult EditTemperature(string unitName = null, string category = null)
        {
            var connString = ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString;

            // Get distinct UnitNames
            var units = new List<string>();
            using (var conn = new MySqlConnection(connString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand("SELECT DISTINCT UnitName FROM ReadTimeColdStorage ORDER BY UnitName", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        units.Add(reader.GetString(0));
                }
            }

            // Get distinct Categories
            var categories = new List<string>();
            using (var conn = new MySqlConnection(connString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand("SELECT DISTINCT Category FROM Settings ORDER BY Category", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        categories.Add(reader.GetString(0));
                }
            }

            // For filtering table and dropdown selection
            string unitNameFilter = unitName;
            string categoryFilter = category;

            ViewBag.UnitNames = new SelectList(units, unitNameFilter);
            ViewBag.Categories = new SelectList(categories, categoryFilter);

            var model = new Alertempareture();

            if (!string.IsNullOrEmpty(unitName) && !string.IsNullOrEmpty(category))
            {
                using (var conn = new MySqlConnection(connString))
                {
                    conn.Open();
                    string sql = "SELECT * FROM Settings WHERE UnitName = @unitName AND Category = @category";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@unitName", unitName);
                        cmd.Parameters.AddWithValue("@category", category);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                model.Id = reader.GetInt32(reader.GetOrdinal("id"));
                                model.UnitName = reader.GetString(reader.GetOrdinal("UnitName"));
                                model.threshold = reader.IsDBNull(reader.GetOrdinal("threshold")) ? 0 : reader.GetDouble(reader.GetOrdinal("threshold"));
                                model.message_display = reader.IsDBNull(reader.GetOrdinal("message_display")) ? "" : reader.GetString(reader.GetOrdinal("message_display"));
                                model.enabled = reader.IsDBNull(reader.GetOrdinal("enabled")) ? false : reader.GetInt32(reader.GetOrdinal("enabled")) == 1;
                                model.enabled_sensitivity = reader.IsDBNull(reader.GetOrdinal("enabled_sensitivity")) ? "" : reader.GetString(reader.GetOrdinal("enabled_sensitivity"));
                                model.TimeInMinutes = reader.IsDBNull(reader.GetOrdinal("TimeInMinutes"))
      ? TimeSpan.Zero
      : reader.GetTimeSpan(reader.GetOrdinal("TimeInMinutes"));
                                model.Condition_Trigger = reader.IsDBNull(reader.GetOrdinal("Condition_Trigger")) ? "" : reader.GetString(reader.GetOrdinal("Condition_Trigger"));
                                model.Remarks = reader.IsDBNull(reader.GetOrdinal("Remarks")) ? "" : reader.GetString(reader.GetOrdinal("Remarks"));
                                model.Category = category;
                            }
                            else
                            {
                                model.UnitName = unitName;
                                model.Category = category;
                            }
                        }
                    }
                }
            }

            // Load filtered settings for table display
            var allSettings = new List<Alertempareture>();
            using (var conn = new MySqlConnection(connString))
            {
                conn.Open();

                string query = "SELECT * FROM Settings WHERE 1=1";
                var cmd = new MySqlCommand();
                cmd.Connection = conn;

                if (!string.IsNullOrEmpty(categoryFilter))
                {
                    query += " AND Category = @category";
                    cmd.Parameters.AddWithValue("@category", categoryFilter);
                }

                if (!string.IsNullOrEmpty(unitNameFilter))
                {
                    query += " AND UnitName = @unitNameFilter";
                    cmd.Parameters.AddWithValue("@unitNameFilter", unitNameFilter);
                }

                cmd.CommandText = query;

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        allSettings.Add(new Alertempareture
                        {
                            Id = reader.GetInt32("id"),
                            UnitName = reader.GetString("UnitName"),
                            Category = reader.GetString("Category"),
                            threshold = reader.IsDBNull(reader.GetOrdinal("threshold")) ? 0 : Math.Round(reader.GetDouble(reader.GetOrdinal("threshold")), 2),
                            message_display = reader.IsDBNull(reader.GetOrdinal("message_display")) ? "" : reader.GetString("message_display"),
                            Condition_Trigger = reader.IsDBNull(reader.GetOrdinal("Condition_Trigger")) ? "" : reader.GetString("Condition_Trigger"),
                            enabled_sensitivity = reader.IsDBNull(reader.GetOrdinal("enabled_sensitivity")) ? "" : reader.GetString("enabled_sensitivity"),
                            TimeInMinutes = reader.IsDBNull(reader.GetOrdinal("TimeInMinutes")) ? TimeSpan.Zero : reader.GetTimeSpan(reader.GetOrdinal("TimeInMinutes")),
                            Remarks = reader.IsDBNull(reader.GetOrdinal("Remarks")) ? "" : reader.GetString("Remarks"),
                            enabled = reader.IsDBNull(reader.GetOrdinal("enabled")) ? false : reader.GetInt32("enabled") == 1,
                        });
                    }
                }
            }

            ViewBag.AllSettings = allSettings;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateTemperature(Alertempareture model)
        {
            var connString = ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString;
            using (var conn = new MySqlConnection(connString))
            {
                conn.Open();
                if (model.Id > 0)
                {
                    // Update existing
                    string updateQuery = @"
                UPDATE Settings SET 
                UnitName = @UnitName,
                Category = @Category,
                threshold = @threshold,
                message_display = @message_display,
                Condition_Trigger = @Condition_Trigger,
                enabled_sensitivity = @enabled_sensitivity,
                TimeInMinutes = @TimeInMinutes,
                Remarks = @Remarks,
                enabled = @enabled
                WHERE id = @Id";

                    using (var cmd = new MySqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@UnitName", model.UnitName);
                        cmd.Parameters.AddWithValue("@Category", model.Category);
                        cmd.Parameters.AddWithValue("@threshold", model.threshold);
                        cmd.Parameters.AddWithValue("@message_display", model.message_display ?? "");
                        cmd.Parameters.AddWithValue("@Condition_Trigger", model.Condition_Trigger ?? "");
                        cmd.Parameters.AddWithValue("@enabled_sensitivity", model.enabled_sensitivity ?? "");
                        cmd.Parameters.AddWithValue("@TimeInMinutes", model.TimeInMinutes);
                        cmd.Parameters.AddWithValue("@Remarks", model.Remarks ?? "");
                        cmd.Parameters.AddWithValue("@enabled", model.enabled ? 1 : 0);
                        cmd.Parameters.AddWithValue("@Id", model.Id);

                        cmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    // Insert new record
                    string insertQuery = @"
                INSERT INTO Settings
                (UnitName, Category, threshold, message_display, Condition_Trigger, enabled_sensitivity, TimeInMinutes, Remarks, enabled)
                VALUES
                (@UnitName, @Category, @threshold, @message_display, @Condition_Trigger, @enabled_sensitivity, @TimeInMinutes, @Remarks, @enabled)";

                    using (var cmd = new MySqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@UnitName", model.UnitName);
                        cmd.Parameters.AddWithValue("@Category", model.Category);
                        cmd.Parameters.AddWithValue("@threshold", model.threshold);
                        cmd.Parameters.AddWithValue("@message_display", model.message_display ?? "");
                        cmd.Parameters.AddWithValue("@Condition_Trigger", model.Condition_Trigger ?? "");
                        cmd.Parameters.AddWithValue("@enabled_sensitivity", model.enabled_sensitivity ?? "");
                        cmd.Parameters.AddWithValue("@TimeInMinutes", model.TimeInMinutes);
                        cmd.Parameters.AddWithValue("@Remarks", model.Remarks ?? "");
                        cmd.Parameters.AddWithValue("@enabled", model.enabled ? 1 : 0);

                        cmd.ExecuteNonQuery();
                    }
                }
            }

            TempData["SuccessMessage"] = model.Id > 0 ? "Record updated successfully." : "Record added successfully.";
            return RedirectToAction("EditTemperature");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteTemperature(string unitName, string category)
        {
            var connString = ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString;

            using (var conn = new MySqlConnection(connString))
            {
                conn.Open();

                string sql = "DELETE FROM Settings WHERE UnitName = @unitName AND Category = @category";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@unitName", unitName);
                    cmd.Parameters.AddWithValue("@category", category);
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["SuccessMessage"] = $"Setting for '{unitName}' in category '{category}' has been deleted.";
            return RedirectToAction("EditTemperature");
        }

    }
}