namespace GuideMaker.Core;

public sealed record GuideMetadata
{
    public string SchemaVersion { get; init; } = GuideSchema.CurrentVersion;

    public required string Title { get; init; }

    public required string Author { get; init; }

    public string? Description { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }
}
