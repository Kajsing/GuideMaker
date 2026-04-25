using System.Text;
using GuideMaker.Core;

namespace GuideMaker.Export;

public sealed class MarkdownGuideExporter
{
    public string Export(GuideDocument document)
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

            if (!string.IsNullOrWhiteSpace(step.Body))
            {
                builder.AppendLine(step.Body);
                builder.AppendLine();
            }

            foreach (var assetId in step.AssetIds)
            {
                var asset = document.Assets.FirstOrDefault(candidate => candidate.Id == assetId);
                if (asset is null)
                {
                    continue;
                }

                var imagePath = EscapeMarkdownLinkTarget(asset.RelativePath.Replace('\\', '/'));
                var altText = EscapeMarkdownAltText(string.IsNullOrWhiteSpace(asset.AltText) ? asset.Caption ?? step.Title : asset.AltText);
                builder.Append("![").Append(altText).Append("](").Append(imagePath).AppendLine(")");

                if (!string.IsNullOrWhiteSpace(asset.Caption))
                {
                    builder.AppendLine();
                    builder.Append("*").Append(asset.Caption).AppendLine("*");
                }

                builder.AppendLine();
            }
        }

        return builder.ToString();
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
