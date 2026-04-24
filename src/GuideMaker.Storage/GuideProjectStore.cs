using System.Text.Json;
using System.Text.Json.Serialization;
using GuideMaker.Core;

namespace GuideMaker.Storage;

public sealed class GuideProjectStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<GuideProject> CreateAsync(
        string projectDirectory,
        GuideDocument document,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectDirectory);
        ArgumentNullException.ThrowIfNull(document);

        Directory.CreateDirectory(projectDirectory);
        Directory.CreateDirectory(Path.Combine(projectDirectory, GuideProjectLayout.AssetsDirectoryName));
        Directory.CreateDirectory(Path.Combine(projectDirectory, GuideProjectLayout.ExportsDirectoryName));

        var project = new GuideProject
        {
            ProjectDirectory = Path.GetFullPath(projectDirectory),
            Document = document
        };

        await SaveAsync(project, cancellationToken).ConfigureAwait(false);
        return project;
    }

    public async Task SaveAsync(GuideProject project, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);

        Directory.CreateDirectory(project.ProjectDirectory);
        Directory.CreateDirectory(project.AssetsDirectory);
        Directory.CreateDirectory(project.ExportsDirectory);

        var document = project.Document with
        {
            Metadata = project.Document.Metadata with { UpdatedAt = DateTimeOffset.UtcNow }
        };

        await using var stream = File.Create(project.GuideFilePath);
        await JsonSerializer.SerializeAsync(stream, document, JsonOptions, cancellationToken).ConfigureAwait(false);
    }

    public async Task<GuideProject> LoadAsync(string projectDirectory, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectDirectory);

        var fullPath = Path.GetFullPath(projectDirectory);
        var guideFilePath = Path.Combine(fullPath, GuideProjectLayout.GuideFileName);

        if (!File.Exists(guideFilePath))
        {
            throw new FileNotFoundException("Guide project file was not found.", guideFilePath);
        }

        await using var stream = File.OpenRead(guideFilePath);
        var document = await JsonSerializer.DeserializeAsync<GuideDocument>(stream, JsonOptions, cancellationToken)
            .ConfigureAwait(false);

        if (document is null)
        {
            throw new InvalidDataException("Guide project file was empty or invalid.");
        }

        return new GuideProject
        {
            ProjectDirectory = fullPath,
            Document = document
        };
    }
}
