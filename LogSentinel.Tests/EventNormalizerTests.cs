using LogSentinel.BUS.Services;
using Xunit;

namespace LogSentinel.Tests
{
    public class EventNormalizerTests
    {
        private readonly EventNormalizer _normalizer;

        public EventNormalizerTests()
        {
            _normalizer = new EventNormalizer();
        }

        [Fact]
        public void Normalize_ValidLogLine_ReturnsEventDto()
        {
            // Arrange
            var logLine = "2024-01-15 14:30:45 [INFO] HOST1 alice svchost.exe Application started successfully";

            // Act
            var result = _normalizer.Normalize(logLine);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("INFO", result.Level);
            Assert.Equal("HOST1", result.Host);
            Assert.Equal("alice", result.User);
            Assert.Equal("svchost.exe", result.Process);
            Assert.Equal("Application started successfully", result.Message);
        }

        [Fact]
        public void Normalize_InvalidLogLine_ReturnsFallbackDto()
        {
            // Arrange
            var logLine = "This is an invalid log line";

            // Act
            var result = _normalizer.Normalize(logLine);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Info", result.Level);
            Assert.Equal(logLine, result.Message);
            Assert.Equal("unknown", result.Process);
        }

        [Fact]
        public void Normalize_WarningLevel_ParsesCorrectly()
        {
            // Arrange
            var logLine = "2024-01-15 10:20:30 [WARNING] SERVER01 bob chrome.exe Memory usage high";

            // Act
            var result = _normalizer.Normalize(logLine);

            // Assert
            Assert.Equal("WARNING", result.Level);
            Assert.Equal("SERVER01", result.Host);
            Assert.Equal("bob", result.User);
            Assert.Equal("chrome.exe", result.Process);
        }

        [Fact]
        public void Normalize_ErrorLevel_ParsesCorrectly()
        {
            // Arrange
            var logLine = "2024-01-15 09:15:00 [ERROR] DB-SERVER eve sqlservr.exe Connection failed";

            // Act
            var result = _normalizer.Normalize(logLine);

            // Assert
            Assert.Equal("ERROR", result.Level);
            Assert.Equal("DB-SERVER", result.Host);
            Assert.Equal("eve", result.User);
            Assert.Equal("sqlservr.exe", result.Process);
            Assert.Contains("Connection failed", result.Message);
        }
    }
}
