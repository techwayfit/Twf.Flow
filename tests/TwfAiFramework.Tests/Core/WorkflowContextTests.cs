using twf_ai_framework.Core.Models;
using Twf.Flow.Core.Extensions;

namespace Twf.Flow.Tests.Core;

/// <summary>
/// Tests for WorkflowContext - the execution context shared across all nodes.
/// Covers logging, tracking, state management, and chat history (via extensions).
/// </summary>
public class WorkflowContextTests
{
    [Fact]
    public void Constructor_Should_Initialize_With_Unique_RunId()
    {
      // Act
        var context1 = new WorkflowContext("Workflow1", NullLogger.Instance);
        var context2 = new WorkflowContext("Workflow1", NullLogger.Instance);

        // Assert
     context1.RunId.Should().NotBeNullOrEmpty();
        context2.RunId.Should().NotBeNullOrEmpty();
        context1.RunId.Should().NotBe(context2.RunId);
    }

    [Fact]
   public void Constructor_Should_Set_WorkflowName()
    {
  // Act
     var context = new WorkflowContext("MyWorkflow", NullLogger.Instance);

    // Assert
context.WorkflowName.Should().Be("MyWorkflow");
    }

    [Fact]
    public void Constructor_Should_Set_StartedAt()
{
        // Arrange
   var before = DateTime.UtcNow;

  // Act
      var context = new WorkflowContext("Test", NullLogger.Instance);

        // Assert
   context.StartedAt.Should().BeOnOrAfter(before);
        context.StartedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public void State_Set_And_Get_Should_Store_And_Retrieve_Values()
 {
   // Arrange
     var context = new WorkflowContext("Test", NullLogger.Instance);

    // Act
        context.State.Set("key1", "value1");
        context.State.Set("key2", 42);

       // Assert
   context.State.Get<string>("key1").Should().Be("value1");
        context.State.Get<int>("key2").Should().Be(42);
    }

    [Fact]
    public void State_Get_Should_Return_Default_When_Key_Not_Found()
    {
        // Arrange
        var context = new WorkflowContext("Test", NullLogger.Instance);

        // Assert
        context.State.Get<string>("nonexistent").Should().BeNull();
        context.State.Get<int>("nonexistent").Should().Be(0);
    }

[Fact]
public void State_Has_Should_Return_True_When_Key_Exists()
    {
        // Arrange
        var context = new WorkflowContext("Test", NullLogger.Instance);
   context.State.Set("existing", "value");

 // Assert
     context.State.Has("existing").Should().BeTrue();
        context.State.Has("nonexistent").Should().BeFalse();
 }

    [Fact]
  public void State_AppendMessage_Extension_Should_Add_To_Chat_History()
    {
// Arrange
     var context = new WorkflowContext("Test", NullLogger.Instance);

 // Act
        context.State.AppendMessage(ChatMessage.User("Hello"));
        context.State.AppendMessage(ChatMessage.Assistant("Hi there!"));

        // Assert
  var history = context.State.GetChatHistory();
        history.Should().HaveCount(2);
        history[0].Role.Should().Be("user");
     history[0].Content.Should().Be("Hello");
      history[1].Role.Should().Be("assistant");
        history[1].Content.Should().Be("Hi there!");
    }

    [Fact]
  public void State_GetChatHistory_Extension_Should_Return_Empty_List_Initially()
    {
   // Arrange
        var context = new WorkflowContext("Test", NullLogger.Instance);

  // Act
        var history = context.State.GetChatHistory();

        // Assert
        history.Should().BeEmpty();
    }

   [Fact]
    public void State_ClearChatHistory_Extension_Should_Remove_All_Messages()
    {
 // Arrange
       var context = new WorkflowContext("Test", NullLogger.Instance);
     context.State.AppendMessage(ChatMessage.User("Message 1"));
    context.State.AppendMessage(ChatMessage.User("Message 2"));

        // Act
        context.State.ClearChatHistory();

        // Assert
    context.State.GetChatHistory().Should().BeEmpty();
    }

    [Fact]
    public void ChatMessage_System_Should_Create_System_Message()
    {
        // Act
        var message = ChatMessage.System("System instruction");

        // Assert
        message.Role.Should().Be("system");
message.Content.Should().Be("System instruction");
        message.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

 [Fact]
 public void ChatMessage_User_Should_Create_User_Message()
    {
        // Act
     var message = ChatMessage.User("User question");

  // Assert
     message.Role.Should().Be("user");
 message.Content.Should().Be("User question");
  }

    [Fact]
    public void ChatMessage_Assistant_Should_Create_Assistant_Message()
{
  // Act
        var message = ChatMessage.Assistant("Assistant response");

// Assert
    message.Role.Should().Be("assistant");
        message.Content.Should().Be("Assistant response");
    }

    [Fact]
    public void CancellationToken_Should_Default_To_None()
    {
        // Arrange & Act
        var context = new WorkflowContext("Test", NullLogger.Instance);

        // Assert
        context.CancellationToken.Should().Be(CancellationToken.None);
    }

    [Fact]
    public void CancellationToken_Should_Be_Passed_Through()
    {
        // Arrange
     var cts = new CancellationTokenSource();

   // Act
    var context = new WorkflowContext("Test", NullLogger.Instance, cancellationToken: cts.Token);

        // Assert
        context.CancellationToken.Should().Be(cts.Token);
    }

    [Fact]
    public void Tracker_Should_Be_Initialized()
    {
// Arrange & Act
        var context = new WorkflowContext("Test", NullLogger.Instance);

        // Assert
        context.Tracker.Should().NotBeNull();
    }

    [Fact]
    public void State_Should_NotBeNull()
    {
        // Arrange & Act
        var context = new WorkflowContext("Test", NullLogger.Instance);

       // Assert
   context.State.Should().NotBeNull();
    }

    [Fact]
public void State_Clear_Should_Remove_All_Entries()
    {
        // Arrange
        var context = new WorkflowContext("Test", NullLogger.Instance);
     context.State.Set("key1", "value1");
   context.State.Set("key2", "value2");

      // Act
        context.State.Clear();

        // Assert
        context.State.Has("key1").Should().BeFalse();
     context.State.Has("key2").Should().BeFalse();
    }

    [Fact]
    public void State_Remove_Should_Delete_Specific_Key()
    {
        // Arrange
      var context = new WorkflowContext("Test", NullLogger.Instance);
        context.State.Set("key1", "value1");
        context.State.Set("key2", "value2");

   // Act
  context.State.Remove("key1");

     // Assert
 context.State.Has("key1").Should().BeFalse();
      context.State.Has("key2").Should().BeTrue();
    }

   [Fact]
    public void State_GetAll_Should_Return_Snapshot()
    {
      // Arrange
   var context = new WorkflowContext("Test", NullLogger.Instance);
context.State.Set("key1", "value1");
        context.State.Set("key2", 42);

        // Act
      var all = context.State.GetAll();

    // Assert
     all.Should().HaveCount(2);
        all["key1"].Should().Be("value1");
        all["key2"].Should().Be(42);
    }

    // ??? Backward Compatibility Tests (Deprecated Methods) ???????????????????

    [Fact]
    [Obsolete("Testing deprecated method")]
    public void Deprecated_SetState_Should_Still_Work()
  {
        // Arrange
        var context = new WorkflowContext("Test", NullLogger.Instance);

        // Act
#pragma warning disable CS0618 // Type or member is obsolete
        context.SetState("key", "value");
#pragma warning restore CS0618

        // Assert
    context.State.Get<string>("key").Should().Be("value");
    }

    [Fact]
    [Obsolete("Testing deprecated method")]
    public void Deprecated_AppendMessage_Should_Still_Work()
    {
        // Arrange
        var context = new WorkflowContext("Test", NullLogger.Instance);

 // Act
#pragma warning disable CS0618
        context.AppendMessage(ChatMessage.User("Test"));
#pragma warning restore CS0618

    // Assert
        context.State.GetChatHistory().Should().HaveCount(1);
    }
}
