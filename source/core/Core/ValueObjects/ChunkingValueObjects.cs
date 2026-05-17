namespace Twf.Flow.Core.ValueObjects;

/// <summary>
/// Value object representing a chunk size for text processing.
/// Valid range: 50 to 10,000 characters.
/// </summary>
/// <remarks>
/// Immutable value object with built-in validation.
/// Use factory methods to create instances:
/// <code>
/// var size = ChunkSize.FromValue(512);
/// var standard = ChunkSize.Standard;  // 500
/// </code>
/// </remarks>
public readonly record struct ChunkSize
{
    /// <summary>
    /// The chunk size value (50 to 10,000 characters).
    /// </summary>
    public int Value { get; }

    private ChunkSize(int value)
    {
        if (value < 50 || value > 10_000)
            throw new ArgumentOutOfRangeException(
           nameof(value),
                  value,
         "Chunk size must be between 50 and 10,000 characters");

        Value = value;
    }

    /// <summary>
    /// Creates a ChunkSize from an integer value.
    /// </summary>
    /// <param name="value">Chunk size (50 to 10,000).</param>
    /// <returns>A validated ChunkSize instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value is outside valid range.</exception>
    public static ChunkSize FromValue(int value) => new(value);

    /// <summary>
    /// Predefined: Small chunks (200 characters).
    /// Best for: sentence-level granularity, precise matching.
    /// </summary>
    public static ChunkSize Small => new(200);

    /// <summary>
    /// Predefined: Standard chunks (500 characters).
    /// Best for: balanced RAG pipelines, typical embeddings.
    /// </summary>
    public static ChunkSize Standard => new(500);

    /// <summary>
    /// Predefined: Large chunks (1000 characters).
    /// Best for: paragraph-level context, comprehensive searches.
    /// </summary>
    public static ChunkSize Large => new(1000);

    /// <summary>
    /// Predefined: Extra large chunks (2000 characters).
    /// Best for: section-level chunks, broader context.
    /// </summary>
    public static ChunkSize ExtraLarge => new(2000);

    /// <summary>
    /// Implicit conversion to int for backward compatibility.
    /// </summary>
    public static implicit operator int(ChunkSize size) => size.Value;

    /// <summary>
    /// Explicit conversion from int with validation.
    /// </summary>
    public static explicit operator ChunkSize(int value) => FromValue(value);

    /// <inheritdoc/>
    public override string ToString() => $"{Value:N0} chars";
}

/// <summary>
/// Value object representing overlap between text chunks.
/// Valid range: 0 to 500 characters.
/// </summary>
/// <remarks>
/// Overlap prevents semantic breaks at chunk boundaries.
/// Immutable value object with built-in validation.
/// </remarks>
public readonly record struct ChunkOverlap
{
    /// <summary>
    /// The overlap value (0 to 500 characters).
    /// </summary>
    public int Value { get; }

    private ChunkOverlap(int value)
    {
        if (value < 0 || value > 500)
            throw new ArgumentOutOfRangeException(
      nameof(value),
    value,
            "Chunk overlap must be between 0 and 500 characters");

        Value = value;
    }

    /// <summary>
    /// Creates a ChunkOverlap from an integer value.
    /// </summary>
    /// <param name="value">Overlap size (0 to 500).</param>
    /// <returns>A validated ChunkOverlap instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value is outside valid range.</exception>
    public static ChunkOverlap FromValue(int value) => new(value);

    /// <summary>
    /// Predefined: No overlap (0 characters).
    /// Best for: maximum throughput, when boundaries aren't critical.
    /// </summary>
    public static ChunkOverlap None => new(0);

    /// <summary>
    /// Predefined: Minimal overlap (25 characters).
    /// Best for: slight context preservation.
    /// </summary>
    public static ChunkOverlap Minimal => new(25);

    /// <summary>
    /// Predefined: Standard overlap (50 characters).
    /// Best for: balanced context and performance.
    /// </summary>
    public static ChunkOverlap Standard => new(50);

    /// <summary>
    /// Predefined: High overlap (100 characters).
    /// Best for: preserving sentence boundaries, better semantic matching.
    /// </summary>
    public static ChunkOverlap High => new(100);

    /// <summary>
    /// Implicit conversion to int for backward compatibility.
    /// </summary>
    public static implicit operator int(ChunkOverlap overlap) => overlap.Value;

    /// <summary>
    /// Explicit conversion from int with validation.
    /// </summary>
    public static explicit operator ChunkOverlap(int value) => FromValue(value);

    /// <inheritdoc/>
    public override string ToString() => $"{Value:N0} chars overlap";
}
