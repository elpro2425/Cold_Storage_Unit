using Cold_Storage_Unit.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using MathNet.Numerics.LinearAlgebra.Factorization;
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
using System.Data;
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
                    double truncatedEth = Math.Truncate(r.EthyleneLevel * 100) / 100;
                    table.AddCell(CreateCenterCell(r.Id.ToString(), cellFont));

                    if (includeName)
                        table.AddCell(CreateCenterCell(r.Name, cellFont));

                    table.AddCell(CreateCenterCell((Math.Truncate(r.Temperature * 10) / 10).ToString("0.0"), cellFont));
                    table.AddCell(CreateCenterCell((Math.Truncate(r.Humidity * 10) / 10).ToString("0.0"), cellFont));
                    table.AddCell(CreateCenterCell(r.PowerStatus, cellFont));
                    table.AddCell(CreateCenterCell(r.DoorStatus, cellFont));
                    table.AddCell(CreateCenterCell((Math.Truncate(r.Co2Level * 10) / 10).ToString("0.0"), cellFont));
                  
                    table.AddCell(CreateCenterCell(truncatedEth.ToString("0.00"), cellFont));
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
                    summaryTable.AddCell(new Phrase((Math.Truncate(avgEth * 100) / 100).ToString("0.00"), summaryCellFont));
                    // Min Row
                    summaryTable.AddCell(new Phrase("Minimum", summaryCellFont));
                    summaryTable.AddCell(new Phrase((Math.Truncate(minTemp * 10) / 10).ToString("0.0"), summaryCellFont));
                    summaryTable.AddCell(new Phrase((Math.Truncate(minHum * 10) / 10).ToString("0.0"), summaryCellFont));
                    summaryTable.AddCell(new Phrase((Math.Truncate(minCO2 * 10) / 10).ToString("0.0"), summaryCellFont));
                    summaryTable.AddCell(new Phrase((Math.Truncate(minEth * 100) / 100).ToString("0.00"), summaryCellFont));

                    // Max Row
                    summaryTable.AddCell(new Phrase("Maximum", summaryCellFont));
                    summaryTable.AddCell(new Phrase((Math.Truncate(maxTemp * 10) / 10).ToString("0.0"), summaryCellFont));
                    summaryTable.AddCell(new Phrase((Math.Truncate(maxHum * 10) / 10).ToString("0.0"), summaryCellFont));
                    summaryTable.AddCell(new Phrase((Math.Truncate(maxCO2 * 10) / 10).ToString("0.0"), summaryCellFont));
                    summaryTable.AddCell(new Phrase((Math.Truncate(maxEth * 100) / 100).ToString("0.00"), summaryCellFont));

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
            var connStr = ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString;
            DateTime startDt = DateTime.Parse(startDate, CultureInfo.InvariantCulture);
            DateTime endDt = DateTime.Parse(endDate, CultureInfo.InvariantCulture);

            List<string> lineLabels = new List<string>();
            List<double> tempData = new List<double>();
            List<double> humData = new List<double>();
            List<double> co2Data = new List<double>();
            List<double> ethData = new List<double>();

            List<string> barLabels = new List<string>();
            List<double> minValues = new List<double>();
            List<double> maxValues = new List<double>();
            List<double> avgValues = new List<double>();

            List<string> pieLabels = new List<string>();
            List<double> pieData = new List<double>();

            using (var conn = new MySqlConnection(connStr))
            {
                conn.Open();

                // ====================== AGGREGATED LINE CHART ======================
                string groupExpr = startDt.Date == endDt.Date
                    ? "DATE_FORMAT(dt, '%d-%b %H:00')"               // hourly
                    : "CONCAT(DATE(dt), ' ', LPAD(FLOOR(HOUR(dt)/4)*4,2,'0'), ':00')"; // 4-hour blocks

                string lineQuery = $@"
SELECT 
    {groupExpr} AS time_group,
    ROUND(AVG(Temperature),2) AS avg_temperature,
    ROUND(AVG(Humidity),2) AS avg_humidity,
    ROUND(AVG(Co2Level),2) AS avg_co2,
    ROUND(AVG(EthyleneLevel),2) AS avg_ethylene
FROM (
    SELECT STR_TO_DATE(Hardwaredate, '%e/%c/%Y, %r') AS dt, Temperature, Humidity, Co2Level, EthyleneLevel
    FROM ColdStorageUnit1
    WHERE STR_TO_DATE(Hardwaredate, '%e/%c/%Y, %r') BETWEEN @startDateTime AND @endDateTime
    UNION ALL
    SELECT STR_TO_DATE(Hardwaredate, '%e/%c/%Y, %r') AS dt, Temperature, Humidity, Co2Level, EthyleneLevel
    FROM ColdStorageUnit2
    WHERE STR_TO_DATE(Hardwaredate, '%e/%c/%Y, %r') BETWEEN @startDateTime AND @endDateTime
) combined
GROUP BY time_group
ORDER BY time_group;
";

                DataTable dtLine = new DataTable();
                using (var da = new MySqlDataAdapter(lineQuery, conn))
                {
                    da.SelectCommand.Parameters.AddWithValue("@startDateTime", startDt.ToString("yyyy-MM-dd 00:00:00"));
                    da.SelectCommand.Parameters.AddWithValue("@endDateTime", endDt.ToString("yyyy-MM-dd 23:59:59"));
                    da.Fill(dtLine);
                }

                foreach (DataRow row in dtLine.Rows)
                {
                    lineLabels.Add(row["time_group"].ToString());
                    tempData.Add(Convert.ToDouble(row["avg_temperature"]));
                    humData.Add(Convert.ToDouble(row["avg_humidity"]));
                    co2Data.Add(Convert.ToDouble(row["avg_co2"]));
                    ethData.Add(Convert.ToDouble(row["avg_ethylene"]));
                }

                // ====================== APPEND CURRENT POINTS AFTER LAST AGGREGATE ======================
                string newestQuery = @"
                SELECT dt, Temperature, Humidity, Co2Level, EthyleneLevel FROM (
                    SELECT STR_TO_DATE(Hardwaredate, '%e/%c/%Y, %r') AS dt, Temperature, Humidity, Co2Level, EthyleneLevel
                    FROM ColdStorageUnit1
                    WHERE STR_TO_DATE(Hardwaredate, '%e/%c/%Y, %r') BETWEEN @startDateTime AND @endDateTime
                    UNION ALL
                    SELECT STR_TO_DATE(Hardwaredate, '%e/%c/%Y, %r') AS dt, Temperature, Humidity, Co2Level, EthyleneLevel
                    FROM ColdStorageUnit2
                    WHERE STR_TO_DATE(Hardwaredate, '%e/%c/%Y, %r') BETWEEN @startDateTime AND @endDateTime
                ) allData
                WHERE dt > @lastAggregated
                ORDER BY dt ASC;
                ";

                DateTime lastAggregated = dtLine.Rows.Count > 0
                    ? DateTime.ParseExact(dtLine.Rows[dtLine.Rows.Count - 1]["time_group"].ToString(),
                                          startDt.Date == endDt.Date ? "dd-MMM HH:00" : "yyyy-MM-dd HH:00",
                                          CultureInfo.InvariantCulture)
                    : startDt;

                using (var cmdNewest = new MySqlCommand(newestQuery, conn))
                {
                    cmdNewest.Parameters.AddWithValue("@startDateTime", startDt.ToString("yyyy-MM-dd 00:00:00"));
                    cmdNewest.Parameters.AddWithValue("@endDateTime", endDt.ToString("yyyy-MM-dd 23:59:59"));
                    cmdNewest.Parameters.AddWithValue("@lastAggregated", lastAggregated);

                    using (var reader = cmdNewest.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string label = Convert.ToDateTime(reader["dt"]).ToString("dd-MMM HH:mm");
                            if (!lineLabels.Contains(label))
                            {
                                lineLabels.Add(label);
                                tempData.Add(Convert.ToDouble(reader["Temperature"]));
                                humData.Add(Convert.ToDouble(reader["Humidity"]));
                                co2Data.Add(Convert.ToDouble(reader["Co2Level"]));
                                ethData.Add(Convert.ToDouble(reader["EthyleneLevel"]));
                            }
                        }
                    }
                }

                // ====================== METRICS (MIN/MAX/AVG) ======================
                string metricsQuery = @"
               SELECT 
                   ROUND(MIN(Temperature),2) AS min_temp, ROUND(MAX(Temperature),2) AS max_temp, ROUND(AVG(Temperature),2) AS avg_temp,
                   ROUND(MIN(Humidity),2) AS min_hum, ROUND(MAX(Humidity),2) AS max_hum, ROUND(AVG(Humidity),2) AS avg_hum,
                   ROUND(MIN(Co2Level),2) AS min_co2, ROUND(MAX(Co2Level),2) AS max_co2, ROUND(AVG(Co2Level),2) AS avg_co2,
                   ROUND(MIN(EthyleneLevel),2) AS min_eth, ROUND(MAX(EthyleneLevel),2) AS max_eth, ROUND(AVG(EthyleneLevel),2) AS avg_eth
               FROM (
                   SELECT Temperature, Humidity, Co2Level, EthyleneLevel FROM ColdStorageUnit1
                   WHERE STR_TO_DATE(Hardwaredate, '%e/%c/%Y, %r') BETWEEN @startDateTime AND @endDateTime
                   UNION ALL
                   SELECT Temperature, Humidity, Co2Level, EthyleneLevel FROM ColdStorageUnit2
                   WHERE STR_TO_DATE(Hardwaredate, '%e/%c/%Y, %r') BETWEEN @startDateTime AND @endDateTime
               ) combined;
                 ";

                using (var cmd = new MySqlCommand(metricsQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@startDateTime", startDt.ToString("yyyy-MM-dd 00:00:00"));
                    cmd.Parameters.AddWithValue("@endDateTime", endDt.ToString("yyyy-MM-dd 23:59:59"));
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            barLabels = new List<string> { "Temperature", "Humidity", "CO₂", "Ethylene" };
                            minValues = new List<double>
                    {
                        Convert.ToDouble(reader["min_temp"]),
                        Convert.ToDouble(reader["min_hum"]),
                        Convert.ToDouble(reader["min_co2"]),
                        Convert.ToDouble(reader["min_eth"])
                    };
                            maxValues = new List<double>
                    {
                        Convert.ToDouble(reader["max_temp"]),
                        Convert.ToDouble(reader["max_hum"]),
                        Convert.ToDouble(reader["max_co2"]),
                        Convert.ToDouble(reader["max_eth"])
                    };
                            avgValues = new List<double>
                    {
                        Convert.ToDouble(reader["avg_temp"]),
                        Convert.ToDouble(reader["avg_hum"]),
                        Convert.ToDouble(reader["avg_co2"]),
                        Convert.ToDouble(reader["avg_eth"])
                    };
                        }
                    }
                }

                // ====================== PIE CHART ======================
                string pieQuery = @"
                  SELECT 
                      TRIM(LOWER(Severity)) AS severity,
                      ROUND(COUNT(*) * 100.0 / SUM(COUNT(*)) OVER (), 2) AS percentage
                  FROM Alerts
                  WHERE CAST(Alert_Date AS DATETIME) BETWEEN @startDateTime AND @endDateTime
                  GROUP BY Severity;
                  ";

                using (var cmd = new MySqlCommand(pieQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@startDateTime", startDt.ToString("yyyy-MM-dd 00:00:00"));
                    cmd.Parameters.AddWithValue("@endDateTime", endDt.ToString("yyyy-MM-dd 23:59:59"));

                    using (var da = new MySqlDataAdapter(cmd))
                    {
                        DataTable dtPie = new DataTable();
                        da.Fill(dtPie);
                        foreach (DataRow row in dtPie.Rows)
                        {
                            pieLabels.Add($"{row["severity"]} ({row["percentage"]}%)");
                            pieData.Add(Convert.ToDouble(row["percentage"]));
                        }
                    }
                }
            }

            // ====================== FINAL JSON ======================
            var chartData = new
            {
                hasData = lineLabels.Any(),
                lineChart = new
                {
                    labels = lineLabels,
                    datasets = new[]
                    {
                new { label = "Temperature", data = tempData, borderColor = "rgb(220,53,69)", fill = false },
                new { label = "Humidity", data = humData, borderColor = "rgb(0,123,255)", fill = false },
                new { label = "CO₂", data = co2Data, borderColor = "rgb(40,167,69)", fill = false },
                new { label = "Ethylene", data = ethData, borderColor = "rgb(255,193,7)", fill = false }
            }
                },
                barChart = new
                {
                    labels = barLabels,
                    datasets = new[]
                    {
                new { label = "Low", data = minValues, backgroundColor = "#60A5FA" },
                new { label = "High", data = maxValues, backgroundColor = "#F87171" },
                new { label = "Avg", data = avgValues, backgroundColor = "#34D399" }
            }
                },
                pieChart = new
                {
                    labels = pieLabels,
                    datasets = new[]
                    {
                new { data = pieData, backgroundColor = new[] { "#4F46E5", "#60A5FA", "#A78BFA" } }
            }
                }
            };

            return Json(chartData, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GeneratePdfSummarazieReport(string name, string startDate, string endDate, bool download = false)
        {
            using (var ms = new MemoryStream())
            {
                Document doc = new Document(PageSize.A4, 20f, 20f, 100f, 110f);
                var writer = PdfWriter.GetInstance(doc, ms);

                string logoPath = Server.MapPath("~/Images/logo.jpg");
                string selectedStatus = string.IsNullOrEmpty(name) ? "All Units" : name;
                writer.PageEvent = new PdfFooter(logoPath, selectedStatus, startDate, endDate);

                doc.Open();
                var subTitleFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);

                DataTable summaryDt = new DataTable();
                DataTable severityDt = new DataTable();
                DataTable lineDt = new DataTable();

                using (var conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString))
                {
                    conn.Open();

                    // === 1. Summary Query for Bar Chart ===
                    string summaryQuery = @"
                     SELECT
                    ROUND(AVG(Temperature), 2) AS avg_temperature,
                    MIN(Temperature) AS min_temperature,
                    MAX(Temperature) AS max_temperature,

                    ROUND(AVG(Humidity), 2) AS avg_humidity,
                    MIN(Humidity) AS min_humidity,
                    MAX(Humidity) AS max_humidity,

                    ROUND(AVG(Co2Level), 2) AS avg_co2,
                    MIN(Co2Level) AS min_co2,
                    MAX(Co2Level) AS max_co2,

                    ROUND(AVG(EthyleneLevel), 2) AS avg_ethylene,
                    MIN(EthyleneLevel) AS min_ethylene,
                    MAX(EthyleneLevel) AS max_ethylene
                FROM (
                    SELECT Temperature, Humidity, Co2Level, EthyleneLevel
                    FROM ColdStorageUnit1
                    WHERE STR_TO_DATE(Hardwaredate, '%e/%c/%Y, %r')
                          BETWEEN @StartDate AND @EndDate
                    UNION ALL
                    SELECT Temperature, Humidity, Co2Level, EthyleneLevel
                    FROM ColdStorageUnit2
                    WHERE STR_TO_DATE(Hardwaredate, '%e/%c/%Y, %r')
                          BETWEEN @StartDate AND @EndDate
                ) AS combined_data;
            ";

                    using (var cmd = new MySqlCommand(summaryQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@StartDate", startDate + " 00:00:00");
                        cmd.Parameters.AddWithValue("@EndDate", endDate + " 23:59:59");

                        using (var da = new MySqlDataAdapter(cmd))
                        {
                            da.Fill(summaryDt);
                        }
                    }

                    // === 2. Line Chart Query (hourly or 4-hour) ===
                    bool sameDate = DateTime.Parse(startDate).Date == DateTime.Parse(endDate).Date;

                    string groupExpr = sameDate
                        ? "DATE_FORMAT(dt, '%Y-%m-%d %H:00:00')"     // Hourly
                        : "DATE_FORMAT(DATE_ADD(DATE(dt), INTERVAL (HOUR(dt) DIV 4)*4 HOUR), '%Y-%m-%d %H:00:00')"; // 4-hour

                    string lineQuery = $@"
                SELECT 
                    {groupExpr} AS timeblock,
                    ROUND(AVG(Temperature),2) AS avg_temp,
                    ROUND(AVG(Humidity),2) AS avg_hum,
                    ROUND(AVG(Co2Level),2) AS avg_co2,
                    ROUND(AVG(EthyleneLevel),2) AS avg_eth
                FROM (
                    SELECT STR_TO_DATE(Hardwaredate, '%e/%c/%Y, %r') AS dt,
                           Temperature, Humidity, Co2Level, EthyleneLevel
                    FROM ColdStorageUnit1
                    WHERE STR_TO_DATE(Hardwaredate, '%e/%c/%Y, %r')
                          BETWEEN @StartDate AND @EndDate
                    UNION ALL
                    SELECT STR_TO_DATE(Hardwaredate, '%e/%c/%Y, %r') AS dt,
                           Temperature, Humidity, Co2Level, EthyleneLevel
                    FROM ColdStorageUnit2
                    WHERE STR_TO_DATE(Hardwaredate, '%e/%c/%Y, %r')
                          BETWEEN @StartDate AND @EndDate
                ) AS combined
                GROUP BY timeblock
                ORDER BY timeblock;
            ";

                    using (var cmd = new MySqlCommand(lineQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@StartDate", startDate + " 00:00:00");
                        cmd.Parameters.AddWithValue("@EndDate", endDate + " 23:59:59");

                        using (var da = new MySqlDataAdapter(cmd))
                        {
                            da.Fill(lineDt);
                        }
                    }

                    // === 3. Pie Chart Severity Query ===
                    string severityQuery = @"
                          SELECT 
                              Severity,
                              ROUND(COUNT(*) * 100.0 / SUM(COUNT(*)) OVER (), 2) AS percentage
                          FROM Alerts
                          WHERE CAST(Alert_Date AS DATETIME) BETWEEN @StartDate AND @EndDate
                          GROUP BY Severity;
                      ";

                    using (var cmd2 = new MySqlCommand(severityQuery, conn))
                    {
                        cmd2.Parameters.AddWithValue("@StartDate", startDate + " 00:00:00");
                        cmd2.Parameters.AddWithValue("@EndDate", endDate + " 23:59:59");

                        using (var da2 = new MySqlDataAdapter(cmd2))
                        {
                            da2.Fill(severityDt);
                        }
                    }
                }

                // === Summary Table ===
                if (summaryDt.Rows.Count > 0)
                {
                    var row = summaryDt.Rows[0];
                    double minTemp = Convert.ToDouble(row["min_temperature"]);
                    double maxTemp = Convert.ToDouble(row["max_temperature"]);
                    double avgTemp = Convert.ToDouble(row["avg_temperature"]);

                    double minHum = Convert.ToDouble(row["min_humidity"]);
                    double maxHum = Convert.ToDouble(row["max_humidity"]);
                    double avgHum = Convert.ToDouble(row["avg_humidity"]);

                    double minCO2 = Convert.ToDouble(row["min_co2"]);
                    double maxCO2 = Convert.ToDouble(row["max_co2"]);
                    double avgCO2 = Convert.ToDouble(row["avg_co2"]);

                    double minEth = Convert.ToDouble(row["min_ethylene"]);
                    double maxEth = Convert.ToDouble(row["max_ethylene"]);
                    double avgEth = Convert.ToDouble(row["avg_ethylene"]);

                    PdfPTable summaryTable = new PdfPTable(5)
                    {
                        WidthPercentage = 90f,
                        SpacingBefore = 5f,
                        SpacingAfter = 10f
                    };
                    summaryTable.SetWidths(new float[] { 1.5f, 1f, 1f, 1f, 1f });
                    // Add extra left/right padding
                    summaryTable.DefaultCell.PaddingLeft = 10f;
                    summaryTable.DefaultCell.PaddingRight = 10f;
                    summaryTable.HorizontalAlignment = Element.ALIGN_CENTER;

                    string[] headers = { "Metric", "Min", "Max", "Avg", "Range" };
                    foreach (string h in headers)
                        summaryTable.AddCell(new PdfPCell(new Phrase(h)) { BackgroundColor = BaseColor.LIGHT_GRAY });

                    void AddRow(string names, double min, double max, double avg)
                    {
                        summaryTable.AddCell(names);
                        summaryTable.AddCell(min.ToString("0.00"));
                        summaryTable.AddCell(max.ToString("0.00"));
                        summaryTable.AddCell(avg.ToString("0.00"));
                        summaryTable.AddCell((max - min).ToString("0.00"));
                    }

                    AddRow("Temperature (°C)", minTemp, maxTemp, avgTemp);
                    AddRow("Humidity (%)", minHum, maxHum, avgHum);
                    AddRow("CO₂ (ppm)", minCO2, maxCO2, avgCO2);
                    AddRow("Ethylene (ppm)", minEth, maxEth, avgEth);

                    doc.Add(summaryTable);
                }
                // === Bar Chart (Min/Max/Avg from SummaryDt) ===

                // === Line Chart (Hourly or 4-Hour) ===
                if (lineDt.Rows.Count > 0)
                {
                    var chart = new System.Web.UI.DataVisualization.Charting.Chart();
                    chart.Width =750;
                    chart.Height = 300;
                    chart.BackColor = System.Drawing.Color.White;

                    // Chart Area styling
                    ChartArea chartArea = new ChartArea();
                    chartArea.AxisX.Title = "Date";
                    chartArea.AxisY.Title = "Sensor Values";
                    chartArea.AxisX.LabelStyle.Angle = -45;
                    chartArea.AxisX.LabelStyle.IsStaggered = true;
                    //chartArea.Area3DStyle.Enable3D = true;
                    chartArea.AxisX.IntervalAutoMode = IntervalAutoMode.VariableCount;
                    chartArea.BackColor = System.Drawing.Color.White;
                    chartArea.AxisX.MajorGrid.Enabled = false; // Remove vertical grid lines
                    chartArea.AxisY.MajorGrid.LineColor = System.Drawing.Color.LightGray; // Subtle horizontal grid
                    chartArea.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
                    chartArea.BorderWidth = 0;
                    chartArea.BorderColor = System.Drawing.Color.Transparent;
                    chartArea.AxisX.LineColor = System.Drawing.Color.Black;
                    chartArea.AxisY.LineColor = System.Drawing.Color.Black;


                    chart.ChartAreas.Add(chartArea);
                    chart.Titles.Add(new Title("Performance",
                    Docking.Top,
                    new System.Drawing.Font("Arial", 12, System.Drawing.FontStyle.Bold),
                    System.Drawing.Color.Black));

                    // Define 4 smooth styled series
                    Series sTemp = new Series("Temperature")
                    {
                        ChartType = SeriesChartType.Spline,
                        BorderWidth = 3,
                        Color = System.Drawing.Color.FromArgb(220, 53, 69), // Red-ish
                        MarkerStyle = MarkerStyle.Circle,
                        MarkerSize = 6,
                        XValueType = ChartValueType.DateTime
                    };

                    Series sHum = new Series("Humidity")
                    {
                        ChartType = SeriesChartType.Spline,
                        BorderWidth = 3,
                        Color = System.Drawing.Color.FromArgb(0, 123, 255), // Blue
                        MarkerStyle = MarkerStyle.Circle,
                        MarkerSize = 6,
                        XValueType = ChartValueType.DateTime
                    };

                    Series sCO2 = new Series("CO₂")
                    {
                        ChartType = SeriesChartType.Spline,
                        BorderWidth = 3,
                        Color = System.Drawing.Color.FromArgb(40, 167, 69), // Green
                        MarkerStyle = MarkerStyle.Circle,
                        MarkerSize = 6,
                        XValueType = ChartValueType.DateTime
                    };

                    Series sEth = new Series("Ethylene")
                    {
                        ChartType = SeriesChartType.Spline,
                        BorderWidth = 3,
                        Color = System.Drawing.Color.FromArgb(255, 193, 7), // Yellow
                        MarkerStyle = MarkerStyle.Circle,
                        MarkerSize = 6,
                        XValueType = ChartValueType.DateTime
                    };

                    // Bind data from lineDt (same logic as before)
                    foreach (DataRow r in lineDt.Rows)
                    {
                       
                        DateTime t = Convert.ToDateTime(r["timeblock"]);
                        sTemp.Points.AddXY(t, Convert.ToDouble(r["avg_temp"]));
                        sHum.Points.AddXY(t, Convert.ToDouble(r["avg_hum"]));
                        sCO2.Points.AddXY(t, Convert.ToDouble(r["avg_co2"]));
                        sEth.Points.AddXY(t, Convert.ToDouble(r["avg_eth"]));
                    }

                    chart.Series.Add(sTemp);
                    chart.Series.Add(sHum);
                    chart.Series.Add(sCO2);
                    chart.Series.Add(sEth);

                    DateTime minDate = lineDt.AsEnumerable().Min(r => Convert.ToDateTime(r["timeblock"].ToString()));
                    DateTime maxDate = lineDt.AsEnumerable().Max(r => Convert.ToDateTime(r["timeblock"].ToString()));

                    DateTime now = DateTime.Now;

                    // Extend maximum axis to "now" if current time is later
                    if (now > maxDate)
                        maxDate = now;

                    chartArea.AxisX.Minimum = minDate.ToOADate();
                    chartArea.AxisX.Maximum = maxDate.ToOADate();

                    // Always show date + time
                    if (minDate.Date == maxDate.Date)
                    {
                        chartArea.AxisX.IntervalType = DateTimeIntervalType.Hours;
                        chartArea.AxisX.Interval = 2;
                    }
                    else
                    {
                        chartArea.AxisX.IntervalType = DateTimeIntervalType.Hours;
                        chartArea.AxisX.Interval = 4;
                    }

                    // Force date + time in labels
                    chartArea.AxisX.LabelStyle.Format = "dd-MMM HH:mm";

                    // Legend styling
                    Legend legends = new Legend("Sensors");
                    legends.Docking = Docking.Top;
                    legends.Font = new System.Drawing.Font("Arial", 9, System.Drawing.FontStyle.Bold);
                    legends.BackColor = System.Drawing.Color.Transparent;
                    legends.BorderColor = System.Drawing.Color.Transparent;
                    chart.Legends.Add(legends);

                    // Save and add to PDF
                    using (var msChart = new MemoryStream())
                    {
                        chart.SaveImage(msChart, ChartImageFormat.Png);
                        msChart.Position = 0;
                        var img = iTextSharp.text.Image.GetInstance(msChart.ToArray());
                        img.Alignment = Element.ALIGN_CENTER;
                        img.ScaleToFit(500f, 200f);
                        doc.Add(img);
                    }
                }

                if (summaryDt.Rows.Count > 0)
                {
                    var row = summaryDt.Rows[0];

                    // Get values from summaryDt
                    double minTemp = Convert.ToDouble(row["min_temperature"]);
                    double maxTemp = Convert.ToDouble(row["max_temperature"]);
                    double avgTemp = Convert.ToDouble(row["avg_temperature"]);

                    double minHum = Convert.ToDouble(row["min_humidity"]);
                    double maxHum = Convert.ToDouble(row["max_humidity"]);
                    double avgHum = Convert.ToDouble(row["avg_humidity"]);

                    double minCO2 = Convert.ToDouble(row["min_co2"]);
                    double maxCO2 = Convert.ToDouble(row["max_co2"]);
                    double avgCO2 = Convert.ToDouble(row["avg_co2"]);

                    double minEth = Convert.ToDouble(row["min_ethylene"]);
                    double maxEth = Convert.ToDouble(row["max_ethylene"]);
                    double avgEth = Convert.ToDouble(row["avg_ethylene"]);

                    // Create chart
                    var barChart = new System.Web.UI.DataVisualization.Charting.Chart();
                    barChart.Width = 700;
                    barChart.Height = 220;
                    barChart.BackColor = System.Drawing.Color.White;

                    ChartArea barArea = new ChartArea();
                    barArea.AxisX.Interval = 1;
                    barArea.AxisX.MajorGrid.Enabled = false;
                   
                    barArea.AxisY.MajorGrid.LineColor = System.Drawing.Color.LightGray;
                    barArea.AxisX.Title = "Sensors";
                    barArea.AxisY.Title = "Values";
                    barArea.AxisX.LabelStyle.Angle = -20;
                    barChart.ChartAreas.Add(barArea);
                    barChart.Titles.Add(new Title("Alert Type Distribution",
                    Docking.Top,
                    new System.Drawing.Font("Arial", 12, System.Drawing.FontStyle.Bold),
                    System.Drawing.Color.Black));

                    Legend legend = new Legend();
                    legend.Docking = Docking.Bottom;
                    legend.Alignment = StringAlignment.Center;
                    legend.Font = new System.Drawing.Font("Arial", 9f, System.Drawing.FontStyle.Bold);
                    barChart.Legends.Add(legend);

                    // Series
                    Series lowSeries = new Series("Min")
                    {
                        ChartType = SeriesChartType.Column,
                        Color = System.Drawing.Color.FromArgb(150, 79, 70, 229) // light blue
                    };

                    Series highSeries = new Series("Max")
                    {
                        ChartType = SeriesChartType.Column,
                        Color = System.Drawing.Color.FromArgb(150, 220, 38, 38) // light red
                    };

                    Series avgSeries = new Series("Avg")
                    {
                        ChartType = SeriesChartType.Column,
                        Color = System.Drawing.Color.FromArgb(150, 34, 197, 94) // light green
                    };

                    // Add points using summary data
                    lowSeries.Points.AddXY("Temperature", minTemp);
                    avgSeries.Points.AddXY("Temperature", avgTemp);
                    highSeries.Points.AddXY("Temperature", maxTemp);

                    lowSeries.Points.AddXY("Humidity", minHum);
                    avgSeries.Points.AddXY("Humidity", avgHum);
                    highSeries.Points.AddXY("Humidity", maxHum);

                    lowSeries.Points.AddXY("CO₂", minCO2);
                    avgSeries.Points.AddXY("CO₂", avgCO2);
                    highSeries.Points.AddXY("CO₂", maxCO2);

                    lowSeries.Points.AddXY("Ethylene", minEth);
                    avgSeries.Points.AddXY("Ethylene", avgEth);
                    highSeries.Points.AddXY("Ethylene", maxEth);

                    barChart.Series.Add(lowSeries);
                    barChart.Series.Add(avgSeries);
                    barChart.Series.Add(highSeries);

                    // Consistent width
                    lowSeries["PointWidth"] = "0.3";
                    avgSeries["PointWidth"] = "0.3";
                    highSeries["PointWidth"] = "0.3";

                    // Save and add to PDF
                    using (var barStream = new MemoryStream())
                    {
                        barChart.SaveImage(barStream, ChartImageFormat.Png);
                        barStream.Position = 0;
                        var barImage = iTextSharp.text.Image.GetInstance(barStream.ToArray());
                        barImage.Alignment = Element.ALIGN_CENTER;
                        barImage.ScaleToFit(500f, 250f);
                        doc.Add(barImage);
                    }
                }

                // === Pie Chart ===

                if (severityDt.Rows.Count > 0)
                {
                    var pieChart = new System.Web.UI.DataVisualization.Charting.Chart { Width = 450, Height = 240 };
                    pieChart.BackColor = System.Drawing.Color.White;

                    ChartArea chartArea = new ChartArea();
                    chartArea.Area3DStyle.Enable3D = true;
                    pieChart.ChartAreas.Add(chartArea);
                    pieChart.Titles.Add(new Title("Alert Severity Breakdown",
                        Docking.Top,
                        new System.Drawing.Font("Arial", 12, System.Drawing.FontStyle.Bold),
                        System.Drawing.Color.Black));

                    // Legend at bottom center
                    Legend legend = new Legend
                    {
                        Docking = Docking.Bottom,
                        Alignment = System.Drawing.StringAlignment.Center
                    };
                    pieChart.Legends.Add(legend);

                    Series pie = new Series
                    {
                        ChartType = SeriesChartType.Pie,
                        IsValueShownAsLabel = true,
                        Label = "#VALX: #PERCENT{P0}",
                        LegendText = "#VALX",
                        ["PieLabelStyle"] = "Inside",
                        ["PieLineColor"] = "Black"
                    };

                    // Define your custom colors
                    var colors = new List<System.Drawing.Color>
{
    System.Drawing.ColorTranslator.FromHtml("#4F46E5"),
    System.Drawing.ColorTranslator.FromHtml("#60A5FA"),
    System.Drawing.ColorTranslator.FromHtml("#A78BFA")
};

                    int colorIndex = 0;

                    foreach (DataRow dr in severityDt.Rows)
                    {
                        double percentage = Convert.ToDouble(dr["percentage"]);
                        if (percentage > 0)  // skip 0% slices
                        {
                            int pointIndex = pie.Points.AddXY(dr["Severity"].ToString(), percentage);
                            pie.Points[pointIndex].Color = colors[colorIndex % colors.Count]; // assign custom color
                            colorIndex++;
                        }
                    }

                    pieChart.Series.Add(pie);

                    using (var msPie = new MemoryStream())
                    {
                        pieChart.SaveImage(msPie, ChartImageFormat.Png);
                        msPie.Position = 0;
                        var img = iTextSharp.text.Image.GetInstance(msPie.ToArray());

                        // Scale and center the chart
                        img.ScaleToFit(350f, 180f);
                        img.Alignment = iTextSharp.text.Element.ALIGN_CENTER;

                        doc.Add(img);
                    }
                }



                doc.Close();
                var fileBytes = ms.ToArray();
                var fileName = $"ColdStorage_Summary_{DateTime.Now:yyyyMMdd_HHmm}.pdf";

                Response.Clear();
                Response.ContentType = "application/pdf";
                Response.AddHeader("Content-Disposition", download
                    ? $"attachment; filename={fileName}"
                    : $"inline; filename={fileName}");
                Response.OutputStream.Write(fileBytes, 0, fileBytes.Length);
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
                    // Populate severity dropdown
                    var allSeverities = new List<string>();
                    using (var cmd = new MySqlCommand("SELECT DISTINCT TRIM(Severity) as Severity FROM Alerts ORDER BY Severity", conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var severityValue = reader["Severity"]?.ToString();
                                if (!string.IsNullOrWhiteSpace(severityValue))   // ✅ skip null/empty/whitespace
                                {
                                    allSeverities.Add(severityValue);
                                }
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