using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LogSentinel.BUS.Interfaces;
using LogSentinel.BUS.Models;
using LogSentinel.DAL.Repositories;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace LogSentinel.BUS.Services
{
    public class RuleProvider : IRuleProvider
    {
        private readonly IRuleRepository _ruleRepository;
        private readonly IDeserializer _yamlDeserializer;

        public RuleProvider(IRuleRepository ruleRepository)
        {
            _ruleRepository = ruleRepository;
            _yamlDeserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();
        }

        public async Task<IEnumerable<RuleDefinition>> LoadRulesAsync()
        {
            var ruleEntities = await _ruleRepository.GetEnabledAsync();
            var rules = new List<RuleDefinition>();

            foreach (var entity in ruleEntities)
            {
                try
                {
                    var rule = _yamlDeserializer.Deserialize<RuleDefinition>(entity.YamlContent);
                    rules.Add(rule);
                }
                catch (Exception ex)
                {
                    // Log error and continue
                    Console.WriteLine($"Error parsing rule {entity.Name}: {ex.Message}");
                }
            }

            return rules;
        }

        public async Task<RuleDefinition?> GetRuleByNameAsync(string name)
        {
            var entity = await _ruleRepository.GetByNameAsync(name);
            if (entity == null) return null;

            try
            {
                return _yamlDeserializer.Deserialize<RuleDefinition>(entity.YamlContent);
            }
            catch
            {
                return null;
            }
        }

        public async Task SaveRuleAsync(RuleDefinition rule)
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            var yamlContent = serializer.Serialize(rule);

            var entity = await _ruleRepository.GetByNameAsync(rule.Name);
            if (entity != null)
            {
                entity.YamlContent = yamlContent;
                entity.Description = rule.Description;
                entity.Severity = rule.Severity;
                entity.IsEnabled = rule.Enabled;
                entity.UpdatedAt = DateTime.UtcNow;
                await _ruleRepository.UpdateAsync(entity);
            }
            else
            {
                var newEntity = new DAL.Data.RuleEntity
                {
                    Name = rule.Name,
                    Description = rule.Description,
                    Severity = rule.Severity,
                    IsEnabled = rule.Enabled,
                    YamlContent = yamlContent
                };
                await _ruleRepository.AddAsync(newEntity);
            }
        }
    }
}
