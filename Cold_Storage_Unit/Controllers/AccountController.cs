using Cold_Storage_Unit.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
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
                    cmd.Parameters.AddWithValue("@password", model.Password); // Optional: hash it if stored hashed

                    var reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        Session["Username"] = reader["username"].ToString();
                        Session["Name"] = reader["name"].ToString();

                        // Set role manually based on username
                        Session["UserRole"] = reader["username"].ToString().ToLower() == "admin" ? "admin" : "User";

                        return RedirectToAction("Index", "ColdStorageUnit");
                    }


                }

                ModelState.AddModelError("", "Invalid username or password.");
            }

            return View(model);
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
            string currentUser = Session["Username"]?.ToString();
            string currentRole = Session["UserRole"]?.ToString();

            using (var conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString))
            {
                conn.Open();

                MySqlCommand cmd;

                if (currentRole == "admin")
                {
                    // Admin sees all users
                    cmd = new MySqlCommand("SELECT * FROM UserLogin", conn);
                }
                else
                {
                    // Normal user sees only their own record
                    cmd = new MySqlCommand("SELECT * FROM UserLogin WHERE username = @username", conn);
                    cmd.Parameters.AddWithValue("@username", currentUser);
                }

                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    users.Add(new LoginViewModel
                    {
                        Username = reader["username"].ToString(),
                        Name = reader["name"].ToString(),
                        Surname = reader["surname"].ToString(),
                        Email = reader["email"].ToString(),
                        Date = reader["date"].ToString()
                    });
                }
            }

            return View("ManageUsers", Tuple.Create(new LoginViewModel(), users));
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


    }
}