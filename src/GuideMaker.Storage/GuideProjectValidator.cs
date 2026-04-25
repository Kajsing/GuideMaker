using GuideMaker.Core;

namespace GuideMaker.Storage;

public static class GuideProjectValidator
{
    public static IReadOnlyList<string> Validate(string projectDirectory, GuideDocument document)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectDirectory);
        ArgumentNullException.ThrowIfNull(document);

        var errors = new List<string>();

        ValidateMetadata(document, errors);
        ValidateAssets(projectDirectory, document, errors);
        ValidateSteps(document, errors);

        return errors;
    }

    public static IReadOnlyList<string> FindMissingAssetFiles(string projectDirectory, GuideDocument document)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectDirectory);
        ArgumentNullException.ThrowIfNull(document);

        return document.Assets
            .Where(asset => IsSafeAssetPath(projectDirectory, asset.RelativePath))
            .Select(asset => asset.RelativePath)
            .Where(relativePath => !File.Exists(Path.Combine(projectDirectory, relativePath)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static void ValidateMetadata(GuideDocument document, List<string> errors)
    {
        if (document.Metadata.SchemaVersion != GuideSchema.CurrentVersion)
        {
            errors.Add($"Unsupported guide schema version '{document.Metadata.SchemaVersion}'. Expected '{GuideSchema.CurrentVersion}'.");
        }

        if (string.IsNullOrWhiteSpace(document.Metadata.Title))
        {
            errors.Add("Guide title is required.");
        }

        if (string.IsNullOrWhiteSpace(document.Metadata.Author))
        {
            errors.Add("Guide author is required.");
        }
    }

    private static void ValidateAssets(string projectDirectory, GuideDocument document, List<string> errors)
    {
        var assetIds = new HashSet<Guid>();

        foreach (var asset in document.Assets)
        {
            if (!assetIds.Add(asset.Id))
            {
                errors.Add($"Duplicate asset id '{asset.Id}'.");
            }

            if (string.IsNullOrWhiteSpace(asset.RelativePath))
            {
                errors.Add($"Asset '{asset.Id}' must have a relative path.");
                continue;
            }

            if (!IsSafeAssetPath(projectDirectory, asset.RelativePath))
            {
                errors.Add($"Asset '{asset.Id}' has an unsafe path: '{asset.RelativePath}'.");
            }
        }
    }

    private static void ValidateSteps(GuideDocument document, List<string> errors)
    {
        var stepIds = new HashSet<Guid>();
        var assetIds = document.Assets.Select(asset => asset.Id).ToHashSet();

        foreach (var step in document.Steps)
        {
            if (!stepIds.Add(step.Id))
            {
                errors.Add($"Duplicate step id '{step.Id}'.");
            }

            if (step.Order < 1)
            {
                errors.Add($"Step '{step.Id}' must have an order of 1 or greater.");
            }

            if (string.IsNullOrWhiteSpace(step.Title))
            {
                errors.Add($"Step '{step.Id}' must have a title.");
            }

            foreach (var assetId in step.AssetIds)
            {
                if (!assetIds.Contains(assetId))
                {
                    errors.Add($"Step '{step.Id}' references unknown asset '{assetId}'.");
                }
            }

            ValidateImageRefs(step, assetIds, errors);

            foreach (var annotation in step.Annotations)
            {
                if (!assetIds.Contains(annotation.AssetId))
                {
                    errors.Add($"Annotation '{annotation.Id}' references unknown asset '{annotation.AssetId}'.");
                }

                ValidateBounds(annotation, errors);
            }
        }
    }

    private static void ValidateImageRefs(GuideStep step, ISet<Guid> assetIds, List<string> errors)
    {
        var imageRefIds = new HashSet<Guid>();

        foreach (var imageRef in step.ImageRefs)
        {
            if (!imageRefIds.Add(imageRef.Id))
            {
                errors.Add($"Step '{step.Id}' has duplicate image reference id '{imageRef.Id}'.");
            }

            if (!assetIds.Contains(imageRef.AssetId))
            {
                errors.Add($"Image reference '{imageRef.Id}' references unknown asset '{imageRef.AssetId}'.");
            }

            if (imageRef.Crop is not null && !IsValidBounds(imageRef.Crop.X, imageRef.Crop.Y, imageRef.Crop.Width, imageRef.Crop.Height))
            {
                errors.Add($"Image reference '{imageRef.Id}' must have normalized crop bounds within the image.");
            }

            foreach (var annotation in imageRef.Annotations)
            {
                if (!assetIds.Contains(annotation.AssetId))
                {
                    errors.Add($"Annotation '{annotation.Id}' references unknown asset '{annotation.AssetId}'.");
                }

                if (annotation.AssetId != imageRef.AssetId)
                {
                    errors.Add($"Annotation '{annotation.Id}' must reference the same asset as image reference '{imageRef.Id}'.");
                }

                ValidateBounds(annotation, errors);
            }
        }
    }

    private static void ValidateBounds(GuideAnnotation annotation, List<string> errors)
    {
        var bounds = annotation.Bounds;
        if (!IsValidBounds(bounds.X, bounds.Y, bounds.Width, bounds.Height))
        {
            errors.Add($"Annotation '{annotation.Id}' must have normalized bounds within the image.");
        }
    }

    private static bool IsValidBounds(double x, double y, double width, double height)
    {
        return IsNormalized(x) &&
            IsNormalized(y) &&
            IsNormalized(width) &&
            IsNormalized(height) &&
            width > 0 &&
            height > 0 &&
            x + width <= 1 &&
            y + height <= 1;
    }

    private static bool IsNormalized(double value)
    {
        return value is >= 0 and <= 1;
    }

    private static bool IsSafeAssetPath(string projectDirectory, string relativePath)
    {
        if (Path.IsPathRooted(relativePath))
        {
            return false;
        }

        if (relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(part => part is "" or "." or ".."))
        {
            return false;
        }

        var projectFullPath = EnsureTrailingSeparator(Path.GetFullPath(projectDirectory));
        var assetsFullPath = EnsureTrailingSeparator(Path.Combine(projectFullPath, GuideProjectLayout.AssetsDirectoryName));
        var assetFullPath = Path.GetFullPath(Path.Combine(projectFullPath, relativePath));

        return assetFullPath.StartsWith(assetsFullPath, StringComparison.OrdinalIgnoreCase);
    }

    private static string EnsureTrailingSeparator(string path)
    {
        return path.EndsWith(Path.DirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
    }
}
