# Twf.Flow Documentation

Complete documentation for the Twf.Flow - a lightweight, node-based AI workflow automation library for .NET 10.

---

## Core Library Documentation

These documents are packaged with the NuGet package and located in `source/core/`:

### [README.md](source/core/README.md)
**Quick start and core concepts**
- Installation instructions
- Basic usage examples
- Core concepts (WorkflowData, WorkflowContext, INode)
- Built-in nodes overview
- Common patterns

### [ARCHITECTURE.md](source/core/ARCHITECTURE.md)
**System architecture and design patterns**
- Architectural principles
- Layer-by-layer breakdown
- Data flow architecture
- Error handling at multiple levels
- Concurrency model
- Extension points
- Design patterns summary

### [USE_CASES.md](source/core/USE_CASES.md)
**Real-world applications with ROI metrics**
- 10 detailed use cases:
  - Customer Support Automation
  - Document Intelligence and RAG
  - Content Generation Pipelines
  - Data Enrichment and Integration
  - Monitoring and Alerting
  - E-commerce Personalization
  - Code Generation and Analysis
  - Multi-Agent Systems
  - Compliance and Governance
  - Research and Analysis
- Complete implementation code
- Benefits and ROI analysis

### [HOW_TO_GUIDE.md](source/core/HOW_TO_GUIDE.md)
**Step-by-step tutorials**
- Getting Started
- Building Your First Workflow
- Working with LLMs (OpenAI, Anthropic, Azure, Ollama)
- Data Transformation
- Control Flow (branching, loops, parallel)
- Error Handling (retry, timeout, fallbacks)
- Working with External APIs
- RAG and Embeddings
- Multi-Turn Conversations
- Testing Workflows
- Performance Optimization
- Deployment (ASP.NET Core, Azure Functions, Docker)

### [NODE_REFERENCE.md](source/core/NODE_REFERENCE.md)
**Complete API reference for all nodes**
- 27 built-in nodes documented:
  - 4 AI Nodes (LLM, PromptBuilder, OutputParser, Embedding)
  - 6 Data Nodes (Transform, Mapper, Filter, Chunking, Memory)
  - 8 Control Nodes (Condition, Branch, TryCatch, Loop, Delay, etc.)
  - 4 IO Nodes (HTTP, FileReader, FileWriter, GoogleSearch)
  - 3 Base Classes (BaseNode, SimpleTransformNode, LambdaNode)
- Constructor signatures, parameters, examples

### [DIAGRAMS.md](source/core/DIAGRAMS.md)
**Visual system diagrams with Mermaid**
- 14 comprehensive Mermaid diagrams:
  - System Architecture
  - Request Flow (API & Designer)
  - Execution Flow (complete pipeline)
  - Node Execution Lifecycle
  - Data Flow (WorkflowData & State)
  - Error Handling Flow (multi-layer)
  - Parallel Execution Flow
  - Loop Execution Flow
  - Branch Execution Flow
  - LLM Integration Flow
  - RAG Pipeline Flow
  - Retry Mechanism Flow
  - State Management
  - Class Hierarchy

---

## Project-Wide Documentation

These documents are in the `docs/` folder and cover the entire solution:

### [docs/guide.md](docs/guide.md)
**Workflow Designer user guide**
- Designer layout and interface
- Building workflows visually
- Node reference with UI parameters
- Sub-workflows
- Data flow and mapping
- Error handling
- Common workflow patterns

### [docs/creating-a-new-node.md](docs/creating-a-new-node.md)
**Guide to extending the framework**
- Step-by-step node creation
- UI schema definition
- Palette registration
- Routing ports configuration
- Complete examples

### [docs/NAMING_CONVENTIONS.md](docs/NAMING_CONVENTIONS.md)
**Code style and standards**
- C# naming conventions
- Project structure
- TypeScript/React conventions
- Documentation standards

---

## Quick Navigation

### By Role

**Developers:**
1. Start: [README.md](source/core/README.md) ? [HOW_TO_GUIDE.md](source/core/HOW_TO_GUIDE.md)
2. Reference: [NODE_REFERENCE.md](source/core/NODE_REFERENCE.md)
3. Advanced: [creating-a-new-node.md](docs/creating-a-new-node.md)

**Architects:**
1. Architecture: [ARCHITECTURE.md](source/core/ARCHITECTURE.md)
2. Deployment: [HOW_TO_GUIDE.md](source/core/HOW_TO_GUIDE.md) Section 12
3. Integration: [USE_CASES.md](source/core/USE_CASES.md)

**Product Managers:**
1. Use Cases: [USE_CASES.md](source/core/USE_CASES.md)
2. ROI Analysis: [USE_CASES.md](source/core/USE_CASES.md) ROI Summary
3. Capabilities: [README.md](source/core/README.md)

**End Users (Designer):**
1. Designer Guide: [docs/guide.md](docs/guide.md)
2. Workflow Patterns: [docs/guide.md](docs/guide.md) Section 14

### By Task

| I want to... | Go to... |
|--------------|----------|
| **Get started with code-first workflows** | [README.md](source/core/README.md) Quick Start |
| **Understand the architecture** | [ARCHITECTURE.md](source/core/ARCHITECTURE.md) or [DIAGRAMS.md](source/core/DIAGRAMS.md) |
| **See production examples** | [USE_CASES.md](source/core/USE_CASES.md) or `source/console/examples/` |
| **Learn specific techniques** | [HOW_TO_GUIDE.md](source/core/HOW_TO_GUIDE.md) |
| **Look up a node's API** | [NODE_REFERENCE.md](source/core/NODE_REFERENCE.md) |
| **Create a custom node** | [docs/creating-a-new-node.md](docs/creating-a-new-node.md) |
| **Use the visual designer** | [docs/guide.md](docs/guide.md) |
| **Understand code standards** | [docs/NAMING_CONVENTIONS.md](docs/NAMING_CONVENTIONS.md) |
| **View system diagrams** | [DIAGRAMS.md](source/core/DIAGRAMS.md) |

---

## Additional Resources

### Code Examples

**Console Examples** (`source/console/examples/`)
- CustomerSupportChatbot.cs - Multi-turn chatbot
- RagDocumentQA.cs - RAG pipeline
- ContentGenerationPipeline.cs - Content automation

**Framework Concepts** (`source/console/concepts/`)
- WorkflowDataFluentApi.cs
- NodeChainingAndBranching.cs
- ParallelExecution.cs
- LoopForEach.cs
- ErrorHandlingAndRetry.cs

### Test Suite

**Comprehensive Tests** (`tests/Twf.Flow.Tests/`)
- Core framework tests
- Node-specific tests
- Integration tests
- See `tests/README.md` for details

---

## Documentation Statistics

- **Total Documents:** 9 comprehensive guides
- **Total Lines:** 30,000+ lines of documentation
- **Code Examples:** 300+ working examples
- **Use Cases:** 10 detailed scenarios with ROI
- **Node References:** 27 complete API references
- **Tutorial Steps:** 60+ how-to guides
- **Visual Diagrams:** 14 Mermaid diagrams

---

## Getting Started in 3 Steps

### 1. Installation
```bash
dotnet add package Twf.Flow
```

### 2. Read the Quick Start
- [README.md](source/core/README.md) - Core concepts
- [HOW_TO_GUIDE.md](source/core/HOW_TO_GUIDE.md) Section 1-2 - First workflow

### 3. Explore Examples
- Run console examples: `dotnet run --project source/console`
- Browse use cases: [USE_CASES.md](source/core/USE_CASES.md)

---

## Contributing

- Review [NAMING_CONVENTIONS.md](docs/NAMING_CONVENTIONS.md) for code standards
- Study [ARCHITECTURE.md](source/core/ARCHITECTURE.md) to understand design patterns
- Follow [creating-a-new-node.md](docs/creating-a-new-node.md) for node contributions
- Add tests for new features (see `tests/` for examples)

---

## Support

- **Issues:** [GitHub Issues](https://github.com/techwayfit/Twf_AI_Framework/issues)
- **Discussions:** [GitHub Discussions](https://github.com/techwayfit/Twf_AI_Framework/discussions)
- **Repository:** [github.com/techwayfit/Twf_AI_Framework](https://github.com/techwayfit/Twf_AI_Framework)

---

**Framework Version:** 1.0.1  
**Documentation Last Updated:** January 2025

---

Made with by the Twf.Flow team
