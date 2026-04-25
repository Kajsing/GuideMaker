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

                RenderAsset(builder, asset, step.Title, assetPathPrefix);
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
                    RenderAsset(builder, asset, step.Title, assetPathPrefix);
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

    private static void RenderAsset(StringBuilder builder, GuideAsset asset, string fallbackTitle, string assetPathPrefix)
    {
        var imagePath = WebUtility.HtmlEncode(assetPathPrefix + asset.RelativePath.Replace('\\', '/'));
        var altText = WebUtility.HtmlEncode(asset.AltText ?? asset.Caption ?? fallbackTitle);
        builder.Append("    <img src=\"").Append(imagePath).Append("\" alt=\"")
            .Append(altText).AppendLine("\">");

        if (!string.IsNullOrWhiteSpace(asset.Caption))
        {
            builder.Append("    <p><em>").Append(WebUtility.HtmlEncode(asset.Caption)).AppendLine("</em></p>");
        }
    }

    private static string NormalizeAssetPath(string value)
    {
        return value.Trim().Replace('\\', '/');
    }
}
