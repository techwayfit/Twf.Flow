using Microsoft.Extensions.Logging;
using Twf.Flow.Core;
using Twf.Flow.Core.ValueObjects;
using Twf.Flow.Nodes.AI;
using Twf.Flow.Nodes.Control;
using Twf.Flow.Nodes.Data;

namespace twf_ai_framework.console.examples;

/// <summary>
/// ═══════════════════════════════════════════════════════════════════════════
/// EXAMPLE 1: Multi-Turn Customer Support Chatbot
/// ═══════════════════════════════════════════════════════════════════════════
///
/// Pipeline:
///   Input Validation
///     ↓
///   Safety/Guardrail Check (LLM)
///     ├─ SAFE  → Sentiment Analysis (LLM) → Route by sentiment
///     │           ├─ ANGRY    → Escalation Response (LLM)
///     │           └─ NEUTRAL  → Standard Response (LLM)
///     └─ UNSAFE → Rejection Response (static)
///
/// Demonstrates: Chaining, branching, conversation memory, retry, logging
/// </summary>
public static class CustomerSupportChatbot
{
    public static async Task RunAsync(string apiKey)
    {
        Console.WriteLine("\n╔══════════════════════════════════════════╗");
        Console.WriteLine("  Example 1: Customer Support Chatbot");
        Console.WriteLine("╚══════════════════════════════════════════╝\n");

        using var logFactory = LoggerFactory.Create(b =>
            b.AddConsole().SetMinimumLevel(LogLevel.Information));
        var logger = logFactory.CreateLogger("ChatbotExample");

        // ─── Shared context (persists conversation history) ───────────────────
        var context = new WorkflowContext("CustomerSupportBot", logger);
        context.SetState("company_name", "TechFlow Inc.");
        context.SetState("support_tier", "standard");

        // ─── Define the LLM config once, reuse across nodes ──────────────────
        var llm = LlmConfig.OpenAI(apiKey);

        // ─── Build the workflow ───────────────────────────────────────────────
        var workflow = Workflow.Create("CustomerSupportBot")
            .UseLogger(logger)
            // 1. Validate input
            .AddNode(new FilterNode("ValidateInput")
            .RequireNonEmpty("user_message")
            .MaxLength("user_message", 2000))

            // 2. Log checkpoint
            .AddNode(LogNode.Keys("InputReceived", "user_message"))
            // 3. Safety check — detect harmful input
            .AddNode(new PromptBuilderNode(
                name: "SafetyCheckPrompt",
                promptTemplate: """Classify if this customer message is safe to respond to. Message: "{{user_message}}" Respond ONLY with JSON: {"is_safe": true/false, "reason": "brief reason"}""",
                systemTemplate: "You are a content safety classifier. Be concise."
            ))
            .AddNode(new LlmNode("SafetyChecker", llm with { MaxTokens = TokenCount.FromValue(100) }),
                NodeOptions.WithRetry(2))
            .AddNode(OutputParserNode.WithMapping("SafetyParser",
            ("is_safe", "is_safe"),
            ("reason", "safety_reason")))
            // 4. Branch: safe vs unsafe
            .Branch(data => data.Get<bool>("is_safe"),
            trueBranch: safe => safe
                    // 5a. Sentiment analysis
                    .AddNode(new PromptBuilderNode(
                        name: "SentimentAnalyzer",
                        promptTemplate: "Analyze the sentiment: \"{{user_message}}\". " +
                        "JSON: {\"sentiment\": \"positive|neutral|negative|angry\", \"score\": 1-10}"))
                    .AddNode(new LlmNode("SentimentAnalyzer", llm with { MaxTokens = TokenCount.FromValue(100) }))
                    .AddNode(OutputParserNode.WithMapping("SentimentParser",
                        ("sentiment", "sentiment"),
                        ("score", "anger_score")))
                      // 6. Branch by sentiment
                    .Branch( data => data.GetString("sentiment") == "angry" || data.Get<int>("anger_score") >= 7,
                        trueBranch: angry => angry
                                    .AddNode(new PromptBuilderNode(
                                        name: "EscalationPrompt",
                                        promptTemplate: @"
                                            Customer is angry. Be empathetic and offer concrete help.
                                            Company: {{company_name}}
                                            Message: {{user_message}}
                                            Respond empathetically and offer to escalate if needed.
                                            ",
                                        systemTemplate: "You are an empathetic senior support agent."
                                    ))
                                    .AddNode(new LlmNode("EscalationResponder", llm with
                                    {
                                        MaintainHistory = true
                                    }))
                                    .AddStep("TagEscalation", (data, _) =>
                                    Task.FromResult(data.Set("response_type", "escalation"))),

                      falseBranch: normal => normal
                       .AddNode(new PromptBuilderNode(
                           name: "StandardResponsePrompt",
                           promptTemplate: @"
                                            Help this customer professionally.
                                            Company: {{company_name}}
                                            Message: {{user_message}}
                                            ",
                           systemTemplate: "You are a helpful support agent for {{company_name}}."
                            ))
                       .AddNode(new LlmNode("StandardResponder", llm with
                                   {
                                       MaintainHistory = true
                                   }))
                       .AddStep("TagStandard", (data, _) =>
                               Task.FromResult(data.Set("response_type", "standard")))
                           ),

             falseBranch: unsafeBranch => unsafeBranch
                .AddStep("RejectUnsafe", (data, _) => 
                    Task.FromResult(
                        data.Set("llm_response",
                        "I'm unable to process that request. Please contact support@company.com.")
                        .Set("response_type", "rejected")))
            )

        // 7. Save to memory for next turn
        .AddNode(MemoryNode.Write("last_response_type"))
            .AddNode(LogNode.Keys("FinalOutput", "llm_response", "response_type", "sentiment"));

        // ─── Simulate a multi-turn conversation ───────────────────────────────
        var conversations = new[]
        {
            "Hi, I need help with my account login",
            "I've been waiting 3 days and NOBODY has helped me! This is unacceptable!!!",
            "Thanks for the quick response, that solved my problem"
        };

        foreach (var message in conversations)
        {
            Console.WriteLine($"\n👤 Customer: {message}");

            var input = WorkflowData.From("user_message", message)
                .Set("company_name", "TechFlow Inc.");

            var result = await workflow.RunAsync(input, context);

            if (result.IsSuccess)
            {
                var response = result.Data.GetString("llm_response") ?? "No response";
                var type = result.Data.GetString("response_type") ?? "unknown";
                Console.WriteLine($"🤖 Bot [{type}]: {response}");
            }
            else
            {
                Console.WriteLine($"❌ Error: {result.ErrorMessage}");
            }
        }

        Console.WriteLine("\n📊 Final Execution Report:");
        Console.WriteLine(context.Tracker.GenerateReport("CustomerSupportBot", context.RunId).ToTable());
    }
}
