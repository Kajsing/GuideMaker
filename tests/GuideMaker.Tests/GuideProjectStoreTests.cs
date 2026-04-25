using GuideMaker.Core;
using GuideMaker.Storage;
using Xunit;

namespace GuideMaker.Tests;

public sealed class GuideProjectStoreTests
{
    [Fact]
    public async Task CreateAndLoadAsync_RoundTripsGuideDocument()
    {
        var projectDirectory = Path.Combine(Path.GetTempPath(), "GuideMaker.Tests", Guid.NewGuid().ToString("N"));

        try
        {
            var document = GuideDocument.Create("Printer setup", "Codex") with
            {
                Steps =
                [
                    GuideStep.Create(1, "Open settings", "Open the Windows Settings app.")
                ]
            };

            var store = new GuideProjectStore();

            await store.CreateAsync(projectDirectory, document);
            var loaded = await store.LoadAsync(projectDirectory);

            Assert.Equal("Printer setup", loaded.Document.Metadata.Title);
            Assert.Single(loaded.Document.Steps);
            Assert.Equal("Open settings", loaded.Document.Steps[0].Title);
            Assert.True(File.Exists(Path.Combine(projectDirectory, GuideProjectLayout.GuideFileName)));
            Assert.True(Directory.Exists(Path.Combine(projectDirectory, GuideProjectLayout.AssetsDirectoryName)));
            Assert.True(Directory.Exists(Path.Combine(projectDirectory, GuideProjectLayout.ExportsDirectoryName)));
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
