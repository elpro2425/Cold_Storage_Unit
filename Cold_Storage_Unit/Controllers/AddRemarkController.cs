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
    public class AddRemarkController : Controller
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString;

        // GET: Remarks
        public ActionResult Index(string category = null)
        {
            ViewBag.FilterCategory = category;
            List<Remark> remarks = new List<Remark>();

            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = string.IsNullOrEmpty(category)
                    ? "SELECT * FROM Remarks"
                    : "SELECT * FROM Remarks WHERE Category = @Category";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    if (!string.IsNullOrEmpty(category))
                        cmd.Parameters.AddWithValue("@Category", category);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            remarks.Add(new Remark
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Category = reader["Category"].ToString(),
                                RemarkText = reader["Remark"].ToString()
                            });
                        }
                    }
                }
            }

            return View(remarks);
        }

        [HttpPost]
        public ActionResult Add(string category, string remarkText)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "INSERT INTO Remarks (Category, Remark) VALUES (@Category, @Remark)";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Category", category);
                    cmd.Parameters.AddWithValue("@Remark", remarkText);
                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult Edit(int id, string category, string remarkText)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "UPDATE Remarks SET Category = @Category, Remark = @Remark WHERE Id = @Id";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@Category", category);
                    cmd.Parameters.AddWithValue("@Remark", remarkText);
                    cmd.ExecuteNonQuery();
                }
            }
            TempData["UpdateMessage"] = "Record Updated successfully.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult Delete(int id)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "DELETE FROM Remarks WHERE Id = @Id";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["Message"] = "Record deleted successfully!";
            return RedirectToAction("Index");
        }

    }
}