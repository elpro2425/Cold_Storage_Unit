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
    public class DoorStatusController : Controller
    {
        // GET: DoorStatus
        public ActionResult Index()
        {
                DoorStatus latest = null;

                string connStr = ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString;

                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string query = "SELECT * FROM door_status ORDER BY Actualdate DESC LIMIT 1";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                latest = new DoorStatus
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    Unitname = reader["Unitname"].ToString(),
                                    Status = reader["Status"].ToString(),
                                    Hardwaredate = reader["Hardwaredate"].ToString()
                                };
                            }
                        }
                    }
                }
                return View(latest);
        }
    }
}
