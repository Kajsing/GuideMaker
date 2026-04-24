namespace GuideMaker.Core;

public sealed record GuideAsset
{
    public required Guid Id { get; init; }

    public required string RelativePath { get; init; }

    public required GuideAssetKind Kind { get; init; }

    public string? Caption { get; init; }

    public string? AltText { get; init; }

    public DateTimeOffset CapturedAt { get; init; }
}
