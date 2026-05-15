using SchoolManagementSystem.Models;
using System;
using System.IO;
using System.Text;

namespace SchoolManagementSystem.Services
{
    public class ReceiptGenerator
    {
        public static string GenerateReceipt(FeePayment payment, Parent parent, Fee fee, string savePath)
        {
            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset='utf-8'>");
            html.AppendLine("<title>Fee Receipt</title>");
            html.AppendLine("<style>");
            html.AppendLine("body { font-family: Arial, sans-serif; margin: 40px; }");
            html.AppendLine(".header { text-align: center; border-bottom: 2px solid #333; padding-bottom: 20px; margin-bottom: 30px; }");
            html.AppendLine(".header h1 { color: #6f42c1; margin: 0; }");
            html.AppendLine(".info-row { display: flex; margin-bottom: 10px; }");
            html.AppendLine(".label { font-weight: bold; width: 200px; }");
            html.AppendLine(".value { flex: 1; }");
            html.AppendLine(".amount-box { background: #f8f9fa; padding: 20px; margin: 20px 0; text-align: center; border: 2px solid #6f42c1; }");
            html.AppendLine(".amount-box h2 { margin: 0; color: #6f42c1; }");
            html.AppendLine(".footer { margin-top: 50px; text-align: center; font-size: 12px; color: #666; }");
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");

            html.AppendLine("<div class='header'>");
            html.AppendLine("<h1>Smart Eduverse</h1>");
            html.AppendLine("<p>Fee Payment Receipt</p>");
            html.AppendLine("</div>");

            html.AppendLine("<div class='info-row'>");
            html.AppendLine("<div class='label'>Receipt No:</div>");
            html.AppendLine($"<div class='value'>REC{payment.PaymentId:D6}</div>");
            html.AppendLine("</div>");

            html.AppendLine("<div class='info-row'>");
            html.AppendLine("<div class='label'>Transaction ID:</div>");
            html.AppendLine($"<div class='value'>{payment.TransactionId}</div>");
            html.AppendLine("</div>");

            html.AppendLine("<div class='info-row'>");
            html.AppendLine("<div class='label'>Payment Date:</div>");
            html.AppendLine($"<div class='value'>{payment.PaymentDate?.ToString("dd MMM yyyy HH:mm")}</div>");
            html.AppendLine("</div>");

            html.AppendLine("<div class='info-row'>");
            html.AppendLine("<div class='label'>Student Name:</div>");
            html.AppendLine($"<div class='value'>{parent.StudentFirstName} {parent.StudentLastName}</div>");
            html.AppendLine("</div>");

            html.AppendLine("<div class='info-row'>");
            html.AppendLine("<div class='label'>Class:</div>");
            html.AppendLine($"<div class='value'>{fee.ClassName}</div>");
            html.AppendLine("</div>");

            html.AppendLine("<div class='info-row'>");
            html.AppendLine("<div class='label'>Fee Type:</div>");
            html.AppendLine($"<div class='value'>{fee.FeeType}</div>");
            html.AppendLine("</div>");

            html.AppendLine("<div class='info-row'>");
            html.AppendLine("<div class='label'>Payment Method:</div>");
            html.AppendLine($"<div class='value'>{payment.PaymentMethod}</div>");
            html.AppendLine("</div>");

            html.AppendLine("<div class='amount-box'>");
            html.AppendLine("<h2>Amount Paid: ₹" + fee.Amount.ToString("N2") + "</h2>");
            html.AppendLine("</div>");

            html.AppendLine("<div class='footer'>");
            html.AppendLine("<p>This is a computer-generated receipt and does not require a signature.</p>");
            html.AppendLine("<p>© 2026 Smart Eduverse. All rights reserved.</p>");
            html.AppendLine("</div>");

            html.AppendLine("</body>");
            html.AppendLine("</html>");

            string fileName = $"Receipt_{payment.PaymentId}_{DateTime.Now:yyyyMMddHHmmss}.html";
            string fullPath = Path.Combine(savePath, fileName);

            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }

            File.WriteAllText(fullPath, html.ToString());

            return fileName;
        }
    }
}