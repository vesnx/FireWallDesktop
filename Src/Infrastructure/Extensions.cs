using System;
using System.IO;
using System.Net.Mail;
using System.Reflection;
using System.Runtime.CompilerServices;
using Desktop.Model;
using log4net;
using Prism.Services.Dialogs;

namespace Desktop.Infrastructure
{
    public static class Extensions
    {
        public static void LogDialogOpened(this ILog logger, IDialogParameters parameters, [CallerMemberName] string method = null, [CallerFilePath] string fileName = null)
        {
            logger.LogInfo($" Open dialog with parameters {string.Join(", ", parameters.Keys)}", method, fileName);
        }



        /// <summary>
        /// Emails the specified report with the report with attachments.
        /// </summary>
        /// <param name="report">The report to send.</param>
        /// <param name="attachments">The attachments to add.</param>
        public static void Email(this SimpleAbuseReport report,  params FileInfo[] attachments)
        {
            MimeKit.MimeMessage mailMessage = new MimeKit.MimeMessage();
            var from = RunTime.SelectedFireWall.ContactDetails?.EMail ?? $"info@{RunTime.SelectedFireWall.Domain.DnsSafeHost}";
            mailMessage.From.Add(MimeKit.InternetAddress.Parse(from));
            mailMessage.To.Add(MimeKit.InternetAddress.Parse(report.AbuseEmail));
            mailMessage.Subject = report.Reference;
            var bodyBuilder = new MimeKit.BodyBuilder();
            bodyBuilder.TextBody = report.Note;
            report.SendToAbuse = DateTime.UtcNow;

            foreach (var file in attachments)
            {
                bodyBuilder.Attachments.Add(file.FullName);
            }
            mailMessage.Body= bodyBuilder.ToMessageBody();

            var filename =Path.Combine(Path.GetTempPath(),$"{report.ReportId}.eml");

            //save the MailMessage to the file system
            mailMessage.WriteTo(filename);
            var startInfo = new System.Diagnostics.ProcessStartInfo(filename)
            {
                UseShellExecute = true
            };

            //Open the file with the default associated application registered on the local machine
            System.Diagnostics.Process.Start(startInfo);
            

        }

        
    }
}
