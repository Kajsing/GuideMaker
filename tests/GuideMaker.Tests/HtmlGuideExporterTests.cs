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
}
