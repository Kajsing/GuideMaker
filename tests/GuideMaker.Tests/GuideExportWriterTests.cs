using GuideMaker.Core;
using GuideMaker.Export;
using GuideMaker.Storage;
using Xunit;

namespace GuideMaker.Tests;

public sealed class GuideExportWriterTests
{
    [Fact]
    public async Task ExportAsync_WritesMarkdownAndHtmlFiles()
    {
        var projectDirectory = Path.Combine(Path.GetTempPath(), "GuideMaker.Tests", Guid.NewGuid().ToString("N"));
        var document = GuideDocument.Create("Printer setup", "Codex") with
        {
            Steps =
            [
                GuideStep.Create(1, "Open settings", "Open the Windows Settings app.")
            ]
        };

        try
        {
            var writer = new GuideExportWriter();

            var result = await writer.ExportAsync(projectDirectory, document);

            Assert.True(File.Exists(result.MarkdownPath));
            Assert.True(File.Exists(result.HtmlPath));
            Assert.EndsWith(Path.Combine(GuideProjectLayout.ExportsDirectoryName, GuideExportWriter.MarkdownFileName), result.MarkdownPath);
            Assert.EndsWith(Path.Combine(GuideProjectLayout.ExportsDirectoryName, GuideExportWriter.HtmlFileName), result.HtmlPath);

            var markdown = await File.ReadAllTextAsync(result.MarkdownPath);
            var html = await File.ReadAllTextAsync(result.HtmlPath);

            Assert.Contains("# Printer setup", markdown);
            Assert.Contains("<h1>Printer setup</h1>", html);
        }
        finally
        {
            if (Directory.Exists(projectDirectory))
            {
                Directory.Delete(projectDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ExportAsync_ExportsStarterGuideSample()
    {
        var sampleDirectory = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "samples",
            "starter-guide"));

        var sandboxDirectory = Path.Combine(Path.GetTempPath(), "GuideMaker.Tests", Guid.NewGuid().ToString("N"));

        try
        {
            CopyDirectory(sampleDirectory, sandboxDirectory);
            var project = await new GuideProjectStore().LoadAsync(sandboxDirectory);

            var result = await new GuideExportWriter().ExportAsync(sandboxDirectory, project.Document);

            var markdown = await File.ReadAllTextAsync(result.MarkdownPath);
            var html = await File.ReadAllTextAsync(result.HtmlPath);

            Assert.Contains("# Starter guide", markdown);
            Assert.Contains("![Screenshot of the application start screen.](assets/open-application.png)", markdown);
            Assert.Contains("<h1>Starter guide</h1>", html);
            Assert.Contains("<img src=\"assets/open-application.png\" alt=\"Screenshot of the application start screen.\">", html);
        }
        finally
        {
            if (Directory.Exists(sandboxDirectory))
            {
                Directory.Delete(sandboxDirectory, recursive: true);
            }
        }
    }

    private static void CopyDirectory(string sourceDirectory, string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);

        foreach (var directory in Directory.EnumerateDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(directory.Replace(sourceDirectory, destinationDirectory, StringComparison.Ordinal));
        }

        foreach (var file in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            File.Copy(
                file,
                file.Replace(sourceDirectory, destinationDirectory, StringComparison.Ordinal),
                overwrite: true);
        }
    }
}
