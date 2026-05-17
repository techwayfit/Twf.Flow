using twf_ai_framework.console.concepts;
using twf_ai_framework.console.examples;
using twf_ai_framework.console.node_examples;

namespace twf_ai_framework.console;

class Program
{
    static async Task Main(string[] args)
    {
       // SearchExample.RunAsync("31cc9b23c8dd747dd978b91fa5a856b8489b8c7f5bbaed6d49d04bd39d1db969", "What is Twf.Flow?").Wait();


        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║ Twf.Flow - Demo Console Application     ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝\n");

        while (true)
     {
            DisplayMenu();
         var choice = Console.ReadLine()?.Trim();

   if (choice?.ToLower() == "q")
            {
   Console.WriteLine("\n👋 Goodbye!");
                break;
       }

        try
     {
    await ExecuteChoice(choice);
            }
            catch (Exception ex)
            {
            Console.WriteLine($"\n❌ Error: {ex.Message}");
       Console.WriteLine($"{ex.GetType().Name}");
      }

            Console.WriteLine("\n" + new string('─', 60));
   Console.WriteLine("Press any key to continue...");
    Console.ReadKey();
   Console.Clear();
        }
    }

    static void DisplayMenu()
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║          MAIN MENU   ║");
        Console.WriteLine("╠════════════════════════════════════════════════════════════╣");
        Console.WriteLine("║  CONCEPTS - Framework Fundamentals         ║");
        Console.WriteLine("╠════════════════════════════════════════════════════════════╣");
        Console.WriteLine("║  1. WorkflowData Fluent API            ║");
        Console.WriteLine("║  2. Node Chaining & Branching       ║");
        Console.WriteLine("║  3. Parallel Execution    ║");
     Console.WriteLine("║  4. Loop (ForEach)        ║");
        Console.WriteLine("║  5. Error Handling & Retry    ║");
      Console.WriteLine("╠════════════════════════════════════════════════════════════╣");
        Console.WriteLine("║  EXAMPLES - Complete AI Workflows           ║");
        Console.WriteLine("╠════════════════════════════════════════════════════════════╣");
    Console.WriteLine("║  6. Customer Support Chatbot       ║");
        Console.WriteLine("║  7. RAG Document Q&A Pipeline           ║");
    Console.WriteLine("║  8. Content Generation Pipeline   ║");
        Console.WriteLine("╠════════════════════════════════════════════════════════════╣");
        Console.WriteLine("║  NODE EXAMPLES - Individual Node Use Cases    ║");
Console.WriteLine("╠════════════════════════════════════════════════════════════╣");
        Console.WriteLine("║  9. AI Nodes (LLM, Embedding, PromptBuilder, Parser)     ║");
   Console.WriteLine("║  10. Control Nodes (Branch, Loop, TryCatch, Delay, etc.)   ║");
        Console.WriteLine("║  11. Data Nodes (Chunk, Map, Filter, Transform, etc.)      ║");
        Console.WriteLine("║  12. I/O Nodes (HTTP, File Read/Write, Google Search)      ║");
        Console.WriteLine("╠════════════════════════════════════════════════════════════╣");
  Console.WriteLine("║  Q. Quit    ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
     Console.Write("\nEnter your choice: ");
    }

    static async Task ExecuteChoice(string? choice)
    {
        Console.Clear();

     switch (choice)
        {
    // Concepts
     case "1":
    await WorkflowDataFluentApi.RunAsync();
      break;

      case "2":
       await NodeChainingAndBranching.RunAsync();
    break;

       case "3":
     await ParallelExecution.RunAsync();
      break;

    case "4":
                await LoopForEach.RunAsync();
              break;

      case "5":
    await ErrorHandlingAndRetry.RunAsync();
      break;

            // Examples (require API key)
 case "6":
      var apiKey6 = GetApiKey("Anthropic");
         if (!string.IsNullOrEmpty(apiKey6))
   await CustomerSupportChatbot.RunAsync(apiKey6);
  break;

  case "7":
                var apiKey7 = GetApiKey("OpenAI (for embeddings) and Anthropic (for LLM)");
                if (!string.IsNullOrEmpty(apiKey7))
        await RagDocumentQA.RunAsync(apiKey7);
  break;

            case "8":
            var apiKey8 = GetApiKey("Anthropic");
      if (!string.IsNullOrEmpty(apiKey8))
       await ContentGenerationPipeline.RunAsync(apiKey8);
     break;

       // Node Examples
         case "9":
                var apiKey9 = GetApiKey("Anthropic/OpenAI for AI nodes");
     if (!string.IsNullOrEmpty(apiKey9))
await AINodeExamples.RunAllExamples(apiKey9);
      break;

   case "10":
       await ControlNodeExamples.RunAllExamples();
                break;

            case "11":
      await DataNodeExamples.RunAllExamples();
                break;

            case "12":
     Console.WriteLine("\n🔑 I/O Examples require optional API keys:");
        Console.WriteLine("   - HTTP examples work without keys");
    Console.WriteLine("- Google Search requires SerpApi key (https://serpapi.com)");
           Console.Write("\nEnter SerpApi key (or press Enter to skip Google Search): ");
 var serpApiKey = Console.ReadLine()?.Trim();
         await IONodeExamples.RunAllExamples(serpApiKey);
          break;

        default:
          Console.WriteLine("❌ Invalid choice. Please select a valid option.");
    break;
        }
    }

    static string? GetApiKey(string provider)
    {
     Console.WriteLine($"\n🔑 This example requires an API key for {provider}");
        Console.WriteLine("   You can:");
        Console.WriteLine("   1. Enter it now");
        Console.WriteLine("   2. Set environment variable: AI_API_KEY");
     Console.WriteLine("   3. Press Enter to skip");
        Console.Write("\nEnter API key (or press Enter to check env): ");

        var input = Console.ReadLine()?.Trim();

 if (!string.IsNullOrEmpty(input))
    return input;

        var envKey = Environment.GetEnvironmentVariable("AI_API_KEY");
        if (!string.IsNullOrEmpty(envKey))
        {
            Console.WriteLine("✅ Using API key from AI_API_KEY environment variable");
       return envKey;
        }

        Console.WriteLine("⚠️  No API key provided. Skipping this example.");
        return null;
    }
}