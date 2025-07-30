using Cold_Storage_Unit.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using MySql.Data.MySqlClient;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web.Hosting;
using System.Web.Mvc;

namespace Cold_Storage_Unit.Controllers
{
    public class ReportsController : Controller
    {
        //Datatable for ColdstorageUnit2

        public ActionResult Index(
       string name = null, string startDate = null, string endDate = null,
       string status = null, string startDate1 = null, string endDate1 = null)

        {
            var coldStorageData = GetFilteredColdStorageData(name, startDate, endDate);
            var doorStatusData = GetFilteredDoorStatusData(status, startDate1, endDate1);

            ViewBag.SelectedName = name;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;

            ViewBag.Selectedstatus = status;
            ViewBag.StartDate1 = startDate1;
            ViewBag.EndDate1 = endDate1;

            //for Door Status

            var viewModel = new ReportsViewModel
            {
                ColdStorageData = coldStorageData,
                DoorStatusData = doorStatusData
            };

            return View(viewModel); // pass both data sets in one model
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
                if (!string.IsNullOrEmpty(startDate))
                    filterQuery += " AND STR_TO_DATE(SUBSTRING_INDEX(Hardwaredate, ',', 1), '%d/%m/%Y') >= STR_TO_DATE(@startDate, '%Y-%m-%d')";
                if (!string.IsNullOrEmpty(endDate))
                    filterQuery += " AND STR_TO_DATE(SUBSTRING_INDEX(Hardwaredate, ',', 1), '%d/%m/%Y') < DATE_ADD(STR_TO_DATE(@endDate, '%Y-%m-%d'), INTERVAL 1 DAY)";

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

                Rectangle border = new Rectangle(
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
                logoCell.Border = Rectangle.NO_BORDER;
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
                    Border = Rectangle.NO_BORDER,
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
                    Border = Rectangle.NO_BORDER,
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
                footer.DefaultCell.Border = Rectangle.NO_BORDER;
                footer.DefaultCell.HorizontalAlignment = Element.ALIGN_CENTER;

                PdfPCell footerCell = new PdfPCell
                {
                    Border = Rectangle.NO_BORDER,
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
            ISheet sheet = workbook.CreateSheet("ColdStorageReport");

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
            IRow row0 = sheet.CreateRow(0);
            row0.HeightInPoints = 80; // Reduced height

            // Insert image in (0,0) to (1,1) and scale it
            string imagePath = HostingEnvironment.MapPath("~/Images/logo.jpg");
            if (System.IO.File.Exists(imagePath))
            {
                byte[] imageBytes = System.IO.File.ReadAllBytes(imagePath);
                int pictureIdx = workbook.AddPicture(imageBytes, PictureType.PNG);

                IDrawing drawing = sheet.CreateDrawingPatriarch();
                IClientAnchor anchor = workbook.GetCreationHelper().CreateClientAnchor();

                // Place image from (0,0) to (1,1)
                anchor.Col1 = 0;
                anchor.Row1 = 1;
                anchor.Col2 = 2;
                anchor.Row2 = 3;

                var picture = drawing.CreatePicture(anchor, pictureIdx);
                picture.Resize(1.0); // Resize to 50% (both width and height)
            }

            // Title Cell
            ICell titleCell = row0.CreateCell(2);
            titleCell.SetCellValue("Banana Cold Storage");
            titleCell.CellStyle = titleStyle;
            sheet.AddMergedRegion(new CellRangeAddress(0, 0, 2, 7));

            // Start Date
            row0.CreateCell(8).SetCellValue("Start Date:");
            row0.CreateCell(9).SetCellValue(startDate + " 00:00:00");

            // --- Row 1: Subtitle + End Date ---
            IRow row1 = sheet.CreateRow(1);
            ICell subCell = row1.CreateCell(2);
            subCell.SetCellValue($"Summary Report for {(string.IsNullOrEmpty(name) ? "All Units" : name)}");
            subCell.CellStyle = subStyle;
            sheet.AddMergedRegion(new CellRangeAddress(1, 1, 2, 7));

            // Merge cells for visual spacing if needed (adjust columns as per design)
            sheet.AddMergedRegion(new CellRangeAddress(2, 2, 2, 7));

            row1.CreateCell(8).SetCellValue("End Date:");
            row1.CreateCell(9).SetCellValue(endDate + " 23:59:59");

            // --- Row 2: Generated Timestamp ---
            IRow row2 = sheet.CreateRow(2);
            row2.CreateCell(8).SetCellValue("Generated:");
            row2.CreateCell(9).SetCellValue(DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"));

            // --- Row 3: Horizontal Divider ---
            IRow dividerRow = sheet.CreateRow(3);
            for (int i = 0; i <= 11; i++)
            {
                ICell cell = dividerRow.CreateCell(i);
                ICellStyle borderStyle = workbook.CreateCellStyle();
                borderStyle.BorderBottom = BorderStyle.Medium;
                cell.CellStyle = borderStyle;
            }

            if (data.Any())
            {
                double avgTemp = data.Average(x => x.Temperature);
                double minTemp = data.Min(x => x.Temperature);
                double maxTemp = data.Max(x => x.Temperature);

                double avgHum = data.Average(x => x.Humidity);
                double minHum = data.Min(x => x.Humidity);
                double maxHum = data.Max(x => x.Humidity);

                double avgEth = data.Average(x => x.EthyleneLevel);
                double minEth = data.Min(x => x.EthyleneLevel);
                double maxEth = data.Max(x => x.EthyleneLevel);

                double avgCO2 = data.Average(x => x.Co2Level);
                double minCO2 = data.Min(x => x.Co2Level);
                double maxCO2 = data.Max(x => x.Co2Level);

                // Create bold cell style
                ICellStyle boldStyle = workbook.CreateCellStyle();
                IFont boldFont = workbook.CreateFont();
                boldFont.IsBold = true;
                boldStyle.SetFont(boldFont);
                boldStyle.Alignment = HorizontalAlignment.Center;

                // Add header row
                IRow metricHeader = sheet.CreateRow(4);
                metricHeader.CreateCell(1).SetCellValue("Metric");
                metricHeader.CreateCell(2).SetCellValue("Temperature (°C)");
                metricHeader.CreateCell(3).SetCellValue("Humidity (%)");
                metricHeader.CreateCell(4).SetCellValue("CO₂ (ppm)");
                metricHeader.CreateCell(5).SetCellValue("Ethylene (ppm)");
                for (int i = 1; i <= 5; i++) metricHeader.GetCell(i).CellStyle = boldStyle;

                // Average row
                IRow avgRow = sheet.CreateRow(5);
                avgRow.CreateCell(1).SetCellValue("Average");
                avgRow.CreateCell(2).SetCellValue(Math.Round(avgTemp, 1));
                avgRow.CreateCell(3).SetCellValue(Math.Round(avgHum, 1));
                avgRow.CreateCell(4).SetCellValue(Math.Round(avgCO2, 1));
                avgRow.CreateCell(5).SetCellValue(Math.Round(avgEth, 1));

                // Min row
                IRow minRow = sheet.CreateRow(6);
                minRow.CreateCell(1).SetCellValue("Minimum");
                minRow.CreateCell(2).SetCellValue(Math.Round(minTemp, 1));
                minRow.CreateCell(3).SetCellValue(Math.Round(minHum, 1));
                minRow.CreateCell(4).SetCellValue(Math.Round(minCO2, 1));
                minRow.CreateCell(5).SetCellValue(Math.Round(minEth, 1));

                // Max row
                IRow maxRow = sheet.CreateRow(7);
                maxRow.CreateCell(1).SetCellValue("Maximum");
                maxRow.CreateCell(2).SetCellValue(Math.Round(maxTemp, 1));
                maxRow.CreateCell(3).SetCellValue(Math.Round(maxHum, 1));
                maxRow.CreateCell(4).SetCellValue(Math.Round(maxCO2, 1));
                maxRow.CreateCell(5).SetCellValue(Math.Round(maxEth, 1));
            }
            // --- Header Row (Row 3) ---

            bool isAll = string.IsNullOrEmpty(name) || name.Trim().ToLower() == "all";

            // Define headers conditionally
            string[] headers = isAll
                ? new[] { "S.No", "Unit Name", "Temperature (°C)", "Humidity (%)", "Power Status", "Door Status", "CO₂ Level (ppm)", "Ethylene Level (ppm)", "Fan Speed", "DateTime" }
                : new[] { "S.No", "Temperature (°C)", "Humidity (%)", "Power Status", "Door Status", "CO₂ Level (ppm)", "Ethylene Level (ppm)", "Fan Speed", "DateTime" };

            // Header row
            IRow headerRow = sheet.CreateRow(8);
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



            // --- Data Rows (Start from Row 4) ---
            int rowIndex = 9;
            int serialNo = 1;

            foreach (var row in data)
            {
                IRow sheetRow = sheet.CreateRow(rowIndex++);
                int colIndex = 0;

                sheetRow.CreateCell(colIndex).SetCellValue(serialNo++);
                colIndex++;

                if (isAll)
                {
                    sheetRow.CreateCell(colIndex).SetCellValue(row.Name);
                    colIndex++;
                }

                sheetRow.CreateCell(colIndex++).SetCellValue((double)row.Temperature);
                sheetRow.CreateCell(colIndex++).SetCellValue((double)row.Humidity);
                sheetRow.CreateCell(colIndex++).SetCellValue(row.PowerStatus);
                sheetRow.CreateCell(colIndex++).SetCellValue(row.DoorStatus);
                sheetRow.CreateCell(colIndex++).SetCellValue((double)row.Co2Level);
                sheetRow.CreateCell(colIndex++).SetCellValue((double)row.EthyleneLevel);
                sheetRow.CreateCell(colIndex++).SetCellValue(row.FanSpeed);
                sheetRow.CreateCell(colIndex).SetCellValue(row.Hardwaredate);

                for (int i = 0; i <= colIndex; i++)
                {
                    sheetRow.GetCell(i).CellStyle = centerStyle;
                }
            }
            if (isAll)
            {
                var units = data.Select(d => d.Name).Distinct();

                // Define a reusable header style for unit sheets
                ICellStyle unitHeaderStyle = workbook.CreateCellStyle();
                IFont unitHeaderFont = workbook.CreateFont();
                unitHeaderFont.IsBold = true;
                unitHeaderStyle.SetFont(unitHeaderFont);
                unitHeaderStyle.Alignment = HorizontalAlignment.Center;
                unitHeaderStyle.VerticalAlignment = VerticalAlignment.Center;
                unitHeaderStyle.FillForegroundColor = IndexedColors.Grey25Percent.Index;
                unitHeaderStyle.FillPattern = FillPattern.SolidForeground;

                foreach (var unit in units)
                {
                    var unitData = data.Where(d => d.Name == unit).ToList();
                    ISheet unitSheet = workbook.CreateSheet(unit);

                    // Create header row
                    IRow header = unitSheet.CreateRow(0);
                    string[] unitHeaders = { "S.No", "Temperature (°C)", "Humidity (%)", "Power Status", "Door Status", "CO₂ Level (ppm)", "Ethylene Level (ppm)", "Fan Speed", "DateTime" };

                    for (int i = 0; i < unitHeaders.Length; i++)
                    {
                        ICell cell = header.CreateCell(i);
                        cell.SetCellValue(unitHeaders[i]);
                        cell.CellStyle = unitHeaderStyle;  // ✅ Apply new header style
                    }

                    int unitRowIdx = 1;
                    int sn = 1;
                    // Create numeric format style for 1 decimal
                    ICellStyle oneDecimalStyle = workbook.CreateCellStyle();
                    oneDecimalStyle.CloneStyleFrom(centerStyle); // inherit center alignment etc.
                    IDataFormat dataFormatCustom = workbook.CreateDataFormat();
                    oneDecimalStyle.DataFormat = dataFormatCustom.GetFormat("0.0");

                    // Ethylene should keep default (full) precision
                    ICellStyle ethyleneStyle = workbook.CreateCellStyle();
                    ethyleneStyle.CloneStyleFrom(centerStyle);
                    ethyleneStyle.DataFormat = dataFormatCustom.GetFormat("0.00"); // or leave as is

                    foreach (var item in unitData)
                    {
                        IRow r = unitSheet.CreateRow(unitRowIdx++);
                        r.CreateCell(0).SetCellValue(sn++);
                        r.GetCell(0).CellStyle = centerStyle;

                        r.CreateCell(1).SetCellValue(Math.Round((double)item.Temperature, 1)); // 1 decimal
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


                    // (Optional) Auto-size columns for better display
                    for (int col = 0; col < unitHeaders.Length; col++)
                        unitSheet.AutoSizeColumn(col);
                }
            }


            // --- Final Output ---
            using (var stream = new MemoryStream())
            {
                workbook.Write(stream);
                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ColdStorage_Summary.xlsx");
            }
        }

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
                if (!string.IsNullOrEmpty(startDate1))
                    filterQuery += " AND STR_TO_DATE(SUBSTRING_INDEX(Hardwaredate, ',', 1), '%d/%m/%Y') >= STR_TO_DATE(@startDate, '%Y-%m-%d')";
                if (!string.IsNullOrEmpty(endDate1))
                    filterQuery += " AND STR_TO_DATE(SUBSTRING_INDEX(Hardwaredate, ',', 1), '%d/%m/%Y') < DATE_ADD(STR_TO_DATE(@endDate, '%Y-%m-%d'), INTERVAL 1 DAY)";

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
            IRow row0 = sheet.CreateRow(0);
            row0.HeightInPoints = 70; // Reduced height

            // Insert image in (0,0) to (1,1) and scale it
            string imagePath = HostingEnvironment.MapPath("~/Images/logo.jpg");
            if (System.IO.File.Exists(imagePath))
            {
                byte[] imageBytes = System.IO.File.ReadAllBytes(imagePath);
                int pictureIdx = workbook.AddPicture(imageBytes, PictureType.PNG);

                IDrawing drawing = sheet.CreateDrawingPatriarch();
                IClientAnchor anchor = workbook.GetCreationHelper().CreateClientAnchor();

                // Place image from (0,0) to (1,1)
                anchor.Col1 = 0;
                anchor.Row1 = 1;
                anchor.Col2 = 2;
                anchor.Row2 = 2;

                var picture = drawing.CreatePicture(anchor, pictureIdx);
                picture.Resize(1.4); // Resize to 90% (both width and height)
            }


            // Title Cell
            ICell titleCell = row0.CreateCell(2);
            titleCell.SetCellValue("Banana Cold Storage");
            titleCell.CellStyle = titleStyle;
            sheet.AddMergedRegion(new CellRangeAddress(0, 0, 2, 7));

            // Start Date
            row0.CreateCell(8).SetCellValue("Start Date:");
            row0.CreateCell(9).SetCellValue(startDate + " 00:00:00");

            // --- Row 1: Subtitle + End Date ---
            IRow row1 = sheet.CreateRow(1);
            ICell subCell = row1.CreateCell(2);
            subCell.SetCellValue($"Summary Report for {(string.IsNullOrEmpty(status) ? "All Units" : status)}");
            subCell.CellStyle = subStyle;
            sheet.AddMergedRegion(new CellRangeAddress(1, 1, 2, 7));


            // Merge cells for visual spacing if needed (adjust columns as per design)
            sheet.AddMergedRegion(new CellRangeAddress(2, 2, 2, 7));

            row1.CreateCell(8).SetCellValue("End Date:");
            row1.CreateCell(9).SetCellValue(endDate + " 23:59:59");

            // --- Row 2: Generated Timestamp ---
            IRow row2 = sheet.CreateRow(2);
            row2.CreateCell(8).SetCellValue("Generated:");
            row2.CreateCell(9).SetCellValue(DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"));

            // --- Row 3: Horizontal Divider ---
            IRow dividerRow = sheet.CreateRow(4);
            for (int i = 0; i <= 11; i++)
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

    }
}