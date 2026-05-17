using Microsoft.Extensions.Logging;
using Twf.Flow.Core;
using Twf.Flow.Core.ValueObjects;
using Twf.Flow.Nodes;
using Twf.Flow.Nodes.AI;
using Twf.Flow.Nodes.Control;
using Twf.Flow.Nodes.Data;

namespace twf_ai_framework.console.examples;

/// <summary>
/// ═══════════════════════════════════════════════════════════════════════════
/// EXAMPLE 3: Autonomous Multi-Format Content Generation Pipeline
/// ═══════════════════════════════════════════════════════════════════════════
///
/// Pipeline:
///   TopicInput → Research (web/data) → OutlineGeneration
///       → [PARALLEL] BlogPost | TweetThread | LinkedInPost | EmailNewsletter
///       → SEO Optimization → Quality Review → FileOutput
///
/// Demonstrates: Parallel execution, multi-step chaining, content transformation,
///               quality gating, file output, token tracking
/// </summary>
public static class ContentGenerationPipeline
{
    public static async Task RunAsync(string apiKey)
    {
        Console.WriteLine("\n╔══════════════════════════════════════════╗");
        Console.WriteLine("  Example 3: Content Generation Pipeline");
        Console.WriteLine("╚══════════════════════════════════════════╝\n");

        using var logFactory = LoggerFactory.Create(b =>
            b.AddConsole().SetMinimumLevel(LogLevel.Information));
        var logger = logFactory.CreateLogger("ContentPipeline");

        var llm = LlmConfig.Anthropic(apiKey);
        var fastLlm = llm with { MaxTokens = TokenCount.FromValue(300), Temperature = Temperature.FromValue(0.8f) };
        var writingLlm = llm with { MaxTokens = TokenCount.FromValue(1500), Temperature = Temperature.FromValue(0.75f) };

        // ─── Token usage tracker ──────────────────────────────────────────────
        var totalTokens = 0;

        var workflow = Workflow.Create("ContentGenerator")
            .UseLogger(logger)

            // ── Step 1: Validate inputs ─────────────────────────────────────────
            .AddNode(new FilterNode("ValidateInputs")
                .RequireNonEmpty("topic")
                .RequireNonEmpty("target_audience")
                .RequireNonEmpty("brand_voice"))

            // ── Step 2: Research phase — simulate web research ─────────────────
            .AddStep("ResearchSimulator", (data, ctx) =>
            {
                // In production, this would call a search API
                var topic = data.GetString("topic")!;
                ctx.Logger.LogInformation("🔍 Researching: {Topic}", topic);

                var researchData = $"""
                    Key findings on "{topic}":
                    1. Market size growing at 35% YoY
                    2. Top 3 use cases: automation, personalization, efficiency
                    3. Main challenges: data quality, model hallucination, cost
                    4. Leading companies: OpenAI, Anthropic, Google, Microsoft
                    5. Developer adoption up 180% in last 12 months
                    6. Average ROI: 3-5x within first year of deployment
                    """;

                return Task.FromResult(data
                    .Set("research_data", researchData)
                    .Set("research_timestamp", DateTime.UtcNow.ToString("O")));
            })

            // ── Step 3: Generate content outline ──────────────────────────────
            .AddNode(new PromptBuilderNode(
                name: "OutlineBuilder",
                promptTemplate: """
                    Create a detailed content outline for:
                    Topic: {{topic}}
                    Target audience: {{target_audience}}
                    Brand voice: {{brand_voice}}
                    
                    Research data:
                    {{research_data}}
                    
                    Return JSON:
                    {
                      "title": "engaging title",
                      "hook": "opening hook sentence",
                      "key_points": ["point1", "point2", "point3", "point4, "point5"],
                      "cta": "call to action",
                      "keywords": ["kw1", "kw2", "kw3"]
                    }
                    """,
                systemTemplate: "You are an expert content strategist. Return only valid JSON."
            ))
            .AddNode(new LlmNode("OutlineGenerator", fastLlm),
                NodeOptions.WithRetry(2).AndTimeout(TimeSpan.FromSeconds(20)))
            .AddNode(new OutputParserNode("OutlineParser", new Dictionary<string, string>
            {
                ["title"] = "content_title",
                ["hook"] = "content_hook",
                ["key_points"] = "key_points",
                ["cta"] = "content_cta",
                ["keywords"] = "seo_keywords"
            }, strict: false))
            .AddNode(LogNode.Keys("OutlineReady", "content_title", "seo_keywords"))

            // ── Step 4: PARALLEL content generation for all formats ────────────
            .Parallel(

                // 4a. Long-form blog post
                new LambdaMultiStepNode("BlogPostWriter", async (input, ctx) =>
                {
                    var prompt = BuildBlogPrompt(input);
                    input.Set("prompt", prompt)
                         .Set("system_prompt", "You are a senior tech blogger. Write engaging, " +
                             "detailed content with proper H2/H3 structure using Markdown.");

                    var llmNode = new LlmNode("BlogLLM", writingLlm);
                    var result = await llmNode.ExecuteAsync(input, ctx);

                    return result.IsSuccess
                        ? result.Data.Set("blog_post", result.Data.GetString("llm_response"))
                        : throw new Exception(result.ErrorMessage);
                }),

                // 4b. Twitter/X thread
                new LambdaMultiStepNode("TweetThreadWriter", async (input, ctx) =>
                {
                    input.Set("prompt", $"""
                        Write a 5-tweet thread about: {input.GetString("content_title")}
                        
                        Key points: {string.Join(", ", input.Get<List<object>>("key_points") ?? new())}
                        Audience: {input.GetString("target_audience")}
                        Voice: {input.GetString("brand_voice")}
                        
                        Format: "1/5 [tweet]\n\n2/5 [tweet]..." etc. Max 280 chars per tweet.
                        End with a CTA: {input.GetString("content_cta")}
                        """)
                         .Set("system_prompt", "You are a viral Twitter/X content creator. " +
                             "Write punchy, engaging tweets that drive engagement.");

                    var llmNode = new LlmNode("TweetLLM", fastLlm);
                    var result = await llmNode.ExecuteAsync(input, ctx);

                    return result.IsSuccess
                        ? result.Data.Set("tweet_thread", result.Data.GetString("llm_response"))
                        : throw new Exception(result.ErrorMessage);
                }),

                // 4c. LinkedIn post
                new LambdaMultiStepNode("LinkedInWriter", async (input, ctx) =>
                {
                    input.Set("prompt", $"""
                        Write a professional LinkedIn post about: {input.GetString("content_title")}
                        
                        Hook: {input.GetString("content_hook")}
                        Key insights: {string.Join(", ", input.Get<List<object>>("key_points") ?? new())}
                        CTA: {input.GetString("content_cta")}
                        
                        Use line breaks for readability. Include 3-5 relevant hashtags at the end.
                        Target: {input.GetString("target_audience")}
                        """)
                         .Set("system_prompt", "You are a LinkedIn thought leader. Write " +
                             "professional posts that establish authority and drive engagement.");

                    var llmNode = new LlmNode("LinkedInLLM", fastLlm with { MaxTokens = TokenCount.FromValue(600) });
                    var result = await llmNode.ExecuteAsync(input, ctx);

                    return result.IsSuccess
                        ? result.Data.Set("linkedin_post", result.Data.GetString("llm_response"))
                        : throw new Exception(result.ErrorMessage);
                }),

                // 4d. Email newsletter section
                new LambdaMultiStepNode("EmailWriter", async (input, ctx) =>
                {
                    input.Set("prompt", $@"
    Write an email newsletter section about: {input.GetString("content_title")}

      Subject line: craft a compelling subject
    Preview text: 50-char teaser
      Body: 150-200 words with the key insights
  CTA button text: compelling action words

  Return JSON: {{""subject"": ""..."", ""preview"": ""..."", ""body"": ""..."", ""cta_button"": ""...""}}
         Audience: {input.GetString("target_audience")}
          ")
         .Set("system_prompt", "You are an email marketing expert. Write " +
   "high-converting newsletter content.");

                    var llmNode = new LlmNode("EmailLLM", fastLlm with { MaxTokens = TokenCount.FromValue(400) });
                    var result = await llmNode.ExecuteAsync(input, ctx);

                    if (!result.IsSuccess) throw new Exception(result.ErrorMessage);

                    // Parse email components
                    var parser = new OutputParserNode("EmailParser", new Dictionary<string, string>
                    {
      ["subject"] = "email_subject",
        ["preview"] = "email_preview",
["body"] = "email_body",
     ["cta_button"] = "email_cta"
   });
   var parseResult = await parser.ExecuteAsync(result.Data, ctx);
return parseResult.IsSuccess ? parseResult.Data : result.Data;
                })
            )

            // ── Step 5: Token usage aggregation ──────────────────────────────
            .AddStep("AggregateMetrics", (data, ctx) =>
            {
                var tokens = data.Get<int>("prompt_tokens") + data.Get<int>("completion_tokens");
                totalTokens += tokens;
                ctx.Logger.LogInformation("📊 Cumulative tokens used: {Total}", totalTokens);
                return Task.FromResult(data.Set("total_tokens_used", totalTokens));
            })

            // ── Step 6: SEO optimization check ───────────────────────────────
            .AddNode(new PromptBuilderNode(
                name: "SEOAnalysisPrompt",
                promptTemplate: """
                    Review this blog post for SEO quality:
                    Title: {{content_title}}
                    Keywords to include: {{seo_keywords}}
                    
                    Blog post preview (first 500 chars):
                    {{blog_post}}
                    
                    Return JSON: {
                      "seo_score": 1-10,
                      "keyword_density": "percentage",
                      "readability": "easy|medium|hard",
                      "improvements": ["suggestion1", "suggestion2"]
                    }
                    """,
                systemTemplate: "You are an SEO expert. Evaluate content quality concisely."
            ))
            .AddNode(new LlmNode("SEOReviewer", fastLlm),
                NodeOptions.WithRetry(2).AndContinueOnError()) // Non-blocking
      .AddNode(new OutputParserNode("SEOParser", new Dictionary<string, string>
  {
["seo_score"] = "seo_score",
  ["readability"] = "readability_level",
      ["improvements"] = "seo_improvements"
    }))

            // ── Step 7: Quality gate — check SEO score ────────────────────────
            .AddNode(new ConditionNode("QualityGate",
                ("passes_quality", data => data.Get<int>("seo_score") >= 7 ||
                                           !data.Has("seo_score"))))
            .AddNode(LogNode.Keys("QualityCheck", "seo_score", "readability_level", "passes_quality"))

            // ── Step 8: Assemble final content package ───────────────────────
            .AddStep("AssemblePackage", (data, _) =>
            {
                var package = new
                {
                    title = data.GetString("content_title"),
                    generated_at = DateTime.UtcNow,
                    topic = data.GetString("topic"),
                    blog_post = data.GetString("blog_post"),
                    tweet_thread = data.GetString("tweet_thread"),
                    linkedin_post = data.GetString("linkedin_post"),
                    email = new
                    {
                        subject = data.GetString("email_subject"),
                        preview = data.GetString("email_preview"),
                        body = data.GetString("email_body"),
                        cta = data.GetString("email_cta")
                    },
                    seo = new
                    {
                        score = data.Get<int>("seo_score"),
                        readability = data.GetString("readability_level"),
                        keywords = data.Get<List<object>>("seo_keywords")
                    },
                    metrics = new
                    {
                        total_tokens = data.Get<int>("total_tokens_used")
                    }
                };

                return Task.FromResult(data.Set("content_package", package));
            })

            // ── Step 9: Output report ────────────────────────────────────────
            .OnComplete(result =>
            {
                Console.WriteLine("\n✅ Content Generation Complete!\n");
                var data = result.Data;

                Console.WriteLine($"📰 Title: {data.GetString("content_title")}");
                Console.WriteLine($"🐦 Tweet Thread:\n{data.GetString("tweet_thread")}");
                Console.WriteLine($"\n💼 LinkedIn:\n{data.GetString("linkedin_post")?[..Math.Min(300, data.GetString("linkedin_post")?.Length ?? 0)]}...");
                Console.WriteLine($"\n📧 Email Subject: {data.GetString("email_subject")}");
                Console.WriteLine($"📊 SEO Score: {data.Get<int>("seo_score")}/10 | " +
                    $"Readability: {data.GetString("readability_level")}");
                Console.WriteLine($"🔢 Total tokens: {data.Get<int>("total_tokens_used")}");
                Console.WriteLine(result.Report.ToTable());
            });

        // ─── Run the pipeline ─────────────────────────────────────────────────
        var initialData = new WorkflowData()
            .Set("topic", "Generative AI in Enterprise Software Development")
            .Set("target_audience", "Senior software engineers and CTOs")
            .Set("brand_voice", "authoritative, practical, innovation-forward");

        Console.WriteLine($"🎯 Topic: {initialData.GetString("topic")}");
        Console.WriteLine($"👥 Audience: {initialData.GetString("target_audience")}\n");

        var result = await workflow.RunAsync(initialData);

        if (result.IsFailure)
            Console.WriteLine($"\n❌ Pipeline failed: {result.ErrorMessage}");
    }

    private static string BuildBlogPrompt(WorkflowData data)
    {
        var keyPoints = data.Get<List<object>>("key_points");
        var pointsText = keyPoints is not null
            ? string.Join("\n", keyPoints.Select((p, i) => $"{i + 1}. {p}"))
            : "See research data";

        return $"""
            Write a comprehensive blog post:
            
            Title: {data.GetString("content_title")}
            Hook: {data.GetString("content_hook")}
            Target: {data.GetString("target_audience")}
            Voice: {data.GetString("brand_voice")}
            
            Key points to cover:
            {pointsText}
            
            Research data:
            {data.GetString("research_data")}
            
            Structure: Hook → Introduction → 5 main sections with H2 headers → Conclusion → CTA
            SEO keywords to include naturally: {string.Join(", ", data.Get<List<object>>("seo_keywords") ?? new())}
            CTA: {data.GetString("content_cta")}
            
            Write in Markdown format. Aim for ~800 words.
            """;
    }
}

// ─── Helper: Multi-step node from a lambda ────────────────────────────────────

/// <summary>
/// Allows defining multi-step logic as a single named node using a lambda.
/// Used in Parallel() blocks where each parallel branch needs multiple steps.
/// </summary>
public sealed class LambdaMultiStepNode : BaseNode
{
    private readonly Func<WorkflowData, WorkflowContext, Task<WorkflowData>> _logic;

    public override string Name { get;}
    public override string Category => "Custom";
    public override string Description => $"Multi-step lambda node: {Name}";

    public LambdaMultiStepNode(string name,
        Func<WorkflowData, WorkflowContext, Task<WorkflowData>> logic)
    {
        Name = name;
        _logic = logic;
    }

    public async Task<NodeResult> ExecuteAsync(WorkflowData data, WorkflowContext context)
    {
        var start = DateTime.UtcNow;
        try
        {
            var output = await _logic(data.Clone(), context);
            return NodeResult.Success(Name, output, DateTime.UtcNow - start, start);
        }
        catch (Exception ex)
        {
            return NodeResult.Failure(Name, data, ex.Message, ex, DateTime.UtcNow - start, start);
        }
    }

    protected override Task<WorkflowData> RunAsync(WorkflowData input, WorkflowContext context, NodeExecutionContext nodeCtx)
    {
        throw new NotImplementedException();
    }
}
