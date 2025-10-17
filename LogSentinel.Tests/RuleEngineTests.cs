using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LogSentinel.BUS.Interfaces;
using LogSentinel.BUS.Models;
using LogSentinel.BUS.Services;
using LogSentinel.DAL.Data;
using LogSentinel.DAL.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LogSentinel.Tests
{
    public class RuleEngineTests
    {
        private readonly Mock<IRuleProvider> _mockRuleProvider;
        private readonly Mock<IEventRepository> _mockEventRepository;
        private readonly Mock<IRuleRepository> _mockRuleRepository;
        private readonly Mock<IAlertService> _mockAlertService;
        private readonly Mock<ILogger<RuleEngine>> _mockLogger;
        private readonly RuleEngine _ruleEngine;

        public RuleEngineTests()
        {
            _mockRuleProvider = new Mock<IRuleProvider>();
            _mockEventRepository = new Mock<IEventRepository>();
            _mockRuleRepository = new Mock<IRuleRepository>();
            _mockAlertService = new Mock<IAlertService>();
            _mockLogger = new Mock<ILogger<RuleEngine>>();

            _ruleEngine = new RuleEngine(
                _mockRuleProvider.Object,
                _mockEventRepository.Object,
                _mockRuleRepository.Object,
                _mockAlertService.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task EvaluateEvent_FailedLoginRule_TriggersAlert()
        {
            // Arrange - Failed Login Threshold Rule (5 failed logins in 5 minutes)
            var rule = new RuleDefinition
            {
                Name = "Failed Login Threshold",
                Severity = "High",
                Selection = new SelectionCriteria { EventId = 4625 },
                Condition = new ConditionCriteria { Count = 5, Timeframe = 300, GroupBy = "user" },
                Action = new ActionCriteria { Alert = true, Title = "Multiple Failed Logins" }
            };

            _mockRuleProvider.Setup(x => x.LoadRulesAsync())
                .ReturnsAsync(new List<RuleDefinition> { rule });

            var ruleEntity = new RuleEntity { Id = 1, Name = "Failed Login Threshold", Severity = "High" };
            _mockRuleRepository.Setup(x => x.GetByNameAsync("Failed Login Threshold"))
                .ReturnsAsync(ruleEntity);

            await _ruleEngine.InitializeAsync();

            // Create 5 failed login events
            var events = Enumerable.Range(1, 5).Select(i => new EventEntity
            {
                Id = i,
                EventId = 4625,
                EventTime = DateTime.UtcNow.AddMinutes(-i),
                User = "alice",
                Level = "Warning",
                Host = "HOST1",
                Message = "Failed login"
            }).ToList();

            _mockEventRepository.Setup(x => x.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(events);

            // Act
            var lastEvent = events.Last();
            var result = await _ruleEngine.EvaluateEventAsync(lastEvent);

            // Assert
            Assert.True(result);
            _mockAlertService.Verify(x => x.CreateAlertAsync(
                It.IsAny<RuleEntity>(),
                It.IsAny<IEnumerable<EventEntity>>(),
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task EvaluateEvent_AdminUserCreatedRule_TriggersAlert()
        {
            // Arrange - Admin User Created Rule (Event 4732 with "administrators" in details)
            var rule = new RuleDefinition
            {
                Name = "Admin User Created",
                Severity = "Critical",
                Selection = new SelectionCriteria { EventId = 4732 },
                Condition = new ConditionCriteria 
                { 
                    Pattern = "(?i)(administrators|admin)",
                    Field = "details_json"
                },
                Action = new ActionCriteria { Alert = true, Title = "Admin Account Created" }
            };

            _mockRuleProvider.Setup(x => x.LoadRulesAsync())
                .ReturnsAsync(new List<RuleDefinition> { rule });

            var ruleEntity = new RuleEntity { Id = 2, Name = "Admin User Created", Severity = "Critical" };
            _mockRuleRepository.Setup(x => x.GetByNameAsync("Admin User Created"))
                .ReturnsAsync(ruleEntity);

            await _ruleEngine.InitializeAsync();

            var evt = new EventEntity
            {
                Id = 1,
                EventId = 4732,
                EventTime = DateTime.UtcNow,
                User = "admin",
                Host = "DC-01",
                DetailsJson = "{\"group\": \"Administrators\", \"action\": \"member_added\"}",
                Message = "Member added to Administrators group"
            };

            // Act
            var result = await _ruleEngine.EvaluateEventAsync(evt);

            // Assert
            Assert.True(result);
            _mockAlertService.Verify(x => x.CreateAlertAsync(
                It.IsAny<RuleEntity>(),
                It.Is<IEnumerable<EventEntity>>(events => events.Count() == 1),
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task EvaluateEvent_SuspiciousPowerShellRule_TriggersAlert()
        {
            // Arrange - Suspicious PowerShell Rule
            var rule = new RuleDefinition
            {
                Name = "Suspicious PowerShell Execution",
                Severity = "High",
                Selection = new SelectionCriteria { Process = "powershell.exe" },
                Condition = new ConditionCriteria 
                { 
                    Pattern = "(?i)(-enc|-nop|-w hidden)",
                    Field = "details_json"
                },
                Action = new ActionCriteria { Alert = true, Title = "Suspicious PowerShell Command" }
            };

            _mockRuleProvider.Setup(x => x.LoadRulesAsync())
                .ReturnsAsync(new List<RuleDefinition> { rule });

            var ruleEntity = new RuleEntity { Id = 3, Name = "Suspicious PowerShell Execution", Severity = "High" };
            _mockRuleRepository.Setup(x => x.GetByNameAsync("Suspicious PowerShell Execution"))
                .ReturnsAsync(ruleEntity);

            await _ruleEngine.InitializeAsync();

            var evt = new EventEntity
            {
                Id = 1,
                EventTime = DateTime.UtcNow,
                User = "alice",
                Host = "WORKSTATION-01",
                Process = "powershell.exe",
                DetailsJson = "{\"command_line\": \"powershell.exe -NoP -W Hidden -Enc aGVsbG8=\"}",
                Message = "PowerShell execution detected"
            };

            // Act
            var result = await _ruleEngine.EvaluateEventAsync(evt);

            // Assert
            Assert.True(result);
            _mockAlertService.Verify(x => x.CreateAlertAsync(
                It.IsAny<RuleEntity>(),
                It.Is<IEnumerable<EventEntity>>(events => events.Count() == 1),
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task EvaluateEvent_EventDoesNotMatchRule_DoesNotTriggerAlert()
        {
            // Arrange
            var rule = new RuleDefinition
            {
                Name = "Test Rule",
                Selection = new SelectionCriteria { EventId = 9999 },
                Action = new ActionCriteria { Alert = true }
            };

            _mockRuleProvider.Setup(x => x.LoadRulesAsync())
                .ReturnsAsync(new List<RuleDefinition> { rule });

            await _ruleEngine.InitializeAsync();

            var evt = new EventEntity
            {
                Id = 1,
                EventId = 1000, // Different event ID
                EventTime = DateTime.UtcNow
            };

            // Act
            var result = await _ruleEngine.EvaluateEventAsync(evt);

            // Assert
            Assert.False(result);
            _mockAlertService.Verify(x => x.CreateAlertAsync(
                It.IsAny<RuleEntity>(),
                It.IsAny<IEnumerable<EventEntity>>(),
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Never);
        }
    }
}
