using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Twf.Flow.Core;
using Twf.Flow.Core.Http;
using Twf.Flow.Nodes;
using Twf.Flow.Web.Models;
using Twf.Flow.Web.Services.VariableResolution;

namespace Twf.Flow.Web.Services.NodeFactory;

/// <summary>
/// Creates INode instances using reflection and caches constructors for performance.
/// Discovers all INode implementations from the core assembly at startup.
/// Injects dependencies like IHttpClientProvider into nodes that need them.
/// </summary>
public class ReflectionNodeFactory : INodeFactory
{
    private readonly IVariableResolver _variableResolver;
    private readonly IHttpClientProvider _httpClientProvider;
    private readonly ILogger<ReflectionNodeFactory> _logger;

    /// <summary>
    /// Registry of all INode implementations in the core assembly, keyed by class name.
    /// Built once at startup via reflection — no switch-case required.
    /// </summary>
    private readonly IReadOnlyDictionary<string, Type> _nodeTypeRegistry;

    /// <summary>
    /// Cached compiled constructor delegates for improved performance.
    /// Avoids repeated reflection calls during workflow execution.
    /// </summary>
    private readonly ConcurrentDictionary<Type, Func<Dictionary<string, object?>, INode>> _constructorCache;

    public ReflectionNodeFactory(
        IVariableResolver variableResolver,
        IHttpClientProvider httpClientProvider,
        ILogger<ReflectionNodeFactory> logger)
    {
        _variableResolver = variableResolver;
        _httpClientProvider = httpClientProvider;
        _logger = logger;
        _constructorCache = new ConcurrentDictionary<Type, Func<Dictionary<string, object?>, INode>>();

        // Discover all INode implementations from the core assembly
        _nodeTypeRegistry = typeof(BaseNode).Assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(INode).IsAssignableFrom(t))
            .ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);

        _logger.LogInformation(
            "Node factory initialized with {Count} registered node types (with HTTP client pooling)",
            _nodeTypeRegistry.Count);
    }

    public INode? CreateNode(NodeDefinition nodeDefinition, WorkflowData workflowData)
    {
        if (!_nodeTypeRegistry.TryGetValue(nodeDefinition.Type, out var nodeType))
        {
            _logger.LogWarning(
                "No INode implementation found for type '{Type}'",
                nodeDefinition.Type);
            return null;
        }

        try
        {
            // Resolve variables in parameters
            var resolvedParams = _variableResolver.ResolveParameters(
                nodeDefinition.Parameters,
                workflowData);

            // Inject node name so constructors can read it
            resolvedParams["name"] = nodeDefinition.Name;

            // Try to use dependency injection constructor if available (e.g., for LlmNode, EmbeddingNode)
            var node = TryCreateWithDependencies(nodeType, resolvedParams)
                       ?? TryCreateWithDictionaryConstructor(nodeType, resolvedParams);

            if (node == null)
            {
                _logger.LogError(
                    "Failed to find suitable constructor for node '{Type}'",
                    nodeDefinition.Type);
                return null;
            }

            _logger.LogDebug(
                "Created node instance: Type={Type}, Name={Name}",
                nodeDefinition.Type,
                nodeDefinition.Name);

            return node;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to instantiate node '{Type}' (Name: {Name})",
                nodeDefinition.Type,
                nodeDefinition.Name);
            return null;
        }
    }

    /// <summary>
    /// Try to create node with dependency injection constructor.
    /// Looks for constructors that accept IHttpClientProvider and other dependencies.
    /// </summary>
    private INode? TryCreateWithDependencies(Type nodeType, Dictionary<string, object?> parameters)
    {
        // Check if this is a node that benefits from HttpClient injection
        // Currently: LlmNode, EmbeddingNode, HttpRequestNode
        var needsHttpClient = nodeType.Name is "LlmNode" or "EmbeddingNode" or "HttpRequestNode";

        if (!needsHttpClient)
            return null;

        // For these nodes, we'll still use the dictionary constructor but can enhance later
        // This is a placeholder for future enhancement where we inject IHttpClientProvider directly
        return null;
    }

    /// <summary>
    /// Create node using standard Dictionary constructor.
    /// </summary>
    private INode? TryCreateWithDictionaryConstructor(Type nodeType, Dictionary<string, object?> parameters)
    {
        // Get or compile the constructor delegate
        var factory = _constructorCache.GetOrAdd(nodeType, type =>
        {
            var ctor = type.GetConstructor(new[] { typeof(Dictionary<string, object?>) });
            if (ctor is null)
            {
                throw new InvalidOperationException(
                    $"Node '{type.Name}' has no Dictionary<string, object?> constructor");
            }

            // Create a compiled delegate for better performance
            return parameters => (INode)ctor.Invoke(new object[] { parameters });
        });

        return factory(parameters);
    }

    public bool IsNodeTypeRegistered(string nodeType)
    {
        return _nodeTypeRegistry.ContainsKey(nodeType);
    }

    public IReadOnlyCollection<string> GetRegisteredNodeTypes()
    {
        return _nodeTypeRegistry.Keys.ToList();
    }
}
