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
        //door get data
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
        //update realtime data
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
        //edit setting data
        [HttpGet]
        public ActionResult EditTemperature(int? id = null, string unitName = null, string category = null)
        {
            var connString = ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString;

            var units = new List<string>();
            var categories = new List<string>();
            var allSettings = new List<Alertempareture>();
            var model = new Alertempareture();

            using (var conn = new MySqlConnection(connString))
            {
                conn.Open();

                // Load UnitNames
                using (var cmd = new MySqlCommand("SELECT DISTINCT UnitName FROM ReadTimeColdStorage ORDER BY UnitName", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        units.Add(reader.GetString(0));
                }

                // Load Categories
                using (var cmd = new MySqlCommand("SELECT DISTINCT Category FROM Settings ORDER BY Category", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        categories.Add(reader.GetString(0));
                }

                // ✅ EDIT MODE (load by ID only)
                if (id.HasValue)
                {
                    string sql = "SELECT * FROM Settings WHERE id = @id";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id.Value);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                model.Id = reader.IsDBNull(reader.GetOrdinal("id"))
                                    ? 0
                                    : reader.GetInt32(reader.GetOrdinal("id"));

                                model.UnitName = reader.IsDBNull(reader.GetOrdinal("UnitName"))
                                    ? "0"
                                    : reader.GetString(reader.GetOrdinal("UnitName"));

                                model.Category = reader.IsDBNull(reader.GetOrdinal("Category"))
                                    ? "0"
                                    : reader.GetString(reader.GetOrdinal("Category"));

                                model.threshold = reader.IsDBNull(reader.GetOrdinal("threshold"))
                                    ? 0
                                    : reader.GetDouble(reader.GetOrdinal("threshold"));

                                model.message_display = reader.IsDBNull(reader.GetOrdinal("message_display"))
                                    ? "0"
                                    : reader.GetString(reader.GetOrdinal("message_display"));

                                model.enabled = reader.IsDBNull(reader.GetOrdinal("enabled"))
                                    ? false
                                    : reader.GetInt32(reader.GetOrdinal("enabled")) == 1;

                                model.enabled_sensitivity = reader.IsDBNull(reader.GetOrdinal("enabled_sensitivity"))
                                    ? "0"
                                    : reader.GetString(reader.GetOrdinal("enabled_sensitivity"));

                                model.TimeInMinutes = reader.IsDBNull(reader.GetOrdinal("TimeInMinutes"))
                                    ? TimeSpan.Zero
                                    : reader.GetTimeSpan(reader.GetOrdinal("TimeInMinutes"));

                                model.Condition_Trigger = reader.IsDBNull(reader.GetOrdinal("Condition_Trigger"))
                                    ? "0"
                                    : reader.GetString(reader.GetOrdinal("Condition_Trigger"));

                                model.Remarks = reader.IsDBNull(reader.GetOrdinal("Remarks"))
                                    ? "0"
                                    : reader.GetString(reader.GetOrdinal("Remarks"));
                            }
                        }
                    }
                }

                // ✅ ADD NEW MODE (based on unitName + category)
                else if (!string.IsNullOrEmpty(unitName) && !string.IsNullOrEmpty(category))
                {
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
                                model.Category = reader.GetString(reader.GetOrdinal("Category"));
                                model.threshold = reader.IsDBNull(reader.GetOrdinal("threshold")) ? 0 : reader.GetDouble(reader.GetOrdinal("threshold"));
                                model.message_display = reader.IsDBNull(reader.GetOrdinal("message_display")) ? "" : reader.GetString(reader.GetOrdinal("message_display"));
                                model.enabled = reader.IsDBNull(reader.GetOrdinal("enabled")) ? false : reader.GetInt32(reader.GetOrdinal("enabled")) == 1;
                                model.enabled_sensitivity = reader.IsDBNull(reader.GetOrdinal("enabled_sensitivity")) ? "" : reader.GetString(reader.GetOrdinal("enabled_sensitivity"));
                                model.TimeInMinutes = reader.IsDBNull(reader.GetOrdinal("TimeInMinutes")) ? TimeSpan.Zero : reader.GetTimeSpan(reader.GetOrdinal("TimeInMinutes"));
                                model.Condition_Trigger = reader.IsDBNull(reader.GetOrdinal("Condition_Trigger")) ? "" : reader.GetString(reader.GetOrdinal("Condition_Trigger"));
                                model.Remarks = reader.IsDBNull(reader.GetOrdinal("Remarks")) ? "" : reader.GetString(reader.GetOrdinal("Remarks"));
                            }
                            else
                            {
                                model.UnitName = unitName;
                                model.Category = category;
                            }
                        }
                    }
                }

                // ✅ Load allSettings for table display (can be filtered)
                string query = "SELECT * FROM Settings WHERE 1=1";
                var cmdSettings = new MySqlCommand();
                cmdSettings.Connection = conn;

                if (!string.IsNullOrEmpty(category))
                {
                    query += " AND Category = @category";
                    cmdSettings.Parameters.AddWithValue("@category", category);
                }

                if (!string.IsNullOrEmpty(unitName))
                {
                    query += " AND UnitName = @unitNameFilter";
                    cmdSettings.Parameters.AddWithValue("@unitNameFilter", unitName);
                }

                cmdSettings.CommandText = query;

                using (var reader = cmdSettings.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        allSettings.Add(new Alertempareture
                        {
                            Id = reader.IsDBNull(reader.GetOrdinal("id"))
                                ? 0
                                : reader.GetInt32(reader.GetOrdinal("id")),

                            UnitName = reader.IsDBNull(reader.GetOrdinal("UnitName"))
                                ? "0"
                                : reader.GetString(reader.GetOrdinal("UnitName")),

                            Category = reader.IsDBNull(reader.GetOrdinal("Category"))
                                ? "0"
                                : reader.GetString(reader.GetOrdinal("Category")),

                            threshold = reader.IsDBNull(reader.GetOrdinal("threshold"))
                                ? 0
                                : Math.Round(reader.GetDouble(reader.GetOrdinal("threshold")), 2),

                            message_display = reader.IsDBNull(reader.GetOrdinal("message_display"))
                                ? "0"
                                : reader.GetString(reader.GetOrdinal("message_display")),

                            Condition_Trigger = reader.IsDBNull(reader.GetOrdinal("Condition_Trigger"))
                                ? "0"
                                : reader.GetString(reader.GetOrdinal("Condition_Trigger")),

                            enabled_sensitivity = reader.IsDBNull(reader.GetOrdinal("enabled_sensitivity"))
                                ? "0"
                                : reader.GetString(reader.GetOrdinal("enabled_sensitivity")),

                            TimeInMinutes = reader.IsDBNull(reader.GetOrdinal("TimeInMinutes"))
                                ? TimeSpan.Zero
                                : reader.GetTimeSpan(reader.GetOrdinal("TimeInMinutes")),

                            Remarks = reader.IsDBNull(reader.GetOrdinal("Remarks"))
                                ? "0"
                                : reader.GetString(reader.GetOrdinal("Remarks")),

                            enabled = reader.IsDBNull(reader.GetOrdinal("enabled"))
                                ? false
                                : reader.GetInt32(reader.GetOrdinal("enabled")) == 1,
                        });
                    }

                }
            }

            // Setup dropdowns
            ViewBag.UnitNames = new SelectList(units, model.UnitName);
            ViewBag.Categories = new SelectList(categories, model.Category);

            // Filter AlertDefinitions by Category
            var categoryAlertMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
    {
        { "Temperature", new List<string> { "A3H", "A3L", "A7H", "A7L" } },
        { "Humidity", new List<string> { "A2H", "A2L" } },
        { "CO2", new List<string> { "A1H", "A1L" } },
        { "Ethylene", new List<string> { "A4H", "A4L" } },
        { "Fan", new List<string> { "A6H", "A6L" } },
        { "Door", new List<string> { "A5H" } }
    };

            var selectedCategory = model.Category ?? category ?? "";
            var filteredAlertNames = categoryAlertMap.ContainsKey(selectedCategory)
                ? categoryAlertMap[selectedCategory]
                : new List<string>();
            // Full alert definitions
            var allAlertDefinitions = new List<AlertDefinition>
{
    new AlertDefinition { AlertName = "A1H", MessageDisplay = "High CO₂ Level Alert", ConditionTrigger = "CO₂ > 1200 ppm", Severity = "High" },
    new AlertDefinition { AlertName = "A1L", MessageDisplay = "Low CO₂ Level Alert", ConditionTrigger = "CO₂ < 300 ppm", Severity = "Low" },
    new AlertDefinition { AlertName = "A2H", MessageDisplay = "High Humidity Alert", ConditionTrigger = "Humidity > 90%", Severity = "Medium" },
    new AlertDefinition { AlertName = "A2L", MessageDisplay = "Low Humidity Alert", ConditionTrigger = "Humidity < 70%", Severity = "Medium" },
    new AlertDefinition { AlertName = "A3H", MessageDisplay = "High Temperature Alert", ConditionTrigger = "Temp > 13°C", Severity = "High" },
    new AlertDefinition { AlertName = "A3L", MessageDisplay = "Low Temperature Alert", ConditionTrigger = "Temp < 11°C", Severity = "Medium" },
    new AlertDefinition { AlertName = "A4H", MessageDisplay = "High Ethylene Level Alert", ConditionTrigger = "Ethylene > 2 ppm", Severity = "High" },
    new AlertDefinition { AlertName = "A4L", MessageDisplay = "Low Ethylene Level Alert", ConditionTrigger = "Ethylene < 0.5 ppm", Severity = "Low" },
    new AlertDefinition { AlertName = "A5H", MessageDisplay = "Door Open Too Long Alert", ConditionTrigger = "Door open > 2 minutes", Severity = "Medium" },
    new AlertDefinition { AlertName = "A6H", MessageDisplay = "Fan Not Running Alert", ConditionTrigger = "Fan speed = 0 when CO₂/Ethylene high", Severity = "Critical" },
    new AlertDefinition { AlertName = "A6L", MessageDisplay = "Fan Over-Running Alert", ConditionTrigger = "Fan speed unusually high (> expected)", Severity = "Low" },
    new AlertDefinition { AlertName = "A7H", MessageDisplay = "Rapid Temperature Increase", ConditionTrigger = "ΔTemp > +2°C in 5 minutes", Severity = "Medium" },
    new AlertDefinition { AlertName = "A7L", MessageDisplay = "Rapid Temperature Drop", ConditionTrigger = "ΔTemp < -2°C in 5 minutes", Severity = "Medium" },
    new AlertDefinition { AlertName = "A8", MessageDisplay = "Compressor Irregularity Alert", ConditionTrigger = "Unexpected compressor ON/OFF cycles", Severity = "Medium" },
    new AlertDefinition { AlertName = "A9", MessageDisplay = "Sensor Failure Alert", ConditionTrigger = "No data / invalid readings", Severity = "High" },
    new AlertDefinition { AlertName = "A10", MessageDisplay = "Data Not Received Alert", ConditionTrigger = "No data push within 5 minutes", Severity = "High" }
};


            // Filter alert definitions by selected category
            var filteredAlertDefinitions = allAlertDefinitions
                .Where(def => filteredAlertNames.Contains(def.AlertName))
                .ToList();
            //static data for severity
            ViewBag.Severities = new List<SelectListItem>
         {
        new SelectListItem { Text = "High", Value = "High" },
        new SelectListItem { Text = "Medium", Value = "Medium" },
        new SelectListItem { Text = "Low", Value = "Low" },
        new SelectListItem { Text = "Critical", Value = "Critical" }
         };

            ViewBag.AllAlertDefinitions = allAlertDefinitions;
            ViewBag.AlertDefinitions = filteredAlertDefinitions;

            ViewBag.AllSettings = allSettings;

            return View(model);
        }
        //insert setting data
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

                    TempData["SuccessMessage"] = "Record updated successfully.";
                }
                else
                {
                    //// Duplicate check
                    //string checkQuery = "SELECT id FROM Settings WHERE UnitName = @UnitName AND Category = @Category";
                    //using (var checkCmd = new MySqlCommand(checkQuery, conn))
                    //{
                    //    checkCmd.Parameters.AddWithValue("@UnitName", model.UnitName);
                    //    checkCmd.Parameters.AddWithValue("@Category", model.Category);

                    //    var existingId = checkCmd.ExecuteScalar() as object;
                    //    if (existingId != null)
                    //    {
                    //        // Duplicate exists
                    //        TempData["ErrorMessage"] = "Duplicate record found.";
                    //        TempData["HighlightId"] = (int)existingId;
                    //        return RedirectToAction("EditTemperature", new { unitName = model.UnitName, category = model.Category });
                    //    }
                    //}

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

                    TempData["SuccessMessage"] = "Record added successfully.";
                }
            }

            return RedirectToAction("EditTemperature");
        }
        //delete setting record
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

        public JsonResult GetRemarksByCategory(string category)
        {
            List<Remark> remarks = new List<Remark>();

            var connString = ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString;

            using (var conn = new MySqlConnection(connString))
            {
                conn.Open();
                string query = "SELECT * FROM Remarks WHERE Category = @Category";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Category", category);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            remarks.Add(new Remark
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                RemarkText = reader["Remark"].ToString()
                            });
                        }
                    }
                }
            }

            return Json(remarks, JsonRequestBehavior.AllowGet);
        }

    }
}