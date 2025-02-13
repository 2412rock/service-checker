using System;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Threading.Timer;

namespace HttpChecker
{
    class Program
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private static string baseUrlOverflow = "https://overflowapp.xyz:4200";
        private static string baseUrlFfhubFrontend = "https://ffhub.co";
        private static string baseUrlFfhubBackend = "https://ffhub.co:4500";


        static async Task Main(string[] args)
        {
            // Set the timer to run every 1 minute (60000 milliseconds)
            var _timer = new Timer(async _ => await CheckOverflowStatus(null), null, 0, 60000);
            //var _timer2 = new Timer(async _ => await CheckFfhubStatus(null), null, 0, 60000);
            string senderPassword = Environment.GetEnvironmentVariable("EMAIL_PASSWD");
            Console.WriteLine($"Monitoring started. Press Enter to stop... {senderPassword}");
            if(senderPassword == null)
            {
                Console.WriteLine("No email password");
            }
            // Keep the program running indefinitely
            await Task.Delay(Timeout.Infinite, _cancellationTokenSource.Token);
        }

        private static async Task CheckFfhubStatus(object state) 
        {
            string healthUrl = $"{baseUrlFfhubBackend}/api/health";
            try
            {
                var ffhubFrontendReponse = await _httpClient.GetAsync(baseUrlFfhubFrontend);
                if (ffhubFrontendReponse.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    SendEmail("Ffhub frontend Down", $"Ffhub frontend is down. Status code: {(int)ffhubFrontendReponse.StatusCode}");
                }
                var ffhubBackendReponse = await _httpClient.GetAsync(healthUrl);
                if (ffhubBackendReponse.StatusCode == System.Net.HttpStatusCode.OK) 
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true // Ignore case when matching properties
                    };

                    var heathcheckReponseContent = await ffhubBackendReponse.Content.ReadAsStringAsync();
                    var healthCheckMaybe = JsonSerializer.Deserialize<Maybe<string>>(heathcheckReponseContent, options);
                    // Check the queue size
                    if (healthCheckMaybe == null || healthCheckMaybe.IsException)
                    {
                        if (healthCheckMaybe != null)
                        {
                            SendEmail("Ffhub Backend returned exception", $"{healthCheckMaybe.ExceptionMessage}");
                        }
                        else
                        {
                            SendEmail("Ffhub Backend returned no maybe", $"Ffhub Backend returned no maybe");
                        }

                    }
                }
                else
                {
                    SendEmail("Ffhub backend Down", $"Ffhub backend is down. Status code: {(int)ffhubBackendReponse.StatusCode}");
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                SendEmail("Exception occured checking ffhub service status", $"Exception: {ex.Message}");
            }
        }

        private static async Task CheckOverflowStatus(object state)
        {
            string healthUrl = $"{baseUrlOverflow}/api/health"; // URL to check
            try
            {
                var healthCheckReponse = await _httpClient.GetAsync(healthUrl);
                Console.WriteLine($"Got healthcheck reposne {healthCheckReponse.StatusCode}");
                if (healthCheckReponse.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    SendEmail("Overflow Server Down", $"The service at {healthUrl} is down. Status code: {(int)healthCheckReponse.StatusCode}");
                }
                else if (healthCheckReponse.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true // Ignore case when matching properties
                    };

                    var heathcheckReponseContent = await healthCheckReponse.Content.ReadAsStringAsync();
                    var healthCheckMaybe = JsonSerializer.Deserialize<Maybe<string>>(heathcheckReponseContent, options);
                    // Check the queue size
                    if (healthCheckMaybe == null ||  healthCheckMaybe.IsException)
                    {
                        if(healthCheckMaybe != null)
                        {
                            SendEmail("Overflow Server returned exception", $"Overflow server returned exception {healthCheckMaybe.ExceptionMessage}");
                        }
                        else
                        {
                            SendEmail("Overflow Server returned no maybe", $"Overflow Server returned no maybe");
                        }
                        
                    }
                    string queueSizeUrl = $"{baseUrlOverflow}/api/getQueueSize"; // URL to check
                    var queueSizeResponse = await _httpClient.GetAsync(queueSizeUrl);
                    if (queueSizeResponse.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        // Read the response content
                        var queueSizeContent = await queueSizeResponse.Content.ReadAsStringAsync();

                        
                        // Deserialize the JSON response
                        var queueSizeResponseObject = JsonSerializer.Deserialize<Maybe<int>>(queueSizeContent, options);

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


    public class Maybe<T>
    {
        public T Data { get; set; }
        public bool IsSuccess { get; set; }
        public bool IsException { get; set; }
        public string? ExceptionMessage { get; set; }
    }
}
