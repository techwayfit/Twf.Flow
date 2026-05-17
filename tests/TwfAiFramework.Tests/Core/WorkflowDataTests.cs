namespace Twf.Flow.Tests.Core;

/// <summary>
/// Tests for WorkflowData - the dynamic data packet that flows between nodes.
/// Covers type-safe access, cloning, merging, serialization, and history tracking.
/// </summary>
public class WorkflowDataTests
{
    [Fact]
    public void Set_And_Get_Should_Store_And_Retrieve_Values()
    {
      // Arrange
        var data = new WorkflowData();

    // Act
      data.Set("name", "Alice")
            .Set("age", 30)
     .Set("isActive", true);

        // Assert
     data.Get<string>("name").Should().Be("Alice");
   data.Get<int>("age").Should().Be(30);
        data.Get<bool>("isActive").Should().BeTrue();
    }

    [Fact]
    public void Get_Should_Be_Case_Insensitive()
    {
        // Arrange
        var data = new WorkflowData().Set("UserName", "Bob");

        // Assert
        data.Get<string>("username").Should().Be("Bob");
        data.Get<string>("USERNAME").Should().Be("Bob");
        data.Get<string>("UserName").Should().Be("Bob");
    }

    [Fact]
    public void Get_Should_Return_Default_When_Key_Not_Found()
    {
     // Arrange
    var data = new WorkflowData();

        // Assert
        data.Get<string>("nonexistent").Should().BeNull();
        data.Get<int>("nonexistent").Should().Be(0);
        data.Get<bool>("nonexistent").Should().BeFalse();
    }

  [Fact]
    public void GetRequired_Should_Throw_When_Key_Not_Found()
    {
     // Arrange
        var data = new WorkflowData();

  // Act & Assert
        var act = () => data.GetRequired<string>("nonexistent");
        act.Should().Throw<KeyNotFoundException>()
            .WithMessage("*Required key 'nonexistent' not found*");
    }

    [Fact]
    public void GetRequired_Should_Return_Value_When_Key_Exists()
    {
        // Arrange
     var data = new WorkflowData().Set("key", "value");

        // Act
        var result = data.GetRequired<string>("key");

        // Assert
        result.Should().Be("value");
    }

    [Fact]
    public void Has_Should_Return_True_When_Key_Exists_And_Not_Null()
    {
        // Arrange
     var data = new WorkflowData()
            .Set("existing", "value")
 .Set<string?>("nullValue", null);

    // Assert
      data.Has("existing").Should().BeTrue();
        data.Has("nullValue").Should().BeFalse();
        data.Has("nonexistent").Should().BeFalse();
  }

    [Fact]
    public void TryGet_Should_Return_True_And_Value_When_Key_Exists()
    {
        // Arrange
        var data = new WorkflowData().Set("key", 42);

    // Act
        var success = data.TryGet<int>("key", out var value);

     // Assert
        success.Should().BeTrue();
   value.Should().Be(42);
    }

    [Fact]
    public void TryGet_Should_Return_False_When_Key_Not_Found()
    {
        // Arrange
        var data = new WorkflowData();

        // Act
        var success = data.TryGet<string>("key", out var value);

        // Assert
        success.Should().BeFalse();
      value.Should().BeNull();
    }

    [Fact]
    public void Clone_Should_Create_Independent_Copy()
    {
        // Arrange
        var original = new WorkflowData()
            .Set("name", "Alice")
       .Set("age", 30);

        // Act
 var clone = original.Clone();
      clone.Set("name", "Bob");
        clone.Set("age", 25);

        // Assert
        original.Get<string>("name").Should().Be("Alice");
        original.Get<int>("age").Should().Be(30);
        clone.Get<string>("name").Should().Be("Bob");
        clone.Get<int>("age").Should().Be(25);
    }

    [Fact]
    public void Merge_Should_Combine_Two_WorkflowData_Objects()
    {
     // Arrange
  var data1 = new WorkflowData()
     .Set("name", "Alice")
 .Set("age", 30);

        var data2 = new WorkflowData()
   .Set("age", 25)
            .Set("city", "New York");

      // Act
        data1.Merge(data2);

        // Assert
        data1.Get<string>("name").Should().Be("Alice");
        data1.Get<int>("age").Should().Be(25); // Overwritten
        data1.Get<string>("city").Should().Be("New York");
    }

    [Fact]
    public void SetMany_Should_Add_Multiple_Values()
    {
        // Arrange
var data = new WorkflowData();
   var values = new Dictionary<string, object?>
        {
            ["name"] = "Alice",
        ["age"] = 30,
          ["isActive"] = true
        };

 // Act
    data.SetMany(values);

   // Assert
        data.Get<string>("name").Should().Be("Alice");
        data.Get<int>("age").Should().Be(30);
   data.Get<bool>("isActive").Should().BeTrue();
    }

    [Fact]
  public void Remove_Should_Delete_Key()
    {
        // Arrange
     var data = new WorkflowData()
            .Set("key1", "value1")
            .Set("key2", "value2");

        // Act
  data.Remove("key1");

      // Assert
        data.Has("key1").Should().BeFalse();
        data.Has("key2").Should().BeTrue();
    }

  [Fact]
    public void From_Should_Create_WorkflowData_With_Initial_Value()
    {
        // Act
        var data = WorkflowData.From("name", "Alice");

        // Assert
        data.Get<string>("name").Should().Be("Alice");
    }

    [Fact]
    public void FromDictionary_Should_Create_WorkflowData_From_Dictionary()
    {
        // Arrange
      var dict = new Dictionary<string, object?>
        {
         ["name"] = "Alice",
  ["age"] = 30
    };

        // Act
 var data = WorkflowData.FromDictionary(dict);

        // Assert
      data.Get<string>("name").Should().Be("Alice");
      data.Get<int>("age").Should().Be(30);
    }

    [Fact]
  public void Keys_Should_Return_All_Keys()
    {
        // Arrange
        var data = new WorkflowData()
     .Set("key1", "value1")
     .Set("key2", "value2")
        .Set("key3", "value3");

        // Act
        var keys = data.Keys;

        // Assert
        keys.Should().HaveCount(3);
        keys.Should().Contain("key1");
        keys.Should().Contain("key2");
        keys.Should().Contain("key3");
    }

  [Fact]
    public void ToJson_Should_Serialize_To_JSON()
    {
    // Arrange
        var data = new WorkflowData()
     .Set("name", "Alice")
            .Set("age", 30);

// Act
        var json = data.ToJson();

        // Assert
        json.Should().Contain("\"name\"");
      json.Should().Contain("\"Alice\"");
    json.Should().Contain("\"age\"");
        json.Should().Contain("30");
    }

    [Fact]
    public void FromJson_Should_Deserialize_From_JSON()
    {
        // Arrange
        var json = """{"name":"Alice","age":30}""";

        // Act
        var data = WorkflowData.FromJson(json);

        // Assert
        data.Get<string>("name").Should().Be("Alice");
        data.Get<int>("age").Should().Be(30);
    }

    [Fact]
    public void WriteHistory_Should_Track_Set_Operations()
    {
  // Arrange
        var data = new WorkflowData();

        // Act
        data.Set("key1", "value1")
            .Set("key2", "value2")
            .Set("key1", "updated"); // Overwrite key1

        // Assert
        data.WriteHistory.Should().HaveCount(3);
        data.WriteHistory[0].Should().Be("key1");
        data.WriteHistory[1].Should().Be("key2");
        data.WriteHistory[2].Should().Be("key1");
    }

    [Fact]
    public void ToString_Should_Return_Readable_Summary()
    {
     // Arrange
        var data = new WorkflowData()
            .Set("key1", "value1")
   .Set("key2", "value2");

        // Act
    var str = data.ToString();

        // Assert
        str.Should().Contain("WorkflowData");
        str.Should().Contain("2 keys");
   str.Should().Contain("key1");
        str.Should().Contain("key2");
 }
}
