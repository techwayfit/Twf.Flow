using Microsoft.Extensions.Logging;
using Twf.Flow.Core;
using Twf.Flow.Nodes.Control;
using Twf.Flow.Nodes.Data;

namespace twf_ai_framework.console.node_examples;

/// <summary>
/// Examples for all Control-category nodes:
/// - BranchNode
/// - ConditionNode
/// - LoopNode
/// - TryCatchNode
/// - DelayNode
/// - LogNode
/// - MergeNode
/// - ErrorRouteNode
/// </summary>
public static class ControlNodeExamples
{
    public static async Task RunAllExamples()
    {
        Console.WriteLine("\n╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║      CONTROL NODE EXAMPLES                                 ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝\n");

        await BranchNodeExample();
        await ConditionNodeExample();
        await LoopNodeExample();
        await TryCatchNodeExample();
        await DelayNodeExample();
        await LogNodeExample();
        await MergeNodeExample();
        await ErrorRouteNodeExample();
    }

    /// <summary>
    /// BranchNode: Route workflow based on value matching (switch/case pattern)
    /// Use Case: Route requests by type, status, category
    /// </summary>
    private static async Task BranchNodeExample()
    {
        Console.WriteLine("\n─── 1. BranchNode Example ────────────────────────────────────");
        Console.WriteLine("Use Case: Route orders by status (pending/approved/rejected)\n");

        using var logFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
        var logger = logFactory.CreateLogger("BranchExample");

        // Define branch workflows
        var approvedFlow = Workflow.Create("ApprovedHandler")
 .AddStep("ProcessApproved", (data, ctx) =>
        {
            ctx.Logger.LogInformation("✓ Processing approved order: {OrderId}", data.GetString("order_id"));
            return Task.FromResult(data.Set("message", "Order approved and shipped"));
        });

        var pendingFlow = Workflow.Create("PendingHandler")
            .AddStep("ProcessPending", (data, ctx) =>
            {
                ctx.Logger.LogInformation("⏳ Order pending review: {OrderId}", data.GetString("order_id"));
                return Task.FromResult(data.Set("message", "Order awaiting approval"));
            });

        var rejectedFlow = Workflow.Create("RejectedHandler")
        .AddStep("ProcessRejected", (data, ctx) =>
            {
                ctx.Logger.LogInformation("✗ Order rejected: {OrderId}", data.GetString("order_id"));
                return Task.FromResult(data.Set("message", "Order rejected - refund initiated"));
            });

        var defaultFlow = Workflow.Create("DefaultHandler")
       .AddStep("ProcessUnknown", (data, ctx) =>
     {
         ctx.Logger.LogWarning("⚠️  Unknown status: {Status}", data.GetString("status"));
         return Task.FromResult(data.Set("message", "Unknown status - manual review required"));
     });

        var workflow = Workflow.Create("OrderRouter")
         .UseLogger(logger)
     .AddNode(new BranchNode(
      name: "RouteByStatus",
                valueKey: "status",
                new KeyValuePair<string, Workflow>("approved", approvedFlow),
                new KeyValuePair<string, Workflow>("pending", pendingFlow),
     new KeyValuePair<string, Workflow>("rejected", rejectedFlow),
           new KeyValuePair<string, Workflow>("default", defaultFlow)
   ))
        .OnComplete(result =>
            {
                Console.WriteLine($"✓ Route taken: {result.Data.GetString("branch_route")}");
                Console.WriteLine($"✓ Message: {result.Data.GetString("message")}");
            });

        // Test different statuses
        var testCases = new[] { "approved", "pending", "rejected", "unknown" };
        foreach (var status in testCases)
        {
            Console.WriteLine($"\n  Testing status: {status}");
            var input = new WorkflowData()
                .Set("order_id", $"ORD-{Random.Shared.Next(1000, 9999)}")
               .Set("status", status);
            await workflow.RunAsync(input);
        }
    }

    /// <summary>
    /// ConditionNode: Evaluate conditions and write boolean flags
    /// Use Case: Conditional routing, validation checks
    /// </summary>
    private static async Task ConditionNodeExample()
    {
        Console.WriteLine("\n─── 2. ConditionNode Example ─────────────────────────────────");
        Console.WriteLine("Use Case: Check if user qualifies for discount\n");

        using var logFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
        var logger = logFactory.CreateLogger("ConditionExample");

        var workflow = Workflow.Create("DiscountEligibility")
  .UseLogger(logger)
    .AddNode(new ConditionNode("CheckEligibility",
         ("is_premium", data => data.GetString("tier") == "premium"),
     ("is_loyal", data => data.Get<int>("years_member") >= 3),
                ("high_value", data => data.Get<decimal>("total_spent") >= 1000m),
    ("qualifies_discount", data =>
    (data.GetString("tier") == "premium" && data.Get<int>("years_member") >= 1) ||
       data.Get<decimal>("total_spent") >= 500m)
       ))
 .AddStep("ApplyDiscount", (data, ctx) =>
            {
                var qualifies = data.Get<bool>("qualifies_discount");
                var discount = qualifies ? "20%" : "0%";
                ctx.Logger.LogInformation("Discount: {Discount}", discount);
                return Task.FromResult(data.Set("discount_rate", discount));
            })
      .OnComplete(result =>
            {
                Console.WriteLine($"✓ Premium: {result.Data.Get<bool>("is_premium")}");
                Console.WriteLine($"✓ Loyal: {result.Data.Get<bool>("is_loyal")}");
                Console.WriteLine($"✓ High Value: {result.Data.Get<bool>("high_value")}");
                Console.WriteLine($"✓ Qualifies: {result.Data.Get<bool>("qualifies_discount")}");
                Console.WriteLine($"✓ Discount: {result.Data.GetString("discount_rate")}");
            });

        var input = new WorkflowData()
      .Set("tier", "premium")
            .Set("years_member", 5)
    .Set("total_spent", 2500m);

        await workflow.RunAsync(input);
    }

    /// <summary>
    /// LoopNode: Iterate over collections (ForEach pattern)
    /// Use Case: Process lists of items, batch operations
    /// </summary>
    private static async Task LoopNodeExample()
    {
        Console.WriteLine("\n─── 3. LoopNode Example ──────────────────────────────────────");
        Console.WriteLine("Use Case: Process a batch of customer emails\n");

        using var logFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
        var logger = logFactory.CreateLogger("LoopExample");

        var workflow = Workflow.Create("EmailProcessor")
        .UseLogger(logger)
       .AddNode(new LoopNode(
       name: "ProcessEmails",
    itemsKey: "emails",
      outputKey: "processed_emails",
        loopItemKey: "__email__",
        bodyBuilder: loop => loop
                .AddStep("ValidateEmail", (data, ctx) =>
  {
      var email = data.GetString("__email__") ?? "";
      var isValid = email.Contains("@") && email.Contains(".");
      ctx.Logger.LogInformation("Validating: {Email} -> {Valid}", email, isValid);
      return Task.FromResult(data
          .Set("email", email)
       .Set("is_valid", isValid)
      .Set("domain", email.Split('@').LastOrDefault() ?? ""));
  })
     ))
  .OnComplete(result =>
     {
         var processed = result.Data.Get<List<WorkflowData>>("processed_emails") ?? new();
         Console.WriteLine($"\n✓ Processed {processed.Count} emails:");
         foreach (var item in processed)
         {
             Console.WriteLine($"  - {item.GetString("email")}: {(item.Get<bool>("is_valid") ? "✓" : "✗")} (domain: {item.GetString("domain")})");
         }
     });

        var input = new WorkflowData()
          .Set("emails", new List<string>
         {
     "john@example.com",
            "invalid-email",
    "jane@company.org",
              "test@domain.net"
      });

        await workflow.RunAsync(input);
    }

    /// <summary>
    /// TryCatchNode: Error handling with fallback workflows
    /// Use Case: Graceful degradation, retry logic, error recovery
    /// </summary>
    private static async Task TryCatchNodeExample()
    {
        Console.WriteLine("\n─── 4. TryCatchNode Example ──────────────────────────────────");
        Console.WriteLine("Use Case: Call external API with fallback on failure\n");

        using var logFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
        var logger = logFactory.CreateLogger("TryCatchExample");

        var workflow = Workflow.Create("ResilientAPI")
  .UseLogger(logger)
   .AddNode(new TryCatchNode(
     name: "CallAPI",
tryBuilder: w => w
              .AddStep("PrimaryAPI", (data, ctx) =>
      {
          // Simulate API failure
          ctx.Logger.LogWarning("Primary API failed!");
          throw new Exception("API timeout - service unavailable");
      }),
      catchBuilder: w => w
            .AddStep("FallbackCache", (data, ctx) =>
    {
        ctx.Logger.LogInformation("Using cached data as fallback");
        var errorMsg = data.GetString("caught_error_message") ?? "";
        return Task.FromResult(data
.Set("response", "Cached data from 1 hour ago")
.Set("source", "cache")
  .Set("original_error", errorMsg));
    })
    ))
         .OnComplete(result =>
       {
           Console.WriteLine($"✓ Route: {result.Data.GetString("try_catch_route")}");
           Console.WriteLine($"✓ Response: {result.Data.GetString("response")}");
           Console.WriteLine($"✓ Source: {result.Data.GetString("source")}");
           if (result.Data.Has("original_error"))
               Console.WriteLine($"✓ Recovered from: {result.Data.GetString("original_error")}");
       });

        await workflow.RunAsync(new WorkflowData().Set("user_id", "123"));
    }

    /// <summary>
    /// DelayNode: Introduce pauses in workflows
    /// Use Case: Rate limiting, waiting for async processes
    /// </summary>
    private static async Task DelayNodeExample()
    {
        Console.WriteLine("\n─── 5. DelayNode Example ─────────────────────────────────────");
        Console.WriteLine("Use Case: Rate limit API calls to 2 per second\n");

        using var logFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
        var logger = logFactory.CreateLogger("DelayExample");

        var workflow = Workflow.Create("RateLimitedAPI")
     .UseLogger(logger)
            .AddStep("APICall1", (data, ctx) =>
         {
             ctx.Logger.LogInformation("⚡ API Call 1 at {Time}", DateTime.Now.ToString("HH:mm:ss.fff"));
             return Task.FromResult(data);
         })
            .AddNode(DelayNode.Milliseconds(500, "Rate limit"))
            .AddStep("APICall2", (data, ctx) =>
            {
                ctx.Logger.LogInformation("⚡ API Call 2 at {Time}", DateTime.Now.ToString("HH:mm:ss.fff"));
                return Task.FromResult(data);
            })
         .AddNode(DelayNode.Milliseconds(500, "Rate limit"))
            .AddStep("APICall3", (data, ctx) =>
            {
                ctx.Logger.LogInformation("⚡ API Call 3 at {Time}", DateTime.Now.ToString("HH:mm:ss.fff"));
                return Task.FromResult(data);
            })
    .OnComplete(result =>
            {
                Console.WriteLine("✓ All API calls completed with rate limiting");
            });

        await workflow.RunAsync(new WorkflowData());
    }

    /// <summary>
    /// LogNode: Explicit logging checkpoints
    /// Use Case: Debugging, monitoring, audit trails
    /// </summary>
    private static async Task LogNodeExample()
    {
        Console.WriteLine("\n─── 6. LogNode Example ───────────────────────────────────────");
        Console.WriteLine("Use Case: Add logging checkpoints for debugging\n");

        using var logFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
        var logger = logFactory.CreateLogger("LogExample");

        var workflow = Workflow.Create("DebuggableWorkflow")
      .UseLogger(logger)
            .AddStep("ProcessInput", (data, ctx) =>
            {
                return Task.FromResult(data
                  .Set("user_id", "U-12345")
                .Set("action", "purchase")
               .Set("amount", 99.99m));
            })
        .AddNode(LogNode.Keys("AfterInput", "user_id", "action", "amount"))
            .AddStep("CalculateTotal", (data, ctx) =>
         {
             var amount = data.Get<decimal>("amount");
             var tax = amount * 0.08m;
             var total = amount + tax;
             return Task.FromResult(data
         .Set("tax", tax)
        .Set("total", total));
         })
   .AddNode(LogNode.All("BeforeFinalize"))
        .OnComplete(result =>
{
    Console.WriteLine($"\n✓ Final total: ${result.Data.Get<decimal>("total"):F2}");
});

        await workflow.RunAsync(new WorkflowData());
    }

    /// <summary>
    /// MergeNode: Combine multiple data keys into one
    /// Use Case: Concatenate parallel results, combine strings
    /// </summary>
    private static async Task MergeNodeExample()
    {
        Console.WriteLine("\n─── 7. MergeNode Example ─────────────────────────────────────");
        Console.WriteLine("Use Case: Combine multiple report sections into final document\n");

        using var logFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
        var logger = logFactory.CreateLogger("MergeExample");

        var workflow = Workflow.Create("ReportGenerator")
 .UseLogger(logger)
            .AddStep("GenerateSections", (data, ctx) =>
            {
                return Task.FromResult(data
                   .Set("executive_summary", "Q4 showed 25% growth in revenue...")
               .Set("financial_data", "Total revenue: $5.2M, Expenses: $3.1M...")
              .Set("recommendations", "1. Expand to new markets\n2. Increase R&D budget..."));
            })
       .AddNode(new MergeNode(
  name: "CombineReport",
       outputKey: "full_report",
             separator: "\n\n==========\n\n",
      "executive_summary", "financial_data", "recommendations"
            ))
    .OnComplete(result =>
            {
                Console.WriteLine("✓ Full Report Generated:\n");
                Console.WriteLine(result.Data.GetString("full_report"));
            });

        await workflow.RunAsync(new WorkflowData());
    }

    /// <summary>
    /// ErrorRouteNode: Route by error indicators (error message, HTTP status)
    /// Use Case: Handle API responses with success/error paths
    /// </summary>
    private static async Task ErrorRouteNodeExample()
    {
        Console.WriteLine("\n─── 8. ErrorRouteNode Example ────────────────────────────────");
        Console.WriteLine("Use Case: Route HTTP responses by status code\n");

        using var logFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
        var logger = logFactory.CreateLogger("ErrorRouteExample");

        var workflow = Workflow.Create("APIResponseHandler")
 .UseLogger(logger)
            .AddNode(new ErrorRouteNode(
      name: "CheckResponse",
             statusCodeKey: "http_status_code",
       errorStatusThreshold: 400
      ))
    .AddStep("HandleRoute", (data, ctx) =>
         {
             var route = data.GetString("error_route");
             if (route == "success")
             {
                 ctx.Logger.LogInformation("✓ Request succeeded");
                 data.Set("message", "Data retrieved successfully");
             }
             else
             {
                 ctx.Logger.LogError("✗ Request failed: {Error}", data.GetString("routed_error_message"));
                 data.Set("message", "Error occurred - retry or contact support");
             }
             return Task.FromResult(data);
         })
            .OnComplete(result =>
            {
                Console.WriteLine($"✓ Route: {result.Data.GetString("error_route")}");
                Console.WriteLine($"✓ Message: {result.Data.GetString("message")}");
            });

        // Test success case
        Console.WriteLine("\n  Testing HTTP 200 (success):");
        await workflow.RunAsync(new WorkflowData().Set("http_status_code", 200));

        // Test error case
        Console.WriteLine("\n  Testing HTTP 500 (error):");
        await workflow.RunAsync(new WorkflowData().Set("http_status_code", 500));
    }
}
