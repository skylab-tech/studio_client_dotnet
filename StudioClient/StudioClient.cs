using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using RestSharp;


namespace SkylabStudio {
    public class StudioOptions
    {
        public int? MaxConcurrentDownloads { get; set; }
        public bool? ResizeImageIfOversized { get; set; }
    }

    public partial class StudioClient
    {
        private readonly RestClient _httpClient;
        private readonly string _apiKey;
        private readonly int _maxConcurrentDownloads = 5;
        private readonly bool _resizeImageIfOversized = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="StudioClient"/> class with the specified API key and options.
        /// </summary>
        /// <param name="apiKey">The API key used for authentication.</param>
        /// <param name="options">Optional options for configuring the StudioClient.</param>
        public StudioClient(string? apiKey = null, StudioOptions? options = null)
        {
            if (apiKey == null) throw new Exception("No API key provided");

            string baseUrl = Environment.GetEnvironmentVariable("SKYLAB_API_URL") ?? "https://studio.skylabtech.ai";
            _httpClient = new RestClient(baseUrl);
            _apiKey = apiKey;
            _maxConcurrentDownloads = options?.MaxConcurrentDownloads ?? 5;
            _resizeImageIfOversized = options?.ResizeImageIfOversized ?? false;
        }

        /// <summary>
        /// Makes an HTTP request to the Skylab API endpoint with the specified parameters.
        /// </summary>
        /// <param name="endpoint">The API endpoint to request.</param>
        /// <param name="httpMethod">The HTTP method (GET, POST, PUT, DELETE, etc.) to use for the request.</param>
        /// <param name="payload">Optional payload data to include in the request.</param>
        /// <returns>The dynamic response data from the Skylab API.</returns>
        /// <exception cref="Exception">Thrown when the HTTP request fails or when the response is not successful.</exception>
        private async Task<dynamic> Request(string endpoint, Method httpMethod, object? payload = null)
        {
            var apiEndpoint = $"api/public/v1/{endpoint}";
            RestRequest request = new RestRequest(apiEndpoint, httpMethod);
            var headers = BuildRequestHeaders();

            foreach (var header in headers)
            {
                request.AddHeader(header.Key, header.Value);
            }

            if (payload != null)
            {
                var jsonObj = JsonConvert.SerializeObject(payload);
                request.AddJsonBody(jsonObj);
            }

            try
            {
                // Send the request and get the response
                RestResponse response = await _httpClient.ExecuteAsync(request);

                string responseContent = response?.Content ?? "";
                if (response?.IsSuccessStatusCode ?? false) {
                    dynamic? jsonData = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    if (jsonData != null) return jsonData;
                }

                throw new Exception($"{response?.Content}");
            }
            catch (Exception ex)
            {
                // Rethrow the exception to be caught by user
                throw ex;
            }
        }

        /// <summary>
        /// Builds and returns a dictionary of headers to be included in the Skylab API request.
        /// </summary>
        /// <returns>A dictionary of headers with their corresponding values.</returns>
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


        /// <summary>
        /// Validates HMAC headers by comparing the computed HMAC signature with the provided signature.
        /// Used to validate job json object in callback is from Skylab.
        /// </summary>
        /// <param name="secretKey">The secret key used for HMAC hashing.</param>
        /// <param name="jobJson">The JSON representation of the job.</param>
        /// <param name="requestTimestamp">The timestamp of the API request.</param>
        /// <param name="signature">The provided HMAC signature to be validated.</param>
        /// <returns>
        ///   <c>true</c> if the computed HMAC signature matches the provided signature; otherwise, <c>false</c>.
        /// </returns>
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