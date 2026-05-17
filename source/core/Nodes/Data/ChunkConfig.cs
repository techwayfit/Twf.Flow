using Twf.Flow.Core.ValueObjects;

namespace Twf.Flow.Nodes.Data;

/// <summary>
/// Configuration for text chunking operations.
/// </summary>
/// <remarks>
/// Use value objects for type-safe, validated configuration:
/// <code>
/// var config = new ChunkConfig
/// {
///     ChunkSize = ChunkSize.Standard,  // 500 chars
///     Overlap = ChunkOverlap.Standard,  // 50 chars
///     Strategy = ChunkStrategy.Sentence
/// };
/// </code>
/// </remarks>
public sealed class ChunkConfig
{
    /// <summary>
    /// Target size for each chunk. Default is 500 characters (Standard).
    /// </summary>
    /// <remarks>
    /// Use predefined values or create custom:
    /// <code>
    /// ChunkSize.Small      // 200 - fine-grained
    /// ChunkSize.Standard   // 500 - balanced
    /// ChunkSize.Large      // 1000 - broad context
    /// ChunkSize.FromValue(750) // custom
    /// </code>
    /// </remarks>
    public ChunkSize ChunkSize { get; init; } = ChunkSize.Standard;

    /// <summary>
    /// Number of characters to overlap between chunks. Default is 50 (Standard).
    /// </summary>
    /// <remarks>
    /// Overlap prevents semantic breaks at boundaries.
    /// Use predefined values or create custom:
    /// <code>
    /// ChunkOverlap.None     // 0 - no overlap
    /// ChunkOverlap.Standard // 50 - balanced
    /// ChunkOverlap.High     // 100 - max context
    /// ChunkOverlap.FromValue(75) // custom
    /// </code>
    /// </remarks>
    public ChunkOverlap Overlap { get; init; } = ChunkOverlap.Standard;

    /// <summary>
    /// Strategy for determining chunk boundaries.
    /// </summary>
    public ChunkStrategy Strategy { get; init; } = ChunkStrategy.Character;
}