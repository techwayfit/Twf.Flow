namespace Twf.Flow.Core.ValueObjects;

/// <summary>
/// Value object representing an LLM temperature parameter.
/// Valid range: 0.0 to 2.0
/// Lower values (0.0-0.3) = more focused/deterministic
/// Medium values (0.4-0.9) = balanced creativity
/// Higher values (1.0-2.0) = more creative/random
/// </summary>
/// <remarks>
/// Immutable value object with built-in validation.
/// Use factory methods to create instances:
/// <code>
/// var temp = Temperature.FromValue(0.7f);
/// var creative = Temperature.Creative;  // 1.0
/// var focused = Temperature.Focused;    // 0.3
/// </code>
/// </remarks>
public readonly record struct Temperature
{
    /// <summary>
    /// The temperature value (0.0 to 2.0).
    /// </summary>
    public float Value { get; }

    private Temperature(float value)
    {
        if (value < 0.0f || value > 2.0f)
            throw new ArgumentOutOfRangeException(
              nameof(value),
          value,
            "Temperature must be between 0.0 and 2.0");

        Value = value;
    }

    /// <summary>
    /// Creates a Temperature from a float value.
    /// </summary>
    /// <param name="value">Temperature value (0.0 to 2.0).</param>
    /// <returns>A validated Temperature instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value is outside valid range.</exception>
    public static Temperature FromValue(float value) => new(value);

    /// <summary>
    /// Predefined: Highly focused and deterministic (0.0).
    /// Best for: factual Q&A, classification, structured data extraction.
    /// </summary>
    public static Temperature Deterministic => new(0.0f);

    /// <summary>
    /// Predefined: Very focused with minimal variation (0.3).
    /// Best for: technical writing, code generation, precise answers.
    /// </summary>
    public static Temperature Focused => new(0.3f);

    /// <summary>
    /// Predefined: Balanced creativity and coherence (0.7).
    /// Best for: general chatbots, content generation, dialogue.
    /// </summary>
    public static Temperature Balanced => new(0.7f);

    /// <summary>
    /// Predefined: Creative and exploratory (1.0).
    /// Best for: brainstorming, creative writing, diverse ideas.
    /// </summary>
    public static Temperature Creative => new(1.0f);

    /// <summary>
    /// Predefined: Highly creative and unpredictable (1.5).
    /// Best for: experimental content, poetry, unusual perspectives.
    /// </summary>
    public static Temperature VeryCreative => new(1.5f);

    /// <summary>
    /// Implicit conversion to float for backward compatibility.
    /// </summary>
    public static implicit operator float(Temperature temp) => temp.Value;

    /// <summary>
    /// Explicit conversion from float with validation.
    /// </summary>
    public static explicit operator Temperature(float value) => FromValue(value);

    /// <inheritdoc/>
    public override string ToString() => Value.ToString("F2");
}
