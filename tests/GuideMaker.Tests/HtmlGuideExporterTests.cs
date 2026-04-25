using GuideMaker.Core;
using GuideMaker.Export;
using Xunit;

namespace GuideMaker.Tests;

public sealed class HtmlGuideExporterTests
{
    [Fact]
    public void Export_EncodesHtmlContent()
    {
        var document = GuideDocument.Create("App <setup>", "Codex") with
        {
            Metadata = GuideDocument.Create("App <setup>", "Codex").Metadata with
            {
                Description = "Use <safe> values only."
            },
            Steps =
            [
                GuideStep.Create(1, "Open <settings>", "Click <Start>.")
            ]
        };

        var html = new HtmlGuideExporter().Export(document);

        Assert.Contains("<title>App &lt;setup&gt;</title>", html);
        Assert.Contains("<h1>App &lt;setup&gt;</h1>", html);
        Assert.Contains("<p>Use &lt;safe&gt; values only.</p>", html);
        Assert.Contains("<h2>1. Open &lt;settings&gt;</h2>", html);
        Assert.Contains("<p>Click &lt;Start&gt;.</p>", html);
    }

    [Fact]
    public void Export_RendersImageReferenceTokenInBody()
    {
        var assetId = Guid.NewGuid();
        var document = GuideDocument.Create("App guide", "Codex") with
        {
            Steps =
            [
                GuideStep.Create(1, "Open app", "Before image.\n[[image:assets/open-app.png]]\nAfter image.") with
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

        var html = new HtmlGuideExporter().Export(document, "../");

        Assert.Contains("<p>Before image.</p>", html);
        Assert.Contains("<img src=\"../assets/open-app.png\" alt=\"Application start screen\">", html);
        Assert.Contains("<p>After image.</p>", html);
        Assert.Equal(1, CountOccurrences(html, "<img src=\"../assets/open-app.png\""));
    }

    [Fact]
    public void Export_RendersAnnotationsAsImageOverlays()
    {
        var assetId = Guid.NewGuid();
        var document = GuideDocument.Create("App guide", "Codex") with
        {
            Steps =
            [
                GuideStep.Create(1, "Open app") with
                {
                    AssetIds = [assetId],
                    Annotations =
                    [
                        new GuideAnnotation
                        {
                            Id = Guid.NewGuid(),
                            Kind = GuideAnnotationKind.Rectangle,
                            AssetId = assetId,
                            Bounds = new AnnotationBounds
                            {
                                X = 0.1,
                                Y = 0.2,
                                Width = 0.3,
                                Height = 0.4
                            }
                        },
                        new GuideAnnotation
                        {
                            Id = Guid.NewGuid(),
                            Kind = GuideAnnotationKind.Label,
                            AssetId = assetId,
                            Text = "Click here",
                            Bounds = new AnnotationBounds
                            {
                                X = 0.5,
                                Y = 0.1,
                                Width = 0.2,
                                Height = 0.1
                            }
                        }
                    ]
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

        var html = new HtmlGuideExporter().Export(document, "../");

        Assert.Contains("class=\"guide-image-frame\"", html);
        Assert.Contains("guide-annotation-rectangle", html);
        Assert.Contains("left:10%;top:20%;width:30%;height:40%;", html);
        Assert.Contains("guide-annotation-label", html);
        Assert.Contains("Click here", html);
    }

    private static int CountOccurrences(string value, string search)
    {
        var count = 0;
        var index = 0;

        while ((index = value.IndexOf(search, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += search.Length;
        }

        return count;
    }
}
