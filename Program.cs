using System;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HttpChecker
{
    class Program
    {
        private static Timer _timer;
        private static readonly HttpClient _httpClient = new HttpClient();
        private static CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private static string baseUrl = "https://overflowapp.xyz:4200";


        static async Task Main(string[] args)
        {
            // Set the timer to run every 1 minute (60000 milliseconds)
            _timer = new Timer(CheckServiceStatus, null, 0, 60000);

            Console.WriteLine("Monitoring started. Press Enter to stop...");
            // Keep the program running indefinitely
            await Task.Delay(Timeout.Infinite, _cancellationTokenSource.Token);
        }

        private static async void CheckServiceStatus(object state)
        {
            string healthUrl = $"{baseUrl}/api/health"; // URL to check
            try
            {
                var response = await _httpClient.GetAsync(healthUrl);
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    SendEmail("Overflow Server Down", $"The service at {healthUrl} is down. Status code: {(int)response.StatusCode}");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    // Check the queue size
                    string queueSizeUrl = $"{baseUrl}/api/getQueueSize"; // URL to check
                    var queueSizeResponse = await _httpClient.GetAsync(queueSizeUrl);
                    if (queueSizeResponse.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        // Read the response content
                        var queueSizeContent = await queueSizeResponse.Content.ReadAsStringAsync();

                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true // Ignore case when matching properties
                        };
                        // Deserialize the JSON response
                        var queueSizeResponseObject = JsonSerializer.Deserialize<ApiResponse<int>>(queueSizeContent, options);

                        // Check if the deserialization was successful and if the isSuccess is true
                        if (queueSizeResponseObject != null && queueSizeResponseObject.IsSuccess)
                        {
                            int queueSize = queueSizeResponseObject.Data;

                            if (queueSize == 0)
                            {
                                SendEmail("Overflow Queue Size Alert", $"The queue size is {queueSize}.");
                            }
                        }
                        else
                        {
                            SendEmail("Overflow Queue Size Error", $"The response indicates failure or is not in the expected format.");
                        }
                    }
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

    public class ApiResponse<T>
    {
        public T Data { get; set; }
        public bool IsSuccess { get; set; }
        public bool IsException { get; set; }
        public string? ExceptionMessage { get; set; }
    }
}
