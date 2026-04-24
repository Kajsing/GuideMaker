namespace GuideMaker.Core;

public sealed record GuideAnnotation
{
    public required Guid Id { get; init; }

    public required GuideAnnotationKind Kind { get; init; }

    public required Guid AssetId { get; init; }

    public required AnnotationBounds Bounds { get; init; }

    public string? Text { get; init; }
}
