using Cold_Storage_Unit.Controllers;
using Quartz;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using SmtpClient = System.Net.Mail.SmtpClient;

namespace Cold_Storage_Unit.Models
{
    public class EmailService : IJob
    {

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                var controller = new ReportsController();

                // 1. Set 24-hour date range
                string name = ""; // all units
                string startDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
                string endDate = DateTime.Now.ToString("yyyy-MM-dd");

                // 2. Get the PDF bytes
                FileContentResult pdfResult = controller.GenerateFullPdf(name, startDate, endDate);
                byte[] pdfBytes = pdfResult.FileContents;

                // 3. Get the Excel bytes
                FileContentResult excelResult = controller.GenerateFullExcel(name, startDate, endDate);
                byte[] excelBytes = excelResult.FileContents;

                // 4. Create and send email with both attachments
                using (MailMessage mail = new MailMessage())
                {
                    mail.From = new MailAddress("sankalpunam.elpro@gmail.com");
                    mail.To.Add("sankalpunam.elpro@gmail.com");
                    mail.To.Add("info@elprosolutions.com");
                    mail.To.Add("operations@elprosolutions.com");

                    string greeting;
                    int hour = DateTime.Now.Hour;
                    if (hour < 12)
                        greeting = "Good Morning";
                    else if (hour < 17)
                        greeting = "Good Afternoon";
                    else
                        greeting = "Good Evening";

                    string reportDate = DateTime.Now.AddDays(-1).ToString("dd MMMM yyyy");

                    mail.Subject = $"Daily  Report - {reportDate}";
                    mail.Body = $@"
               <html>
               <body style='font-family: Arial, sans-serif; font-size: 14px; color: #333;'>
                   <div style='border-bottom: 1px solid #ccc; padding-bottom: 10px; margin-bottom: 20px;'>
                       <img src='https://www.elprosolutions.com/images/logo.png' alt='Company Logo' height='50' />
                   </div>
                   <p>{greeting},</p>

                   <p>Please find attached the <b>Daily Cold Storage Monitoring Report</b> for <b>{reportDate}</b>.</p>

                   <p>The report includes the following data:</p>
                   <ul>
                       <li>Temperature and Humidity Levels</li>
                       <li>Power and Door Status</li>
                       <li>CO₂ and Ethylene Gas Levels</li>
                       <li>Fan Speed and Hardware Time</li>
                   </ul>

                   <p>Best regards,<br/>
                   <b>Cold Storage Monitoring System</b><br/>
                   <a href='https://www.elprosolutions.com'>ELPRO Solutions Pvt. Ltd.</a></p>
               </body>
               </html>";
                    mail.IsBodyHtml = true;

                    // Attach PDF
                    mail.Attachments.Add(new Attachment(new MemoryStream(pdfBytes), "DailyReport.pdf"));

                    // Attach Excel
                    mail.Attachments.Add(new Attachment(new MemoryStream(excelBytes), "DailyReport.xlsx"));

                    using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
                    {
                        smtp.Credentials = new NetworkCredential("sankalpunam.elpro@gmail.com", "ysjfdlhwllfcfwpz"); // app password
                        smtp.EnableSsl = true;
                        await smtp.SendMailAsync(mail);
                    }
                }

                Console.WriteLine("Daily report (PDF + Excel) sent.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending report: " + ex.Message);
            }
        }

        //public async Task Execute(IJobExecutionContext context)
        //{
        //    try
        //    {
        //        var controller = new ReportsController();

        //        // 1. Set 24-hour date range
        //        string name = ""; // all names
        //        string startDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
        //        string endDate = DateTime.Now.ToString("yyyy-MM-dd");

        //        // 2. Get the PDF file as bytes
        //        FileContentResult pdfResult = controller.GenerateFullPdf(name, startDate, endDate);
        //        byte[] pdfBytes = pdfResult.FileContents;

        //        // 3. Send Email with attachment
        //        using (MailMessage mail = new MailMessage())
        //        {
        //            mail.From = new MailAddress("sankalpunam.elpro@gmail.com");
        //            mail.To.Add("sankalpunam.elpro@gmail.com");
        //            mail.To.Add("info@elprosolutions.com");
        //            mail.To.Add("operations@elprosolutions.com");
        //            // Get current greeting
        //            string greeting;
        //            int hour = DateTime.Now.Hour;
        //            if (hour < 12)
        //                greeting = "Good Morning";
        //            else if (hour < 17)
        //                greeting = "Good Afternoon";
        //            else
        //                greeting = "Good Evening";

        //            string reportDate = DateTime.Now.AddDays(-1).ToString("dd MMMM yyyy");

        //            // Email body with logo and summary
        //            mail.Subject = $"Daily Cold Storage Report - {reportDate}";
        //            mail.Body = $@"
        //          <html>
        //          <body style='font-family: Arial, sans-serif; font-size: 14px; color: #333;'>
        //              <div style='border-bottom: 1px solid #ccc; padding-bottom: 10px; margin-bottom: 20px;'>
        //                  <img src='~/Images/logo.jpg' alt='Company Logo' height='50' />
        //              </div>
        //              <p>{greeting},</p>

        //              <p>Please find attached the <b>Cold Storage Monitoring Report</b> for <b>{reportDate}</b>.</p>

        //              <p>The report includes the following data:</p>
        //              <ul>
        //                  <li>Temperature and Humidity Levels</li>
        //                  <li>Power and Door Status</li>
        //                  <li>CO₂ and Ethylene Gas Levels</li>
        //                  <li>Fan Speed and Hardware Time</li>
        //              </ul>

        //              <p>Best regards,<br/>
        //              <b>Cold Storage Monitoring System</b><br/>
        //              <a href='https://www.elprosolutions.com'>ELPRO Solutions Pvt. Ltd.</a></p>
        //          </body>
        //          </html>";
        //            mail.IsBodyHtml = true;

        //            mail.Attachments.Add(new Attachment(new MemoryStream(pdfBytes), "ColdStorageReport.pdf"));
        //            mail.Attachments.Add(new Attachment(new MemoryStream(excelBytes), "ColdStorageReport.xlsx"));

        //            using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
        //            {
        //                smtp.Credentials = new NetworkCredential("sankalpunam.elpro@gmail.com", "ysjfdlhwllfcfwpz");
        //                smtp.EnableSsl = true;
        //                await smtp.SendMailAsync(mail);
        //            }
        //        }

        //        Console.WriteLine("Daily report sent.");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("Error sending report: " + ex.Message);
        //    }
        //}
    }
}