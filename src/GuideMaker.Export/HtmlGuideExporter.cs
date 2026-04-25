using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using GuideMaker.Core;

namespace GuideMaker.Export;

public sealed class HtmlGuideExporter
{
    private static readonly Regex ImageReferenceRegex = new(@"\[\[image:(?<path>[^\]]+)\]\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Export(GuideDocument document, string assetPathPrefix = "")
    {
        ArgumentNullException.ThrowIfNull(document);

        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html lang=\"da\">");
        builder.AppendLine("<head>");
        builder.AppendLine("  <meta charset=\"utf-8\">");
        builder.Append("  <title>").Append(WebUtility.HtmlEncode(document.Metadata.Title)).AppendLine("</title>");
        builder.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        builder.AppendLine("  <style>");
        builder.AppendLine("    .guide-image-frame { position: relative; display: inline-block; max-width: 100%; }");
        builder.AppendLine("    .guide-image-frame img { display: block; max-width: 100%; height: auto; }");
        builder.AppendLine("    .guide-annotation { position: absolute; box-sizing: border-box; }");
        builder.AppendLine("    .guide-annotation-rectangle { border: 4px solid #fbbc04; background: rgba(251,188,4,.18); }");
        builder.AppendLine("    .guide-annotation-blur { background: rgba(32,33,36,.62); backdrop-filter: blur(8px); -webkit-backdrop-filter: blur(8px); }");
        builder.AppendLine("    .guide-annotation-label { border: 2px solid #1a73e8; background: rgba(26,115,232,.92); color: #fff; padding: 4px 8px; font: 600 14px Segoe UI, sans-serif; }");
        builder.AppendLine("    .guide-annotation-arrow { border-top: 4px solid #ea4335; transform: rotate(-8deg); transform-origin: left center; }");
        builder.AppendLine("    .guide-annotation-arrow::after { content: ''; position: absolute; right: -2px; top: -8px; border-left: 12px solid #ea4335; border-top: 6px solid transparent; border-bottom: 6px solid transparent; }");
        builder.AppendLine("  </style>");
        builder.AppendLine("</head>");
        builder.AppendLine("<body>");
        builder.Append("  <h1>").Append(WebUtility.HtmlEncode(document.Metadata.Title)).AppendLine("</h1>");

        if (!string.IsNullOrWhiteSpace(document.Metadata.Description))
        {
            builder.Append("  <p>").Append(WebUtility.HtmlEncode(document.Metadata.Description)).AppendLine("</p>");
        }

        foreach (var step in document.Steps.OrderBy(step => step.Order))
        {
            builder.AppendLine("  <section>");
            builder.Append("    <h2>").Append(step.Order).Append(". ")
                .Append(WebUtility.HtmlEncode(step.Title)).AppendLine("</h2>");

            HashSet<Guid> renderedAssetIds;
            if (!string.IsNullOrWhiteSpace(step.Body))
            {
                RenderBody(builder, document, step, assetPathPrefix, out renderedAssetIds);
            }
            else
            {
                renderedAssetIds = [];
            }

            foreach (var assetId in step.AssetIds)
            {
                if (renderedAssetIds.Contains(assetId))
                {
                    continue;
                }

                var asset = document.Assets.FirstOrDefault(candidate => candidate.Id == assetId);
                if (asset is null)
                {
                    continue;
                }

                RenderAsset(builder, asset, step.Title, step.Annotations, assetPathPrefix);
            }

            builder.AppendLine("  </section>");
        }

        builder.AppendLine("</body>");
        builder.AppendLine("</html>");

        return builder.ToString();
    }

    private static void RenderBody(
        StringBuilder builder,
        GuideDocument document,
        GuideStep step,
        string assetPathPrefix,
        out HashSet<Guid> renderedAssetIds)
    {
        renderedAssetIds = [];
        var lines = step.Body.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

        foreach (var line in lines)
        {
            var match = ImageReferenceRegex.Match(line.Trim());
            if (match.Success && match.Value == line.Trim())
            {
                var relativePath = NormalizeAssetPath(match.Groups["path"].Value);
                var asset = document.Assets.FirstOrDefault(candidate =>
                    string.Equals(NormalizeAssetPath(candidate.RelativePath), relativePath, StringComparison.OrdinalIgnoreCase));

                if (asset is not null)
                {
                    RenderAsset(builder, asset, step.Title, step.Annotations, assetPathPrefix);
                    renderedAssetIds.Add(asset.Id);
                    continue;
                }
            }

            if (!string.IsNullOrWhiteSpace(line))
            {
                builder.Append("    <p>").Append(WebUtility.HtmlEncode(line)).AppendLine("</p>");
            }
        }
    }

    private static void RenderAsset(
        StringBuilder builder,
        GuideAsset asset,
        string fallbackTitle,
        IReadOnlyCollection<GuideAnnotation> annotations,
        string assetPathPrefix)
    {
        var imagePath = WebUtility.HtmlEncode(assetPathPrefix + asset.RelativePath.Replace('\\', '/'));
        var altText = WebUtility.HtmlEncode(asset.AltText ?? asset.Caption ?? fallbackTitle);
        builder.AppendLine("    <div class=\"guide-image-frame\">");
        builder.Append("      <img src=\"").Append(imagePath).Append("\" alt=\"")
            .Append(altText).AppendLine("\">");

        foreach (var annotation in annotations.Where(annotation => annotation.AssetId == asset.Id))
        {
            RenderAnnotation(builder, annotation);
        }

        builder.AppendLine("    </div>");

        if (!string.IsNullOrWhiteSpace(asset.Caption))
        {
            builder.Append("    <p><em>").Append(WebUtility.HtmlEncode(asset.Caption)).AppendLine("</em></p>");
        }
    }

    private static void RenderAnnotation(StringBuilder builder, GuideAnnotation annotation)
    {
        var cssClass = annotation.Kind switch
        {
            GuideAnnotationKind.Rectangle => "guide-annotation-rectangle",
            GuideAnnotationKind.Arrow => "guide-annotation-arrow",
            GuideAnnotationKind.Label => "guide-annotation-label",
            GuideAnnotationKind.Blur => "guide-annotation-blur",
            _ => "guide-annotation-rectangle"
        };
        var bounds = annotation.Bounds;

        builder.Append("      <div class=\"guide-annotation ").Append(cssClass).Append("\" style=\"left:")
            .Append(ToPercent(bounds.X)).Append("%;top:")
            .Append(ToPercent(bounds.Y)).Append("%;width:")
            .Append(ToPercent(bounds.Width)).Append("%;height:")
            .Append(ToPercent(bounds.Height)).Append("%;\">");

        if (annotation.Kind == GuideAnnotationKind.Label && !string.IsNullOrWhiteSpace(annotation.Text))
        {
            builder.Append(WebUtility.HtmlEncode(annotation.Text));
        }

        builder.AppendLine("</div>");
    }

    private static string ToPercent(double value)
    {
        return (value * 100).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string NormalizeAssetPath(string value)
    {
        return value.Trim().Replace('\\', '/');
    }
}
