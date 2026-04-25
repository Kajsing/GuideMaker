using GuideMaker.Core;
using GuideMaker.Export;
using Xunit;

namespace GuideMaker.Tests;

public sealed class PdfGuideExporterTests
{
    [Fact]
    public void Export_CreatesPdfBytes()
    {
        var document = GuideDocument.Create("Printer setup", "Codex") with
        {
            Metadata = GuideDocument.Create("Printer setup", "Codex").Metadata with
            {
                Description = "Local export smoke test."
            },
            Steps =
            [
                GuideStep.Create(1, "Open settings", "Open the Windows Settings app.")
            ]
        };

        var bytes = new PdfGuideExporter().Export(document, Path.GetTempPath());

        Assert.True(bytes.Length > 100);
        Assert.Equal("%PDF-"u8.ToArray(), bytes.Take(5).ToArray());
    }

    [Fact]
    public void Export_ToleratesMissingImages()
    {
        var assetId = Guid.NewGuid();
        var document = GuideDocument.Create("Missing image guide", "Codex") with
        {
            Steps =
            [
                GuideStep.Create(1, "Open app") with
                {
                    AssetIds = [assetId]
                }
            ],
            Assets =
            [
                new GuideAsset
                {
                    Id = assetId,
                    RelativePath = "assets/missing.png",
                    Kind = GuideAssetKind.Screenshot,
                    CapturedAt = DateTimeOffset.UtcNow
                }
            ]
        };

        var bytes = new PdfGuideExporter().Export(document, Path.GetTempPath());

        Assert.Equal("%PDF-"u8.ToArray(), bytes.Take(5).ToArray());
    }

    [Fact]
    public void Export_RendersImageAssets()
    {
        var projectDirectory = Path.Combine(Path.GetTempPath(), "GuideMaker.Tests", Guid.NewGuid().ToString("N"));
        var assetsDirectory = Path.Combine(projectDirectory, "assets");
        Directory.CreateDirectory(assetsDirectory);

        try
        {
            File.WriteAllBytes(
                Path.Combine(assetsDirectory, "pixel.png"),
                Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII="));

            var assetId = Guid.NewGuid();
            var document = GuideDocument.Create("Image guide", "Codex") with
            {
                Steps =
                [
                    GuideStep.Create(1, "Open app") with
                    {
                        AssetIds = [assetId]
                    }
                ],
                Assets =
                [
                    new GuideAsset
                    {
                        Id = assetId,
                        RelativePath = "assets/pixel.png",
                        Kind = GuideAssetKind.Screenshot,
                        Caption = "Tiny image",
                        CapturedAt = DateTimeOffset.UtcNow
                    }
                ]
            };

            var bytes = new PdfGuideExporter().Export(document, projectDirectory);

            Assert.Equal("%PDF-"u8.ToArray(), bytes.Take(5).ToArray());
        }
        finally
        {
            if (Directory.Exists(projectDirectory))
            {
                Directory.Delete(projectDirectory, recursive: true);
            }
        }
    }
}
