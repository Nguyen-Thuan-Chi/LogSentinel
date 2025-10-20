# RuleEngineService Refactoring Summary

## Overview

The existing static `RuleEngine.cs` with hardcoded if-else blocks has been successfully replaced with a simplified dynamic `RuleEngineService.cs` that processes any active rule from the database using YAML-based key-value detection.

## What Was Replaced

### Old Implementation (RuleEngine.cs)
- Complex rule definitions with `RuleDefinition`, `SelectionCriteria`, `ConditionCriteria`
- Hardcoded if-else logic for specific rule types
- Pattern matching, regex evaluation, threshold counting
- Required specific rule format and structure

### New Implementation (RuleEngineService.cs)
- Simple YAML key-value format for detection
- Dynamic rule loading from database
- All active rules processed uniformly
- Simple AND conditions for all detection criteria

## Target YAML Format

The new rule engine supports this simplified format:

```yaml
# Example: notepad_execution.yml
name: Notepad Execution
description: Generates an alert when notepad.exe is started.
severity: Low
enabled: true

log_source:
  provider: 'Microsoft-Windows-Sysmon'
  event_id: 1

detection:
  Image: 'C:\Windows\System32\notepad.exe'
```

### Format Requirements

1. **log_source section**: Specifies the event source
   - `provider`: Event provider name (e.g., 'Microsoft-Windows-Sysmon')
   - `event_id`: Event ID number (e.g., 1)

2. **detection section**: Simple key-value pairs
   - All pairs must be true (AND condition)
   - Keys match fields in event's `DetailsJson`
   - Values use case-insensitive string comparison

## Implementation Details

### Core Methods

#### `ProcessEventAsync(EventEntity newEvent)`
- **Purpose**: Main entry point replacing old hardcoded logic
- **Process**:
  1. Load ALL active rules from database (`GetEnabledAsync()`)
  2. Loop through each rule
  3. Call `DoesEventMatchRule(newEvent, rule)` for each
  4. If match found, create alert and update rule statistics
- **Returns**: `true` if any alert was triggered

#### `DoesEventMatchRule(EventEntity event, RuleEntity rule)`
- **Purpose**: Core dynamic rule matching logic
- **Process**:
  1. Parse rule's YAML content using YamlDotNet
  2. Validate event's Provider and EventId against `log_source`
  3. Parse event's `DetailsJson` into `Dictionary<string, object>`
  4. Get `detection` section from YAML
  5. Loop through each key-value pair in detection
  6. Check if key exists in event data AND value matches (case-insensitive)
  7. Return `false` immediately if any condition fails
  8. Return `true` if all conditions match
- **Returns**: `true` if event matches all detection criteria

### Key Features

- **Dynamic Loading**: Rules loaded from database on each event
- **YAML Parsing**: Uses YamlDotNet for robust YAML processing
- **JSON Parsing**: Uses System.Text.Json for event details parsing
- **Case-Insensitive**: All string comparisons are case-insensitive
- **Error Handling**: Try-catch blocks prevent crashes during parsing
- **Logging**: Comprehensive logging for debugging and monitoring
- **Statistics**: Updates rule trigger count and last triggered time

## Sample Rules

Six sample rules have been added to the seed data:

1. **Notepad Execution**: Detects notepad.exe process creation
2. **Calculator Execution**: Detects calc.exe process creation  
3. **PowerShell Process Creation**: Detects PowerShell execution
4. **Command Prompt Execution**: Detects cmd.exe execution
5. **Network Connection to External IP**: Detects TCP connections
6. **Admin User Logon**: Detects administrator logons

## Files Modified

### New Files
- `LogSentinel.BUS/Services/RuleEngineService.cs` - New dynamic rule engine
- `LogSentinel.Tests/RuleEngineServiceTests.cs` - Comprehensive unit tests
- `sample-rules/notepad_execution.yml` - Example YAML rule
- `sample-rules/powershell_execution.yml` - Example multi-criteria rule

### Modified Files
- `Log Sentinel/App.xaml.cs` - Updated service registration
- `LogSentinel.DAL/Data/SeedData.cs` - Added simplified sample rules

## Testing

### Unit Tests
- `RuleEngineServiceTests.cs` includes comprehensive test coverage:
  - YAML parsing and rule matching
  - Provider and Event ID validation
  - Multiple detection criteria (AND conditions)
  - Case-insensitive matching
  - Negative test cases (wrong values, missing keys)

### Manual Testing
1. Build and run the application
2. Navigate to Rules view to see new simplified rules
3. Create test events matching rule criteria
4. Verify alerts are generated in Alerts view
5. Check rule trigger statistics in Rules view

## Benefits

### For Developers
- **Simplified**: No more complex rule definitions or hardcoded logic
- **Maintainable**: All rules use the same simple format
- **Extensible**: Easy to add new rules without code changes
- **Debuggable**: Clear logging and error handling

### For Users
- **Easy Rule Creation**: Simple YAML key-value format
- **Intuitive**: Detection criteria are straightforward
- **Flexible**: Any event field can be used for detection
- **Reliable**: Robust parsing and error handling

## Migration Notes

- Existing complex rules will need to be converted to the new format
- The old `RuleProvider` and `RuleDefinition` classes are no longer used
- All rules now come from the database `Rules` table
- The interface `IRuleEngine` is still implemented for compatibility

## Future Enhancements

Possible extensions to the simplified rule engine:

1. **OR Conditions**: Support for alternative detection criteria
2. **Regex Patterns**: Optional regex matching for values
3. **Threshold Rules**: Count-based rules for repeated events
4. **Time Windows**: Event correlation within time frames
5. **Field Operators**: Greater than, less than, contains operators

## Conclusion

The refactoring successfully replaces the hardcoded rule engine with a dynamic, YAML-based system that is easier to maintain and extend. The simplified key-value detection format makes rule creation accessible while maintaining the power and flexibility needed for security monitoring.