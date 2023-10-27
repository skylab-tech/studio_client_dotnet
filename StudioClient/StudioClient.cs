using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;


namespace SkylabStudio {
    public partial class StudioClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public StudioClient(string? apiKey = null)
        {
            if (apiKey == null) throw new Exception("No API key provided");

            string baseUrl = Environment.GetEnvironmentVariable("SKYLAB_API_URL") ?? "https://studio.skylabtech.ai";
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(baseUrl);
            _apiKey = apiKey;
        }

        private async Task<dynamic> Request(string endpoint, HttpMethod httpMethod, object? payload = null)
        {
            var apiEndpoint = $"api/public/v1/{endpoint}";
            var request = new HttpRequestMessage(httpMethod, apiEndpoint);
            var headers = BuildRequestHeaders();

            foreach (var header in headers)
            {
                request.Headers.Add(header.Key, header.Value);
            }

            if (payload != null)
            {
                var jsonObj = JsonConvert.SerializeObject(payload);
                request.Content = new StringContent(jsonObj, Encoding.UTF8, "application/json");
            }

            try
            {
                // Send the request and get the response
                HttpResponseMessage response = await _httpClient.SendAsync(request);

                string responseContent = await response.Content.ReadAsStringAsync();
                dynamic? jsonData = JsonConvert.DeserializeObject<dynamic>(responseContent);
                if (jsonData != null) return jsonData;

                throw new Exception("Failed to get response from server.");
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while making the HTTP request.", ex);
            }
        }

        private Dictionary<string, string> BuildRequestHeaders()
        {
            var clientHeader = "1.1";

            // Create a Dictionary to store the headers
            var headers = new Dictionary<string, string>
            {
                { "API_HEADER_CLIENT", clientHeader },
                { "X-SLT-API-KEY", _apiKey },
                { "ContentType", "application/json" },
                { "Accept", "text/plain" }
            };

            return headers;
        }

        public bool ValidateHmacHeaders(string secretKey, string jobJson, string requestTimestamp, string signature)
        {
            // Convert the secret key and message to byte arrays
            byte[] keyBytes = Encoding.UTF8.GetBytes(secretKey);
            byte[] messageBytes = Encoding.UTF8.GetBytes($"{requestTimestamp}:{jobJson}");

            // Create an instance of the HMACSHA256 algorithm
            string generatedSignature = "";
            using (HMACSHA256 hmac = new HMACSHA256(keyBytes))
            {
                // Compute the hash (HMAC signature) for the message
                byte[] hashBytes = hmac.ComputeHash(messageBytes);

                // Convert the hash to a hexadecimal string
                generatedSignature = Convert.ToBase64String(hashBytes);
            }

            return signature == generatedSignature;
        } 
    }
}