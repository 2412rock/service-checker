using System;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;

namespace HttpChecker
{
    class Program
    {
        private static Timer _timer;
        private static readonly HttpClient _httpClient = new HttpClient();

        static async Task Main(string[] args)
        {
            // Set the timer to run every 1 minute (60000 milliseconds)
            _timer = new Timer(CheckServiceStatus, null, 0, 60000);

            // Prevent the console application from exiting
            Console.WriteLine("Monitoring started. Press Enter to stop...");
            Console.ReadLine();
        }

        private static async void CheckServiceStatus(object state)
        {
            string url = "https://overflowapp.xyz:4200/api/health"; // URL to check
            try
            {
                var response = await _httpClient.GetAsync(url);
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    SendEmail("Service Down", $"The service at {url} is down. Status code: {(int)response.StatusCode}");
                }
                else
                {
                   // Console.WriteLine($"[{DateTime.Now}] Service is running fine.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                SendEmail("Exception occured checking overflow service status", $"Exception: {ex.Message}");
            }
        }

        public static void SendEmail(string subject, string body)
        {
            // Sender's Gmail credentials
            string senderEmail = "overflowthegame@gmail.com";
            string senderPassword = Environment.GetEnvironmentVariable("EMAIL_PASSWD");

            // Create a new MailMessage
            MailMessage mail = new MailMessage(senderEmail, "2412rock@gmail.com")
            {
                Subject = subject,
                Body = body
            };

            // Configure the SMTP client
            SmtpClient smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(senderEmail, senderPassword),
                EnableSsl = true
            };

            try
            {
                // Send the email
                smtpClient.Send(mail);
                Console.WriteLine("Email sent successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email using password {senderPassword}: {ex.Message}");
                //throw;
            }
        }
    }
 }
