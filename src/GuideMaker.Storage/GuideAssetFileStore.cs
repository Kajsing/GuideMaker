using GuideMaker.Core;

namespace GuideMaker.Storage;

public sealed class GuideAssetFileStore
{
    private static readonly HashSet<string> SupportedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".bmp",
        ".gif"
    };

    public async Task<GuideAsset> ImportImageAsync(
        GuideProject project,
        string sourceFilePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceFilePath);

        if (!File.Exists(sourceFilePath))
        {
            throw new FileNotFoundException("Image file was not found.", sourceFilePath);
        }

        var extension = Path.GetExtension(sourceFilePath);
        if (!SupportedImageExtensions.Contains(extension))
        {
            throw new GuideProjectStorageException($"Unsupported image type '{extension}'.");
        }

        var destinationPath = CreateUniqueAssetPath(project.ProjectDirectory, Path.GetFileNameWithoutExtension(sourceFilePath), extension);
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

        await using var source = File.OpenRead(sourceFilePath);
        await using var destination = File.Create(destinationPath);
        await source.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);

        return CreateAsset(project.ProjectDirectory, destinationPath, Path.GetFileNameWithoutExtension(sourceFilePath), GuideAssetKind.ImportedImage);
    }

    public async Task<GuideAsset> SavePngAsync(
        GuideProject project,
        string baseFileName,
        Stream pngStream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseFileName);
        ArgumentNullException.ThrowIfNull(pngStream);

        var destinationPath = CreateUniqueAssetPath(project.ProjectDirectory, baseFileName, ".png");
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

        if (pngStream.CanSeek)
        {
            pngStream.Position = 0;
        }

        await using var destination = File.Create(destinationPath);
        await pngStream.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);

        return CreateAsset(project.ProjectDirectory, destinationPath, baseFileName, GuideAssetKind.Screenshot);
    }

    public IReadOnlyList<GuideAsset> ScanUnregisteredImages(GuideProject project)
    {
        ArgumentNullException.ThrowIfNull(project);

        if (!Directory.Exists(project.AssetsDirectory))
        {
            return [];
        }

        var registeredPaths = project.Document.Assets
            .Select(asset => NormalizeRelativePath(asset.RelativePath))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var discoveredAssets = new List<GuideAsset>();

        foreach (var filePath in Directory.EnumerateFiles(project.AssetsDirectory, "*", SearchOption.AllDirectories)
                     .OrderBy(filePath => filePath, StringComparer.OrdinalIgnoreCase))
        {
            if (!SupportedImageExtensions.Contains(Path.GetExtension(filePath)))
            {
                continue;
            }

            var relativePath = NormalizeRelativePath(Path.GetRelativePath(project.ProjectDirectory, filePath));
            if (registeredPaths.Contains(relativePath))
            {
                continue;
            }

            discoveredAssets.Add(new GuideAsset
            {
                Id = Guid.NewGuid(),
                RelativePath = relativePath,
                Kind = GuideAssetKind.ImportedImage,
                Caption = Path.GetFileNameWithoutExtension(filePath),
                AltText = Path.GetFileNameWithoutExtension(filePath),
                CapturedAt = File.GetCreationTimeUtc(filePath)
            });
        }

        return discoveredAssets;
    }

    public IReadOnlyList<string> FindMissingRegisteredImages(GuideProject project)
    {
        ArgumentNullException.ThrowIfNull(project);

        return project.Document.Assets
            .Select(asset => NormalizeRelativePath(asset.RelativePath))
            .Where(relativePath => !File.Exists(Path.Combine(
                project.ProjectDirectory,
                relativePath.Replace('/', Path.DirectorySeparatorChar))))
            .OrderBy(relativePath => relativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static GuideAsset CreateAsset(string projectDirectory, string assetFilePath, string caption, GuideAssetKind kind)
    {
        return new GuideAsset
        {
            Id = Guid.NewGuid(),
            RelativePath = Path.GetRelativePath(projectDirectory, assetFilePath).Replace('\\', '/'),
            Kind = kind,
            Caption = caption,
            AltText = caption,
            CapturedAt = DateTimeOffset.UtcNow
        };
    }

    private static string NormalizeRelativePath(string relativePath)
    {
        return relativePath.Replace('\\', '/');
    }

    private static string CreateUniqueAssetPath(string projectDirectory, string baseFileName, string extension)
    {
        var safeBaseName = SanitizeFileName(baseFileName);
        var assetsDirectory = Path.Combine(projectDirectory, GuideProjectLayout.AssetsDirectoryName);
        var candidatePath = Path.Combine(assetsDirectory, safeBaseName + extension);
        var suffix = 1;

        while (File.Exists(candidatePath))
        {
            candidatePath = Path.Combine(assetsDirectory, $"{safeBaseName}-{suffix}{extension}");
            suffix++;
        }

        return candidatePath;
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var safeCharacters = fileName
            .Trim()
            .Select(character => invalidCharacters.Contains(character) ? '-' : character)
            .ToArray();
        var safeName = new string(safeCharacters).Trim('-', ' ');

        return string.IsNullOrWhiteSpace(safeName) ? "image" : safeName;
    }
}
