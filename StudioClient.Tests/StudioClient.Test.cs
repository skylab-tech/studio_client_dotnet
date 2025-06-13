using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using RestSharp;
using Xunit;
using SkylabStudio;

namespace StudioClient.Tests
{
    public class StudioClientTests
    {
        private const string TestApiKey = "8QazqWzV3qWxkR8wykKr1Y4z";
        private const string TestSecretKey = "test-secret-key";
        private readonly Mock<RestClient> _mockRestClient;
        private readonly SkylabStudio.StudioClient _client;

        public StudioClientTests()
        {
            _mockRestClient = new Mock<RestClient>();
            _client = new SkylabStudio.StudioClient(TestApiKey);
        }

        [Fact]
        public void Constructor_WithNullApiKey_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<Exception>(() => new SkylabStudio.StudioClient(null));
        }

        [Fact]
        public void Constructor_WithValidApiKey_SetsProperties()
        {
            // Arrange & Act
            var client = new SkylabStudio.StudioClient(TestApiKey);

            // Assert
            Assert.NotNull(client);
            Assert.Equal("100m", Environment.GetEnvironmentVariable("VIPS_DISC_THRESHOLD"));
        }

        [Fact]
        public void Constructor_WithCustomOptions_SetsProperties()
        {
            // Arrange
            var options = new SkylabStudio.StudioOptions
            {
                MaxConcurrentDownloads = 10,
                PngCompression = 8,
                PngEffort = 9,
                MaxMemory = 500
            };

            // Act
            var client = new SkylabStudio.StudioClient(TestApiKey, options);

            // Assert
            Assert.NotNull(client);
            Assert.Equal("500m", Environment.GetEnvironmentVariable("VIPS_DISC_THRESHOLD"));
        }

        [Fact]
        public void Constructor_WithMaxMemoryOption_SetsEnvironmentVariable()
        {
            // Arrange
            var options = new SkylabStudio.StudioOptions
            {
                MaxMemory = 500
            };

            // Act
            var client = new SkylabStudio.StudioClient(TestApiKey, options);

            // Assert
            Assert.Equal("500m", Environment.GetEnvironmentVariable("VIPS_DISC_THRESHOLD"));
        }

        [Fact]
        public void Constructor_WithoutMaxMemoryOption_SetsDefaultEnvironmentVariable()
        {
            // Act
            var client = new SkylabStudio.StudioClient(TestApiKey);

            // Assert
            Assert.Equal("100m", Environment.GetEnvironmentVariable("VIPS_DISC_THRESHOLD"));
        }

        [Fact]
        public void ValidateHmacHeaders_WithValidInput_ReturnsTrue()
        {
            // Arrange
            var client = new SkylabStudio.StudioClient(TestApiKey);
            var jobJson = "{\"id\":\"123\",\"status\":\"completed\"}";
            var timestamp = "2024-03-20T12:00:00Z";
            var message = $"{timestamp}:{jobJson}";

            // Generate a valid signature
            byte[] keyBytes = System.Text.Encoding.UTF8.GetBytes(TestSecretKey);
            byte[] messageBytes = System.Text.Encoding.UTF8.GetBytes(message);
            string validSignature;
            using (var hmac = new System.Security.Cryptography.HMACSHA256(keyBytes))
            {
                byte[] hashBytes = hmac.ComputeHash(messageBytes);
                validSignature = Convert.ToBase64String(hashBytes);
            }

            // Act
            var result = client.ValidateHmacHeaders(TestSecretKey, jobJson, timestamp, validSignature);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ValidateHmacHeaders_WithInvalidSignature_ReturnsFalse()
        {
            // Arrange
            var client = new SkylabStudio.StudioClient(TestApiKey);
            var jobJson = "{\"id\":\"123\",\"status\":\"completed\"}";
            var timestamp = "2024-03-20T12:00:00Z";
            var invalidSignature = "invalid-signature";

            // Act
            var result = client.ValidateHmacHeaders(TestSecretKey, jobJson, timestamp, invalidSignature);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ValidateHmacHeaders_WithEmptyInput_ReturnsFalse()
        {
            // Arrange
            var client = new SkylabStudio.StudioClient(TestApiKey);
            var emptySignature = "";

            // Act
            var result = client.ValidateHmacHeaders(TestSecretKey, "", "", emptySignature);

            // Assert
            Assert.False(result);
        }
    }
}
