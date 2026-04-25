using GuideMaker.Core;
using GuideMaker.Storage;
using Xunit;

namespace GuideMaker.Tests;

public sealed class GuideAssetFileStoreTests
{
    [Fact]
    public async Task ImportImageAsync_CopiesImageIntoAssetsFolder()
    {
        var projectDirectory = Path.Combine(Path.GetTempPath(), "GuideMaker.Tests", Guid.NewGuid().ToString("N"));
        var sourceFilePath = Path.Combine(Path.GetTempPath(), $"source-{Guid.NewGuid():N}.png");

        try
        {
            await File.WriteAllBytesAsync(sourceFilePath, [137, 80, 78, 71]);
            var project = await new GuideProjectStore().CreateAsync(projectDirectory, GuideDocument.Create("Image guide", "Codex"));

            var asset = await new GuideAssetFileStore().ImportImageAsync(project, sourceFilePath);

            Assert.StartsWith("assets/source-", asset.RelativePath, StringComparison.Ordinal);
            Assert.EndsWith(".png", asset.RelativePath, StringComparison.Ordinal);
            Assert.True(File.Exists(Path.Combine(projectDirectory, asset.RelativePath)));
            Assert.Equal(GuideAssetKind.ImportedImage, asset.Kind);
        }
        finally
        {
            if (Directory.Exists(projectDirectory))
            {
                Directory.Delete(projectDirectory, recursive: true);
            }

            if (File.Exists(sourceFilePath))
            {
                File.Delete(sourceFilePath);
            }
        }
    }

    [Fact]
    public async Task ImportImageAsync_CreatesUniqueAssetNames()
    {
        var projectDirectory = Path.Combine(Path.GetTempPath(), "GuideMaker.Tests", Guid.NewGuid().ToString("N"));
        var sourceFilePath = Path.Combine(Path.GetTempPath(), $"duplicate-{Guid.NewGuid():N}.png");

        try
        {
            await File.WriteAllBytesAsync(sourceFilePath, [137, 80, 78, 71]);
            var project = await new GuideProjectStore().CreateAsync(projectDirectory, GuideDocument.Create("Image guide", "Codex"));
            var fileStore = new GuideAssetFileStore();

            var first = await fileStore.ImportImageAsync(project, sourceFilePath);
            var second = await fileStore.ImportImageAsync(project, sourceFilePath);

            Assert.NotEqual(first.RelativePath, second.RelativePath);
            Assert.True(File.Exists(Path.Combine(projectDirectory, first.RelativePath)));
            Assert.True(File.Exists(Path.Combine(projectDirectory, second.RelativePath)));
        }
        finally
        {
            if (Directory.Exists(projectDirectory))
            {
                Directory.Delete(projectDirectory, recursive: true);
            }

            if (File.Exists(sourceFilePath))
            {
                File.Delete(sourceFilePath);
            }
        }
    }

    [Fact]
    public async Task SavePngAsync_WritesPngAsset()
    {
        var projectDirectory = Path.Combine(Path.GetTempPath(), "GuideMaker.Tests", Guid.NewGuid().ToString("N"));

        try
        {
            var project = await new GuideProjectStore().CreateAsync(projectDirectory, GuideDocument.Create("Image guide", "Codex"));
            await using var stream = new MemoryStream([137, 80, 78, 71]);

            var asset = await new GuideAssetFileStore().SavePngAsync(project, "clipboard image", stream);

            Assert.Equal("assets/clipboard image.png", asset.RelativePath);
            Assert.True(File.Exists(Path.Combine(projectDirectory, asset.RelativePath)));
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
