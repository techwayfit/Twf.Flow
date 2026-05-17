using System.Text.Json;
using Twf.Flow.Nodes.Data;

namespace Twf.Flow.Tests.Nodes;

public class DataMapperNodeTests
{
    [Fact]
    public async Task ExecuteAsync_Should_Map_Simple_Source_Key_To_Target_Key()
    {
        var node = new DataMapperNode(
            "MapSimple",
            new Dictionary<string, string> { ["prompt"] = "llm_response" });

        var input = new WorkflowData().Set("llm_response", "hello world");
        var context = new WorkflowContext("Test", NullLogger.Instance);

        var result = await node.ExecuteAsync(input, context);

        result.IsSuccess.Should().BeTrue();
        result.Data.Get<string>("prompt").Should().Be("hello world");
    }

    [Fact]
    public async Task ExecuteAsync_Should_Map_Nested_Dictionary_Path()
    {
        var node = new DataMapperNode(
            "MapNested",
            new Dictionary<string, string> { ["customer_id"] = "http_response.data.id" });

        var input = new WorkflowData()
            .Set("http_response", new Dictionary<string, object?>
            {
                ["data"] = new Dictionary<string, object?>
                {
                    ["id"] = "cust-123"
                }
            });

        var context = new WorkflowContext("Test", NullLogger.Instance);
        var result = await node.ExecuteAsync(input, context);

        result.IsSuccess.Should().BeTrue();
        result.Data.Get<string>("customer_id").Should().Be("cust-123");
    }

    [Fact]
    public async Task ExecuteAsync_Should_Map_JsonElement_Path_With_Array_Index()
    {
        var node = new DataMapperNode(
            "MapJson",
            new Dictionary<string, string> { ["first_name"] = "parsed_output.items.0.name" });

        var json = JsonSerializer.Deserialize<JsonElement>(
            "{\"items\":[{\"name\":\"alpha\"},{\"name\":\"beta\"}]}");

        var input = new WorkflowData().Set("parsed_output", json);
        var context = new WorkflowContext("Test", NullLogger.Instance);
        var result = await node.ExecuteAsync(input, context);

        result.IsSuccess.Should().BeTrue();
        result.Data.Get<string>("first_name").Should().Be("alpha");
    }

    [Fact]
    public async Task ExecuteAsync_Should_Use_Default_When_Source_Is_Missing()
    {
        var node = new DataMapperNode(
            "MapDefaults",
            new Dictionary<string, string> { ["system_prompt"] = "missing.path" },
            defaultValues: new Dictionary<string, object?> { ["system_prompt"] = "Use defaults" });

        var input = new WorkflowData();
        var context = new WorkflowContext("Test", NullLogger.Instance);
        var result = await node.ExecuteAsync(input, context);

        result.IsSuccess.Should().BeTrue();
        result.Data.Get<string>("system_prompt").Should().Be("Use defaults");
    }

    [Fact]
    public async Task ExecuteAsync_Should_Fail_When_ThrowOnMissing_And_Source_Is_Missing()
    {
        var node = new DataMapperNode(
            "MapStrict",
            new Dictionary<string, string> { ["target"] = "does.not.exist" },
            throwOnMissing: true);

        var input = new WorkflowData();
        var context = new WorkflowContext("Test", NullLogger.Instance);
        var result = await node.ExecuteAsync(input, context);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Missing mapping source");
    }

    [Fact]
    public async Task ExecuteAsync_Should_Output_Only_Mapped_Keys_When_RemoveUnmapped_Enabled()
    {
        var node = new DataMapperNode(
            "MapStrictOutput",
            new Dictionary<string, string> { ["renamed"] = "source_value" },
            removeUnmapped: true);

        var input = new WorkflowData()
            .Set("source_value", 42)
            .Set("other_key", "keep?");

        var context = new WorkflowContext("Test", NullLogger.Instance);
        var result = await node.ExecuteAsync(input, context);

        result.IsSuccess.Should().BeTrue();
        result.Data.Get<int>("renamed").Should().Be(42);
        result.Data.Has("source_value").Should().BeFalse();
        result.Data.Has("other_key").Should().BeFalse();
    }
}
