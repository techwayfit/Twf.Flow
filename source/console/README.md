# Twf.Flow Console Application

This console application demonstrates the capabilities of the Twf.Flow through a menu-driven interface.

## Structure

The console project is organized into:

### `/concepts` - Framework Fundamentals
Individual files demonstrating core framework concepts:

1. **WorkflowDataFluentApi.cs** - Shows the fluent API for creating and manipulating workflow data
2. **NodeChainingAndBranching.cs** - Demonstrates sequential execution, transformations, conditions, and branching
3. **ParallelExecution.cs** - Shows how to run multiple nodes concurrently and merge results
4. **LoopForEach.cs** - Demonstrates iterating over collections and processing items
5. **ErrorHandlingAndRetry.cs** - Shows retry mechanisms and error recovery strategies

### `/examples` - Complete AI Workflows
Full-featured examples that require AI API keys:

1. **CustomerSupportChatbot.cs** - Multi-turn chatbot with sentiment analysis and escalation logic
2. **RagDocumentQA.cs** - RAG (Retrieval Augmented Generation) pipeline with document ingestion and Q&A
3. **ContentGenerationPipeline.cs** - Autonomous multi-format content generation with parallel execution

### Program.cs
The main entry point provides an interactive menu system that allows users to:
- Select and run any concept or example
- Input API keys when needed (or use environment variables)
- Navigate easily between different demonstrations

## Running the Application

```bash
cd source/console
dotnet run
```

You'll see a menu with all available options:

```
+-- |    Twf.Flow - Demo Console Application            ?
+-- 
+-- |    MAIN MENU        ?
+-- ?  CONCEPTS - Framework Fundamentals   ?
+-- ?  1. WorkflowData Fluent API            ?
?  2. Node Chaining & Branching   ?
?  3. Parallel Execution ?
?  4. Loop (ForEach)        ?
?  5. Error Handling & Retry         ?
+-- ?  EXAMPLES - Complete AI Workflows           ?
+-- ?  6. Customer Support Chatbot         ?
?  7. RAG Document Q&A Pipeline         ?
?  8. Content Generation Pipeline    ?
+-- ?  Q. Quit   ?
+-- ```

## API Keys

For examples 6-8, you can provide API keys in two ways:

1. **Interactive prompt**: Enter your API key when prompted
2. **Environment variable**: Set `AI_API_KEY` before running
   ```bash
   # Windows (PowerShell)
   $env:AI_API_KEY="your-api-key-here"
   
   # Linux/Mac
   export AI_API_KEY="your-api-key-here"
   ```

## Features

- **No code modification needed**: Run any example without editing files
- **Interactive menu**: Easy navigation between concepts and examples
- **Clear separation**: Concepts vs. complete examples
- **Reusable components**: Each concept/example is self-contained
- **API key management**: Flexible API key input options

## Adding New Concepts or Examples

### To add a new concept:

1. Create a new file in `/concepts` folder
2. Implement a static `RunAsync()` method
3. Add a menu item in `Program.cs` in the `ExecuteChoice()` method

### To add a new example:

1. Create a new file in `/examples` folder
2. Implement a static `RunAsync(string apiKey)` method
3. Add a menu item in `Program.cs` in the `ExecuteChoice()` method
