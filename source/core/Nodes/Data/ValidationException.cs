namespace Twf.Flow.Nodes.Data;

public sealed class ValidationException : Exception
{
    public string NodeName { get; }
    public ValidationException(string nodeName, string message)
        : base($"[{nodeName}] Validation failed: {message}")
    {
        NodeName = nodeName;
    }
}