using System.Net;
using System.Net.Mail;
using System.Configuration;
using System;


namespace ProyectoRelampago.Helpers
{
    public static class EmailService
    {
        public static bool EnviarCorreo(string destino, string asunto, string cuerpoHtml)
        {
            try
            {
                var fromAddress = ConfigurationManager.AppSettings["CorreoOrigen"];
                var smtpUser = ConfigurationManager.AppSettings["SMTP_Usuario"];
                var smtpPass = ConfigurationManager.AppSettings["SMTP_Clave"];

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(fromAddress);
                mail.To.Add(destino);
                mail.Subject = asunto;
                mail.Body = cuerpoHtml;
                mail.IsBodyHtml = true;

                SmtpClient smtp = new SmtpClient();
                smtp.Host = ConfigurationManager.AppSettings["SMTP_Host"];
                smtp.Port = int.Parse(ConfigurationManager.AppSettings["SMTP_Puerto"]);
                smtp.EnableSsl = true;
                smtp.Credentials = new NetworkCredential(smtpUser, smtpPass);
                smtp.Send(mail);

                return true;
            }
            catch (Exception ex)
            {

                // Puedes loguear el error si quieres
                return false;
            }
        }
    }



}