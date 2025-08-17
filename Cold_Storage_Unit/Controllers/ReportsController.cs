using Cold_Storage_Unit.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using MySql.Data.MySqlClient;
using NPOI.POIFS.Crypt.Dsig;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using NPOI.SS.UserModel.Charts;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.Helpers;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Web.UI.DataVisualization.Charting;
using Font = iTextSharp.text.Font;

namespace Cold_Storage_Unit.Controllers
{
    public class ReportsController : Controller
    {
        //Datatable for ColdstorageUnit2


        public ActionResult Index(
       string name = null, string startDate = null, string endDate = null,
       string status = null, string startDate1 = null, string endDate1 = null,
       string severity = null, string startDate2 = null, string endDate2 = null,
       string tableType = null,
       string actionType = null)
        {
            // Handle Summarize Button
            if (actionType == "summarize")
                return RedirectToAction("GeneratePdfSummarazieReport", new { name, startDate, endDate, download = false });

            if (actionType == "pdf")
                return RedirectToAction("GeneratePdfSummarazieReport", new { name, startDate, endDate, download = true });

            var coldStorageData = GetFilteredColdStorageData(name, startDate, endDate);
            var doorStatusData = GetFilteredDoorStatusData(status, startDate1, endDate1);
            var alertsData = GetFilteredAlertsData(severity, startDate2, endDate2);

            ViewBag.SelectedName = name;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;

            ViewBag.Selectedstatus = status;
            ViewBag.StartDate1 = startDate1;
            ViewBag.EndDate1 = endDate1;

            ViewBag.SelectedSeverity = severity;
            ViewBag.StartDate2 = startDate2;
            ViewBag.EndDate2 = endDate2;

            ViewBag.TableType = tableType;
            ViewBag.ActionType = actionType;  // ✅ Pass to View

            var viewModel = new ReportsViewModel
            {
                ColdStorageData = coldStorageData,
                DoorStatusData = doorStatusData,
                AlertsData = alertsData
            };

            return View(viewModel);
        }

        //Coldstorage Table Data
        private List<ColdStorageUnit> GetFilteredColdStorageData(string name, string startDate, string endDate)
        {

            var combinedData = new List<ColdStorageUnit>();

            using (var conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString))
            {
                conn.Open();

                // Get all distinct names for dropdown
                List<string> allNames = new List<string>();
                using (var cmd = new MySqlCommand(@"
              SELECT DISTINCT TRIM(Name) as Name FROM ColdStorageUnit1
              UNION
              SELECT DISTINCT TRIM(Name) as Name FROM ColdStorageUnit2
              ORDER BY Name", conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            allNames.Add(reader["Name"].ToString());
                        }
                    }
                }

                ViewBag.Names = allNames;
                // Build filter query
                string filterQuery = " WHERE 1=1 ";
                if (!string.IsNullOrEmpty(name))
                    filterQuery += " AND TRIM(LOWER(Name)) = TRIM(LOWER(@name))";

                if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                {
                    if (startDate == endDate)
                    {
                        // ✅ SAME DATE → include full day (00:00:00 to 23:59:59)
                        filterQuery += @"
        AND STR_TO_DATE(SUBSTRING_INDEX(Hardwaredate, ',', 1), '%d/%m/%Y')
        = STR_TO_DATE(@startDate, '%Y-%m-%d')";
                    }
                    else
                    {
                        // ✅ RANGE → include end date fully
                        filterQuery += @"
        AND STR_TO_DATE(SUBSTRING_INDEX(Hardwaredate, ',', 1), '%d/%m/%Y')
        BETWEEN STR_TO_DATE(@startDate, '%Y-%m-%d')
        AND DATE_ADD(STR_TO_DATE(@endDate, '%Y-%m-%d'), INTERVAL 1 DAY) - INTERVAL 1 SECOND";
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(startDate))
                        filterQuery += " AND STR_TO_DATE(SUBSTRING_INDEX(Hardwaredate, ',', 1), '%d/%m/%Y') >= STR_TO_DATE(@startDate, '%Y-%m-%d')";

                    if (!string.IsNullOrEmpty(endDate))
                        filterQuery += " AND STR_TO_DATE(SUBSTRING_INDEX(Hardwaredate, ',', 1), '%d/%m/%Y') < DATE_ADD(STR_TO_DATE(@endDate, '%Y-%m-%d'), INTERVAL 1 DAY)";
                }

                string orderBy = " ORDER BY STR_TO_DATE(Hardwaredate, '%d/%m/%Y, %h:%i:%s %p') DESC";

                // Decide which tables to query based on selected name
                List<string> tablesToQuery = new List<string>();

                if (!string.IsNullOrEmpty(name))
                {
                    // Check in which table the selected name exists
                    using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM ColdStorageUnit1 WHERE TRIM(LOWER(Name)) = TRIM(LOWER(@name))", conn))
                    {
                        cmd.Parameters.AddWithValue("@name", name);
                        var count = Convert.ToInt32(cmd.ExecuteScalar());
                        if (count > 0)
                            tablesToQuery.Add("ColdStorageUnit1");
                    }

                    using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM ColdStorageUnit2 WHERE TRIM(LOWER(Name)) = TRIM(LOWER(@name))", conn))
                    {
                        cmd.Parameters.AddWithValue("@name", name);
                        var count = Convert.ToInt32(cmd.ExecuteScalar());
                        if (count > 0)
                            tablesToQuery.Add("ColdStorageUnit2");
                    }
                }
                else
                {
                    // If no specific name selected, query both tables
                    tablesToQuery.Add("ColdStorageUnit1");
                    tablesToQuery.Add("ColdStorageUnit2");
                }

                // Fetch data from selected tables
                foreach (var table in tablesToQuery)
                {
                    string query = $"SELECT * FROM {table} {filterQuery} {orderBy}";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        if (!string.IsNullOrEmpty(name))
                            cmd.Parameters.AddWithValue("@name", name);
                        if (!string.IsNullOrEmpty(startDate))
                            cmd.Parameters.AddWithValue("@startDate", DateTime.Parse(startDate).ToString("yyyy-MM-dd"));
                        if (!string.IsNullOrEmpty(endDate))
                            cmd.Parameters.AddWithValue("@endDate", DateTime.Parse(endDate).ToString("yyyy-MM-dd"));

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                combinedData.Add(new ColdStorageUnit
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
                                });
                            }
                        }
                    }
                }
            }

            ViewBag.SelectedName = name;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            return combinedData;
        }
      //Pdf Report Data- ColdstorageUnit2
        public FileResult GeneratePdfSummaryReport(string name, string startDate, string endDate)
        {
            List<ColdStorageUnit> rows = new List<ColdStorageUnit>();

            using (var conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString))
            {
                conn.Open();
                string filterQuery = " WHERE 1=1 ";
                if (!string.IsNullOrEmpty(name))
                    filterQuery += " AND TRIM(LOWER(Name)) = TRIM(LOWER(@name))";
                if (!string.IsNullOrEmpty(startDate))
                    filterQuery += " AND STR_TO_DATE(SUBSTRING_INDEX(Hardwaredate, ',', 1), '%d/%m/%Y') >= STR_TO_DATE(@startDate, '%Y-%m-%d')";
                if (!string.IsNullOrEmpty(endDate))
                    filterQuery += " AND STR_TO_DATE(SUBSTRING_INDEX(Hardwaredate, ',', 1), '%d/%m/%Y') <= STR_TO_DATE(@endDate, '%Y-%m-%d')";

                string orderBy = " ORDER BY STR_TO_DATE(Hardwaredate, '%d/%m/%Y, %h:%i:%s %p') DESC";

                string[] tables = { "ColdStorageUnit1", "ColdStorageUnit2" };
                int id = 1;
                foreach (var table in tables)
                {
                    string query = $"SELECT * FROM {table} {filterQuery} {orderBy}";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        if (!string.IsNullOrEmpty(name))
                            cmd.Parameters.AddWithValue("@name", name);
                        if (!string.IsNullOrEmpty(startDate))
                            cmd.Parameters.AddWithValue("@startDate", DateTime.Parse(startDate).ToString("yyyy-MM-dd"));
                        if (!string.IsNullOrEmpty(endDate))
                            cmd.Parameters.AddWithValue("@endDate", DateTime.Parse(endDate).ToString("yyyy-MM-dd"));

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {

                                rows.Add(new ColdStorageUnit
                                {
                                    Id = id,
                                    Name = reader["Name"].ToString(),
                                    Temperature = Convert.ToDouble(reader["Temperature"]),
                                    Humidity = Convert.ToDouble(reader["Humidity"]),
                                    PowerStatus = reader["PowerStatus"].ToString(),
                                    DoorStatus = reader["DoorStatus"].ToString(),
                                    Co2Level = Convert.ToDouble(reader["Co2Level"]),
                                    EthyleneLevel = Convert.ToDouble(reader["EthyleneLevel"]),
                                    FanSpeed = Convert.ToInt32(reader["FanSpeed"]),
                                    Hardwaredate = reader["Hardwaredate"].ToString()
                                });
                                id++;
                            }
                        }
                    }
                }
            }

            // PDF generation code unchanged, just use the rows list...

            using (var ms = new MemoryStream())
            {
                Document doc = new Document(PageSize.A4, 20f, 20f, 100f, 110f);

                var writer = PdfWriter.GetInstance(doc, ms);
                string logoPath = Server.MapPath("~/Images/logo.jpg");

                string selectedStatus = string.IsNullOrEmpty(name) ? "All Units" : name;
                writer.PageEvent = new PdfFooter(logoPath, selectedStatus, startDate, endDate);

                doc.Open();

                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11);
                var cellFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);

                bool includeName = string.IsNullOrEmpty(name) || name.Trim().ToLower() == "all";


                var columnCount = includeName ? 10 : 9;
                PdfPTable table = new PdfPTable(columnCount)
                {
                    WidthPercentage = 100f
                };

                // Set specific widths
                if (includeName)
                {
                    table.SetWidths(new float[] { 0.6f, 1.0f, 1.0f, 1.0f, 0.7f, 0.6f, 1.0f, 1.0f, 0.6f, 2.0f });
                }
                else
                {
                    table.SetWidths(new float[] { 0.6f, 1.0f, 1.0f, 0.6f, 0.6f, 1.0f, 1.0f, 0.6f, 2.0f }); // Excludes Name
                }

                table.HeaderRows = 1;

                // Headers
                string[] allHeaders = { "ID", "Name", "Temp (°C)", "Humidity (%)", "Power", "Door", "CO2(ppm)", "Ethylene (ppm)", "Fan Speed", "DateTime" };
                string[] headers = includeName ? allHeaders : allHeaders.Where(h => h != "Name").ToArray();

                foreach (var h in headers)
                {
                    PdfPCell headerCell = new PdfPCell(new Phrase(h, headerFont))
                    {
                        BackgroundColor = BaseColor.YELLOW,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE
                    };
                    table.AddCell(headerCell);
                }

                // Data rows
                foreach (var r in rows)
                {
                    table.AddCell(CreateCenterCell(r.Id.ToString(), cellFont));

                    if (includeName)
                        table.AddCell(CreateCenterCell(r.Name, cellFont));

                    table.AddCell(CreateCenterCell((Math.Truncate(r.Temperature * 10) / 10).ToString("0.0"), cellFont));
                    table.AddCell(CreateCenterCell((Math.Truncate(r.Humidity * 10) / 10).ToString("0.0"), cellFont));
                    table.AddCell(CreateCenterCell(r.PowerStatus, cellFont));
                    table.AddCell(CreateCenterCell(r.DoorStatus, cellFont));
                    table.AddCell(CreateCenterCell((Math.Truncate(r.Co2Level * 10) / 10).ToString("0.0"), cellFont));
                    table.AddCell(CreateCenterCell(r.EthyleneLevel.ToString("F2"), cellFont));
                    table.AddCell(CreateCenterCell(r.FanSpeed.ToString(), cellFont));
                    table.AddCell(CreateCenterCell(r.Hardwaredate, cellFont)); // Make sure r.Hardwaredate is in single line format (e.g. "yyyy-MM-dd HH:mm:ss")
                }
                if (rows.Any())
                {
                    double avgTemp = rows.Average(x => x.Temperature);
                    double minTemp = rows.Min(x => x.Temperature);
                    double maxTemp = rows.Max(x => x.Temperature);

                    double avgHum = rows.Average(x => x.Humidity);
                    double minHum = rows.Min(x => x.Humidity);
                    double maxHum = rows.Max(x => x.Humidity);

                    double avgEth = rows.Average(x => x.EthyleneLevel);
                    double minEth = rows.Min(x => x.EthyleneLevel);
                    double maxEth = rows.Max(x => x.EthyleneLevel);

                    double avgCO2 = rows.Average(x => x.Co2Level);
                    double minCO2 = rows.Min(x => x.Co2Level);
                    double maxCO2 = rows.Max(x => x.Co2Level);

                    PdfPTable summaryTable = new PdfPTable(5)
                    {
                        WidthPercentage = 100f,
                        SpacingAfter = 15f
                    };
                    summaryTable.SetWidths(new float[] { 1.5f, 1f, 1f, 1f, 1f });

                    Font summaryHeaderFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11);
                    Font summaryCellFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);

                    // Header Row
                    summaryTable.AddCell(new PdfPCell(new Phrase("Metric", summaryHeaderFont)) { BackgroundColor = BaseColor.LIGHT_GRAY });
                    summaryTable.AddCell(new PdfPCell(new Phrase("Temperature (°C)", summaryHeaderFont)) { BackgroundColor = BaseColor.LIGHT_GRAY });
                    summaryTable.AddCell(new PdfPCell(new Phrase("Humidity (%)", summaryHeaderFont)) { BackgroundColor = BaseColor.LIGHT_GRAY });
                    summaryTable.AddCell(new PdfPCell(new Phrase("CO₂ (ppm)", summaryHeaderFont)) { BackgroundColor = BaseColor.LIGHT_GRAY });
                    summaryTable.AddCell(new PdfPCell(new Phrase("Ethylene (ppm)", summaryHeaderFont)) { BackgroundColor = BaseColor.LIGHT_GRAY });

                    // Average Row
                    summaryTable.AddCell(new Phrase("Average", summaryCellFont));
                    summaryTable.AddCell(new Phrase((Math.Truncate(avgTemp * 10) / 10).ToString("0.0"), summaryCellFont));
                    summaryTable.AddCell(new Phrase((Math.Truncate(avgHum * 10) / 10).ToString("0.0"), summaryCellFont));
                    summaryTable.AddCell(new Phrase((Math.Truncate(avgCO2 * 10) / 10).ToString("0.0"), summaryCellFont));
                    summaryTable.AddCell(new Phrase((Math.Truncate(avgEth * 10) / 10).ToString("0.0"), summaryCellFont));

                    // Min Row
                    summaryTable.AddCell(new Phrase("Minimum", summaryCellFont));
                    summaryTable.AddCell(new Phrase((Math.Truncate(minTemp * 10) / 10).ToString("0.0"), summaryCellFont));
                    summaryTable.AddCell(new Phrase((Math.Truncate(minHum * 10) / 10).ToString("0.0"), summaryCellFont));
                    summaryTable.AddCell(new Phrase((Math.Truncate(minCO2 * 10) / 10).ToString("0.0"), summaryCellFont));
                    summaryTable.AddCell(new Phrase((Math.Truncate(minEth * 10) / 10).ToString("0.0"), summaryCellFont));

                    // Max Row
                    summaryTable.AddCell(new Phrase("Maximum", summaryCellFont));
                    summaryTable.AddCell(new Phrase((Math.Truncate(maxTemp * 10) / 10).ToString("0.0"), summaryCellFont));
                    summaryTable.AddCell(new Phrase((Math.Truncate(maxHum * 10) / 10).ToString("0.0"), summaryCellFont));
                    summaryTable.AddCell(new Phrase((Math.Truncate(maxCO2 * 10) / 10).ToString("0.0"), summaryCellFont));
                    summaryTable.AddCell(new Phrase((Math.Truncate(maxEth * 10) / 10).ToString("0.0"), summaryCellFont));

                    // Add summary to document before main table
                    doc.Add(new Paragraph("Summary Metrics", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12)));
                    doc.Add(new Paragraph("\n")); // Adds space, but not as clean or adjustable

                    doc.Add(summaryTable);
                }

                doc.Add(table);


                doc.Close();

                return File(ms.ToArray(), "application/pdf", "ColdStorage_Filtered_Report.pdf");
            }
        }
        private class PdfFooter : PdfPageEventHelper
        {
            private string logoPath;
            private string selectedStatus;
            private string startDate;
            private string endDate;

            public PdfFooter(string logoPath, string selectedStatus, string startDate, string endDate)
            {
                this.logoPath = logoPath;
                this.selectedStatus = selectedStatus;
                this.startDate = startDate;
                this.endDate = endDate;
            }

            private Font regularFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
            private Font boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
            private Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
            private Font subtitleFont = FontFactory.GetFont(FontFactory.HELVETICA, 11);
            private Font dateFont = FontFactory.GetFont(FontFactory.HELVETICA, 9);

            public override void OnEndPage(PdfWriter writer, Document document)
            {
                base.OnEndPage(writer, document);
                PdfContentByte cb = writer.DirectContent;

                iTextSharp.text.Rectangle border = new iTextSharp.text.Rectangle(
                    document.Left,
                    document.Bottom,
                    document.Right,
                    document.Top
                );

                cb.SetColorStroke(BaseColor.BLACK);
                cb.SetLineWidth(0.5f);
                cb.Rectangle(border.Left, border.Bottom, border.Width, border.Height);
                cb.Stroke();

                // Create 3-column header (2/6/4 layout)
                PdfPTable header = new PdfPTable(3);
                header.TotalWidth = document.PageSize.Width - document.LeftMargin - document.RightMargin;
                header.SetWidths(new float[] { 2f, 6f, 4f });

                // --- Left: Logo ---
                PdfPCell logoCell = new PdfPCell();
                logoCell.Border = iTextSharp.text.Rectangle.NO_BORDER;
                logoCell.HorizontalAlignment = Element.ALIGN_LEFT;
                logoCell.VerticalAlignment = Element.ALIGN_MIDDLE;

                if (System.IO.File.Exists(logoPath))
                {
                    iTextSharp.text.Image logo = iTextSharp.text.Image.GetInstance(logoPath);
                    logo.ScaleToFit(80f, 80f);
                    logoCell.AddElement(logo);
                }

                header.AddCell(logoCell);

                // --- Center: Title + Subtitle ---
                Paragraph centerPara = new Paragraph
                {
                    Alignment = Element.ALIGN_CENTER
                };
                centerPara.Add(new Chunk("Banana Cold Storage Report", titleFont));
                centerPara.Add(new Chunk("\n\n")); // 2-line spacing between title and subtitle
                centerPara.Add(new Chunk($"Summary Report for {(string.IsNullOrEmpty(selectedStatus) ? "All Units" : selectedStatus)}", subtitleFont));

                PdfPCell centerCell = new PdfPCell(centerPara)
                {
                    Border = iTextSharp.text.Rectangle.NO_BORDER,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    VerticalAlignment = Element.ALIGN_MIDDLE
                };
                header.AddCell(centerCell);

                // --- Right: Start, End, Generated Dates ---
                Paragraph rightPara = new Paragraph
                {
                    Alignment = Element.ALIGN_RIGHT
                };
                rightPara.Add(new Chunk($"  Start Date:  {startDate} 00:00:00\n\n", dateFont));
                rightPara.Add(new Chunk($"  End Date:   {endDate} 23:59:59\n\n", dateFont));
                rightPara.Add(new Chunk($"  Generated:  {DateTime.Now:dd-MM-yyyy HH:mm:ss}", dateFont));

                PdfPCell rightCell = new PdfPCell(rightPara)
                {
                    Border = iTextSharp.text.Rectangle.NO_BORDER,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    VerticalAlignment = Element.ALIGN_MIDDLE
                };
                header.AddCell(rightCell);

                // Render header
                float yPosition = document.PageSize.Height - 25;
                header.WriteSelectedRows(0, -1, document.LeftMargin, yPosition, cb);

                // --- Horizontal divider line ---
                float lineY = yPosition - 65f;
                cb.SetLineWidth(1f);
                cb.MoveTo(document.LeftMargin, lineY);
                cb.LineTo(document.PageSize.Width - document.RightMargin, lineY);
                cb.Stroke();

                // 3. Optional: Draw vertical dividers between sections
                float tableTop = document.PageSize.Height - 20;
                float tableBottom = tableTop - 70;

                float section1Right = document.LeftMargin + (header.TotalWidth * 2f / 12f);
                float section2Right = document.LeftMargin + (header.TotalWidth * 8f / 12f);

                cb.SetLineWidth(0.5f);
                cb.MoveTo(section1Right, tableBottom);
                cb.LineTo(section1Right, tableTop);
                cb.MoveTo(section2Right, tableBottom);
                cb.LineTo(section2Right, tableTop);
                cb.Stroke();

                // 4. Footer (adjusted position)
                float footerY = document.PageSize.GetBottom(90);  // Move footer slightly up
                float linelast = document.PageSize.GetBottom(100);   // Move line slightly above footer

                // Draw horizontal line slightly above footer
                cb.MoveTo(document.PageSize.Left + 20, linelast);
                cb.LineTo(document.PageSize.Right - 20, linelast);
                cb.Stroke();

                // Setup footer table
                PdfPTable footer = new PdfPTable(1);
                footer.TotalWidth = document.PageSize.Width - 80;
                footer.DefaultCell.Border = iTextSharp.text.Rectangle.NO_BORDER;
                footer.DefaultCell.HorizontalAlignment = Element.ALIGN_CENTER;

                PdfPCell footerCell = new PdfPCell
                {
                    Border = iTextSharp.text.Rectangle.NO_BORDER,
                    PaddingTop = 0f,
                    PaddingBottom = 10f // Slightly smaller padding to fit higher
                };

                Paragraph p = new Paragraph();

                // Company name as clickable website link
                Anchor companyLink = new Anchor(" ELPRO Solutions", boldFont);
                companyLink.Reference = "http://elprosolutions.com";
                companyLink.Font.Color = BaseColor.BLUE;
                p.Add(companyLink);
                p.Add(new Chunk("\n"));

                // Email and phone
                Chunk phoneChunk = new Chunk("+91 7385373434 | ", regularFont);
                p.Add(phoneChunk);

                Anchor email1 = new Anchor("info@elprosolutions.com", regularFont);
                email1.Reference = "mailto:info@elprosolutions.com";
                email1.Font.Color = BaseColor.BLACK;
                p.Font.Color = BaseColor.BLACK;
                p.Add(email1);

                p.Add(new Chunk(" | "));

                Anchor email2 = new Anchor("operations@elprosolutions.com", regularFont);
                email2.Reference = "mailto:operations@elprosolutions.com";
                email2.Font.Color = BaseColor.BLACK;
                p.Font.Color = BaseColor.BLACK;
                p.Add(email2);
                p.Add(new Chunk("\n"));

                // Addresses
                p.Add(new Chunk("Plot no: 97, Nimbalkar Colony, Kolhapur, 416002", regularFont));
                p.Add(new Chunk("\n"));
                p.Add(new Chunk("B-902, VTP Belair, Mahalunge, Pune, 411045", regularFont));
                p.Font.Color = BaseColor.BLACK;
                footerCell.AddElement(p);
                footer.AddCell(footerCell);

                // Render footer slightly higher
                footer.WriteSelectedRows(0, -1, 40, footerY, writer.DirectContent);


            }
        }
        //
        //
        //All Option Selection 

        [HttpGet]
        public JsonResult GetChartData(string name, string startDate, string endDate)
        {
            List<ColdStorageUnit> rows = new List<ColdStorageUnit>();

            using (var conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString))
            {
                conn.Open();
                string filterQuery = " WHERE 1=1 ";
                if (!string.IsNullOrEmpty(name))
                    filterQuery += " AND TRIM(LOWER(Name)) = TRIM(LOWER(@name))";
                if (!string.IsNullOrEmpty(startDate))
                    filterQuery += " AND STR_TO_DATE(SUBSTRING_INDEX(Hardwaredate, ',', 1), '%e/%c/%Y') >= STR_TO_DATE(@startDate, '%Y-%m-%d')";
                if (!string.IsNullOrEmpty(endDate))
                    filterQuery += " AND STR_TO_DATE(SUBSTRING_INDEX(Hardwaredate, ',', 1), '%e/%c/%Y') <= STR_TO_DATE(@endDate, '%Y-%m-%d')";

                string orderBy = "ORDER BY STR_TO_DATE(Hardwaredate, '%d/%m/%Y, %h:%i:%s %p') DESC";
                string[] tables = { "ColdStorageUnit1", "ColdStorageUnit2" };
                int id = 1;

                foreach (var table in tables)
                {
                    string query = $"SELECT * FROM {table} {filterQuery} {orderBy}";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        if (!string.IsNullOrEmpty(name))
                            cmd.Parameters.AddWithValue("@name", name);
                        if (!string.IsNullOrEmpty(startDate))
                            cmd.Parameters.AddWithValue("@startDate", DateTime.Parse(startDate).ToString("yyyy-MM-dd"));
                        if (!string.IsNullOrEmpty(endDate))
                            cmd.Parameters.AddWithValue("@endDate", DateTime.Parse(endDate).ToString("yyyy-MM-dd"));

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                rows.Add(new ColdStorageUnit
                                {
                                    Id = id,
                                    Name = reader["Name"].ToString(),
                                    Temperature = Convert.ToDouble(reader["Temperature"]),
                                    Humidity = Convert.ToDouble(reader["Humidity"]),
                                    PowerStatus = reader["PowerStatus"].ToString(),
                                    DoorStatus = reader["DoorStatus"].ToString(),
                                    Co2Level = Convert.ToDouble(reader["Co2Level"]),
                                    EthyleneLevel = Convert.ToDouble(reader["EthyleneLevel"]),
                                    FanSpeed = Convert.ToInt32(reader["FanSpeed"]),
                                    Hardwaredate = reader["Hardwaredate"].ToString()
                                });
                                id++;
                            }
                        }
                    }
                }
            }

            if (!rows.Any())
            {
                return Json(new { hasData = false }, JsonRequestBehavior.AllowGet);
            }

            // ✅ 4-Hour Averages for Line Chart
            var hourlyData = rows
                .GroupBy(r =>
                {
                    var dt = DateTime.ParseExact(
                        r.Hardwaredate.Trim(),
                        "d/M/yyyy, h:mm:ss tt",
                        CultureInfo.InvariantCulture
                    );
                    return new { Date = dt.Date, HourGroup = dt.Hour / 4 };
                })
                .Select(g => new
                {
                    Date = g.Key.Date.AddHours(g.Key.HourGroup * 4),
                    AvgTemp = g.Average(x => x.Temperature),
                    AvgHum = g.Average(x => x.Humidity),
                    AvgCO2 = g.Average(x => x.Co2Level),
                    AvgEth = g.Average(x => x.EthyleneLevel)
                })
                .OrderBy(d => d.Date)
                .ToList();

            // ✅ Metrics summary
            var metrics = new[]
            {
                new {
                    Name = "Temperature",
                    Min = rows.Min(x => x.Temperature),
                    Max = rows.Max(x => x.Temperature),
                    Avg = rows.Average(x => x.Temperature),
                },
                new {
                    Name = "Humidity",
                    Min = rows.Min(x => x.Humidity),
                    Max = rows.Max(x => x.Humidity),
                    Avg = rows.Average(x => x.Humidity),
                },
                new {
                    Name = "CO₂",
                    Min = rows.Min(x => x.Co2Level),
                    Max = rows.Max(x => x.Co2Level),
                    Avg = rows.Average(x => x.Co2Level),
                },
                new {
                    Name = "Ethylene",
                    Min = rows.Min(x => x.EthyleneLevel),
                    Max = rows.Max(x => x.EthyleneLevel),
                    Avg = rows.Average(x => x.EthyleneLevel),
                }
            };

            // ✅ Bar chart: Low/High/Avg values with labels
            var barChartLabels = metrics.Select(m => m.Name).ToList(); // only sensor names

            var lowValues = metrics.Select(m => m.Min).ToList();
            var highValues = metrics.Select(m => m.Max).ToList();
            var avgValues = metrics.Select(m => m.Avg).ToList();

          

            // ✅ Severity Pie Chart
            int severityLow = 0, severityMedium = 0, severityHigh = 0;
            using (var conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString))
            {
                conn.Open();

                string severityQuery = "SELECT TRIM(LOWER(Severity)) AS Severity, COUNT(*) AS Count FROM Alerts GROUP BY TRIM(LOWER(Severity))";
                using (var cmd = new MySqlCommand(severityQuery, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string sev = reader["Severity"].ToString();
                            int count = Convert.ToInt32(reader["Count"]);
                            if (sev == "low")
                                severityLow = count;
                            else if (sev == "medium")
                                severityMedium = count;
                            else if (sev == "high")
                                severityHigh = count;
                        }
                    }
                }
            }

            double severityTotal = severityLow + severityMedium + severityHigh;
            double severityLowPercent = severityTotal > 0 ? (severityLow / severityTotal) * 100 : 0;
            double severityMediumPercent = severityTotal > 0 ? (severityMedium / severityTotal) * 100 : 0;
            double severityHighPercent = severityTotal > 0 ? (severityHigh / severityTotal) * 100 : 0;

            // ✅ Final JSON Response
            var chartData = new
            {
                hasData = true,
                lineChart = new
                {
                    labels = hourlyData.Select(d => d.Date.ToString("dd-MMM HH:mm")).ToList(),
                    datasets = new[]
                    {
                new { label = "Temperature", data = hourlyData.Select(d => d.AvgTemp).ToList(), borderColor = "rgb(220, 53, 69)", fill = false },
                new { label = "Humidity", data = hourlyData.Select(d => d.AvgHum).ToList(), borderColor = "rgb(0, 123, 255)", fill = false },
                new { label = "CO₂", data = hourlyData.Select(d => d.AvgCO2).ToList(), borderColor = "rgb(40, 167, 69)", fill = false },
                new { label = "Ethylene", data = hourlyData.Select(d => d.AvgEth).ToList(), borderColor = "rgb(255, 193, 7)", fill = false }
            }
                },
                barChart = new
                {

                    labels = barChartLabels,
                    datasets = new[]
                   {
                       new { label = "Low",  data = lowValues,  backgroundColor = "#60A5FA" },
                       new { label = "High", data = highValues, backgroundColor = "#F87171" },
                       new { label = "Avg",  data = avgValues,  backgroundColor = "#34D399" }
                   }
                },
                pieChart = new
                {
                    labels = new[]
                    {
                $"Low ({severityLowPercent:F1}%)",
                $"Medium ({severityMediumPercent:F1}%)",
                $"High ({severityHighPercent:F1}%)"
            },
                    datasets = new[]
                    {
                new
                {
                    data = new[] { severityLow, severityMedium, severityHigh },
                    backgroundColor = new[] { "#4F46E5", "#60A5FA", "#A78BFA" }
                }
            }
                }
            };

            return Json(chartData, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GeneratePdfSummarazieReport(string name, string startDate, string endDate, bool download = false)
        {
            List<ColdStorageUnit> rows = new List<ColdStorageUnit>();

            using (var conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString))
            {
                conn.Open();
                string filterQuery = " WHERE 1=1 ";
                if (!string.IsNullOrEmpty(name))
                    filterQuery += " AND TRIM(LOWER(Name)) = TRIM(LOWER(@name))";
                if (!string.IsNullOrEmpty(startDate))
                    filterQuery += " AND STR_TO_DATE(SUBSTRING_INDEX(Hardwaredate, ',', 1), '%e/%c/%Y') >= STR_TO_DATE(@startDate, '%Y-%m-%d')";
                if (!string.IsNullOrEmpty(endDate))
                    filterQuery += " AND STR_TO_DATE(SUBSTRING_INDEX(Hardwaredate, ',', 1), '%e/%c/%Y') <= STR_TO_DATE(@endDate, '%Y-%m-%d')";

                string orderBy = "ORDER BY STR_TO_DATE(Hardwaredate, '%d/%m/%Y, %h:%i:%s %p') DESC";
                string[] tables = { "ColdStorageUnit1", "ColdStorageUnit2" };
                int id = 1;

                foreach (var table in tables)
                {
                    string query = $"SELECT * FROM {table} {filterQuery} {orderBy}";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        if (!string.IsNullOrEmpty(name))
                            cmd.Parameters.AddWithValue("@name", name);
                        if (!string.IsNullOrEmpty(startDate))
                            cmd.Parameters.AddWithValue("@startDate", DateTime.Parse(startDate).ToString("yyyy-MM-dd"));
                        if (!string.IsNullOrEmpty(endDate))
                            cmd.Parameters.AddWithValue("@endDate", DateTime.Parse(endDate).ToString("yyyy-MM-dd"));

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                rows.Add(new ColdStorageUnit
                                {
                                    Id = id,
                                    Name = reader["Name"].ToString(),
                                    Temperature = Convert.ToDouble(reader["Temperature"]),
                                    Humidity = Convert.ToDouble(reader["Humidity"]),
                                    PowerStatus = reader["PowerStatus"].ToString(),
                                    DoorStatus = reader["DoorStatus"].ToString(),
                                    Co2Level = Convert.ToDouble(reader["Co2Level"]),
                                    EthyleneLevel = Convert.ToDouble(reader["EthyleneLevel"]),
                                    FanSpeed = Convert.ToInt32(reader["FanSpeed"]),
                                    Hardwaredate = reader["Hardwaredate"].ToString()
                                });
                                id++;
                            }
                        }
                    }
                }
            }

            using (var ms = new MemoryStream())
            {
                Document doc = new Document(PageSize.A4, 20f, 20f, 100f, 110f);
                var writer = PdfWriter.GetInstance(doc, ms);

                string logoPath = Server.MapPath("~/Images/logo.jpg");
                string selectedStatus = string.IsNullOrEmpty(name) ? "All Units" : name;
                writer.PageEvent = new PdfFooter(logoPath, selectedStatus, startDate, endDate);

                doc.Open();

                var subTitleFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);

                if (rows.Any())
                {
                    // === Summary calculations ===
                    double avgTemp = rows.Average(x => x.Temperature);
                    double minTemp = rows.Min(x => x.Temperature);
                    double maxTemp = rows.Max(x => x.Temperature);

                    double avgHum = rows.Average(x => x.Humidity);
                    double minHum = rows.Min(x => x.Humidity);
                    double maxHum = rows.Max(x => x.Humidity);

                    double avgEth = rows.Average(x => x.EthyleneLevel);
                    double minEth = rows.Min(x => x.EthyleneLevel);
                    double maxEth = rows.Max(x => x.EthyleneLevel);

                    double avgCO2 = rows.Average(x => x.Co2Level);
                    double minCO2 = rows.Min(x => x.Co2Level);
                    double maxCO2 = rows.Max(x => x.Co2Level);

                    // === Summary Table ===
                    PdfPTable summaryTable = new PdfPTable(5)
                    {
                        WidthPercentage = 100f,
                        SpacingBefore = 10f,
                        SpacingAfter = 10f
                    };
                    summaryTable.SetWidths(new float[] { 1.5f, 1f, 1f, 1f, 1f });

                    var summaryHeaderFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, BaseColor.WHITE);
                    var summaryCellFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.BLACK);
                    BaseColor headerBackground = new BaseColor(52, 73, 94);
                    BaseColor evenRowColor = new BaseColor(245, 245, 245);
                    BaseColor oddRowColor = BaseColor.WHITE;
                    BaseColor borderColor = new BaseColor(200, 200, 200);

                    string[] headers = { "Metric", "Min", "Max", "Avg", "Range" };
                    foreach (var h in headers)
                    {
                        PdfPCell headerCell = new PdfPCell(new Phrase(h, summaryHeaderFont))
                        {
                            BackgroundColor = headerBackground,
                            Padding = 6f,
                            HorizontalAlignment = Element.ALIGN_CENTER,
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            BorderColor = borderColor,
                            BorderWidth = 1f
                        };
                        summaryTable.AddCell(headerCell);
                    }

                    int rowIndex = 0;
                    void AddSensorRow(string metricName, double min, double max, double avg)
                    {
                        double range = max - min;
                        BaseColor bgColor = (rowIndex % 2 == 0) ? evenRowColor : oddRowColor;

                        string[] value = {
                    metricName,
                    (Math.Truncate(min * 10) / 10).ToString("0.0"),
                    (Math.Truncate(max * 10) / 10).ToString("0.0"),
                    (Math.Truncate(avg * 10) / 10).ToString("0.0"),
                    (Math.Truncate(range * 10) / 10).ToString("0.0")
                };

                        foreach (string val in value)
                        {
                            PdfPCell cell = new PdfPCell(new Phrase(val, summaryCellFont))
                            {
                                BackgroundColor = bgColor,
                                Padding = 5f,
                                BorderWidth = 1f,
                                BorderColor = borderColor,
                                HorizontalAlignment = Element.ALIGN_CENTER,
                                VerticalAlignment = Element.ALIGN_MIDDLE
                            };
                            summaryTable.AddCell(cell);
                        }
                        rowIndex++;
                    }

                    AddSensorRow("Temperature (°C)", minTemp, maxTemp, avgTemp);
                    AddSensorRow("Humidity (%)", minHum, maxHum, avgHum);
                    AddSensorRow("CO2 (ppm)", minCO2, maxCO2, avgCO2);
                    AddSensorRow("Ethylene (ppm)", minEth, maxEth, avgEth);

                    PdfPTable paddedWrapper = new PdfPTable(1) { WidthPercentage = 100f };
                    PdfPCell paddedCell = new PdfPCell(summaryTable)
                    {
                        PaddingLeft = 20f,
                        PaddingRight = 20f,
                        PaddingTop = 15f,
                        PaddingBottom = 0f,
                        Border = iTextSharp.text.Rectangle.NO_BORDER
                    };
                    paddedWrapper.AddCell(paddedCell);
                    doc.Add(paddedWrapper);
                    // iTextSharp font for threshold definitions text
                    var dateFont = FontFactory.GetFont(FontFactory.HELVETICA, 10f, iTextSharp.text.Font.NORMAL, BaseColor.BLACK);

                    var hourlyData = rows
                        .GroupBy(r =>
                        {
                            var dt = DateTime.ParseExact(
                                r.Hardwaredate.Trim(),
                                "d/M/yyyy, h:mm:ss tt", // full datetime format
                                CultureInfo.InvariantCulture
                            );
                            return new { Date = dt.Date, HourGroup = dt.Hour / 4 };
                        })
                        .Select(g => new
                        {
                            Date = g.Key.Date.AddHours(g.Key.HourGroup * 4),
                            AvgTemp = g.Average(x => x.Temperature),
                            AvgHum = g.Average(x => x.Humidity),
                            AvgCO2 = g.Average(x => x.Co2Level),
                            AvgEth = g.Average(x => x.EthyleneLevel)
                        })
                        .OrderBy(d => d.Date)
                        .ToList();


                    // === Create Modern Multi-Line Chart ===
                    var chart = new System.Web.UI.DataVisualization.Charting.Chart();
                    chart.Width = 650;
                    chart.Height = 320;
                    chart.BackColor = System.Drawing.Color.White;

                    // Chart Area styling
                    ChartArea chartArea = new ChartArea();
                    chartArea.AxisX.Interval = 4;
                    chartArea.AxisX.Title = "Date";
                    chartArea.AxisY.Title = "Sensor Values";
                    chartArea.AxisX.LabelStyle.Angle = -45;
                  
                    chartArea.BackColor = System.Drawing.Color.White;
                    chartArea.AxisX.MajorGrid.Enabled = false; // Remove vertical grid lines
                    chartArea.AxisY.MajorGrid.LineColor = System.Drawing.Color.LightGray; // Subtle horizontal grid
                    chartArea.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
                    chartArea.BorderWidth = 0;
                    chartArea.BorderColor = System.Drawing.Color.Transparent;
                    chartArea.AxisX.LineColor = System.Drawing.Color.Black;
                    chartArea.AxisY.LineColor = System.Drawing.Color.Black;
                    chart.ChartAreas.Add(chartArea);

                    // Helper function to make smooth series
                    Func<string, System.Drawing.Color, Func<dynamic, double>, Series> makeSeries = (seriesName, seriesColor, selector) =>
                    {
                        Series s = new Series(seriesName)
                        {
                            ChartType = SeriesChartType.Spline,
                            BorderWidth = 3,
                            Color = seriesColor,
                            MarkerStyle = MarkerStyle.Circle,
                            MarkerSize = 6,
                            XValueType = ChartValueType.DateTime


                        };

                        foreach (var d in hourlyData)
                            s.Points.AddXY(d.Date, selector(d));

                        return s;


                    };

                    // Format axis labels
                    // Set X-axis range to match your data
                    chart.ChartAreas[0].AxisX.Minimum = hourlyData.Min(d => d.Date).ToOADate();
                    chart.ChartAreas[0].AxisX.Maximum = hourlyData.Max(d => d.Date).ToOADate();

                    // Ensure interval type is hours for 4-hour blocks
                    chart.ChartAreas[0].AxisX.IntervalType = DateTimeIntervalType.Hours;
                    chart.ChartAreas[0].AxisX.Interval = 4;

                    // Format label
                    chart.ChartAreas[0].AxisX.LabelStyle.Format = "dd-MMM HH:mm";

                    // Add 4 smooth series
                    chart.Series.Add(makeSeries("Temperature", System.Drawing.Color.FromArgb(220, 53, 69), x => x.AvgTemp)); // Red-ish
                    chart.Series.Add(makeSeries("Humidity", System.Drawing.Color.FromArgb(0, 123, 255), x => x.AvgHum));    // Blue
                    chart.Series.Add(makeSeries("CO₂", System.Drawing.Color.FromArgb(40, 167, 69), x => x.AvgCO2));         // Green
                    chart.Series.Add(makeSeries("Ethylene", System.Drawing.Color.FromArgb(255, 193, 7), x => x.AvgEth));    // Yellow

                    // Legend styling
                    Legend legends = new Legend("Sensors");
                    legends.Docking = Docking.Top;
                    legends.Font = new System.Drawing.Font("Arial", 9, System.Drawing.FontStyle.Bold);
                    legends.BackColor = System.Drawing.Color.Transparent;
                    legends.BorderColor = System.Drawing.Color.Transparent;
                    chart.Legends.Add(legends);

                    // Save to MemoryStream
                    using (var chartStream = new MemoryStream())
                    {
                        chart.SaveImage(chartStream, ChartImageFormat.Png);
                        chartStream.Position = 0;

                        var chartImage = iTextSharp.text.Image.GetInstance(chartStream.ToArray());
                        chartImage.Alignment = Element.ALIGN_CENTER;
                        chartImage.ScaleToFit(500f, 155f);
                        doc.Add(chartImage);
                    }

                    // === Bar Chart for Min/Avg/Max Sensor Values (Single Color, One Legend) ===
                    var barChart = new System.Web.UI.DataVisualization.Charting.Chart();
                    barChart.Width = 700;
                    barChart.Height = 250;
                    barChart.BackColor = System.Drawing.Color.White;

                    // Chart area
                    // Chart area
                    ChartArea barArea = new ChartArea();
                    barArea.AxisX.Interval = 1;
                    barArea.AxisX.MajorGrid.Enabled = false;
                    barArea.AxisY.MajorGrid.LineColor = System.Drawing.Color.LightGray;
                    barArea.AxisX.Title = "Sensors";
                    barArea.AxisY.Title = "Values";
                    barArea.AxisX.LabelStyle.Angle = -20;
                    barChart.ChartAreas.Add(barArea);

                    // Legend
                    Legend legend = new Legend();
                    legend.Docking = Docking.Bottom;
                    legend.Alignment = StringAlignment.Center;
                    legend.Font = new System.Drawing.Font("Arial", 9f, System.Drawing.FontStyle.Bold);
                    barChart.Legends.Add(legend);

                    // Create 3 separate series (grouped by sensor)
                    Series lowSeries = new Series("Low")
                    {
                        ChartType = SeriesChartType.Column,
                        Color = System.Drawing.Color.FromArgb(150, 79, 70, 229) // light blue
                    };

                    Series highSeries = new Series("High")
                    {
                        ChartType = SeriesChartType.Column,
                        Color = System.Drawing.Color.FromArgb(150, 220, 38, 38) // light red
                    };

                    Series avgSeries = new Series("Avg")
                    {
                        ChartType = SeriesChartType.Column,
                        Color = System.Drawing.Color.FromArgb(150, 34, 197, 94) // light green
                    };

                    string[] sensors = { "Temperature", "Humidity", "CO₂", "Ethylene" };

                    foreach (string sensor in sensors)
                    {
                        double minVal = sensor == "Temperature" ? rows.Min(x => x.Temperature) :
                                        sensor == "Humidity" ? rows.Min(x => x.Humidity) :
                                        sensor == "CO₂" ? rows.Min(x => x.Co2Level) : rows.Min(x => x.EthyleneLevel);

                        double avgVal = sensor == "Temperature" ? rows.Average(x => x.Temperature) :
                                        sensor == "Humidity" ? rows.Average(x => x.Humidity) :
                                        sensor == "CO₂" ? rows.Average(x => x.Co2Level) : rows.Average(x => x.EthyleneLevel);

                        double maxVal = sensor == "Temperature" ? rows.Max(x => x.Temperature) :
                                        sensor == "Humidity" ? rows.Max(x => x.Humidity) :
                                        sensor == "CO₂" ? rows.Max(x => x.Co2Level) : rows.Max(x => x.EthyleneLevel);

                        // Add each value to its corresponding series (all share same category name)
                        lowSeries.Points.AddXY(sensor, minVal);
                        avgSeries.Points.AddXY(sensor, avgVal);
                        highSeries.Points.AddXY(sensor, maxVal);
                    }

                    // Add all 3 series
                    barChart.Series.Add(lowSeries);
                    barChart.Series.Add(avgSeries);
                    barChart.Series.Add(highSeries);

                    // Consistent width
                    lowSeries["PointWidth"] = "0.3";
                    avgSeries["PointWidth"] = "0.3";
                    highSeries["PointWidth"] = "0.3";

                    // === Save into PDF ===
                    using (var barStream = new MemoryStream())
                    {
                        barChart.SaveImage(barStream, ChartImageFormat.Png);
                        barStream.Position = 0;
                        var barImage = iTextSharp.text.Image.GetInstance(barStream.ToArray());
                        barImage.Alignment = Element.ALIGN_CENTER;
                        barImage.ScaleToFit(500f, 250f);
                        doc.Add(barImage);
                    }

                    // === Pie Chart from Severity column in Alerts table ===
                    int severityLow = 0, severityMedium = 0, severityHigh = 0;

                    using (var conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString))
                    {
                        conn.Open();

                        string severityQuery = "SELECT TRIM(LOWER(Severity)) AS Severity, COUNT(*) AS Count FROM Alerts GROUP BY TRIM(LOWER(Severity))";
                        using (var cmd = new MySqlCommand(severityQuery, conn))
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string sev = reader["Severity"].ToString();
                                int count = Convert.ToInt32(reader["Count"]);
                                if (sev == "low")
                                    severityLow = count;
                                else if (sev == "medium")
                                    severityMedium = count;
                                else if (sev == "high")
                                    severityHigh = count;
                            }
                        }
                    }

                    double severityTotal = severityLow + severityMedium + severityHigh;
                    double severityLowPercent = severityTotal > 0 ? (severityLow / severityTotal) * 100 : 0;
                    double severityMediumPercent = severityTotal > 0 ? (severityMedium / severityTotal) * 100 : 0;
                    double severityHighPercent = severityTotal > 0 ? (severityHigh / severityTotal) * 100 : 0;

                  
                    var pieChart = new System.Web.UI.DataVisualization.Charting.Chart();
                    pieChart.Width = 400;
                    pieChart.Height = 200;
                    pieChart.BackColor = System.Drawing.Color.White;
                    pieChart.ChartAreas.Add(new ChartArea("PieArea"));

                    Legend pieLegend = new Legend("Legend")
                    {
                        Docking = Docking.Right,
                        Alignment = StringAlignment.Center,
                        Font = new System.Drawing.Font("Arial", 10f, System.Drawing.FontStyle.Bold)
                    };
                    pieChart.Legends.Add(pieLegend);

                    Series pieSeries = new Series("Severity");
                    pieSeries.ChartType = SeriesChartType.Pie;
                    pieSeries.Points.AddXY("Low", severityLow);
                    pieSeries.Points.AddXY("Medium", severityMedium);
                    pieSeries.Points.AddXY("High", severityHigh);

                    // Colors from your request
                    pieSeries.Points[0].Color = System.Drawing.ColorTranslator.FromHtml("#A78BFA"); // Indigo
                    pieSeries.Points[1].Color = System.Drawing.ColorTranslator.FromHtml("#60A5FA"); // Light Blue
                    pieSeries.Points[2].Color = System.Drawing.ColorTranslator.FromHtml("#4F46E5"); // Purple

                    pieSeries.Label = "#VALX: #PERCENT{P1}";
                    pieSeries.LegendText = "#VALX";

                    pieChart.Series.Add(pieSeries);

                    // Save pie chart to stream
                    // Save pie chart to stream
                    using (var pieStream = new MemoryStream())
                    {
                        pieChart.SaveImage(pieStream, ChartImageFormat.Png);
                        pieStream.Position = 0;

                        doc.Add(new Paragraph("\n\n")); // ✅ spacing before pie chart
                        var pieImage = iTextSharp.text.Image.GetInstance(pieStream.ToArray());
                        pieImage.Alignment = Element.ALIGN_CENTER;
                        pieImage.ScaleToFit(350f, 120f); // ✅ smaller so it fits on same page
                        doc.Add(pieImage);
                    }

                }
                else
                {
                    doc.Add(new Paragraph("No data found for the selected filters.", subTitleFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingBefore = 20f
                    });
                }

                doc.Close();

                var fileBytes = ms.ToArray();
                var fileName = $"ColdStorage_Summary_{DateTime.Now:yyyyMMdd_HHmm}.pdf";

                Response.Clear();
                Response.ContentType = "application/pdf";
                if (download)
                    Response.AddHeader("Content-Disposition", $"attachment; filename={fileName}");
                else
                    Response.AddHeader("Content-Disposition", $"inline; filename={fileName}");

                Response.OutputStream.Write(fileBytes, 0, fileBytes.Length);
                Response.Flush();
                Response.End();

                return null;
            }
        }

        private PdfPCell CreateCenterCell(string text, Font font)
        {
            return new PdfPCell(new Phrase(text, font))
            {
                HorizontalAlignment = Element.ALIGN_CENTER,
                VerticalAlignment = Element.ALIGN_MIDDLE
            };
        }

        //For Excel Genration  data- ColdstorageUnit 2
        public FileResult GenerateExcelSummaryReport(string name, string startDate, string endDate)
        {
            List<ColdStorageUnit> data = GetFilteredColdStorageData(name, startDate, endDate);
            IWorkbook workbook = new XSSFWorkbook();
            bool isAll = string.IsNullOrEmpty(name) || name.Trim().ToLower() == "all";

            // Common Styles
            ICellStyle titleStyle = workbook.CreateCellStyle();
            IFont titleFont = workbook.CreateFont();
            titleFont.FontHeightInPoints = 20;
            titleFont.IsBold = true;
            titleStyle.SetFont(titleFont);
            titleStyle.Alignment = HorizontalAlignment.Center;

            ICellStyle subStyle = workbook.CreateCellStyle();
            IFont subFont = workbook.CreateFont();
            subFont.FontHeightInPoints = 12;
            subStyle.SetFont(subFont);
            subStyle.Alignment = HorizontalAlignment.Center;

            ICellStyle centerStyle = workbook.CreateCellStyle();
            centerStyle.Alignment = HorizontalAlignment.Center;
            centerStyle.VerticalAlignment = VerticalAlignment.Center;

            ICellStyle boldStyle = workbook.CreateCellStyle();
            IFont boldFont = workbook.CreateFont();
            boldFont.IsBold = true;
            boldStyle.SetFont(boldFont);
            boldStyle.Alignment = HorizontalAlignment.Center;

            ICellStyle headerStyle = workbook.CreateCellStyle();
            IFont headerFont = workbook.CreateFont();
            headerFont.IsBold = true;
            headerStyle.SetFont(headerFont);
            headerStyle.Alignment = HorizontalAlignment.Center;
            headerStyle.VerticalAlignment = VerticalAlignment.Center;
            headerStyle.FillForegroundColor = IndexedColors.Yellow.Index;
            headerStyle.FillPattern = FillPattern.SolidForeground;
            headerStyle.BorderBottom = BorderStyle.Thin;

            ICellStyle oneDecimalStyle = workbook.CreateCellStyle();
            oneDecimalStyle.CloneStyleFrom(centerStyle);
            oneDecimalStyle.DataFormat = workbook.CreateDataFormat().GetFormat("0.0");

            ICellStyle ethyleneStyle = workbook.CreateCellStyle();
            ethyleneStyle.CloneStyleFrom(centerStyle);
            ethyleneStyle.DataFormat = workbook.CreateDataFormat().GetFormat("0.00");

            if (isAll)
            {
                var units = data.Select(d => d.Name).Distinct();

                foreach (var unit in units)
                {
                    var unitData = data.Where(d => d.Name == unit).ToList();
                    ISheet sheet = workbook.CreateSheet(unit);

                    // Set column widths
                    sheet.SetColumnWidth(0, 10 * 256);
                    sheet.SetColumnWidth(1, 10 * 256);
                    for (int i = 2; i <= 7; i++) sheet.SetColumnWidth(i, 18 * 256);
                    for (int i = 8; i <= 11; i++) sheet.SetColumnWidth(i, 20 * 256);

                    // --- Row 0: Logo + Title ---
                    IRow row0 = sheet.CreateRow(0);
                    row0.HeightInPoints = 80;

                    string imagePath = HostingEnvironment.MapPath("~/Images/logo.jpg");
                    if (System.IO.File.Exists(imagePath))
                    {
                        byte[] imageBytes = System.IO.File.ReadAllBytes(imagePath);
                        int pictureIdx = workbook.AddPicture(imageBytes, PictureType.PNG);
                        IDrawing drawing = sheet.CreateDrawingPatriarch();
                        IClientAnchor anchor = workbook.GetCreationHelper().CreateClientAnchor();
                        anchor.Col1 = 0; anchor.Row1 = 1; anchor.Col2 = 2; anchor.Row2 = 3;
                        drawing.CreatePicture(anchor, pictureIdx).Resize(1.0);
                    }

                    ICell titleCell = row0.CreateCell(2);
                    titleCell.SetCellValue("Banana Cold Storage");
                    titleCell.CellStyle = titleStyle;
                    sheet.AddMergedRegion(new CellRangeAddress(0, 0, 2, 7));

                    row0.CreateCell(8).SetCellValue("Start Date:");
                    row0.CreateCell(9).SetCellValue(startDate + " 00:00:00");

                    // --- Row 1: Subtitle + End Date ---
                    IRow row1 = sheet.CreateRow(1);
                    ICell subCell = row1.CreateCell(2);
                    subCell.SetCellValue($"Summary Report for {unit}");
                    subCell.CellStyle = subStyle;
                    sheet.AddMergedRegion(new CellRangeAddress(1, 1, 2, 7));
                    row1.CreateCell(8).SetCellValue("End Date:");
                    row1.CreateCell(9).SetCellValue(endDate + " 23:59:59");

                    // --- Row 2: Generated Date ---
                    IRow row2 = sheet.CreateRow(2);
                    row2.CreateCell(8).SetCellValue("Generated:");
                    row2.CreateCell(9).SetCellValue(DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"));

                    // --- Row 3: Divider ---
                    IRow dividerRow = sheet.CreateRow(3);
                    for (int i = 0; i <= 11; i++)
                    {
                        ICell cell = dividerRow.CreateCell(i);
                        ICellStyle borderStyle = workbook.CreateCellStyle();
                        borderStyle.BorderBottom = BorderStyle.Medium;
                        cell.CellStyle = borderStyle;
                    }

                    // --- Row 4–7: Summary rows ---
                    if (unitData.Any())
                    {
                        double avgTemp = unitData.Average(x => x.Temperature);
                        double minTemp = unitData.Min(x => x.Temperature);
                        double maxTemp = unitData.Max(x => x.Temperature);

                        double avgHum = unitData.Average(x => x.Humidity);
                        double minHum = unitData.Min(x => x.Humidity);
                        double maxHum = unitData.Max(x => x.Humidity);

                        double avgEth = unitData.Average(x => x.EthyleneLevel);
                        double minEth = unitData.Min(x => x.EthyleneLevel);
                        double maxEth = unitData.Max(x => x.EthyleneLevel);

                        double avgCO2 = unitData.Average(x => x.Co2Level);
                        double minCO2 = unitData.Min(x => x.Co2Level);
                        double maxCO2 = unitData.Max(x => x.Co2Level);

                        IRow metricHeader = sheet.CreateRow(4);
                        metricHeader.CreateCell(1).SetCellValue("Metric");
                        metricHeader.CreateCell(2).SetCellValue("Temperature (°C)");
                        metricHeader.CreateCell(3).SetCellValue("Humidity (%)");
                        metricHeader.CreateCell(4).SetCellValue("CO₂ (ppm)");
                        metricHeader.CreateCell(5).SetCellValue("Ethylene (ppm)");
                        for (int i = 1; i <= 5; i++) metricHeader.GetCell(i).CellStyle = boldStyle;

                        IRow avgRow = sheet.CreateRow(5);
                        avgRow.CreateCell(1).SetCellValue("Average");
                        avgRow.CreateCell(2).SetCellValue(Math.Round(avgTemp, 1));
                        avgRow.CreateCell(3).SetCellValue(Math.Round(avgHum, 1));
                        avgRow.CreateCell(4).SetCellValue(Math.Round(avgCO2, 1));
                        avgRow.CreateCell(5).SetCellValue(Math.Round(avgEth, 1));

                        IRow minRow = sheet.CreateRow(6);
                        minRow.CreateCell(1).SetCellValue("Minimum");
                        minRow.CreateCell(2).SetCellValue(Math.Round(minTemp, 1));
                        minRow.CreateCell(3).SetCellValue(Math.Round(minHum, 1));
                        minRow.CreateCell(4).SetCellValue(Math.Round(minCO2, 1));
                        minRow.CreateCell(5).SetCellValue(Math.Round(minEth, 1));

                        IRow maxRow = sheet.CreateRow(7);
                        maxRow.CreateCell(1).SetCellValue("Maximum");
                        maxRow.CreateCell(2).SetCellValue(Math.Round(maxTemp, 1));
                        maxRow.CreateCell(3).SetCellValue(Math.Round(maxHum, 1));
                        maxRow.CreateCell(4).SetCellValue(Math.Round(maxCO2, 1));
                        maxRow.CreateCell(5).SetCellValue(Math.Round(maxEth, 1));
                    }

                    // --- Row 8: Headers ---
                    IRow header = sheet.CreateRow(8);
                    string[] unitHeaders = { "S.No", "Temperature (°C)", "Humidity (%)", "Power Status", "Door Status", "CO₂ Level (ppm)", "Ethylene Level (ppm)", "Fan Speed", "DateTime" };
                    for (int i = 0; i < unitHeaders.Length; i++)
                    {
                        ICell cell = header.CreateCell(i);
                        cell.SetCellValue(unitHeaders[i]);
                        cell.CellStyle = headerStyle;
                    }

                    // --- Row 9+: Data ---
                    int rowIdx = 9;
                    int sn = 1;

                    foreach (var item in unitData)
                    {
                        IRow r = sheet.CreateRow(rowIdx++);
                        r.CreateCell(0).SetCellValue(sn++);
                        r.GetCell(0).CellStyle = centerStyle;

                        r.CreateCell(1).SetCellValue(Math.Round((double)item.Temperature, 1));
                        r.GetCell(1).CellStyle = oneDecimalStyle;

                        r.CreateCell(2).SetCellValue(Math.Round((double)item.Humidity, 1));
                        r.GetCell(2).CellStyle = oneDecimalStyle;

                        r.CreateCell(3).SetCellValue(item.PowerStatus);
                        r.GetCell(3).CellStyle = centerStyle;

                        r.CreateCell(4).SetCellValue(item.DoorStatus);
                        r.GetCell(4).CellStyle = centerStyle;

                        r.CreateCell(5).SetCellValue(Math.Round((double)item.Co2Level, 1));
                        r.GetCell(5).CellStyle = oneDecimalStyle;

                        r.CreateCell(6).SetCellValue(Math.Round((double)item.EthyleneLevel, 2));
                        r.GetCell(6).CellStyle = ethyleneStyle;

                        r.CreateCell(7).SetCellValue(item.FanSpeed);
                        r.GetCell(7).CellStyle = centerStyle;

                        r.CreateCell(8).SetCellValue(item.Hardwaredate);
                        r.GetCell(8).CellStyle = centerStyle;
                    }

                    for (int col = 0; col < unitHeaders.Length; col++)
                        sheet.AutoSizeColumn(col);
                }
            }

            // Export
            using (var stream = new MemoryStream())
            {
                workbook.Write(stream);
                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ColdStorage_Summary.xlsx");
            }
        }
      
        //Get Filterd data for Doorsteps
        private List<DoorStatus> GetFilteredDoorStatusData(string status, string startDate1, string endDate1)
        {
            var filteredData = new List<DoorStatus>();

            using (var conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString))
            {
                conn.Open();

                // Get all distinct unit names for dropdown
                List<string> allNames = new List<string>();
                using (var cmd = new MySqlCommand("SELECT DISTINCT TRIM(Status) as Name FROM door_status ORDER BY Name", conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            allNames.Add(reader["Name"].ToString());
                        }
                    }
                }
                ViewBag.StatusList = allNames;


                // Build filter query
                string filterQuery = " WHERE 1=1 ";
                if (!string.IsNullOrEmpty(status))
                    filterQuery += " AND TRIM(LOWER(Status)) = TRIM(LOWER(@status))";

                if (!string.IsNullOrEmpty(startDate1) && !string.IsNullOrEmpty(endDate1))
                {
                    if (!string.IsNullOrEmpty(startDate1) && !string.IsNullOrEmpty(endDate1))
                    {
                        if (startDate1 == endDate1)
                        {
                            // ✅ Fix for same date (ignore time)
                            filterQuery += @"
            AND DATE(STR_TO_DATE(SUBSTRING_INDEX(Hardwaredate, ',', 1), '%d/%m/%Y')) 
                = DATE(STR_TO_DATE(@startDate, '%Y-%m-%d'))";
                        }
                        else
                        {
                            filterQuery += @"
            AND STR_TO_DATE(SUBSTRING_INDEX(Hardwaredate, ',', 1), '%d/%m/%Y') 
                BETWEEN STR_TO_DATE(@startDate, '%Y-%m-%d') 
                AND STR_TO_DATE(@endDate, '%Y-%m-%d')";
                        }
                    }

                    else
                    {
                        // Range → include end date fully
                        filterQuery += " AND STR_TO_DATE(SUBSTRING_INDEX(Hardwaredate, ',', 1), '%d/%m/%Y') >= STR_TO_DATE(@startDate, '%Y-%m-%d')";
                        filterQuery += " AND STR_TO_DATE(SUBSTRING_INDEX(Hardwaredate, ',', 1), '%d/%m/%Y') < DATE_ADD(STR_TO_DATE(@endDate, '%Y-%m-%d'), INTERVAL 1 DAY)";
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(startDate1))
                        filterQuery += " AND STR_TO_DATE(SUBSTRING_INDEX(Hardwaredate, ',', 1), '%d/%m/%Y') >= STR_TO_DATE(@startDate, '%Y-%m-%d')";

                    if (!string.IsNullOrEmpty(endDate1))
                        filterQuery += " AND STR_TO_DATE(SUBSTRING_INDEX(Hardwaredate, ',', 1), '%d/%m/%Y') < DATE_ADD(STR_TO_DATE(@endDate, '%Y-%m-%d'), INTERVAL 1 DAY)";
                }


                string orderBy = " ORDER BY STR_TO_DATE(Hardwaredate, '%d/%m/%Y, %h:%i:%s %p') DESC";

                // Final query
                string query = $"SELECT * FROM door_status {filterQuery} {orderBy}";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    if (!string.IsNullOrEmpty(status))
                        cmd.Parameters.AddWithValue("@status", status);
                    if (!string.IsNullOrEmpty(startDate1))
                        cmd.Parameters.AddWithValue("@startDate", DateTime.Parse(startDate1).ToString("yyyy-MM-dd"));
                    if (!string.IsNullOrEmpty(endDate1))
                        cmd.Parameters.AddWithValue("@endDate", DateTime.Parse(endDate1).ToString("yyyy-MM-dd"));

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            filteredData.Add(new DoorStatus
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Unitname = reader["Unitname"].ToString(),
                                Status = reader["Status"].ToString(),
                                Hardwaredate = reader["Hardwaredate"].ToString()
                            });
                        }
                    }
                }
            }

            ViewBag.Selectedstatus = status;
            ViewBag.StartDate1 = startDate1;
            ViewBag.EndDate1 = endDate1;

            return filteredData;
        }
        //for doorsttaus excel data 
        public FileResult GenerateDoorStatusExcelReport(string status, string startDate, string endDate)
        {
            List<DoorStatus> rows = new List<DoorStatus>();

            using (var conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString))
            {
                conn.Open();

                string filterQuery = " WHERE 1=1 ";
                if (!string.IsNullOrEmpty(status))
                    filterQuery += " AND TRIM(LOWER(Status)) = TRIM(LOWER(@status))";
                if (!string.IsNullOrEmpty(startDate))
                    filterQuery += " AND STR_TO_DATE(SUBSTRING_INDEX(Hardwaredate, ',', 1), '%d/%m/%Y') >= STR_TO_DATE(@startDate, '%Y-%m-%d')";
                if (!string.IsNullOrEmpty(endDate))
                    filterQuery += " AND STR_TO_DATE(SUBSTRING_INDEX(Hardwaredate, ',', 1), '%d/%m/%Y') <= STR_TO_DATE(@endDate, '%Y-%m-%d')";

                string query = $"SELECT * FROM door_status {filterQuery} ORDER BY STR_TO_DATE(Hardwaredate, '%d/%m/%Y, %h:%i:%s %p') DESC";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    if (!string.IsNullOrEmpty(status))
                        cmd.Parameters.AddWithValue("@status", status);
                    if (!string.IsNullOrEmpty(startDate))
                        cmd.Parameters.AddWithValue("@startDate", DateTime.Parse(startDate).ToString("yyyy-MM-dd"));
                    if (!string.IsNullOrEmpty(endDate))
                        cmd.Parameters.AddWithValue("@endDate", DateTime.Parse(endDate).ToString("yyyy-MM-dd"));

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            rows.Add(new DoorStatus
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Unitname = reader["Unitname"].ToString(),
                                Status = reader["Status"].ToString(),
                                Hardwaredate = reader["Hardwaredate"].ToString()
                            });
                        }
                    }
                }
            }

            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("Door Status");


            // Set column widths (2/6/4 layout)
            sheet.SetColumnWidth(0, 10 * 256); // Logo col
            sheet.SetColumnWidth(1, 10 * 256);
            for (int i = 2; i <= 7; i++) sheet.SetColumnWidth(i, 18 * 256); // Center (title/subtitle)
            for (int i = 8; i <= 11; i++) sheet.SetColumnWidth(i, 20 * 256); // Date section

            // Create styles
            ICellStyle titleStyle = workbook.CreateCellStyle();
            IFont titleFont = workbook.CreateFont();
            titleFont.FontHeightInPoints = 20;
            titleFont.IsBold = true;
            titleStyle.SetFont(titleFont);
            titleStyle.Alignment = HorizontalAlignment.Center;

            ICellStyle subStyle = workbook.CreateCellStyle();
            IFont subFont = workbook.CreateFont();
            subFont.FontHeightInPoints = 12;
            subStyle.SetFont(subFont);
            subStyle.Alignment = HorizontalAlignment.Center;

            ICellStyle centerStyle = workbook.CreateCellStyle();
            centerStyle.Alignment = HorizontalAlignment.Center;
            centerStyle.VerticalAlignment = VerticalAlignment.Center;

            // --- Row 0: Logo + Title ---
            //IRow row0 = sheet.CreateRow(0);
            //row0.HeightInPoints = 70; // Reduced height

            // --- Set column widths ---
            // Layout: [0-1]: Logo (narrow), [2-5]: Title+Subtitle, [6-7]: spacing buffer, [8-9]: Dates
            sheet.SetColumnWidth(0, 9 * 256);   // Logo column
            sheet.SetColumnWidth(1, 9 * 256);
            sheet.SetColumnWidth(2, 14 * 256);  // Title start
            sheet.SetColumnWidth(3, 14 * 256);
            sheet.SetColumnWidth(4, 14 * 256);
            sheet.SetColumnWidth(5, 14 * 256);  // Title end
            sheet.SetColumnWidth(6, 5 * 256);   // Buffer
            sheet.SetColumnWidth(7, 5 * 256);
            sheet.SetColumnWidth(8, 12 * 256);  // Start/End/Generated label
            sheet.SetColumnWidth(9, 20 * 256);  // Start/End/Generated value

            // --- Row 0: Logo + Title ---
            IRow row0 = sheet.CreateRow(0);
            row0.HeightInPoints = 80;

            string imagePath = HostingEnvironment.MapPath("~/Images/logo.jpg");
            if (System.IO.File.Exists(imagePath))
            {
                byte[] imageBytes = System.IO.File.ReadAllBytes(imagePath);
                int pictureIdx = workbook.AddPicture(imageBytes, PictureType.PNG);

                IDrawing drawing = sheet.CreateDrawingPatriarch();
                IClientAnchor anchor = workbook.GetCreationHelper().CreateClientAnchor();
                anchor.Col1 = 0;
                anchor.Row1 = 1;
                anchor.Col2 = 2;
                anchor.Row2 = 2;
                drawing.CreatePicture(anchor, pictureIdx).Resize(1.4); // 90% size
            }

            // Title Cell (reduced width span: cols 2–5)
            ICell titleCell = row0.CreateCell(2);
            titleCell.SetCellValue("Banana Cold Storage");
            titleCell.CellStyle = titleStyle;
            sheet.AddMergedRegion(new CellRangeAddress(0, 0, 2, 5));

            // Start Date (cols 8-9)
            row0.CreateCell(8).SetCellValue("Start Date:");
            row0.CreateCell(9).SetCellValue(startDate + " 00:00:00");

            // --- Row 1: Subtitle + End Date ---
            IRow row1 = sheet.CreateRow(1);
            ICell subCell = row1.CreateCell(2);
            subCell.SetCellValue($"Summary Report for {(string.IsNullOrEmpty(status) ? "All Units" : status)}");
            subCell.CellStyle = subStyle;
            sheet.AddMergedRegion(new CellRangeAddress(1, 1, 2, 5));

            // End Date
            row1.CreateCell(8).SetCellValue("End Date:");
            row1.CreateCell(9).SetCellValue(endDate + " 23:59:59");

            // --- Row 2: Generated Date ---
            IRow row2 = sheet.CreateRow(2);
            row2.CreateCell(8).SetCellValue("Generated:");
            row2.CreateCell(9).SetCellValue(DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"));

            // --- Row 3: (optional spacer or empty) ---
            // (you can skip or use for notes)

            // --- Row 4: Horizontal Divider ---
            IRow dividerRow = sheet.CreateRow(4);
            for (int i = 0; i <= 9; i++)
            {
                ICell cell = dividerRow.CreateCell(i);
                ICellStyle borderStyle = workbook.CreateCellStyle();
                borderStyle.BorderBottom = BorderStyle.Medium;
                cell.CellStyle = borderStyle;
            }


            // ===== Row 3: Column Headers =====
            IRow headerRow = sheet.CreateRow(5);

            string[] headers = { "S.No", "Unit Name", "Status", "DateTime" };


            for (int i = 0; i < headers.Length; i++)
            {
                ICell cell = headerRow.CreateCell(i);
                cell.SetCellValue(headers[i]);

                ICellStyle headerStyle = workbook.CreateCellStyle();
                IFont font = workbook.CreateFont();
                font.IsBold = true;
                headerStyle.SetFont(font);
                headerStyle.Alignment = HorizontalAlignment.Center;
                headerStyle.VerticalAlignment = VerticalAlignment.Center;
                headerStyle.BorderBottom = BorderStyle.Thin;
                headerStyle.FillForegroundColor = IndexedColors.Yellow.Index;
                headerStyle.FillPattern = FillPattern.SolidForeground;

                cell.CellStyle = headerStyle;
            }

            // ===== Data Rows =====
            int rowIndex = 6;
            int serial = 1;
            foreach (var item in rows)
            {
                IRow row = sheet.CreateRow(rowIndex++);

                ICell cell0 = row.CreateCell(0);
                cell0.SetCellValue(serial++);
                cell0.CellStyle = centerStyle;

                ICell cell1 = row.CreateCell(1);
                cell1.SetCellValue(item.Unitname);
                cell1.CellStyle = centerStyle;

                ICell cell2 = row.CreateCell(2);
                cell2.SetCellValue(item.Status);
                cell2.CellStyle = centerStyle;

                ICell cell3 = row.CreateCell(3);
                cell3.SetCellValue(item.Hardwaredate);
                cell3.CellStyle = centerStyle;
            }

            // ===== Auto-size Columns =====
            for (int i = 0; i <= 3; i++)
                sheet.AutoSizeColumn(i);

            // ===== Return File =====
            using (var ms = new MemoryStream())
            {
                workbook.Write(ms);
                return File(ms.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "DoorStatus_Report.xlsx");
            }
        }
        // for pdf of doorstatus
        public FileResult GenerateDoorStatusPdfReport(string status, string startDate, string endDate)
        {
            List<DoorStatus> rows = new List<DoorStatus>();

            using (var conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString))
            {
                conn.Open();

                string filterQuery = " WHERE 1=1 ";
                if (!string.IsNullOrEmpty(status))
                    filterQuery += " AND TRIM(LOWER(Status)) = TRIM(LOWER(@status))";
                if (!string.IsNullOrEmpty(startDate))
                    filterQuery += " AND STR_TO_DATE(SUBSTRING_INDEX(Hardwaredate, ',', 1), '%d/%m/%Y') >= STR_TO_DATE(@startDate, '%Y-%m-%d')";
                if (!string.IsNullOrEmpty(endDate))
                    filterQuery += " AND STR_TO_DATE(SUBSTRING_INDEX(Hardwaredate, ',', 1), '%d/%m/%Y') <= STR_TO_DATE(@endDate, '%Y-%m-%d')";

                string query = $"SELECT * FROM door_status {filterQuery} ORDER BY STR_TO_DATE(Hardwaredate, '%d/%m/%Y, %h:%i:%s %p') DESC";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    if (!string.IsNullOrEmpty(status))
                        cmd.Parameters.AddWithValue("@status", status);
                    if (!string.IsNullOrEmpty(startDate))
                        cmd.Parameters.AddWithValue("@startDate", DateTime.Parse(startDate).ToString("yyyy-MM-dd"));
                    if (!string.IsNullOrEmpty(endDate))
                        cmd.Parameters.AddWithValue("@endDate", DateTime.Parse(endDate).ToString("yyyy-MM-dd"));

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            rows.Add(new DoorStatus
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Unitname = reader["Unitname"].ToString(),
                                Status = reader["Status"].ToString(),
                                Hardwaredate = reader["Hardwaredate"].ToString()
                            });
                        }
                    }
                }
            }

            // ===== PDF Generation =====
            using (var ms = new MemoryStream())
            {
                Document doc = new Document(PageSize.A4, 20f, 20f, 100f, 110f);
                var writer = PdfWriter.GetInstance(doc, ms);

                string logoPath = Server.MapPath("~/Images/logo.jpg");
                string selectedStatus = string.IsNullOrEmpty(status) ? "All Units" : status;

                writer.PageEvent = new PdfFooter(logoPath, selectedStatus, startDate, endDate);

                doc.Open();

                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11);
                var cellFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);

                PdfPTable table = new PdfPTable(4)
                {
                    WidthPercentage = 100f
                };
                table.SetWidths(new float[] { 1f, 3f, 3f, 3f });
                table.HeaderRows = 1;

                string[] headers = { "S.No", "Unit Name", "Status", "DateTime" };
                foreach (var h in headers)
                {
                    PdfPCell cell = new PdfPCell(new Phrase(h, headerFont))
                    {
                        BackgroundColor = BaseColor.YELLOW,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE
                    };
                    table.AddCell(cell);
                }

                int serial = 1;
                foreach (var r in rows)
                {
                    table.AddCell(CreateCenterCell(serial++.ToString(), cellFont));
                    table.AddCell(CreateCenterCell(r.Unitname, cellFont));
                    table.AddCell(CreateCenterCell(r.Status, cellFont));
                    table.AddCell(CreateCenterCell(r.Hardwaredate, cellFont));
                }

                doc.Add(table);
                doc.Close();

                return File(ms.ToArray(), "application/pdf", "DoorStatus_Report.pdf");
            }
        }

        //Get Filterd Data For Alert TABLE
        private List<Alert> GetFilteredAlertsData(string severity, string startDate2, string endDate2)
        {
            var filteredData = new List<Alert>();

            try
            {
                using (var conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString))
                {
                    conn.Open();

                    // Populate severity dropdown
                    var allSeverities = new List<string>();
                    using (var cmd = new MySqlCommand("SELECT DISTINCT TRIM(Severity) as Severity FROM Alerts ORDER BY Severity", conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                allSeverities.Add(reader["Severity"].ToString());
                            }
                        }
                    }
                    ViewBag.SeverityList = allSeverities;

                    // Build query
                    string filterQuery = " WHERE 1=1 ";
                    if (!string.IsNullOrEmpty(severity))
                        filterQuery += " AND TRIM(LOWER(Severity)) = TRIM(LOWER(@severity))";
                    if (!string.IsNullOrEmpty(startDate2))
                        filterQuery += " AND DATE(Alert_Date) >= @startDate";
                    if (!string.IsNullOrEmpty(endDate2))
                        filterQuery += " AND DATE(Alert_Date) <= @endDate";


                    string orderBy = " ORDER BY Alert_Date DESC";
                    string query = $"SELECT * FROM Alerts {filterQuery} {orderBy}";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        if (!string.IsNullOrEmpty(severity))
                            cmd.Parameters.AddWithValue("@severity", severity.Trim());

                        if (!string.IsNullOrEmpty(startDate2))
                            cmd.Parameters.AddWithValue("@startDate", DateTime.Parse(startDate2).Date);

                        if (!string.IsNullOrEmpty(endDate2))
                            cmd.Parameters.AddWithValue("@endDate", DateTime.Parse(endDate2).Date);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                filteredData.Add(new Alert
                                {
                                    ID = Convert.ToInt32(reader["ID"]),
                                    Alert_Name = reader["Alert_Name"].ToString(),
                                    Condition_Trigger = reader["Condition_Trigger"].ToString(),
                                    Severity = reader["Severity"].ToString(),
                                    Remarks = reader["Remarks"].ToString(),
                                    Alert_Date = reader["Alert_Date"].ToString(),
                                    UnitName = reader["UnitName"].ToString(),
                                    Actual_Value =Convert.ToDouble(reader["Actual_Value"].ToString())
                                });
                            }
                        }
                    }

                    ViewBag.SelectedSeverity = severity;
                    ViewBag.StartDate2 = startDate2;
                    ViewBag.EndDate2 = endDate2;
                }
            }
            catch (Exception ex)
            {
                ViewBag.AlertError = "Error loading alert data: " + ex.Message;
            }

            return filteredData;
        }
        public FileResult GenerateAlertsPdfReport(string severity, string startDate2, string endDate2)
        {
            List<Alert> rows = new List<Alert>();

            using (var conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString))
            {
                conn.Open();

                string filterQuery = " WHERE 1=1 ";
                if (!string.IsNullOrEmpty(severity))
                    filterQuery += " AND TRIM(LOWER(Severity)) = TRIM(LOWER(@severity))";

                if (!string.IsNullOrEmpty(startDate2))
                    filterQuery += "AND DATE(STR_TO_DATE(Alert_Date, '%Y-%m-%d %H:%i:%s')) >= STR_TO_DATE(@startDate, '%Y-%m-%d')";
                if (!string.IsNullOrEmpty(endDate2))
                    filterQuery += "AND DATE(STR_TO_DATE(Alert_Date, '%Y-%m-%d %H:%i:%s')) <= STR_TO_DATE(@endDate, '%Y-%m-%d')";

                string query = $"SELECT * FROM Alerts {filterQuery} ORDER BY Alert_Date DESC";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    if (!string.IsNullOrEmpty(severity))
                        cmd.Parameters.AddWithValue("@severity", severity);
                   
                    if (!string.IsNullOrEmpty(startDate2))
                        cmd.Parameters.AddWithValue("@startDate", DateTime.Parse(startDate2).ToString("yyyy-MM-dd"));
                    if (!string.IsNullOrEmpty(endDate2))
                        cmd.Parameters.AddWithValue("@endDate", DateTime.Parse(endDate2).ToString("yyyy-MM-dd"));

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            rows.Add(new Alert
                            {
                                ID = Convert.ToInt32(reader["Id"]),
                                Alert_Name = reader["Alert_Name"].ToString(),
                                Condition_Trigger = reader["Condition_Trigger"].ToString(),
                                Severity = reader["Severity"].ToString(),
                                Remarks = reader["Remarks"].ToString(),
                                Alert_Date = reader["Alert_Date"].ToString(),
                                Actual_Value= Convert.ToDouble(reader["Actual_Value"].ToString()),
                                UnitName = reader["UnitName"].ToString()
                            });
                        }
                    }
                }
            }

            // ===== PDF Generation =====
            using (var ms = new MemoryStream())
            {
                Document doc = new Document(PageSize.A4.Rotate(), 50f, 50f, 110f, 110f);
                var writer = PdfWriter.GetInstance(doc, ms);

                string logoPath = Server.MapPath("~/Images/logo.jpg");
                string selectedSeverity = string.IsNullOrEmpty(severity) ? "All Severities" : severity;

                writer.PageEvent = new PdfFooter(logoPath, selectedSeverity, startDate2, endDate2);

                doc.Open();

                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11);
                var cellFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);

                PdfPTable table = new PdfPTable(8)
                {
                    WidthPercentage = 100f
                };
                table.SetWidths(new float[] { 0.7f, 1f, 2f, 1f, 1f, 3f, 1f,2f });
                table.HeaderRows = 1;

                string[] headers = { "S.No", "Unit", "Alert Name", "Trigger", "Severity", "Remarks", "Actual Value", "DateTime" };
                foreach (var h in headers)
                {
                    PdfPCell cell = new PdfPCell(new Phrase(h, headerFont))
                    {
                        BackgroundColor = BaseColor.LIGHT_GRAY,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE
                    };
                    table.AddCell(cell);
                }

                if (rows.Any())
                {
                    int serial = 1;
                    foreach (var r in rows)
                    {
                        table.AddCell(CreateCenterCell(serial++.ToString(), cellFont));
                        table.AddCell(CreateCenterCell(r.UnitName, cellFont));
                        table.AddCell(CreateCenterCell(r.Alert_Name, cellFont));
                        table.AddCell(CreateCenterCell(r.Condition_Trigger, cellFont));
                        table.AddCell(CreateCenterCell(r.Severity, cellFont));
                        table.AddCell(CreateCenterCell(r.Remarks, cellFont));
                        table.AddCell(CreateCenterCell(r.Actual_Value.ToString(), cellFont));
                        table.AddCell(CreateCenterCell(r.Alert_Date, cellFont));
                        
                    }

                    doc.Add(table);
                }
                else
                {
                    // Show a "No Data" message in the PDF
                    Paragraph noData = new Paragraph("No alerts found for the selected filters.", headerFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingBefore = 50f
                    };
                    doc.Add(noData);
                }

                doc.Close();
                return File(ms.ToArray(), "application/pdf", "Alerts_Report.pdf");

            }
        }
        public FileResult GenerateAlertsExcelReport(string status, string startDate2, string endDate2)
        {
                  List<Alert> alerts = GetFilteredAlertsData(status, startDate2, endDate2);

                  IWorkbook workbook = new XSSFWorkbook();
                  ISheet sheet = workbook.CreateSheet("Alerts Report");
                  bool isAll = string.IsNullOrEmpty(status) || status.Trim().ToLower() == "all";

                 // Common Styles
                 ICellStyle titleStyle = workbook.CreateCellStyle();
                 IFont titleFont = workbook.CreateFont();
                 titleFont.FontHeightInPoints = 20;
                 titleFont.IsBold = true;
                 titleStyle.SetFont(titleFont);
                 titleStyle.Alignment = HorizontalAlignment.Center;

                 ICellStyle subStyle = workbook.CreateCellStyle();
                 IFont subFont = workbook.CreateFont();
                 subFont.FontHeightInPoints = 12;
                 subStyle.SetFont(subFont);
                 subStyle.Alignment = HorizontalAlignment.Center;

                 ICellStyle centerStyle = workbook.CreateCellStyle();
                 centerStyle.Alignment = HorizontalAlignment.Center;
                 centerStyle.VerticalAlignment = VerticalAlignment.Center;

                 ICellStyle boldStyle = workbook.CreateCellStyle();
                 IFont boldFont = workbook.CreateFont();
                 boldFont.IsBold = true;
                 boldStyle.SetFont(boldFont);
                 boldStyle.Alignment = HorizontalAlignment.Center;

                 ICellStyle headerStyle = workbook.CreateCellStyle();
                 IFont headerFont = workbook.CreateFont();
                 headerFont.IsBold = true;
                 headerStyle.SetFont(headerFont);
                 headerStyle.Alignment = HorizontalAlignment.Center;
                 headerStyle.VerticalAlignment = VerticalAlignment.Center;
                 headerStyle.FillForegroundColor = IndexedColors.Yellow.Index;
                 headerStyle.FillPattern = FillPattern.SolidForeground;
                 headerStyle.BorderBottom = BorderStyle.Thin;

                // Set column widths
                sheet.SetColumnWidth(0, 10 * 256);  
                sheet.SetColumnWidth(1, 10 * 256);
                for (int i = 2; i <= 7; i++) sheet.SetColumnWidth(i, 18 * 256);
                for (int i = 8; i <= 11; i++) sheet.SetColumnWidth(i, 20 * 256);

                // --- Row 0: Logo + Title ---
                IRow row0 = sheet.CreateRow(0);
                row0.HeightInPoints = 80;

                string imagePath = HostingEnvironment.MapPath("~/Images/logo.jpg");
                if (System.IO.File.Exists(imagePath))
                {
                    byte[] imageBytes = System.IO.File.ReadAllBytes(imagePath);
                    int pictureIdx = workbook.AddPicture(imageBytes, PictureType.PNG);
                    IDrawing drawing = sheet.CreateDrawingPatriarch();
                    IClientAnchor anchor = workbook.GetCreationHelper().CreateClientAnchor();
                    anchor.Col1 = 0; anchor.Row1 = 1; anchor.Col2 = 1; anchor.Row2 = 2;
                    drawing.CreatePicture(anchor, pictureIdx).Resize(1.5);
                }

                ICell titleCell = row0.CreateCell(3);
                titleCell.SetCellValue("Banana Cold Storage");
                titleCell.CellStyle = titleStyle;
                sheet.AddMergedRegion(new CellRangeAddress(0, 0, 3, 7));

                row0.CreateCell(8).SetCellValue("Start Date:");
                row0.CreateCell(9).SetCellValue(startDate2 + " 00:00:00");

                // --- Row 1: Subtitle + End Date ---
                IRow row1 = sheet.CreateRow(1);
                ICell subCell = row1.CreateCell(3);
                subCell.SetCellValue($"Summary Report for {status}");
                subCell.CellStyle = subStyle;
                sheet.AddMergedRegion(new CellRangeAddress(1, 1, 2, 7));
                row1.CreateCell(8).SetCellValue("End Date:");
                row1.CreateCell(9).SetCellValue(endDate2 + " 23:59:59");

                // --- Row 2: Generated Date ---
                IRow row2 = sheet.CreateRow(2);
                row2.CreateCell(8).SetCellValue("Generated:");
                row2.CreateCell(9).SetCellValue(DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"));

                // --- Row 3: Divider ---
                IRow dividerRow = sheet.CreateRow(3);
                for (int i = 0; i <= 11; i++)
                {
                    ICell cell = dividerRow.CreateCell(i);
                    ICellStyle borderStyle = workbook.CreateCellStyle();
                    borderStyle.BorderBottom = BorderStyle.Medium;
                    cell.CellStyle = borderStyle;
                }

                // --- Row 4: Headers ---
                IRow header = sheet.CreateRow(4);
                string[] headers = { "S.No", "Unit Name", "Alert Name", "Trigger", "Severity", "Remarks", "Actual Value", "DateTime" };

                for (int i = 0; i < headers.Length; i++)
                {
                    ICell cell = header.CreateCell(i);
                    cell.SetCellValue(headers[i]);
                    cell.CellStyle = headerStyle;
                }

                // --- Row 5+: Data ---
                int rowIdx = 5;
                int sn = 1;

                foreach (var r in alerts)
                {
                    IRow row = sheet.CreateRow(rowIdx++);
                    row.CreateCell(0).SetCellValue(sn++);
                    row.CreateCell(1).SetCellValue(r.UnitName ?? "");
                    row.CreateCell(2).SetCellValue(r.Alert_Name ?? "");
                    row.CreateCell(3).SetCellValue(r.Condition_Trigger ?? "");
                    row.CreateCell(4).SetCellValue(r.Severity ?? "");
                    row.CreateCell(5).SetCellValue(r.Remarks ?? "");
                    row.CreateCell(6).SetCellValue(r.Actual_Value);
                    row.CreateCell(7).SetCellValue(r.Alert_Date ?? "");
                

                    for (int i = 0; i <= 7; i++)
                    {
                        row.GetCell(i).CellStyle = centerStyle;
                    }
                }

                // --- Export ---
                using (var stream = new MemoryStream())
                {
                    workbook.Write(stream);
                    return File(
                        stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "Alerts_Report.xlsx"
                    );
                }
            }
        
        // Genartes PDF  of 24 hr data 
        public FileContentResult GenerateFullPdf(string name, string startDate, string endDate)
        {
            var data = GetFilteredColdStorageData(name, startDate, endDate);
            byte[] pdfBytes = GeneratePdfBytes(data, name, startDate, endDate);
            return File(pdfBytes, "application/pdf", "DailyReport.pdf");
        }

        //TO Send Automatic Email pdf
        private byte[] GeneratePdfBytes(List<ColdStorageUnit> data, string name, string startDate, string endDate)
        {
            using (var memoryStream = new MemoryStream())
            {
                Document doc = new Document(PageSize.A4.Rotate(), 20f, 20f, 100f, 110f); // Landscape for more columns
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string logoPath = Path.Combine(basePath, "Images", "logo.jpg");

                string selectedStatus = string.IsNullOrEmpty(name) ? "All Units" : name;

                PdfWriter writer = PdfWriter.GetInstance(doc, memoryStream);
                writer.PageEvent = new PdfFooter(logoPath, selectedStatus, startDate, endDate); // Header/Footer

                doc.Open();

                var cellFont = FontFactory.GetFont(FontFactory.HELVETICA, 9);
                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);

                PdfPTable table = new PdfPTable(10)
                {
                    WidthPercentage = 100
                };

                table.SetWidths(new float[] { 1f, 2f, 1.5f, 1.5f, 2f, 2f, 2f, 2f, 1.5f, 2f });

                // Column headers
                string[] headers = { "ID", "Name", "Temperature (°C)", "Humidity (%)", "Power Status", "Door Status", "CO₂ Level", "Ethylene Level", "Fan Speed", "DateTime" };

                foreach (var h in headers)
                {
                    var headerCell = new PdfPCell(new Phrase(h, headerFont))
                    {
                        BackgroundColor = BaseColor.LIGHT_GRAY,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        Padding = 5
                    };
                    table.AddCell(headerCell);
                }

                // Data rows
                int serialNumber = 1;
                foreach (var item in data)
                {
                    table.AddCell(CreateCenterCell(serialNumber.ToString(), cellFont)); 
                    table.AddCell(CreateCenterCell(item.Name, cellFont));
                    table.AddCell(CreateCenterCell(item.Temperature.ToString("0.0"), cellFont));
                    table.AddCell(CreateCenterCell(item.Humidity.ToString("0.0"), cellFont));
                    table.AddCell(CreateCenterCell(item.PowerStatus, cellFont));
                    table.AddCell(CreateCenterCell(item.DoorStatus, cellFont));
                    table.AddCell(CreateCenterCell(item.Co2Level.ToString("0.00"), cellFont));
                    table.AddCell(CreateCenterCell(item.EthyleneLevel.ToString("0.00"), cellFont));
                    table.AddCell(CreateCenterCell(item.FanSpeed.ToString(), cellFont));
                    table.AddCell(CreateCenterCell(item.Hardwaredate, cellFont));
                    serialNumber++; // Increment manually
                }

                doc.Add(table);
                doc.Close();

                return memoryStream.ToArray();
            }
        }

        // To send Automatic Excel to email 
        public FileContentResult GenerateFullExcel(string name, string startDate, string endDate)
        {
            var data = GetFilteredColdStorageData(name, startDate, endDate);
            byte[] excelBytes = GenerateExcelBytes(data, name, startDate, endDate);
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "DailyReport.xlsx");
        }
        // Saves Excel File
        private byte[] GenerateExcelBytes(List<ColdStorageUnit> data, string name, string startDate, string endDate)
        {
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("ColdStorage Daily  Report");
            // Ensure entire table fits on one page when printing
            sheet.PrintSetup.Landscape = true;   // Landscape layout
            sheet.FitToPage = true;              // Enable Fit To Page
            sheet.PrintSetup.FitWidth = 1;       // Fit to 1 page wide
            sheet.PrintSetup.FitHeight = 0;      // Unlimited pages in height
                                                 // Set custom column widths
            sheet.SetColumnWidth(0, 1500); // ID
            sheet.SetColumnWidth(1, 3500); // Name
            sheet.SetColumnWidth(2, 3000); // Temp
            sheet.SetColumnWidth(3, 3000); // Humidity
            sheet.SetColumnWidth(4, 2500); // Power
            sheet.SetColumnWidth(5, 2500); // Door
            sheet.SetColumnWidth(6, 2500); // CO2
            sheet.SetColumnWidth(7, 2500); // Ethylene
            sheet.SetColumnWidth(8, 2000); // Fan
            sheet.SetColumnWidth(9, 4500); // DateTime

            int rowIndex = 0;

            // Add Title Row
            IRow titleRow = sheet.CreateRow(rowIndex++);
            titleRow.CreateCell(0).SetCellValue("Daily  Report");
            sheet.AddMergedRegion(new CellRangeAddress(0, 0, 0, 9)); // Merge title across 10 columns

            // Add Meta Information Row
            IRow metaRow = sheet.CreateRow(rowIndex++);
            metaRow.CreateCell(0).SetCellValue("Unit:");
            metaRow.CreateCell(1).SetCellValue(string.IsNullOrEmpty(name) ? "All Units" : name);
            metaRow.CreateCell(2).SetCellValue("From:");
            metaRow.CreateCell(3).SetCellValue(startDate);
            metaRow.CreateCell(4).SetCellValue("To:");
            metaRow.CreateCell(5).SetCellValue(endDate);

            rowIndex++; // Leave one empty row

            // Add Header Row
            IRow headerRow = sheet.CreateRow(rowIndex++);
            string[] headers = { "ID", "Name", "Temp (°C)", "Humidity (%)", "Power", "Door", "CO₂", "Ethylene", "Fan", "DateTime" };

            //string[] headers = { "ID", "Name", "Temp(°C)", "Humidity (%)", "Power Status", "Door Status", "CO₂ Level", "Ethylene Level", "Fan Speed", "DateTime" };
            for (int i = 0; i < headers.Length; i++)
            {
                ICell cell = headerRow.CreateCell(i);
                cell.SetCellValue(headers[i]);
                ICellStyle style = workbook.CreateCellStyle();
                IFont font = workbook.CreateFont();
                font.IsBold = true;
                style.SetFont(font);
                style.Alignment = HorizontalAlignment.Center;
                cell.CellStyle = style;
                
            }

            // Add Data Rows
            int serialNumber = 1;
            foreach (var item in data)
            {
                IRow row = sheet.CreateRow(rowIndex++);
                row.CreateCell(0).SetCellValue(serialNumber++);
                row.CreateCell(1).SetCellValue(item.Name);
                row.CreateCell(2).SetCellValue(item.Temperature);
                row.CreateCell(3).SetCellValue(item.Humidity);
                row.CreateCell(4).SetCellValue(item.PowerStatus);
                row.CreateCell(5).SetCellValue(item.DoorStatus);
                row.CreateCell(6).SetCellValue(item.Co2Level);
                row.CreateCell(7).SetCellValue(item.EthyleneLevel);
                row.CreateCell(8).SetCellValue(item.FanSpeed);
                row.CreateCell(9).SetCellValue(item.Hardwaredate);
            }

            // Auto-size columns
            for (int i = 0; i < headers.Length; i++)
                sheet.AutoSizeColumn(i);

            using (var stream = new MemoryStream())
            {
                workbook.Write(stream);
                return stream.ToArray();
            }
        }

    }
}