using System.Text;
using System.Text.RegularExpressions;
using GuideMaker.Core;

namespace GuideMaker.Export;

public sealed class MarkdownGuideExporter
{
    private static readonly Regex ImageReferenceRegex = new(@"\[\[image:(?<path>[^\]]+)\]\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Export(GuideDocument document, string assetPathPrefix = "")
    {
        ArgumentNullException.ThrowIfNull(document);

        var builder = new StringBuilder();
        builder.Append("# ").AppendLine(document.Metadata.Title);
        builder.AppendLine();

        if (!string.IsNullOrWhiteSpace(document.Metadata.Description))
        {
            builder.AppendLine(document.Metadata.Description);
            builder.AppendLine();
        }

        foreach (var step in document.Steps.OrderBy(step => step.Order))
        {
            builder.Append("## ").Append(step.Order).Append(". ").AppendLine(step.Title);
            builder.AppendLine();

            HashSet<Guid> renderedAssetIds;
            if (!string.IsNullOrWhiteSpace(step.Body))
            {
                RenderBody(builder, document, step, assetPathPrefix, out renderedAssetIds);
                builder.AppendLine();
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
        }

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

            builder.AppendLine(line);
        }
    }

    private static void RenderAsset(StringBuilder builder, GuideAsset asset, string fallbackTitle, string assetPathPrefix)
    {
        var imagePath = EscapeMarkdownLinkTarget(assetPathPrefix + asset.RelativePath.Replace('\\', '/'));
        var altText = EscapeMarkdownAltText(string.IsNullOrWhiteSpace(asset.AltText) ? asset.Caption ?? fallbackTitle : asset.AltText);
        builder.Append("![").Append(altText).Append("](").Append(imagePath).AppendLine(")");

        if (!string.IsNullOrWhiteSpace(asset.Caption))
        {
            builder.AppendLine();
            builder.Append("*").Append(asset.Caption).AppendLine("*");
        }

        builder.AppendLine();
    }

    private static string NormalizeAssetPath(string value)
    {
        return value.Trim().Replace('\\', '/');
    }

    private static string EscapeMarkdownAltText(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("[", "\\[", StringComparison.Ordinal)
            .Replace("]", "\\]", StringComparison.Ordinal);
    }

    private static string EscapeMarkdownLinkTarget(string value)
    {
        return value
            .Replace(" ", "%20", StringComparison.Ordinal)
            .Replace("(", "%28", StringComparison.Ordinal)
            .Replace(")", "%29", StringComparison.Ordinal);
    }
}
