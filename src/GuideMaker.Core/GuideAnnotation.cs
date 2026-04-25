namespace GuideMaker.Core;

public sealed record GuideAnnotation
{
    public required Guid Id { get; init; }

    public required GuideAnnotationKind Kind { get; init; }

    public required Guid AssetId { get; init; }

    public required AnnotationBounds Bounds { get; init; }

    public string? Text { get; init; }

    public GuideAnnotationStyle? Style { get; init; }
}

public sealed record GuideAnnotationStyle
{
    public double FontSize { get; init; } = 14;

    public bool IsBold { get; init; } = true;

    public bool IsItalic { get; init; }

    public string TextColor { get; init; } = "#FFFFFF";

    public string BackgroundColor { get; init; } = "#1A73E8";

    public double BackgroundOpacity { get; init; } = 0.92;
}
