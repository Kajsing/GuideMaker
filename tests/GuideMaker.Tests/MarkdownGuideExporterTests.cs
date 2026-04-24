using GuideMaker.Core;
using GuideMaker.Export;

namespace GuideMaker.Tests;

public sealed class MarkdownGuideExporterTests
{
    [Fact]
    public void Export_IncludesStepsAndScreenshotReferences()
    {
        var assetId = Guid.NewGuid();
        var document = GuideDocument.Create("Medication app guide", "Codex") with
        {
            Steps =
            [
                GuideStep.Create(1, "Open the app", "Start the application.") with
                {
                    AssetIds = [assetId]
                }
            ],
            Assets =
            [
                new GuideAsset
                {
                    Id = assetId,
                    RelativePath = "assets/open-app.png",
                    Kind = GuideAssetKind.Screenshot,
                    Caption = "Application start screen",
                    CapturedAt = DateTimeOffset.UtcNow
                }
            ]
        };

        var markdown = new MarkdownGuideExporter().Export(document);

        Assert.Contains("# Medication app guide", markdown);
        Assert.Contains("## 1. Open the app", markdown);
        Assert.Contains("![Application start screen](assets/open-app.png)", markdown);
    }
}
