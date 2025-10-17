using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LogSentinel.BUS.Services;
using LogSentinel.DAL.Data;
using LogSentinel.DAL.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LogSentinel.Tests
{
    public class AlertServiceTests
    {
        private readonly Mock<IAlertRepository> _mockAlertRepository;
        private readonly Mock<ILogger<AlertService>> _mockLogger;
        private readonly AlertService _alertService;

        public AlertServiceTests()
        {
            _mockAlertRepository = new Mock<IAlertRepository>();
            _mockLogger = new Mock<ILogger<AlertService>>();
            _alertService = new AlertService(_mockAlertRepository.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task CreateAlert_ValidData_CreatesAlert()
        {
            // Arrange
            var rule = new RuleEntity
            {
                Id = 1,
                Name = "Test Rule",
                Severity = "High"
            };

            var events = new List<EventEntity>
            {
                new EventEntity { Id = 1, EventTime = DateTime.UtcNow, Host = "HOST1", User = "alice" },
                new EventEntity { Id = 2, EventTime = DateTime.UtcNow, Host = "HOST1", User = "alice" }
            };

            _mockAlertRepository.Setup(x => x.AddAsync(It.IsAny<AlertEntity>()))
                .ReturnsAsync((AlertEntity a) => { a.Id = 100; return a; });

            // Act
            var result = await _alertService.CreateAlertAsync(rule, events, "Test Alert", "Test Description");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Rule", result.RuleName);
            Assert.Equal("High", result.Severity);
            Assert.Equal("Test Alert", result.Title);
            Assert.Equal("Test Description", result.Description);
            _mockAlertRepository.Verify(x => x.AddAsync(It.IsAny<AlertEntity>()), Times.Once);
        }

        [Fact]
        public async Task GetRecentAlerts_ReturnsAlerts()
        {
            // Arrange
            var alerts = new List<AlertEntity>
            {
                new AlertEntity
                {
                    Id = 1,
                    RuleName = "Rule 1",
                    Severity = "High",
                    Timestamp = DateTime.UtcNow,
                    Title = "Alert 1"
                },
                new AlertEntity
                {
                    Id = 2,
                    RuleName = "Rule 2",
                    Severity = "Medium",
                    Timestamp = DateTime.UtcNow.AddMinutes(-2),
                    Title = "Alert 2"
                }
            };

            _mockAlertRepository.Setup(x => x.GetRecentAsync(5))
                .ReturnsAsync(alerts);

            // Act
            var result = await _alertService.GetRecentAlertsAsync(5);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Contains(result, a => a.RuleName == "Rule 1");
        }

        [Fact]
        public async Task AcknowledgeAlert_ValidId_UpdatesAlert()
        {
            // Arrange
            var alert = new AlertEntity
            {
                Id = 1,
                RuleName = "Test Rule",
                IsAcknowledged = false
            };

            _mockAlertRepository.Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(alert);

            // Act
            await _alertService.AcknowledgeAlertAsync(1, "admin");

            // Assert
            Assert.True(alert.IsAcknowledged);
            Assert.Equal("admin", alert.AcknowledgedBy);
            Assert.NotNull(alert.AcknowledgedAt);
            _mockAlertRepository.Verify(x => x.UpdateAsync(alert), Times.Once);
        }

        [Fact]
        public async Task ExportToJson_CreatesJsonFile()
        {
            // Arrange
            var alerts = new List<AlertEntity>
            {
                new AlertEntity { Id = 1, RuleName = "Rule 1", Title = "Alert 1" }
            };

            _mockAlertRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(alerts);

            var tempFile = Path.GetTempFileName();

            try
            {
                // Act
                await _alertService.ExportToJsonAsync(tempFile);

                // Assert
                Assert.True(File.Exists(tempFile));
                var content = await File.ReadAllTextAsync(tempFile);
                Assert.Contains("Rule 1", content);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task ExportToCsv_CreatesCsvFile()
        {
            // Arrange
            var alerts = new List<AlertEntity>
            {
                new AlertEntity
                {
                    Id = 1,
                    RuleName = "Rule 1",
                    Severity = "High",
                    Timestamp = DateTime.UtcNow,
                    Title = "Alert 1",
                    Description = "Test",
                    IsAcknowledged = false
                }
            };

            _mockAlertRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(alerts);

            var tempFile = Path.GetTempFileName();

            try
            {
                // Act
                await _alertService.ExportToCsvAsync(tempFile);

                // Assert
                Assert.True(File.Exists(tempFile));
                var content = await File.ReadAllTextAsync(tempFile);
                Assert.Contains("Id,RuleName,Severity", content);
                Assert.Contains("Rule 1", content);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }
    }
}
