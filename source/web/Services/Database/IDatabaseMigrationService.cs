namespace Twf.Flow.Web.Services.Database;

/// <summary>
/// Service responsible for database schema migrations and initial setup.
/// Encapsulates all database migration logic previously scattered in Program.cs.
/// </summary>
public interface IDatabaseMigrationService
{
    /// <summary>
  /// Ensures the database schema is created and up-to-date.
    /// Creates tables, indexes, and applies any additive schema changes.
    /// Safe to call on every application startup - idempotent operations.
    /// </summary>
    Task MigrateSchemaAsync();

    /// <summary>
    /// Seeds the database with initial data if needed.
    /// Includes node type definitions and workflow imports from JSON files.
 /// Safe to call on every application startup - skips existing data.
    /// </summary>
    Task SeedDataAsync();
}
