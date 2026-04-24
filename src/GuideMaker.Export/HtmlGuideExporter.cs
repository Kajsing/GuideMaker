using System.Net;
using System.Text;
using GuideMaker.Core;

namespace GuideMaker.Export;

public sealed class HtmlGuideExporter
{
    public string Export(GuideDocument document)
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

            if (!string.IsNullOrWhiteSpace(step.Body))
            {
                builder.Append("    <p>").Append(WebUtility.HtmlEncode(step.Body)).AppendLine("</p>");
            }

            foreach (var assetId in step.AssetIds)
            {
                var asset = document.Assets.FirstOrDefault(candidate => candidate.Id == assetId);
                if (asset is null)
                {
                    continue;
                }

                var imagePath = WebUtility.HtmlEncode(asset.RelativePath.Replace('\\', '/'));
                var altText = WebUtility.HtmlEncode(asset.AltText ?? asset.Caption ?? step.Title);
                builder.Append("    <img src=\"").Append(imagePath).Append("\" alt=\"")
                    .Append(altText).AppendLine("\">");

                if (!string.IsNullOrWhiteSpace(asset.Caption))
                {
                    builder.Append("    <p><em>").Append(WebUtility.HtmlEncode(asset.Caption)).AppendLine("</em></p>");
                }
            }

            builder.AppendLine("  </section>");
        }

        builder.AppendLine("</body>");
        builder.AppendLine("</html>");

        return builder.ToString();
    }
}
