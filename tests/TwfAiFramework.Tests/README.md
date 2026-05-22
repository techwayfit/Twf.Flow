# Twf.Flow.Tests

Comprehensive xUnit test suite for the **Twf.Flow** - an n8n-inspired workflow automation library for .NET.

## What's Tested

This test project provides extensive coverage for all core framework components:

### ? Core Components

| Component | Test File | Coverage |
|-----------|-----------|----------|
| **WorkflowData** | `Core/WorkflowDataTests.cs` | Type-safe access, cloning, merging, serialization, history tracking |
| **NodeOptions** | `Core/NodeOptionsTests.cs` | Retry behavior, timeouts, conditional execution, error handling, fluent builders |
| **NodeResult** | `Core/NodeResultTests.cs` | Success/failure states, timing, metadata, callbacks, transformations |
| **WorkflowContext** | `Core/WorkflowContextTests.cs` | State management, services, chat history, cancellation |
| **BaseNode** | `Nodes/BaseNodeTests.cs` | Node execution, error handling, logging, metadata collection |
| **WorkflowBuilder** | `Core/WorkflowBuilderTests.cs` | Sequential execution, branching, parallel nodes, loops, error strategies |

## Test Categories

### Data Flow Tests
- Type-safe data access with `Get<T>()`, `Set<T>()`, `TryGet<T>()`
- Case-insensitive key lookups
- Data cloning and merging
- JSON serialization/deserialization
- Write history tracking

### Execution Control Tests
- Sequential node execution
- Conditional branching (`Branch`)
- Parallel execution (`Parallel`)
- Looping (`ForEach`)
- Error handling and fallback strategies
- Node retry with exponential backoff
- Timeout handling

### Configuration Tests
- Fluent builder patterns (`WithRetry`, `AndTimeout`, `AndContinueOnError`)
- Record-based immutability with `with` expressions
- Value-based equality for configuration objects

### Node Lifecycle Tests
- Execution timing and metadata
- Logging and diagnostics
- Cancellation token propagation
- Success/failure callbacks

## Running Tests

### Visual Studio
1. Open Test Explorer (Test ? Test Explorer)
2. Click "Run All" or right-click specific tests to run

### Command Line
```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~WorkflowDataTests"

# Run with verbose output
dotnet test --logger "console;verbosity=detailed"

# Generate code coverage
dotnet test --collect:"XPlat Code Coverage"
```

### VS Code
1. Install the ".NET Core Test Explorer" extension
2. Tests will appear in the Test Explorer sidebar
3. Click the play button to run tests

## Test Utilities

### TestNode Helper
The `TestNode` class provides convenient test doubles for unit testing:

```csharp
// Create a successful test node
var node = TestNode.Success("MyNode", "outputKey", "outputValue");

// Create a failing test node
var node = TestNode.Failure("FailNode", "Custom error message");

// Create a delayed test node (for timeout testing)
var node = TestNode.Delay("SlowNode", TimeSpan.FromSeconds(5));

// Create a transform node
var node = TestNode.Transform("TransformNode", "input", "output", 
    value => $"transformed_{value}");
```

## Test Coverage Goals

- ? **Core Framework**: 90%+ coverage
- ? **Node Execution**: 85%+ coverage
- ? **Workflow Builder**: 80%+ coverage
- **AI Nodes**: Integration tests (requires external services)
- **IO Nodes**: Integration tests (requires file system/HTTP)

## Dependencies

- **xUnit** - Test framework
- **FluentAssertions** - Readable assertion syntax
- **NSubstitute** - Mocking framework
- **Microsoft.Extensions.Logging** - Logging infrastructure

## Writing New Tests

### Test Naming Convention
```csharp
[Fact]
public void MethodName_Should_ExpectedBehavior_When_Condition()
{
    // Arrange
    var input = ...;
    
    // Act
    var result = ...;
 
    // Assert
    result.Should()...;
}
```

### Example Test
```csharp
[Fact]
public async Task Workflow_Should_Stop_On_First_Failure_By_Default()
{
    // Arrange
    var workflow = Workflow.Create("TestWorkflow")
        .AddNode(TestNode.Success("Node1"))
   .AddNode(TestNode.Failure("FailNode"))
        .AddNode(TestNode.Success("Node3"));

    // Act
 var result = await workflow.RunAsync();

    // Assert
    result.IsFailure.Should().BeTrue();
    result.FailedNodeName.Should().Be("FailNode");
    result.NodeResults.Should().HaveCount(2); // Node3 never executed
}
```

## Project Structure

```
tests/Twf.Flow.Tests/
+-- Core/
|   +-- WorkflowDataTests.cs
|   +-- NodeOptionsTests.cs
|   +-- NodeResultTests.cs
|   +-- WorkflowContextTests.cs
|   +-- WorkflowBuilderTests.cs
+-- Nodes/
|   +-- BaseNodeTests.cs
+-- GlobalUsings.cs
+-- Twf.Flow.Tests.csproj
+-- README.md (this file)
```

## CI/CD Integration

These tests are designed to run in CI/CD pipelines:

```yaml
# Example GitHub Actions workflow
- name: Run Tests
  run: dotnet test --logger trx --results-directory TestResults

- name: Publish Test Results
  uses: dorny/test-reporter@v1
  if: always()
  with:
    name: Test Results
    path: TestResults/*.trx
    reporter: dotnet-trx
```

## Additional Resources

- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions Documentation](https://fluentassertions.com/)
- [.NET Testing Best Practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)

## Contributing

When adding new features to Twf.Flow:
1. Write tests first (TDD approach)
2. Ensure all tests pass before PR
3. Maintain 80%+ code coverage
4. Follow existing test patterns
