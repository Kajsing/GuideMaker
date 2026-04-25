namespace GuideMaker.Core;

public sealed record GuideStep
{
    public required Guid Id { get; init; }

    public required int Order { get; init; }

    public required string Title { get; init; }

    public string Body { get; init; } = string.Empty;

    public List<Guid> AssetIds { get; init; } = [];

    public List<StepImageRef> ImageRefs { get; init; } = [];

    public List<GuideAnnotation> Annotations { get; init; } = [];

    public static GuideStep Create(int order, string title, string body = "")
    {
        return new GuideStep
        {
            Id = Guid.NewGuid(),
            Order = order,
            Title = title,
            Body = body
        };
    }
}

public sealed record StepImageRef
{
    public required Guid Id { get; init; }

    public required Guid AssetId { get; init; }

    public ImageCropBounds? Crop { get; init; }

    public List<GuideAnnotation> Annotations { get; init; } = [];

    public static StepImageRef Create(Guid assetId)
    {
        return new StepImageRef
        {
            Id = Guid.NewGuid(),
            AssetId = assetId
        };
    }
}

public sealed record ImageCropBounds
{
    public required double X { get; init; }

    public required double Y { get; init; }

    public required double Width { get; init; }

    public required double Height { get; init; }
}
