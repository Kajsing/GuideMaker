namespace GuideMaker.Core;

public sealed record GuideDocument
{
    public required GuideMetadata Metadata { get; init; }

    public List<GuideStep> Steps { get; init; } = [];

    public List<GuideAsset> Assets { get; init; } = [];

    public static GuideDocument Create(string title, string author)
    {
        return new GuideDocument
        {
            Metadata = new GuideMetadata
            {
                Title = title,
                Author = author,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            }
        };
    }
}
