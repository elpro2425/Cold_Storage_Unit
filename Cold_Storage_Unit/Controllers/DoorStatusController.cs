using Cold_Storage_Unit.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using MySql.Data.MySqlClient;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Cold_Storage_Unit.Controllers
{
    public class DoorStatusController : Controller
    {
        // GET: DoorStatus
        //public ActionResult Index()
        //{
        //        DoorStatus latest = null;

        //        string connStr = ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString;

        //        using (var conn = new MySqlConnection(connStr))
        //        {
        //            conn.Open();

        //            string query = "SELECT * FROM door_status ORDER BY Actualdate DESC LIMIT 1";

        //            using (var cmd = new MySqlCommand(query, conn))
        //            {
        //                using (var reader = cmd.ExecuteReader())
        //                {
        //                    if (reader.Read())
        //                    {
        //                        latest = new DoorStatus
        //                        {
        //                            Id = Convert.ToInt32(reader["Id"]),
        //                            Unitname = reader["Unitname"].ToString(),
        //                            Status = reader["Status"].ToString(),
        //                            Hardwaredate = reader["Hardwaredate"].ToString()
        //                        };
        //                    }
        //                }
        //            }
        //        }
        //        return View(latest);
        //}

        public ActionResult Index(int? lineNo, string startDate, string endDate)
        {
            ViewBag.SelectedLineNo = lineNo;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;

            var filteredData = GetFilteredWeightData(lineNo, startDate, endDate);

            return View(filteredData);
        }


        private List<WeightRecord> GetFilteredWeightData(int? lineNo, string startDate, string endDate)
        {
            var data = new List<WeightRecord>
         {
             // Your static data
             new WeightRecord { SrNo = 1, LineNo = 101, Weight = 47.5, Condition = "Out of Limits", DateTime = "2025-08-11 10:00:00" },
             new WeightRecord { SrNo = 2, LineNo = 102, Weight = 48.0, Condition = "Within Limits", DateTime = "2025-08-11 10:10:00" },
             new WeightRecord { SrNo = 3, LineNo = 103, Weight = 49.3, Condition = "Within Limits", DateTime = "2025-08-11 10:20:00" },
             new WeightRecord { SrNo = 4, LineNo = 104, Weight = 50.0, Condition = "Within Limits", DateTime = "2025-08-11 10:30:00" },
             new WeightRecord { SrNo = 5, LineNo = 105, Weight = 50.5, Condition = "Out of Limits", DateTime = "2025-08-11 10:40:00" },
             new WeightRecord { SrNo = 6, LineNo = 106, Weight = 48.7, Condition = "Within Limits", DateTime = "2025-08-11 10:50:00" },
             new WeightRecord { SrNo = 7, LineNo = 107, Weight = 46.9, Condition = "Out of Limits", DateTime = "2025-08-11 11:00:00" },
             new WeightRecord { SrNo = 8, LineNo = 108, Weight = 49.9, Condition = "Within Limits", DateTime = "2025-08-11 11:10:00" },
             new WeightRecord { SrNo = 9, LineNo = 109, Weight = 51.2, Condition = "Out of Limits", DateTime = "2025-08-11 11:20:00" },
             new WeightRecord { SrNo = 10, LineNo = 110, Weight = 48.4, Condition = "Within Limits", DateTime = "2025-08-11 11:30:00" }
         };

            DateTime? start = null;
            DateTime? end = null;

            if (DateTime.TryParse(startDate, out var parsedStart))
                start = parsedStart.Date;

            if (DateTime.TryParse(endDate, out var parsedEnd))
                end = parsedEnd.Date.AddDays(1).AddTicks(-1); // end of day

            var filteredData = data.Where(d =>
                (!lineNo.HasValue || d.LineNo == lineNo.Value) &&
                (!start.HasValue || DateTime.Parse(d.DateTime) >= start.Value) &&
                (!end.HasValue || DateTime.Parse(d.DateTime) <= end.Value)
            ).OrderByDescending(d => DateTime.Parse(d.DateTime)).ToList();

            return filteredData;
        }

        public FileResult GeneratePdfSummaryReport(int? lineNo, string startDate, string endDate)
        {
            // 1. Prepare your static data list
            var data = new List<WeightRecord>
         {
             new WeightRecord { SrNo = 1, LineNo = 101, Weight = 47.5, Condition = "Out of Limits", DateTime = "2025-08-11 10:00:00" },
             new WeightRecord { SrNo = 2, LineNo = 102, Weight = 48.0, Condition = "Within Limits", DateTime = "2025-08-11 10:10:00" },
             new WeightRecord { SrNo = 3, LineNo = 103, Weight = 49.3, Condition = "Within Limits", DateTime = "2025-08-11 10:20:00" },
             new WeightRecord { SrNo = 4, LineNo = 104, Weight = 50.0, Condition = "Within Limits", DateTime = "2025-08-11 10:30:00" },
             new WeightRecord { SrNo = 5, LineNo = 105, Weight = 50.5, Condition = "Out of Limits", DateTime = "2025-08-11 10:40:00" },
             new WeightRecord { SrNo = 6, LineNo = 106, Weight = 48.7, Condition = "Within Limits", DateTime = "2025-08-11 10:50:00" },
             new WeightRecord { SrNo = 7, LineNo = 107, Weight = 46.9, Condition = "Out of Limits", DateTime = "2025-08-11 11:00:00" },
             new WeightRecord { SrNo = 8, LineNo = 108, Weight = 49.9, Condition = "Within Limits", DateTime = "2025-08-11 11:10:00" },
             new WeightRecord { SrNo = 9, LineNo = 109, Weight = 51.2, Condition = "Out of Limits", DateTime = "2025-08-11 11:20:00" },
             new WeightRecord { SrNo = 10, LineNo = 110,Weight = 48.4, Condition = "Within Limits", DateTime = "2025-08-11 11:30:00" }
         };

            // 2. Filter the data based on parameters
            DateTime? start = null;
            DateTime? end = null;

            if (DateTime.TryParse(startDate, out var parsedStart))
                start = parsedStart.Date;

            if (DateTime.TryParse(endDate, out var parsedEnd))
                end = parsedEnd.Date.AddDays(1).AddTicks(-1);

            var filteredData = data.Where(d =>
                (!lineNo.HasValue || d.LineNo == lineNo.Value) &&
                (!start.HasValue || DateTime.Parse(d.DateTime) >= start.Value) &&
                (!end.HasValue || DateTime.Parse(d.DateTime) <= end.Value)
            ).OrderByDescending(d => DateTime.Parse(d.DateTime)).ToList();

            // 3. Create PDF
            using (var ms = new MemoryStream())
            {
                Document doc = new Document(PageSize.A4, 20f, 20f, 100f, 110f);
                var writer = PdfWriter.GetInstance(doc, ms);

                // Optional: add header/footer if you have one, e.g.
                string logoPath = Server.MapPath("~/Images/logo.jpg");
           
                 writer.PageEvent = new PdfFooter(logoPath, startDate, endDate);

                doc.Open();

                // Fonts
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11);
                var cellFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);


                // Summary metrics if data exists
                if (filteredData.Any())
                {
                    // Calculate summary values
                    int totalCount = filteredData.Count;
                    int lowerQty = filteredData.Count(x => x.Weight < 48);
                    int higherQty = filteredData.Count(x => x.Weight > 50);

                    // Sum of (weight - 50) for weights > 50
                    double totalExcessWeight = filteredData
                        .Where(x => x.Weight > 50)
                        .Sum(x => x.Weight - 50);

                    var summaryTitle = new Paragraph("Summary Metrics", titleFont)
                    {
                        Alignment = Element.ALIGN_CENTER
                    };

                    doc.Add(summaryTitle);

                    PdfPTable summaryTable = new PdfPTable(4)
                    {
                        WidthPercentage = 70f,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        SpacingBefore = 10f,
                        SpacingAfter = 20f
                    };
                    summaryTable.SetWidths(new float[] {2f, 1f, 2f, 2f });

                    // Headers
                    summaryTable.AddCell(new PdfPCell(new Phrase("Metric", headerFont))
                    {
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        Padding = 5,
                        BorderColor = BaseColor.GRAY
                    });
                    summaryTable.AddCell(new PdfPCell(new Phrase("Total", headerFont))
                    {
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        Padding = 5,
                        BorderColor = BaseColor.GRAY
                    });
                    summaryTable.AddCell(new PdfPCell(new Phrase("Lower Qty (<48)", headerFont))
                    {
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        Padding = 5,
                        BorderColor = BaseColor.GRAY
                    });
                    summaryTable.AddCell(new PdfPCell(new Phrase("Higher Qty (>50)", headerFont))
                    {
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        Padding = 5,
                        BorderColor = BaseColor.GRAY
                    });

                    // Data row 1 - Counts
                    summaryTable.AddCell(new PdfPCell(new Phrase("Count", cellFont))
                    {
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        Padding = 5,
                        BorderColor = BaseColor.GRAY
                    });
                    summaryTable.AddCell(new PdfPCell(new Phrase(totalCount.ToString(), cellFont))
                    {
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        Padding = 5,
                        BorderColor = BaseColor.GRAY
                    });
                    summaryTable.AddCell(new PdfPCell(new Phrase(lowerQty.ToString(), cellFont))
                    {
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        Padding = 5,
                        BorderColor = BaseColor.GRAY
                    });
                    summaryTable.AddCell(new PdfPCell(new Phrase(higherQty.ToString(), cellFont))
                    {
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        Padding = 5,
                        BorderColor = BaseColor.GRAY
                    });

                    // Add another row for total excess weight (merged cells)
                    summaryTable.AddCell(new PdfPCell(new Phrase("Total Excess Weight (>50)", headerFont))
                    {
                        Colspan = 3,
                        HorizontalAlignment = Element.ALIGN_RIGHT,
                        Padding = 5,
                        BorderColor = BaseColor.GRAY
                    });

                    summaryTable.AddCell(new PdfPCell(new Phrase(totalExcessWeight.ToString("0.00"), cellFont))
                    {
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        Padding = 5,
                        BorderColor = BaseColor.GRAY
                    });

                    doc.Add(summaryTable);
                }


                doc.Add(new Chunk(" "));
                // Table with 5 columns
                PdfPTable table = new PdfPTable(5)
                {
                    WidthPercentage = 100f
                };
                table.SetWidths(new float[] { 0.6f, 1.0f, 1.0f, 1.2f, 2.0f });
                table.HeaderRows = 1;

                // Headers with yellow background like your example
                string[] headers = { "SrNo", "Line No", "Weight", "Condition", "DateTime" };
                foreach (var h in headers)
                {
                    var headerCell = new PdfPCell(new Phrase(h, headerFont))
                    {
                        BackgroundColor = BaseColor.YELLOW,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = 5
                    };
                    table.AddCell(headerCell);
                }

                // Data rows
                foreach (var r in filteredData)
                {
                    table.AddCell(new PdfPCell(new Phrase(r.SrNo.ToString(), cellFont))
                    {
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        Padding = 5
                    });
                    table.AddCell(new PdfPCell(new Phrase(r.LineNo.ToString(), cellFont))
                    {
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        Padding = 5
                    });
                    table.AddCell(new PdfPCell(new Phrase(r.Weight.ToString("0.00"), cellFont))
                    {
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        Padding = 5
                    });

                    // Condition cell with red text if "Out of Limits"
                    var conditionFont = r.Condition == "Out of Limits"
                        ? FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.RED)
                        : cellFont;

                    table.AddCell(new PdfPCell(new Phrase(r.Condition, conditionFont))
                    {
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        Padding = 5
                    });

                    table.AddCell(new PdfPCell(new Phrase(r.DateTime, cellFont))
                    {
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        Padding = 5
                    });
                }

                doc.Add(table);

                doc.Close();

                return File(ms.ToArray(), "application/pdf", "WeightRecords_Report.pdf");
            }
        }

        // Helper methods for styled PdfPCell creation
        private PdfPCell CreateCenterCell(string text, Font font, BaseColor bgColor = null)
        {
            return new PdfPCell(new Phrase(text, font))
            {
                HorizontalAlignment = Element.ALIGN_CENTER,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                Padding = 5,
                BackgroundColor = bgColor ?? BaseColor.WHITE
            };
        }

        private PdfPCell CreateHeaderCell(string text, Font font)
        {
            return new PdfPCell(new Phrase(text, font))
            {
                BackgroundColor = BaseColor.LIGHT_GRAY,
                HorizontalAlignment = Element.ALIGN_CENTER,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                Padding = 5
            };
        }

        // Helper method for creating styled cells
        private PdfPCell CreateCell(string text, Font font, BaseColor bgColor = null)
        {
            var cell = new PdfPCell(new Phrase(text, font))
            {
                HorizontalAlignment = Element.ALIGN_CENTER,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                Padding = 5
            };
            if (bgColor != null)
                cell.BackgroundColor = bgColor;
            return cell;
        }
        private class PdfFooter : PdfPageEventHelper
        {
            private string logoPath;
            private string selectedStatus;
            private string startDate;
            private string endDate;

            public PdfFooter(string logoPath,  string startDate, string endDate)
            {
                this.logoPath = logoPath;
             
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
                centerPara.Add(new Chunk("Weight Records  Report", titleFont));
                centerPara.Add(new Chunk("\n\n")); // 2-line spacing between title and subtitle
                centerPara.Add(new Chunk($"Summary Report for {(string.IsNullOrEmpty(selectedStatus) ? "Lines" : selectedStatus)}", subtitleFont));

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
    }
}
