using GuideMaker.Core;
using Drawing = System.Drawing;
using Drawing2D = System.Drawing.Drawing2D;
using Imaging = System.Drawing.Imaging;

namespace GuideMaker.Export;

public sealed class ExportAssetRenderer
{
    public const string ExportAssetsDirectoryName = "assets";

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

        foreach (var asset in document.Assets)
        {
            var sourcePath = Path.Combine(projectDirectory, asset.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            var annotations = document.Steps
                .SelectMany(step => step.Annotations)
                .Where(annotation => annotation.AssetId == asset.Id)
                .ToArray();
            var exportedFileName = CreateExportFileName(asset, annotations.Length > 0, usedFileNames);
            var exportedPath = Path.Combine(exportAssetsDirectory, exportedFileName);

            if (File.Exists(sourcePath))
            {
                if (annotations.Length > 0)
                {
                    RenderAnnotatedImage(sourcePath, exportedPath, annotations);
                }
                else
                {
                    File.Copy(sourcePath, exportedPath, overwrite: true);
                }
            }

            var exportedRelativePath = $"{ExportAssetsDirectoryName}/{exportedFileName}";
            assetPathMap[asset.Id] = exportedRelativePath;
            renderedAssets.Add(asset with
            {
                RelativePath = exportedRelativePath
            });
        }

        return document with
        {
            Assets = renderedAssets,
            Steps = document.Steps.Select(step => step with
            {
                Body = ReplaceImageReferences(step.Body, document.Assets, assetPathMap),
                Annotations = []
            }).ToList()
        };
    }

    private static void RenderAnnotatedImage(string sourcePath, string exportedPath, IReadOnlyCollection<GuideAnnotation> annotations)
    {
        using var sourceImage = Drawing.Image.FromFile(sourcePath);
        using var bitmap = new Drawing.Bitmap(sourceImage.Width, sourceImage.Height, Imaging.PixelFormat.Format32bppArgb);
        using var graphics = Drawing.Graphics.FromImage(bitmap);
        graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        graphics.DrawImage(sourceImage, 0, 0, sourceImage.Width, sourceImage.Height);

        foreach (var annotation in annotations)
        {
            DrawAnnotation(graphics, annotation, sourceImage.Width, sourceImage.Height);
        }

        bitmap.Save(exportedPath, Imaging.ImageFormat.Png);
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
                using (var pen = new Drawing.Pen(Drawing.Color.FromArgb(234, 67, 53), ScaleStroke(imageWidth, 5)))
                {
                    pen.EndCap = Drawing2D.LineCap.ArrowAnchor;
                    graphics.DrawLine(pen, x, y + height / 2, x + width, y + height / 2);
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
