namespace GuideMaker.Core;

public sealed record GuideStep
{
    public required Guid Id { get; init; }

    public required int Order { get; init; }

    public required string Title { get; init; }

    public string Body { get; init; } = string.Empty;

    public List<Guid> AssetIds { get; init; } = [];

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
