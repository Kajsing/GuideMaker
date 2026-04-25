using GuideMaker.Core;
using System.Text.RegularExpressions;
using Drawing = System.Drawing;
using Drawing2D = System.Drawing.Drawing2D;
using Imaging = System.Drawing.Imaging;

namespace GuideMaker.Export;

public sealed class ExportAssetRenderer
{
    public const string ExportAssetsDirectoryName = "assets";

    private static readonly Regex ImageReferenceRegex = new(@"\[\[image:(?<path>[^\]]+)\]\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public GuideDocument RenderExportAssets(string projectDirectory, string exportsDirectory, GuideDocument document)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(exportsDirectory);
        ArgumentNullException.ThrowIfNull(document);

        var exportAssetsDirectory = Path.Combine(exportsDirectory, ExportAssetsDirectoryName);
        if (Directory.Exists(exportAssetsDirectory))
        {
            Directory.Delete(exportAssetsDirectory, recursive: true);
        }

        Directory.CreateDirectory(exportAssetsDirectory);

        var usedFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var renderedAssets = new List<GuideAsset>();
        var assetPathMap = new Dictionary<Guid, string>();
        var sourceAssetsById = document.Assets.ToDictionary(asset => asset.Id);
        var sourceAssetsByPath = document.Assets.ToDictionary(
            asset => NormalizeAssetPath(asset.RelativePath),
            StringComparer.OrdinalIgnoreCase);
        var legacyAssetIds = FindLegacyAssetIds(document, sourceAssetsByPath);
        var imageRefAssetIds = new Dictionary<Guid, Guid>();
        var imageRefPathMap = new Dictionary<Guid, string>();

        foreach (var asset in document.Assets.Where(asset => legacyAssetIds.Contains(asset.Id)))
        {
            var annotations = document.Steps
                .Where(step => step.ImageRefs.Count == 0)
                .SelectMany(step => step.Annotations)
                .Where(annotation => annotation.AssetId == asset.Id)
                .ToArray();
            var exportedRelativePath = RenderExportAsset(
                projectDirectory,
                exportAssetsDirectory,
                asset,
                annotations,
                crop: null,
                usedFileNames);
            assetPathMap[asset.Id] = exportedRelativePath;
            renderedAssets.Add(asset with
            {
                RelativePath = exportedRelativePath
            });
        }

        foreach (var step in document.Steps)
        {
            foreach (var imageRef in step.ImageRefs)
            {
                if (!sourceAssetsById.TryGetValue(imageRef.AssetId, out var sourceAsset))
                {
                    continue;
                }

                var exportedRelativePath = RenderExportAsset(
                    projectDirectory,
                    exportAssetsDirectory,
                    sourceAsset,
                    imageRef.Annotations,
                    imageRef.Crop,
                    usedFileNames);
                imageRefAssetIds[imageRef.Id] = imageRef.Id;
                imageRefPathMap[imageRef.Id] = exportedRelativePath;
                renderedAssets.Add(sourceAsset with
                {
                    Id = imageRef.Id,
                    RelativePath = exportedRelativePath
                });
            }
        }

        return document with
        {
            Assets = renderedAssets,
            Steps = document.Steps.Select(step => step with
            {
                Body = ReplaceStepBodyImageReferences(
                    step,
                    document.Assets,
                    assetPathMap,
                    imageRefPathMap,
                    out var referencedImageRefIds),
                AssetIds = step.ImageRefs.Count > 0
                    ? referencedImageRefIds
                        .Where(imageRefAssetIds.ContainsKey)
                        .Select(imageRefId => imageRefAssetIds[imageRefId])
                        .ToList()
                    : step.AssetIds,
                ImageRefs = [],
                Annotations = []
            }).ToList()
        };
    }

    private static string ReplaceStepBodyImageReferences(
        GuideStep step,
        IReadOnlyCollection<GuideAsset> sourceAssets,
        IReadOnlyDictionary<Guid, string> assetPathMap,
        IReadOnlyDictionary<Guid, string> imageRefPathMap,
        out HashSet<Guid> referencedImageRefIds)
    {
        if (step.ImageRefs.Count > 0)
        {
            return ReplaceImageReferencesForStepImageRefs(
                step.Body,
                step.ImageRefs,
                sourceAssets,
                imageRefPathMap,
                out referencedImageRefIds);
        }

        referencedImageRefIds = [];
        return ReplaceImageReferences(step.Body, sourceAssets, assetPathMap);
    }

    private static string RenderExportAsset(
        string projectDirectory,
        string exportAssetsDirectory,
        GuideAsset asset,
        IReadOnlyCollection<GuideAnnotation> annotations,
        ImageCropBounds? crop,
        ISet<string> usedFileNames)
    {
        var sourcePath = Path.Combine(projectDirectory, asset.RelativePath.Replace('/', Path.DirectorySeparatorChar));
        var shouldRenderImage = annotations.Count > 0 || crop is not null;
        var exportedFileName = CreateExportFileName(asset, shouldRenderImage, usedFileNames);
        var exportedPath = Path.Combine(exportAssetsDirectory, exportedFileName);

        if (File.Exists(sourcePath))
        {
            if (shouldRenderImage)
            {
                RenderImage(sourcePath, exportedPath, annotations, crop);
            }
            else
            {
                File.Copy(sourcePath, exportedPath, overwrite: true);
            }
        }

        return $"{ExportAssetsDirectoryName}/{exportedFileName}";
    }

    private static void RenderImage(
        string sourcePath,
        string exportedPath,
        IReadOnlyCollection<GuideAnnotation> annotations,
        ImageCropBounds? crop)
    {
        using var sourceImage = Drawing.Image.FromFile(sourcePath);
        var cropRectangle = CalculateCropRectangle(sourceImage.Width, sourceImage.Height, crop);
        using var bitmap = new Drawing.Bitmap(cropRectangle.Width, cropRectangle.Height, Imaging.PixelFormat.Format32bppArgb);
        using var graphics = Drawing.Graphics.FromImage(bitmap);
        graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        graphics.DrawImage(
            sourceImage,
            new Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
            cropRectangle,
            Drawing.GraphicsUnit.Pixel);

        foreach (var annotation in annotations)
        {
            DrawAnnotation(graphics, annotation, bitmap.Width, bitmap.Height);
        }

        bitmap.Save(exportedPath, Imaging.ImageFormat.Png);
    }

    private static Drawing.Rectangle CalculateCropRectangle(int imageWidth, int imageHeight, ImageCropBounds? crop)
    {
        if (crop is null)
        {
            return new Drawing.Rectangle(0, 0, imageWidth, imageHeight);
        }

        var left = ClampToRange((int)Math.Round(crop.X * imageWidth), 0, imageWidth - 1);
        var top = ClampToRange((int)Math.Round(crop.Y * imageHeight), 0, imageHeight - 1);
        var right = ClampToRange((int)Math.Round((crop.X + crop.Width) * imageWidth), left + 1, imageWidth);
        var bottom = ClampToRange((int)Math.Round((crop.Y + crop.Height) * imageHeight), top + 1, imageHeight);

        return new Drawing.Rectangle(left, top, right - left, bottom - top);
    }

    private static void DrawAnnotation(Drawing.Graphics graphics, GuideAnnotation annotation, int imageWidth, int imageHeight)
    {
        var bounds = annotation.Bounds;
        var x = (float)(bounds.X * imageWidth);
        var y = (float)(bounds.Y * imageHeight);
        var width = (float)(bounds.Width * imageWidth);
        var height = (float)(bounds.Height * imageHeight);
        var rectangle = new Drawing.RectangleF(x, y, width, height);

        switch (annotation.Kind)
        {
            case GuideAnnotationKind.Rectangle:
                using (var fill = new Drawing.SolidBrush(Drawing.Color.FromArgb(45, 251, 188, 4)))
                using (var pen = new Drawing.Pen(Drawing.Color.FromArgb(251, 188, 4), ScaleStroke(imageWidth, 4)))
                {
                    graphics.FillRectangle(fill, rectangle);
                    graphics.DrawRectangle(pen, x, y, width, height);
                }

                break;

            case GuideAnnotationKind.Blur:
                using (var fill = new Drawing.SolidBrush(Drawing.Color.FromArgb(255, 32, 33, 36)))
                {
                    graphics.FillRectangle(fill, rectangle);
                }

                break;

            case GuideAnnotationKind.Label:
                var style = annotation.Style ?? new GuideAnnotationStyle();
                var backgroundColor = ParseColor(style.BackgroundColor, Drawing.Color.FromArgb(26, 115, 232));
                var textColor = ParseColor(style.TextColor, Drawing.Color.White);
                using (var fill = new Drawing.SolidBrush(Drawing.Color.FromArgb(
                           (int)Math.Round(Math.Clamp(style.BackgroundOpacity, 0, 1) * 255),
                           backgroundColor)))
                using (var pen = new Drawing.Pen(backgroundColor, ScaleStroke(imageWidth, 2)))
                {
                    graphics.FillRectangle(fill, rectangle);
                    graphics.DrawRectangle(pen, x, y, width, height);
                }

                if (!string.IsNullOrWhiteSpace(annotation.Text))
                {
                    var fontStyle = Drawing.FontStyle.Regular;
                    if (style.IsBold)
                    {
                        fontStyle |= Drawing.FontStyle.Bold;
                    }

                    if (style.IsItalic)
                    {
                        fontStyle |= Drawing.FontStyle.Italic;
                    }

                    var fontSize = (float)Math.Max(6, style.FontSize * Math.Max(1, imageWidth / 960d));
                    using var font = new Drawing.Font(
                        Drawing.FontFamily.GenericSansSerif,
                        fontSize,
                        fontStyle,
                        Drawing.GraphicsUnit.Pixel);
                    using var textBrush = new Drawing.SolidBrush(textColor);
                    var textRectangle = new Drawing.RectangleF(x + 6, y + 4, Math.Max(1, width - 12), Math.Max(1, height - 8));
                    graphics.DrawString(annotation.Text, font, textBrush, textRectangle);
                }

                break;

            case GuideAnnotationKind.Arrow:
                var arrowColor = Drawing.Color.FromArgb(234, 67, 53);
                var lineWidth = ScaleStroke(imageWidth, 5);
                var centerY = y + height / 2;
                var endX = x + width;
                var headLength = Math.Min(width, Math.Max(lineWidth * 2.5f, ScaleStroke(imageWidth, 12)));
                var headHalfHeight = Math.Max(lineWidth, Math.Min(height / 2, headLength * 0.45f));

                using (var pen = new Drawing.Pen(arrowColor, lineWidth))
                using (var brush = new Drawing.SolidBrush(arrowColor))
                {
                    pen.StartCap = Drawing2D.LineCap.Round;
                    pen.EndCap = Drawing2D.LineCap.Flat;
                    graphics.DrawLine(pen, x, centerY, Math.Max(x, endX - headLength * 0.55f), centerY);
                    graphics.FillPolygon(
                        brush,
                        [
                            new Drawing.PointF(endX, centerY),
                            new Drawing.PointF(endX - headLength, centerY - headHalfHeight),
                            new Drawing.PointF(endX - headLength, centerY + headHalfHeight)
                        ]);
                }

                break;
        }
    }

    private static string ReplaceImageReferences(string body, IReadOnlyCollection<GuideAsset> sourceAssets, IReadOnlyDictionary<Guid, string> assetPathMap)
    {
        var updatedBody = body;
        foreach (var asset in sourceAssets)
        {
            if (!assetPathMap.TryGetValue(asset.Id, out var exportedPath))
            {
                continue;
            }

            updatedBody = updatedBody.Replace(
                $"[[image:{asset.RelativePath.Replace('\\', '/')}]]",
                $"[[image:{exportedPath}]]",
                StringComparison.OrdinalIgnoreCase);
        }

        return updatedBody;
    }

    private static string ReplaceImageReferencesForStepImageRefs(
        string body,
        IReadOnlyCollection<StepImageRef> imageRefs,
        IReadOnlyCollection<GuideAsset> sourceAssets,
        IReadOnlyDictionary<Guid, string> imageRefPathMap,
        out HashSet<Guid> referencedImageRefIds)
    {
        var referencedIds = new HashSet<Guid>();
        var sourceAssetsById = sourceAssets.ToDictionary(asset => asset.Id);
        var imageRefsByPath = new Dictionary<string, Queue<StepImageRef>>(StringComparer.OrdinalIgnoreCase);

        foreach (var imageRef in imageRefs)
        {
            if (!sourceAssetsById.TryGetValue(imageRef.AssetId, out var sourceAsset) ||
                !imageRefPathMap.ContainsKey(imageRef.Id))
            {
                continue;
            }

            var sourcePath = NormalizeAssetPath(sourceAsset.RelativePath);
            if (!imageRefsByPath.TryGetValue(sourcePath, out var queue))
            {
                queue = new Queue<StepImageRef>();
                imageRefsByPath[sourcePath] = queue;
            }

            queue.Enqueue(imageRef);
        }

        var renderedBody = ImageReferenceRegex.Replace(body, match =>
        {
            var relativePath = NormalizeAssetPath(match.Groups["path"].Value);
            if (!imageRefsByPath.TryGetValue(relativePath, out var queue) || queue.Count == 0)
            {
                return match.Value;
            }

            var imageRef = queue.Count > 1 ? queue.Dequeue() : queue.Peek();
            if (!imageRefPathMap.TryGetValue(imageRef.Id, out var exportedPath))
            {
                return match.Value;
            }

            referencedIds.Add(imageRef.Id);
            return $"[[image:{exportedPath}]]";
        });

        referencedImageRefIds = referencedIds;
        return renderedBody;
    }

    private static HashSet<Guid> FindLegacyAssetIds(
        GuideDocument document,
        IReadOnlyDictionary<string, GuideAsset> sourceAssetsByPath)
    {
        var assetIds = new HashSet<Guid>();

        foreach (var step in document.Steps)
        {
            if (step.ImageRefs.Count == 0)
            {
                foreach (var assetId in step.AssetIds)
                {
                    assetIds.Add(assetId);
                }

                foreach (Match match in ImageReferenceRegex.Matches(step.Body))
                {
                    var relativePath = NormalizeAssetPath(match.Groups["path"].Value);
                    if (sourceAssetsByPath.TryGetValue(relativePath, out var asset))
                    {
                        assetIds.Add(asset.Id);
                    }
                }
            }
        }

        return assetIds;
    }

    private static string CreateExportFileName(GuideAsset asset, bool hasAnnotations, ISet<string> usedFileNames)
    {
        var sourceFileName = Path.GetFileName(asset.RelativePath.Replace('\\', '/'));
        var extension = hasAnnotations ? ".png" : Path.GetExtension(sourceFileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".png";
        }

        var stem = Path.GetFileNameWithoutExtension(sourceFileName);
        if (string.IsNullOrWhiteSpace(stem))
        {
            stem = "image";
        }

        stem = SanitizeFileName(stem);
        var candidate = $"{stem}{extension.ToLowerInvariant()}";
        var index = 2;
        while (!usedFileNames.Add(candidate))
        {
            candidate = $"{stem}-{index}{extension.ToLowerInvariant()}";
            index++;
        }

        return candidate;
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(value.Select(character => invalid.Contains(character) ? '-' : character)).Trim();
    }

    private static string NormalizeAssetPath(string value)
    {
        return value.Trim().Replace('\\', '/');
    }

    private static int ClampToRange(int value, int min, int max)
    {
        return Math.Min(Math.Max(value, min), max);
    }

    private static Drawing.Color ParseColor(string color, Drawing.Color fallback)
    {
        if (color.Length != 7 || color[0] != '#' || !color.Skip(1).All(Uri.IsHexDigit))
        {
            return fallback;
        }

        return Drawing.Color.FromArgb(
            Convert.ToInt32(color.Substring(1, 2), 16),
            Convert.ToInt32(color.Substring(3, 2), 16),
            Convert.ToInt32(color.Substring(5, 2), 16));
    }

    private static float ScaleStroke(int imageWidth, float baseWidth)
    {
        return Math.Max(baseWidth, imageWidth / 320f * baseWidth);
    }
}
