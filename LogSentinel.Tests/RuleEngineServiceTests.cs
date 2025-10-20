using System;
using System.Text.Json;
using System.Threading.Tasks;
using LogSentinel.BUS.Services;
using LogSentinel.DAL.Data;
using LogSentinel.DAL.Repositories;
using LogSentinel.BUS.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System.Collections.Generic;

namespace LogSentinel.Tests
{
    public class RuleEngineServiceTests
    {
        private readonly Mock<IRuleRepository> _mockRuleRepository;
        private readonly Mock<IAlertService> _mockAlertService;
        private readonly Mock<ILogger<RuleEngineService>> _mockLogger;
        private readonly RuleEngineService _ruleEngineService;

        public RuleEngineServiceTests()
        {
            _mockRuleRepository = new Mock<IRuleRepository>();
            _mockAlertService = new Mock<IAlertService>();
            _mockLogger = new Mock<ILogger<RuleEngineService>>();

            _ruleEngineService = new RuleEngineService(
                _mockRuleRepository.Object,
                _mockAlertService.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task ProcessEventAsync_NotepadExecution_TriggersAlert()
        {
            // Arrange
            var rule = new RuleEntity
            {
                Id = 1,
                Name = "Notepad Execution",
                Severity = "Low",
                IsEnabled = true,
                YamlContent = @"name: Notepad Execution
description: Generates an alert when notepad.exe is started.
severity: Low
enabled: true

log_source:
  provider: 'Microsoft-Windows-Sysmon'
  event_id: 1

detection:
  Image: 'C:\Windows\System32\notepad.exe'"
            };

            _mockRuleRepository.Setup(x => x.GetEnabledAsync())
                .ReturnsAsync(new List<RuleEntity> { rule });

            var eventEntity = new EventEntity
            {
                Id = 1,
                Provider = "Microsoft-Windows-Sysmon",
                EventId = 1,
                DetailsJson = JsonSerializer.Serialize(new
                {
                    Image = "C:\\Windows\\System32\\notepad.exe",
                    ProcessId = 1234,
                    User = "WORKSTATION-01\\alice"
                }),
                Host = "WORKSTATION-01",
                User = "alice"
            };

            // Act
            var result = await _ruleEngineService.ProcessEventAsync(eventEntity);

            // Assert
            Assert.True(result);
            _mockAlertService.Verify(x => x.CreateAlertAsync(
                It.Is<RuleEntity>(r => r.Name == "Notepad Execution"),
                It.IsAny<IEnumerable<EventEntity>>(),
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Once);

            _mockRuleRepository.Verify(x => x.UpdateAsync(It.IsAny<RuleEntity>()), Times.Once);
        }

        [Fact]
        public async Task ProcessEventAsync_WrongProvider_DoesNotTriggerAlert()
        {
            // Arrange
            var rule = new RuleEntity
            {
                Id = 1,
                Name = "Notepad Execution",
                Severity = "Low",
                IsEnabled = true,
                YamlContent = @"name: Notepad Execution
log_source:
  provider: 'Microsoft-Windows-Sysmon'
  event_id: 1
detection:
  Image: 'C:\Windows\System32\notepad.exe'"
            };

            _mockRuleRepository.Setup(x => x.GetEnabledAsync())
                .ReturnsAsync(new List<RuleEntity> { rule });

            var eventEntity = new EventEntity
            {
                Id = 1,
                Provider = "Microsoft-Windows-Security-Auditing", // Wrong provider
                EventId = 1,
                DetailsJson = JsonSerializer.Serialize(new
                {
                    Image = "C:\\Windows\\System32\\notepad.exe"
                })
            };

            // Act
            var result = await _ruleEngineService.ProcessEventAsync(eventEntity);

            // Assert
            Assert.False(result);
            _mockAlertService.Verify(x => x.CreateAlertAsync(
                It.IsAny<RuleEntity>(),
                It.IsAny<IEnumerable<EventEntity>>(),
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ProcessEventAsync_WrongEventId_DoesNotTriggerAlert()
        {
            // Arrange
            var rule = new RuleEntity
            {
                Id = 1,
                Name = "Notepad Execution",
                Severity = "Low",
                IsEnabled = true,
                YamlContent = @"name: Notepad Execution
log_source:
  provider: 'Microsoft-Windows-Sysmon'
  event_id: 1
detection:
  Image: 'C:\Windows\System32\notepad.exe'"
            };

            _mockRuleRepository.Setup(x => x.GetEnabledAsync())
                .ReturnsAsync(new List<RuleEntity> { rule });

            var eventEntity = new EventEntity
            {
                Id = 1,
                Provider = "Microsoft-Windows-Sysmon",
                EventId = 3, // Wrong event ID
                DetailsJson = JsonSerializer.Serialize(new
                {
                    Image = "C:\\Windows\\System32\\notepad.exe"
                })
            };

            // Act
            var result = await _ruleEngineService.ProcessEventAsync(eventEntity);

            // Assert
            Assert.False(result);
            _mockAlertService.Verify(x => x.CreateAlertAsync(
                It.IsAny<RuleEntity>(),
                It.IsAny<IEnumerable<EventEntity>>(),
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ProcessEventAsync_WrongDetectionValue_DoesNotTriggerAlert()
        {
            // Arrange
            var rule = new RuleEntity
            {
                Id = 1,
                Name = "Notepad Execution",
                Severity = "Low",
                IsEnabled = true,
                YamlContent = @"name: Notepad Execution
log_source:
  provider: 'Microsoft-Windows-Sysmon'
  event_id: 1
detection:
  Image: 'C:\Windows\System32\notepad.exe'"
            };

            _mockRuleRepository.Setup(x => x.GetEnabledAsync())
                .ReturnsAsync(new List<RuleEntity> { rule });

            var eventEntity = new EventEntity
            {
                Id = 1,
                Provider = "Microsoft-Windows-Sysmon",
                EventId = 1,
                DetailsJson = JsonSerializer.Serialize(new
                {
                    Image = "C:\\Windows\\System32\\calc.exe" // Wrong image
                })
            };

            // Act
            var result = await _ruleEngineService.ProcessEventAsync(eventEntity);

            // Assert
            Assert.False(result);
            _mockAlertService.Verify(x => x.CreateAlertAsync(
                It.IsAny<RuleEntity>(),
                It.IsAny<IEnumerable<EventEntity>>(),
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ProcessEventAsync_MultipleDetectionCriteria_AllMustMatch()
        {
            // Arrange
            var rule = new RuleEntity
            {
                Id = 1,
                Name = "PowerShell System Execution",
                Severity = "Medium",
                IsEnabled = true,
                YamlContent = @"name: PowerShell System Execution
log_source:
  provider: 'Microsoft-Windows-Sysmon'
  event_id: 1
detection:
  Image: 'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe'
  User: 'SYSTEM'"
            };

            _mockRuleRepository.Setup(x => x.GetEnabledAsync())
                .ReturnsAsync(new List<RuleEntity> { rule });

            // Test case 1: Both conditions match
            var eventEntity1 = new EventEntity
            {
                Id = 1,
                Provider = "Microsoft-Windows-Sysmon",
                EventId = 1,
                DetailsJson = JsonSerializer.Serialize(new
                {
                    Image = "C:\\Windows\\System32\\WindowsPowerShell\\v1.0\\powershell.exe",
                    User = "SYSTEM"
                })
            };

            // Test case 2: Only first condition matches
            var eventEntity2 = new EventEntity
            {
                Id = 2,
                Provider = "Microsoft-Windows-Sysmon",
                EventId = 1,
                DetailsJson = JsonSerializer.Serialize(new
                {
                    Image = "C:\\Windows\\System32\\WindowsPowerShell\\v1.0\\powershell.exe",
                    User = "alice" // Wrong user
                })
            };

            // Act
            var result1 = await _ruleEngineService.ProcessEventAsync(eventEntity1);
            var result2 = await _ruleEngineService.ProcessEventAsync(eventEntity2);

            // Assert
            Assert.True(result1); // Should trigger alert
            Assert.False(result2); // Should NOT trigger alert

            _mockAlertService.Verify(x => x.CreateAlertAsync(
                It.IsAny<RuleEntity>(),
                It.IsAny<IEnumerable<EventEntity>>(),
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Once); // Only once for the first event
        }

        [Fact]
        public async Task ProcessEventAsync_CaseInsensitiveMatching_TriggersAlert()
        {
            // Arrange
            var rule = new RuleEntity
            {
                Id = 1,
                Name = "Notepad Execution",
                Severity = "Low",
                IsEnabled = true,
                YamlContent = @"name: Notepad Execution
log_source:
  provider: 'Microsoft-Windows-Sysmon'
  event_id: 1
detection:
  Image: 'C:\Windows\System32\notepad.exe'"
            };

            _mockRuleRepository.Setup(x => x.GetEnabledAsync())
                .ReturnsAsync(new List<RuleEntity> { rule });

            var eventEntity = new EventEntity
            {
                Id = 1,
                Provider = "microsoft-windows-sysmon", // Different case
                EventId = 1,
                DetailsJson = JsonSerializer.Serialize(new
                {
                    Image = "c:\\windows\\system32\\notepad.exe" // Different case
                })
            };

            // Act
            var result = await _ruleEngineService.ProcessEventAsync(eventEntity);

            // Assert
            Assert.True(result);
            _mockAlertService.Verify(x => x.CreateAlertAsync(
                It.IsAny<RuleEntity>(),
                It.IsAny<IEnumerable<EventEntity>>(),
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Once);
        }
    }
}