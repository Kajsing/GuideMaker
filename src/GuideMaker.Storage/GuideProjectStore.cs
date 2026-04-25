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

        var fullPath = Path.GetFullPath(projectDirectory);
        ThrowIfInvalid(fullPath, document);

        Directory.CreateDirectory(fullPath);
        Directory.CreateDirectory(Path.Combine(fullPath, GuideProjectLayout.AssetsDirectoryName));
        Directory.CreateDirectory(Path.Combine(fullPath, GuideProjectLayout.ExportsDirectoryName));

        var project = new GuideProject
        {
            ProjectDirectory = fullPath,
            Document = document
        };

        await SaveAsync(project, cancellationToken).ConfigureAwait(false);
        return project;
    }

    public async Task SaveAsync(GuideProject project, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        ThrowIfInvalid(project.ProjectDirectory, project.Document);

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
            throw new GuideProjectFileMissingException(guideFilePath);
        }

        GuideDocument? document;
        try
        {
            await using var stream = File.OpenRead(guideFilePath);
            document = await JsonSerializer.DeserializeAsync<GuideDocument>(stream, JsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (JsonException exception)
        {
            throw new GuideProjectFormatException(guideFilePath, exception);
        }

        if (document is null)
        {
            throw new GuideProjectValidationException(["Guide project file was empty."]);
        }

        ThrowIfInvalid(fullPath, document);

        return new GuideProject
        {
            ProjectDirectory = fullPath,
            Document = document,
            MissingAssetPaths = GuideProjectValidator.FindMissingAssetFiles(fullPath, document)
        };
    }

    private static void ThrowIfInvalid(string projectDirectory, GuideDocument document)
    {
        var errors = GuideProjectValidator.Validate(projectDirectory, document);
        if (errors.Count > 0)
        {
            throw new GuideProjectValidationException(errors);
        }
    }
}
