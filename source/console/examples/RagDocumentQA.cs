using Microsoft.Extensions.Logging;
using Twf.Flow.Core;
using Twf.Flow.Core.ValueObjects;
using Twf.Flow.Nodes.AI;
using Twf.Flow.Nodes.Control;
using Twf.Flow.Nodes.Data;

namespace twf_ai_framework.console.examples;

/// <summary>
/// ═══════════════════════════════════════════════════════════════════════════
/// EXAMPLE 2: RAG (Retrieval Augmented Generation) Document Q&A Pipeline
/// ═══════════════════════════════════════════════════════════════════════════
///
/// Ingestion Pipeline:
///   ReadFile → ChunkText → GenerateEmbeddings → StoreInVectorDB
///
/// Query Pipeline:
///   ValidateQuery → EmbedQuery → RetrieveChunks → BuildRAGPrompt
///       → LLM (with citations) → ParseAnswer → OutputAnswer
///
/// Demonstrates: Multi-stage pipeline, chunking, embeddings,
///               in-memory vector search, RAG prompt construction
/// </summary>
public static class RagDocumentQA
{
    // Simple in-memory vector store for demo purposes
    private static readonly List<(string Text, float[] Embedding, string Source, int ChunkIndex)>
        _vectorStore = new();

    public static async Task RunAsync(string apiKey)
    {
        Console.WriteLine("\n╔══════════════════════════════════════════╗");
        Console.WriteLine("  Example 2: RAG Document Q&A Pipeline");
        Console.WriteLine("╚══════════════════════════════════════════╝\n");

        using var logFactory = LoggerFactory.Create(b =>
            b.AddConsole().SetMinimumLevel(LogLevel.Information));
        var logger = logFactory.CreateLogger("RAGExample");

        var embeddingConfig = EmbeddingConfig.OpenAI(apiKey);
        var llmConfig = LlmConfig.Anthropic(apiKey);

        // ─── PHASE 1: INGESTION PIPELINE ─────────────────────────────────────
        Console.WriteLine("📥 Phase 1: Ingesting documents...\n");

        // Simulate document content (in production, read from files/DB)
        var documents = new[]
        {
            new { Source = "product_guide.txt", Content = GetSampleDocument() }
        };

        var ingestionWorkflow = Workflow.Create("DocumentIngestion")
            .UseLogger(logger)

            // 1. Chunk the document into overlapping pieces
            .AddNode(new ChunkTextNode(new ChunkConfig
            {
                ChunkSize = ChunkSize.FromValue(300),
                Overlap = ChunkOverlap.FromValue(50),
                Strategy = ChunkStrategy.Sentence
            }))

            // 2. Generate embeddings for each chunk (loop)
            .ForEach(
                itemsKey: "chunks",
                outputKey: "embedded_chunks",
                bodyBuilder: loop => loop
                    .AddStep("PrepareChunkText", (data, _) =>
                    {
                        var chunk = data.Get<TextChunk>("__loop_item__")!;
                        return Task.FromResult(data
                            .Set("text", chunk.Text)
                            .Set("chunk_source", chunk.Source)
                            .Set("chunk_index", chunk.Index));
                    })
                    .AddNode(new EmbeddingNode("ChunkEmbedder", embeddingConfig),
                        NodeOptions.WithRetry(2, TimeSpan.FromSeconds(2))
                                   .AndTimeout(TimeSpan.FromSeconds(15)))
                    .AddNode(new DelayNode(TimeSpan.FromMilliseconds(100), "Rate limit"))
            )

            // 3. Store in our vector store
            .AddStep("StoreEmbeddings", (data, ctx) =>
            {
                var embeddedChunks = data.Get<List<WorkflowData>>("embedded_chunks")
                                     ?? new List<WorkflowData>();

                foreach (var chunkData in embeddedChunks)
                {
                    var text = chunkData.GetString("text") ?? "";
                    var embedding = chunkData.Get<float[]>("embedding") ?? Array.Empty<float>();
                    var source = chunkData.GetString("chunk_source") ?? "unknown";
                    var index = chunkData.Get<int>("chunk_index");

                    if (embedding.Length > 0)
                        _vectorStore.Add((text, embedding, source, index));
                }

                ctx.Logger.LogInformation(
                    "📦 Stored {Count} chunks in vector store", _vectorStore.Count);

                return Task.FromResult(data.Set("indexed_count", _vectorStore.Count));
            });

        foreach (var doc in documents)
        {
            var docData = WorkflowData.From("text", doc.Content)
                .Set("source", doc.Source);

            var ingestionResult = await ingestionWorkflow.RunAsync(docData);
            if (ingestionResult.IsSuccess)
                Console.WriteLine($"✅ Ingested '{doc.Source}' → " +
                    $"{ingestionResult.Data.Get<int>("indexed_count")} chunks indexed\n");
        }

        // ─── PHASE 2: QUERY PIPELINE ──────────────────────────────────────────
        Console.WriteLine("🔍 Phase 2: Answering questions via RAG...\n");

        var queryWorkflow = Workflow.Create("RAGQuery")
            .UseLogger(logger)

            // 1. Validate query
            .AddNode(new FilterNode("ValidateQuery")
                .RequireNonEmpty("query")
                .MaxLength("query", 500))

            // 2. Embed the query
            .AddStep("PrepareQueryForEmbedding", (data, _) =>
                Task.FromResult(data.Set("text", data.GetString("query"))))
            .AddNode(new EmbeddingNode("QueryEmbedder", embeddingConfig),
                NodeOptions.WithRetry(2))

            // 3. Semantic search — find top-K relevant chunks
            .AddStep("SemanticSearch", (data, ctx) =>
            {
                var queryEmbedding = data.Get<float[]>("embedding")!;
                var topK = 3;

                // Score all chunks by cosine similarity
                var scored = _vectorStore
                    .Select(item => new
                    {
                        item.Text,
                        item.Source,
                        item.ChunkIndex,
                        Score = EmbeddingNode.CosineSimilarity(queryEmbedding, item.Embedding)
                    })
                    .OrderByDescending(x => x.Score)
                    .Take(topK)
                    .ToList();

                ctx.Logger.LogInformation(
                    "🎯 Retrieved {Count} relevant chunks (top score: {Score:F3})",
                    scored.Count, scored.FirstOrDefault()?.Score ?? 0);

                // Format context for LLM
                var contextText = string.Join("\n\n---\n\n",
                    scored.Select((s, i) =>
                        $"[Source {i + 1}: {s.Source}, chunk {s.ChunkIndex}]\n{s.Text}"));

                return Task.FromResult(data
                    .Set("retrieved_context", contextText)
                    .Set("retrieved_chunks", scored.Select(s => s.Text).ToList())
                    .Set("top_score", scored.FirstOrDefault()?.Score ?? 0f));
            })

            // 4. Check relevance threshold
            .Branch(
                data => data.Get<float>("top_score") > 0.5f,
                trueBranch: relevant => relevant
                    // 5. Build RAG prompt with context
                    .AddNode(new PromptBuilderNode("RAGPromptBuilder",
                        promptTemplate: """
                            Answer the user's question using ONLY the provided context.
                            If the answer is not in the context, say so clearly.

                            CONTEXT:
                            {{retrieved_context}}

                            QUESTION: {{query}}

                            Provide a clear, accurate answer with specific citations like [Source 1], [Source 2].
                            """,
                        systemTemplate: """
                            You are a precise document Q&A assistant.
                            Answer only based on provided context. Never hallucinate.
                            If unsure, say "The provided documents don't contain enough information."
                            """
                    ))

                    // 6. Call LLM
                    .AddNode(new LlmNode("RAGAnswerer", llmConfig with
          {
            MaxTokens = TokenCount.FromValue(500),
       Temperature = Temperature.Deterministic  // Low temp for factual Q&A
       }), NodeOptions.WithRetry(2).AndTimeout(TimeSpan.FromSeconds(30)))

                    // 7. Add metadata
                    .AddStep("EnrichResponse", (data, _) =>
                        Task.FromResult(data
                            .Set("answer", data.GetString("llm_response"))
                            .Set("confidence", data.Get<float>("top_score"))
                            .Set("answer_type", "rag_grounded"))),

                falseBranch: irrelevant => irrelevant
                    .AddStep("NoContextFound", (data, _) =>
                        Task.FromResult(data
                            .Set("answer",
                                "I couldn't find relevant information in the documents to answer this question.")
                            .Set("answer_type", "no_context")))
            )

            .AddNode(LogNode.Keys("QueryResult", "answer", "answer_type", "top_score"));

        // ─── Run example queries ──────────────────────────────────────────────
        var queries = new[]
        {
            "What are the main features of the product?",
            "How do I configure the authentication settings?",
            "What is the meaning of life?"  // Off-topic — should get no_context
        };

        foreach (var query in queries)
        {
            Console.WriteLine($"\n❓ Query: {query}");

            var queryData = WorkflowData.From("query", query);
            var result = await queryWorkflow.RunAsync(queryData);

            if (result.IsSuccess)
            {
                var answer = result.Data.GetString("answer") ?? "No answer";
                var answerType = result.Data.GetString("answer_type") ?? "unknown";
                var score = result.Data.Get<float>("top_score");
                Console.WriteLine($"📋 [{answerType}] (relevance: {score:F3})");
                Console.WriteLine($"💬 {answer}");
            }
            else
            {
                Console.WriteLine($"❌ Query failed: {result.ErrorMessage}");
            }
        }

        Console.WriteLine("\n" + queryWorkflow
            .GetType().Name); // Print class name as separator
    }

    private static string GetSampleDocument() => """
        FlowForge Product Guide

        Overview
        FlowForge is a powerful workflow automation library for .NET applications.
        It enables developers to build composable, chainable AI pipelines with built-in
        tracking, logging, and error handling.

        Core Features
        The library provides a fluent builder API for constructing workflows.
        Each workflow consists of nodes that are executed sequentially.
        Nodes can be branched conditionally, run in parallel, or looped.
        All executions are tracked with timing and status information.

        Authentication Configuration
        To configure authentication, set your API key in the LlmConfig object.
        Use LlmConfig.OpenAI(apiKey) for OpenAI or LlmConfig.Anthropic(apiKey) for Claude.
        The API key is transmitted securely via Bearer token authentication.
        Never hardcode API keys in source code; use environment variables instead.

        Retry and Error Handling
        Every node supports configurable retry with exponential backoff.
        Use NodeOptions.WithRetry(maxRetries, delay) to configure retry behavior.
        Set ContinueOnError = true to allow the pipeline to continue on failure.
        Timeouts can be set per-node using NodeOptions.WithTimeout(timespan).

        Supported AI Providers
        FlowForge supports OpenAI GPT-4o, Anthropic Claude, Ollama local models,
        and any OpenAI-compatible API endpoint. Switch providers by changing LlmConfig.
        """;
}
