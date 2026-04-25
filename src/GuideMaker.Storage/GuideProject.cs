using GuideMaker.Core;

namespace GuideMaker.Storage;

public sealed record GuideProject
{
    public required string ProjectDirectory { get; init; }

    public required GuideDocument Document { get; init; }

    public IReadOnlyList<string> MissingAssetPaths { get; init; } = [];

    public string GuideFilePath => Path.Combine(ProjectDirectory, GuideProjectLayout.GuideFileName);

    public string AssetsDirectory => Path.Combine(ProjectDirectory, GuideProjectLayout.AssetsDirectoryName);

    public string ExportsDirectory => Path.Combine(ProjectDirectory, GuideProjectLayout.ExportsDirectoryName);
}
