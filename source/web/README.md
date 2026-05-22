# Twf.Flow - Workflow Designer

A visual workflow designer for creating and managing AI workflows using the Twf.Flow.

## Documentation

This project includes comprehensive documentation:

- **[DOCUMENTATION.md](./DOCUMENTATION.md)** - Complete technical documentation covering:
  - Architecture overview
  - Getting started guide
  - Core components
  - Features
  - API reference
  - Database schema
  - Workflow execution
  - Configuration
  - Deployment
  - Troubleshooting

- **[ARCHITECTURE.md](./ARCHITECTURE.md)** - Detailed architectural diagrams and design patterns:
  - Layer architecture
  - Service dependencies
  - Workflow execution flow
  - Data flow
  - Database schema
  - Design patterns (Repository, Factory, Strategy, etc.)
  - Concurrency model
  - Error handling architecture

- **[QUICK_REFERENCE.md](./QUICK_REFERENCE.md)** - Quick lookup guide for:
  - Common tasks
  - API endpoints
  - Code snippets
  - Database queries
  - Troubleshooting tips
  - Configuration examples

- **[API_SPECIFICATION.md](./API_SPECIFICATION.md)** - RESTful API specification:
  - Workflow management endpoints
  - Workflow execution endpoints
  - Node type management endpoints
  - Data models
  - Error responses
  - SSE streaming

## Quick Start

### Prerequisites
- .NET 10.0 SDK
- Modern web browser (Chrome, Firefox, Edge)

### Running the Application

1. **Configure storage** in `appsettings.json`:
   ```json
   {
     "UseDatabase": true,
     "ConnectionStrings": {
       "WorkflowDb": "Data Source=workflows.db"
     }
   }
   ```

2. **Run the application**:
   ```bash
   cd source/web
   dotnet run
   ```

3. **Open your browser** to `https://localhost:5001`

## ? Features

### Visual Workflow Designer
- **Drag-and-drop interface** for creating workflows
- **Node-based visual programming** similar to n8n
- **Connection management** with visual bezier curves
- **Zoom and pan** controls for large workflows
- **Property editing** for node configuration

### Workflow Execution
- **Real-time streaming** via Server-Sent Events (SSE)
- **Retry logic** with exponential backoff
- **Timeout handling** per node
- **Error recovery** with workflow-level error routing
- **Sub-workflow support** for hierarchical composition

### Node Type Management
- **Database-driven node registry** with SQLite
- **Dynamic node schema** generation
- **CRUD operations** for node types
- **Enable/disable** node types

### Advanced Features
- **Loop nodes** for iterating over collections
- **Branch nodes** for conditional routing
- **Sub-workflow nodes** for reusable workflows
- **Variable substitution** with `{{template}}` syntax
- **Scoped outputs** for accessing node results
- **Structured logging** with correlation IDs

## Architecture

The application follows a **clean architecture** pattern with clear separation of concerns:

```
Controllers ? Services ? Repositories ? Database
     |      |            |   ?
 +-- Dependency Injection +--
```

**Key Services:**
- `WorkflowDefinitionRunner` - Orchestrates workflow execution
- `WorkflowGraphWalker` - Graph traversal and node execution
- `NodeExecutor` - Individual node execution with retry/timeout
- `NodeFactory` - Dynamic node instantiation
- `VariableResolver` - Template variable substitution

For detailed architecture diagrams, see [ARCHITECTURE.md](./ARCHITECTURE.md).

## API Endpoints

### Workflow Management
- `GET /Workflow` - List workflows
- `GET /Workflow/GetWorkflow/{id}` - Get workflow JSON
- `POST /Workflow/SaveWorkflow` - Save workflow
- `GET /Workflow/GetAvailableNodes` - Get node types

### Workflow Execution
- `POST /Workflow/Run/{id}` - Execute workflow (blocking)
- `POST /Workflow/RunStream/{id}` - Execute workflow (streaming SSE)
- `GET /Workflow/Runs/{id}` - View execution history
- `GET /Workflow/RunDetail/{instanceId}` - View run details

### Node Type Management
- `GET /Node` - List node types
- `POST /Node/Create` - Create node type
- `POST /Node/Edit/{id}` - Update node type
- `POST /Node/Delete/{id}` - Delete node type

For complete API specification, see [API_SPECIFICATION.md](./API_SPECIFICATION.md).

## Database

The application uses **SQLite** with **Entity Framework Core** for persistence.

**Tables:**
- `Workflows` - Workflow definitions
- `NodeTypes` - Node type registry
- `WorkflowInstances` - Execution history

**Connection String:**
```json
{
  "ConnectionStrings": {
  "WorkflowDb": "Data Source=workflows.db"
  }
}
```

## Configuration

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "UseDatabase": true,
  "ConnectionStrings": {
    "WorkflowDb": "Data Source=workflows.db"
  }
}
```

## Development

### Project Structure

```
source/web/
+-- Controllers/     # MVC controllers
+-- Services/            # Business logic
|   +-- GraphWalker/     # Workflow execution
|   +-- NodeFactory/     # Node instantiation
|   +-- Execution/       # Node execution
|   +-- VariableResolution/ # Template variables
+-- Repositories/        # Data access
+-- Models/           # Data models
+-- Data/      # EF Core DbContext
+-- Middleware/       # Custom middleware
+-- Views/         # Razor views
+-- wwwroot/ # Static files

```

### Adding a Custom Node

See [QUICK_REFERENCE.md](./QUICK_REFERENCE.md#create-a-custom-node-c) for code examples.

## Testing

```bash
cd tests/Twf.Flow.Tests
dotnet test
```

## Deployment

### Docker

```bash
docker build -t twf-ai-framework-web .
docker run -d -p 8080:80 -v /data:/app/data twf-ai-framework-web
```

### IIS

1. Publish: `dotnet publish -c Release -o ./publish`
2. Create IIS application pool (.NET CLR: No Managed Code)
3. Point to publish directory

For detailed deployment instructions, see [DOCUMENTATION.md](./DOCUMENTATION.md#deployment).

## Troubleshooting

### Common Issues

**"Workflow has no Start node"**
- Add a Start node to your workflow in the designer

**"Node type not found"**
- Check that the node type is enabled in `/Node`
- Verify the node type exists in the database

**Database is locked**
- Stop the application
- Delete lock files
- Restart

For more troubleshooting tips, see [QUICK_REFERENCE.md](./QUICK_REFERENCE.md#troubleshooting).

## Performance

- **Async/await** throughout for maximum throughput
- **HTTP client pooling** for AI API calls
- **Database indexes** on frequently queried columns
- **Scoped logging** to reduce verbosity
- **Server-Sent Events** for real-time updates without polling

## Security

- **HTTPS enforcement** in production
- **Anti-forgery tokens** for state-changing requests
- **Input validation** with model binding
- **SQL injection prevention** via EF Core parameterization
- **Exception details filtering** in production

## Contributing

Contributions are welcome! Please:
1. Fork the repository
2. Create a feature branch
3. Follow coding standards
4. Add tests
5. Update documentation
6. Submit pull request

## License

This project is part of the Twf.Flow.

## Acknowledgments

- Built with ASP.NET Core MVC
- Uses Entity Framework Core
- Inspired by n8n and Node-RED

---

For detailed documentation, see:
- [DOCUMENTATION.md](./DOCUMENTATION.md) - Complete technical documentation
- [ARCHITECTURE.md](./ARCHITECTURE.md) - Architectural diagrams
- [QUICK_REFERENCE.md](./QUICK_REFERENCE.md) - Quick lookup guide
- [API_SPECIFICATION.md](./API_SPECIFICATION.md) - API specification

**Version:** 1.0  
**Framework:** .NET 10.0
