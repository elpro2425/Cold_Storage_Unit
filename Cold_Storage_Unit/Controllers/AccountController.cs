using Cold_Storage_Unit.Models;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace Cold_Storage_Unit.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public ActionResult Login()
        {

            return View(new LoginViewModel());
        }

        [HttpPost]
        public ActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                using (var conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString))
                {
                    conn.Open();

                    var cmd = new MySqlCommand("SELECT * FROM UserLogin WHERE username = @username AND password = @password", conn);
                    cmd.Parameters.AddWithValue("@username", model.Username);
                    cmd.Parameters.AddWithValue("@password", model.Password);

                    var reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        string username = reader["username"].ToString();
                        string name = reader["name"].ToString();

                        reader.Close(); // Always close reader before more queries

                        // ✅ Save session values FIRST
                        Session["Username"] = username;
                        Session["Name"] = name;
                        Session["UserRole"] = username.ToLower() == "admin" ? "admin" : "User";
                        Session["LoginTime"] = DateTime.Now;

                        // ✅ THEN log the activity
                        var countCmd = new MySqlCommand("SELECT COUNT(*) FROM UserLogActivity WHERE UserName = @username", conn);
                        countCmd.Parameters.AddWithValue("@username", username);
                        int previousCount = Convert.ToInt32(countCmd.ExecuteScalar());

                        string ip = GetUserIp();
                        string location = GetUserLocation(ip);


                        var logCmd = new MySqlCommand(@"INSERT INTO UserLogActivity 
                    (UserName, LoginFrequency, TimeSpent, ActivityDate, SessionId, IPAddress, Location)
                    VALUES (@UserName, @LoginFrequency, @TimeSpent, @ActivityDate, @SessionId,@IPAddress, @Location)", conn);

                        logCmd.Parameters.AddWithValue("@UserName", username);
                        logCmd.Parameters.AddWithValue("@LoginFrequency", (previousCount + 1).ToString());
                        logCmd.Parameters.AddWithValue("@TimeSpent", "0");
                        logCmd.Parameters.AddWithValue("@ActivityDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        logCmd.Parameters.AddWithValue("@SessionId", Session.SessionID);
                        logCmd.Parameters.AddWithValue("@IPAddress", ip);
                        logCmd.Parameters.AddWithValue("@Location", location);

                        logCmd.ExecuteNonQuery();

                        // ✅ No manual session ID manipulation — let ASP.NET handle it
                        return RedirectToAction("Index", "ColdStorageUnit");
                    }
                    else
                    {
                        reader.Close();
                        ModelState.AddModelError("", "Invalid username or password.");
                    }
                }

            }
            return View(model);
        }

        public ActionResult Logout()
        {
            string username = Session["Username"]?.ToString();
            string sessionId = Session.SessionID;

            if (!string.IsNullOrEmpty(username) && Session["LoginTime"] != null)
            {
                TimeSpan timeSpent = DateTime.Now - (DateTime)Session["LoginTime"];
                string formattedTime = timeSpent.ToString(@"hh\:mm\:ss");

                using (var conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString))
                {
                    conn.Open();
                    var cmd = new MySqlCommand("UPDATE UserLogActivity SET TimeSpent = @TimeSpent WHERE SessionId = @SessionId", conn);
                    cmd.Parameters.AddWithValue("@TimeSpent", formattedTime);
                    cmd.Parameters.AddWithValue("@SessionId", sessionId);
                    cmd.ExecuteNonQuery();
                }
            }

            Session.Clear();
            return RedirectToAction("Login");
        }

        [HttpPost]
        public ActionResult Register(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                using (var conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString))
                {
                    conn.Open();
                    var cmd = new MySqlCommand(@"INSERT INTO UserLogin (username, password, name, surname, email, date)
                                         VALUES (@username, @password, @name, @surname, @email, @date)", conn);

                    cmd.Parameters.AddWithValue("@username", model.Username);
                    cmd.Parameters.AddWithValue("@password", model.Password); // Consider hashing
                    cmd.Parameters.AddWithValue("@name", model.Name);
                    cmd.Parameters.AddWithValue("@surname", model.Surname);
                    cmd.Parameters.AddWithValue("@email", model.Email);
                    cmd.Parameters.AddWithValue("@date", model.Date);

                    cmd.ExecuteNonQuery();
                }

                TempData["Success"] = "Registration successful.";
                return RedirectToAction("Login");
            }

            return View(model);
        }

        public ActionResult ManageUsers()
        {
            var users = new List<LoginViewModel>();
            var logs = new List<UserLogActivityViewModel>();

            using (var conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString))
            {
                conn.Open();

                // Users
                var userCmd = new MySqlCommand("SELECT * FROM UserLogin", conn);
                var userReader = userCmd.ExecuteReader();
                while (userReader.Read())
                {
                    users.Add(new LoginViewModel
                    {
                        Username = userReader["username"].ToString(),
                        Name = userReader["name"].ToString(),
                        Surname = userReader["surname"].ToString(),
                        Email = userReader["email"].ToString(),
                        Date = userReader["date"].ToString(),

                    });
                }
                userReader.Close();

                // Logs
                var logCmd = new MySqlCommand("SELECT * FROM UserLogActivity ORDER BY ActivityDate DESC", conn);
                var logReader = logCmd.ExecuteReader();
                while (logReader.Read())
                {
                    logs.Add(new UserLogActivityViewModel
                    {
                        UserName = logReader["UserName"].ToString(),
                        LoginFrequency = logReader["LoginFrequency"].ToString(),
                        TimeSpent = logReader["TimeSpent"].ToString(),
                        ActivityDate = logReader["ActivityDate"].ToString(),
                        IPAddress = logReader["IPAddress"].ToString(),
                        Location = logReader["Location"].ToString()
                    });
                }
            }
            return View("ManageUsers", Tuple.Create(new LoginViewModel(), users, logs));
        }

        [HttpGet]
        public ActionResult Register()
        {
            return PartialView("_UserForm", new LoginViewModel());
        }

        [HttpGet]
        public ActionResult Edit(string username)
        {
            var model = new LoginViewModel();
            using (var conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString))
            {
                conn.Open();
                var cmd = new MySqlCommand("SELECT * FROM UserLogin WHERE username = @username", conn);
                cmd.Parameters.AddWithValue("@username", username);
                var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    model.Username = reader["username"].ToString();
                    model.Password = reader["password"].ToString();
                    model.Name = reader["name"].ToString();
                    model.Surname = reader["surname"].ToString();
                    model.Email = reader["email"].ToString();
                    model.Date = reader["date"].ToString();
                }
            }
            return PartialView("_UserForm", model);
        }

        [HttpPost]
        public ActionResult Edit(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                using (var conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString))
                {
                    conn.Open();
                    var cmd = new MySqlCommand(@"UPDATE UserLogin SET password = @password, name = @name,
                                         surname = @surname, email = @email, date = @date
                                         WHERE username = @username", conn);

                    cmd.Parameters.AddWithValue("@username", model.Username);
                    cmd.Parameters.AddWithValue("@password", model.Password);
                    cmd.Parameters.AddWithValue("@name", model.Name);
                    cmd.Parameters.AddWithValue("@surname", model.Surname);
                    cmd.Parameters.AddWithValue("@email", model.Email);
                    cmd.Parameters.AddWithValue("@date", model.Date);

                    cmd.ExecuteNonQuery();
                }

                TempData["Success"] = "User updated.";
                return RedirectToAction("ManageUsers");
            }

            return View("Register", model);
        }

        public ActionResult Delete(string username)
        {
            using (var conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString))
            {
                conn.Open();
                var cmd = new MySqlCommand("DELETE FROM UserLogin WHERE username = @username", conn);
                cmd.Parameters.AddWithValue("@username", username);
                cmd.ExecuteNonQuery();
            }

            TempData["Success"] = "User deleted.";
            return RedirectToAction("ManageUsers");
        }
        private string GetUserIp()
        {
            string ip = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

            if (!string.IsNullOrEmpty(ip))
            {
                // In case of multiple IPs, take the first one
                string[] ipRange = ip.Split(',');
                if (ipRange.Length > 0)
                {
                    ip = ipRange[0];
                }
            }
            else
            {
                ip = Request.ServerVariables["REMOTE_ADDR"];
            }

            // Handle local testing
            if (ip == "::1" || ip == "127.0.0.1")
            {
                return "106.213.81.193";
            }

            return ip;
        }
        private string GetUserLocation(string ip)
        {
            try
            {
                using (var client = new WebClient())
                {
                    string json = client.DownloadString($"https://ipwhois.app/json/{ip}");
                    dynamic locationData = JsonConvert.DeserializeObject(json);

                    if (locationData.success == true || locationData.success == null)
                    {
                        return $"{locationData.city}, {locationData.region}, {locationData.country}";
                    }
                }
            }
            catch
            { }
            return "Unknown Location";
        }

        // GET: Settings page - show current user's info
        [HttpGet]
        public ActionResult Settings()
        {
            string username = Session["Username"]?.ToString();
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login");
            }

            var model = new LoginViewModel();

            using (var conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString))
            {
                conn.Open();
                var cmd = new MySqlCommand("SELECT * FROM UserLogin WHERE username = @username", conn);
                cmd.Parameters.AddWithValue("@username", username);
                var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    model.Username = reader["username"].ToString();
                    model.Password = reader["password"].ToString(); // Consider not sending password to view for security
                    model.Name = reader["name"].ToString();
                    model.Surname = reader["surname"].ToString();
                    model.Email = reader["email"].ToString();
                    model.Date = reader["date"].ToString();
                }
            }

            return View(model);  // Create a Settings.cshtml view with a form bound to LoginViewModel
        }

        // POST: Save updated user info from Settings form
        [HttpPost]
        public ActionResult Settings(LoginViewModel model)
        {
            string username = Session["Username"]?.ToString();
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login");
            }

            if (ModelState.IsValid)
            {
                using (var conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString))
                {
                    conn.Open();
                    var cmd = new MySqlCommand(@"UPDATE UserLogin SET password = @password, name = @name,
                                     surname = @surname, email = @email, date = @date
                                     WHERE username = @username", conn);

                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@password", model.Password);
                    cmd.Parameters.AddWithValue("@name", model.Name);
                    cmd.Parameters.AddWithValue("@surname", model.Surname);
                    cmd.Parameters.AddWithValue("@email", model.Email);
                    cmd.Parameters.AddWithValue("@date", model.Date);

                    cmd.ExecuteNonQuery();
                }

                TempData["Success"] = "Your details have been updated.";
                return RedirectToAction("Settings");
            }

            return View(model);
        }

    }
}