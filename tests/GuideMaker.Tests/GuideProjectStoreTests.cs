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
            Assert.Empty(loaded.MissingAssetPaths);
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

    [Fact]
    public async Task LoadAsync_WhenGuideFileIsMissing_ThrowsStorageException()
    {
        var projectDirectory = Path.Combine(Path.GetTempPath(), "GuideMaker.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(projectDirectory);

        try
        {
            var store = new GuideProjectStore();

            var exception = await Assert.ThrowsAsync<GuideProjectFileMissingException>(
                () => store.LoadAsync(projectDirectory));

            Assert.EndsWith(GuideProjectLayout.GuideFileName, exception.GuideFilePath);
        }
        finally
        {
            Directory.Delete(projectDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_UpdatesExistingProject()
    {
        var projectDirectory = Path.Combine(Path.GetTempPath(), "GuideMaker.Tests", Guid.NewGuid().ToString("N"));

        try
        {
            var store = new GuideProjectStore();
            var created = await store.CreateAsync(projectDirectory, GuideDocument.Create("Printer setup", "Codex"));

            await store.SaveAsync(created with
            {
                Document = created.Document with
                {
                    Steps =
                    [
                        GuideStep.Create(1, "Install driver", "Install the printer driver.")
                    ]
                }
            });

            var loaded = await store.LoadAsync(projectDirectory);

            Assert.Single(loaded.Document.Steps);
            Assert.Equal("Install driver", loaded.Document.Steps[0].Title);
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
    public async Task ScanUnregisteredImages_WhenAssetsFolderHasOrphanImages_ReturnsOnlyUnregisteredImages()
    {
        var projectDirectory = Path.Combine(Path.GetTempPath(), "GuideMaker.Tests", Guid.NewGuid().ToString("N"));

        try
        {
            var registeredAsset = new GuideAsset
            {
                Id = Guid.NewGuid(),
                RelativePath = "assets/registered.png",
                Kind = GuideAssetKind.ImportedImage,
                Caption = "Registered",
                CapturedAt = DateTimeOffset.UtcNow
            };
            var store = new GuideProjectStore();
            var project = await store.CreateAsync(projectDirectory, GuideDocument.Create("Printer setup", "Codex") with
            {
                Assets = [registeredAsset]
            });
            var assetsDirectory = Path.Combine(projectDirectory, GuideProjectLayout.AssetsDirectoryName);
            await File.WriteAllBytesAsync(Path.Combine(assetsDirectory, "registered.png"), [1, 2, 3]);
            await File.WriteAllBytesAsync(Path.Combine(assetsDirectory, "orphan.png"), [4, 5, 6]);
            await File.WriteAllTextAsync(Path.Combine(assetsDirectory, "notes.txt"), "not an image");

            var discoveredAssets = new GuideAssetFileStore().ScanUnregisteredImages(project);

            var discoveredAsset = Assert.Single(discoveredAssets);
            Assert.Equal("assets/orphan.png", discoveredAsset.RelativePath);
            Assert.Equal(GuideAssetKind.ImportedImage, discoveredAsset.Kind);
            Assert.Equal("orphan", discoveredAsset.Caption);
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
    public async Task FindMissingRegisteredImages_WhenRegisteredAssetFileIsMissing_ReturnsRelativePath()
    {
        var projectDirectory = Path.Combine(Path.GetTempPath(), "GuideMaker.Tests", Guid.NewGuid().ToString("N"));

        try
        {
            var missingAsset = new GuideAsset
            {
                Id = Guid.NewGuid(),
                RelativePath = "assets/missing.png",
                Kind = GuideAssetKind.ImportedImage,
                Caption = "Missing",
                CapturedAt = DateTimeOffset.UtcNow
            };
            var store = new GuideProjectStore();
            var project = await store.CreateAsync(projectDirectory, GuideDocument.Create("Printer setup", "Codex") with
            {
                Assets = [missingAsset]
            });

            var missingPaths = new GuideAssetFileStore().FindMissingRegisteredImages(project);

            var missingPath = Assert.Single(missingPaths);
            Assert.Equal("assets/missing.png", missingPath);
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
    public async Task LoadAsync_WhenAssetPathLeavesProject_ThrowsValidationException()
    {
        var projectDirectory = Path.Combine(Path.GetTempPath(), "GuideMaker.Tests", Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(projectDirectory);
        Directory.CreateDirectory(Path.Combine(projectDirectory, GuideProjectLayout.AssetsDirectoryName));
        Directory.CreateDirectory(Path.Combine(projectDirectory, GuideProjectLayout.ExportsDirectoryName));
        await File.WriteAllTextAsync(
            Path.Combine(projectDirectory, GuideProjectLayout.GuideFileName),
            """
            {
              "metadata": {
                "schemaVersion": "1.0",
                "title": "Printer setup",
                "author": "Codex",
                "createdAt": "2026-04-25T00:00:00+00:00",
                "updatedAt": "2026-04-25T00:00:00+00:00"
              },
              "steps": [],
              "assets": [
                {
                  "id": "53e283ee-38e4-4cef-9d97-d7b8da39604f",
                  "relativePath": "../secret.png",
                  "kind": "screenshot",
                  "capturedAt": "2026-04-25T00:00:00+00:00"
                }
              ]
            }
            """);

        try
        {
            var store = new GuideProjectStore();

            var exception = await Assert.ThrowsAsync<GuideProjectValidationException>(
                () => store.LoadAsync(projectDirectory));

            Assert.Contains(exception.Errors, error => error.Contains("unsafe path", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(projectDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task LoadAsync_WhenJsonIsMalformed_ThrowsFormatException()
    {
        var projectDirectory = Path.Combine(Path.GetTempPath(), "GuideMaker.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(projectDirectory);
        await File.WriteAllTextAsync(Path.Combine(projectDirectory, GuideProjectLayout.GuideFileName), "{ nope");

        try
        {
            var store = new GuideProjectStore();

            await Assert.ThrowsAsync<GuideProjectFormatException>(() => store.LoadAsync(projectDirectory));
        }
        finally
        {
            Directory.Delete(projectDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task LoadAsync_WhenSchemaVersionIsUnsupported_ThrowsValidationException()
    {
        var projectDirectory = Path.Combine(Path.GetTempPath(), "GuideMaker.Tests", Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(projectDirectory);
        Directory.CreateDirectory(Path.Combine(projectDirectory, GuideProjectLayout.AssetsDirectoryName));
        Directory.CreateDirectory(Path.Combine(projectDirectory, GuideProjectLayout.ExportsDirectoryName));
        await File.WriteAllTextAsync(
            Path.Combine(projectDirectory, GuideProjectLayout.GuideFileName),
            """
            {
              "metadata": {
                "schemaVersion": "99.0",
                "title": "Printer setup",
                "author": "Codex",
                "createdAt": "2026-04-25T00:00:00+00:00",
                "updatedAt": "2026-04-25T00:00:00+00:00"
              },
              "steps": [],
              "assets": []
            }
            """);

        try
        {
            var store = new GuideProjectStore();

            var exception = await Assert.ThrowsAsync<GuideProjectValidationException>(
                () => store.LoadAsync(projectDirectory));

            Assert.Contains(exception.Errors, error => error.Contains("Unsupported guide schema version", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(projectDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task CreateAsync_WhenAssetPathLeavesProject_ThrowsValidationException()
    {
        var projectDirectory = Path.Combine(Path.GetTempPath(), "GuideMaker.Tests", Guid.NewGuid().ToString("N"));
        var document = GuideDocument.Create("Printer setup", "Codex") with
        {
            Assets =
            [
                new GuideAsset
                {
                    Id = Guid.NewGuid(),
                    RelativePath = "../secret.png",
                    Kind = GuideAssetKind.Screenshot,
                    CapturedAt = DateTimeOffset.UtcNow
                }
            ]
        };

        var store = new GuideProjectStore();

        var exception = await Assert.ThrowsAsync<GuideProjectValidationException>(
            () => store.CreateAsync(projectDirectory, document));

        Assert.Contains(exception.Errors, error => error.Contains("unsafe path", StringComparison.Ordinal));
        Assert.False(Directory.Exists(projectDirectory));
    }

    [Fact]
    public async Task CreateAsync_WhenAnnotationBoundsAreOutsideImage_ThrowsValidationException()
    {
        var projectDirectory = Path.Combine(Path.GetTempPath(), "GuideMaker.Tests", Guid.NewGuid().ToString("N"));
        var assetId = Guid.NewGuid();
        var document = GuideDocument.Create("Printer setup", "Codex") with
        {
            Steps =
            [
                GuideStep.Create(1, "Open settings") with
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
                                X = 0.8,
                                Y = 0.8,
                                Width = 0.4,
                                Height = 0.4
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
                    RelativePath = "assets/open-settings.png",
                    Kind = GuideAssetKind.Screenshot,
                    CapturedAt = DateTimeOffset.UtcNow
                }
            ]
        };

        var store = new GuideProjectStore();

        var exception = await Assert.ThrowsAsync<GuideProjectValidationException>(
            () => store.CreateAsync(projectDirectory, document));

        Assert.Contains(exception.Errors, error => error.Contains("normalized bounds", StringComparison.Ordinal));
        Assert.False(Directory.Exists(projectDirectory));
    }

    [Fact]
    public async Task CreateAsync_WhenStepImageRefIsValid_RoundTripsImageRef()
    {
        var projectDirectory = Path.Combine(Path.GetTempPath(), "GuideMaker.Tests", Guid.NewGuid().ToString("N"));
        var assetId = Guid.NewGuid();
        var imageRefId = Guid.NewGuid();
        var document = GuideDocument.Create("Printer setup", "Codex") with
        {
            Steps =
            [
                GuideStep.Create(1, "Open settings") with
                {
                    ImageRefs =
                    [
                        new StepImageRef
                        {
                            Id = imageRefId,
                            AssetId = assetId,
                            Crop = new ImageCropBounds
                            {
                                X = 0.1,
                                Y = 0.2,
                                Width = 0.7,
                                Height = 0.6
                            },
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
                                        Y = 0.1,
                                        Width = 0.3,
                                        Height = 0.2
                                    }
                                }
                            ]
                        }
                    ]
                }
            ],
            Assets =
            [
                new GuideAsset
                {
                    Id = assetId,
                    RelativePath = "assets/open-settings.png",
                    Kind = GuideAssetKind.Screenshot,
                    CapturedAt = DateTimeOffset.UtcNow
                }
            ]
        };

        try
        {
            var store = new GuideProjectStore();

            await store.CreateAsync(projectDirectory, document);
            var loaded = await store.LoadAsync(projectDirectory);

            var imageRef = Assert.Single(loaded.Document.Steps[0].ImageRefs);
            Assert.Equal(imageRefId, imageRef.Id);
            Assert.Equal(assetId, imageRef.AssetId);
            Assert.Equal(0.1, imageRef.Crop?.X);
            Assert.Single(imageRef.Annotations);
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
    public async Task CreateAsync_WhenStepImageRefCropIsOutsideImage_ThrowsValidationException()
    {
        var projectDirectory = Path.Combine(Path.GetTempPath(), "GuideMaker.Tests", Guid.NewGuid().ToString("N"));
        var assetId = Guid.NewGuid();
        var document = GuideDocument.Create("Printer setup", "Codex") with
        {
            Steps =
            [
                GuideStep.Create(1, "Open settings") with
                {
                    ImageRefs =
                    [
                        new StepImageRef
                        {
                            Id = Guid.NewGuid(),
                            AssetId = assetId,
                            Crop = new ImageCropBounds
                            {
                                X = 0.8,
                                Y = 0.1,
                                Width = 0.4,
                                Height = 0.4
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
                    RelativePath = "assets/open-settings.png",
                    Kind = GuideAssetKind.Screenshot,
                    CapturedAt = DateTimeOffset.UtcNow
                }
            ]
        };

        var store = new GuideProjectStore();

        var exception = await Assert.ThrowsAsync<GuideProjectValidationException>(
            () => store.CreateAsync(projectDirectory, document));

        Assert.Contains(exception.Errors, error => error.Contains("normalized crop bounds", StringComparison.Ordinal));
        Assert.False(Directory.Exists(projectDirectory));
    }

    [Fact]
    public async Task CreateAsync_WhenStepImageRefAnnotationReferencesDifferentAsset_ThrowsValidationException()
    {
        var projectDirectory = Path.Combine(Path.GetTempPath(), "GuideMaker.Tests", Guid.NewGuid().ToString("N"));
        var assetId = Guid.NewGuid();
        var otherAssetId = Guid.NewGuid();
        var document = GuideDocument.Create("Printer setup", "Codex") with
        {
            Steps =
            [
                GuideStep.Create(1, "Open settings") with
                {
                    ImageRefs =
                    [
                        new StepImageRef
                        {
                            Id = Guid.NewGuid(),
                            AssetId = assetId,
                            Annotations =
                            [
                                new GuideAnnotation
                                {
                                    Id = Guid.NewGuid(),
                                    Kind = GuideAnnotationKind.Rectangle,
                                    AssetId = otherAssetId,
                                    Bounds = new AnnotationBounds
                                    {
                                        X = 0.1,
                                        Y = 0.1,
                                        Width = 0.3,
                                        Height = 0.2
                                    }
                                }
                            ]
                        }
                    ]
                }
            ],
            Assets =
            [
                new GuideAsset
                {
                    Id = assetId,
                    RelativePath = "assets/open-settings.png",
                    Kind = GuideAssetKind.Screenshot,
                    CapturedAt = DateTimeOffset.UtcNow
                },
                new GuideAsset
                {
                    Id = otherAssetId,
                    RelativePath = "assets/other.png",
                    Kind = GuideAssetKind.Screenshot,
                    CapturedAt = DateTimeOffset.UtcNow
                }
            ]
        };

        var store = new GuideProjectStore();

        var exception = await Assert.ThrowsAsync<GuideProjectValidationException>(
            () => store.CreateAsync(projectDirectory, document));

        Assert.Contains(exception.Errors, error => error.Contains("must reference the same asset", StringComparison.Ordinal));
        Assert.False(Directory.Exists(projectDirectory));
    }

    [Fact]
    public async Task LoadAsync_WhenAssetFileIsMissing_ReportsMissingAssetPath()
    {
        var projectDirectory = Path.Combine(Path.GetTempPath(), "GuideMaker.Tests", Guid.NewGuid().ToString("N"));
        var assetId = Guid.NewGuid();
        var document = GuideDocument.Create("Printer setup", "Codex") with
        {
            Steps =
            [
                GuideStep.Create(1, "Open settings", "Open the Windows Settings app.") with
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

        await WriteGuideFileAsync(projectDirectory, document);

        try
        {
            var store = new GuideProjectStore();

            var project = await store.LoadAsync(projectDirectory);

            Assert.Equal(["assets/missing.png"], project.MissingAssetPaths);
        }
        finally
        {
            Directory.Delete(projectDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task LoadAsync_OpensStarterGuideSample()
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

        var store = new GuideProjectStore();

        var project = await store.LoadAsync(sampleDirectory);

        Assert.Equal("Starter guide", project.Document.Metadata.Title);
        Assert.Single(project.Document.Steps);
        Assert.Equal(["assets/open-application.png"], project.MissingAssetPaths);
    }

    private static async Task WriteGuideFileAsync(string projectDirectory, GuideDocument document)
    {
        Directory.CreateDirectory(projectDirectory);
        Directory.CreateDirectory(Path.Combine(projectDirectory, GuideProjectLayout.AssetsDirectoryName));
        Directory.CreateDirectory(Path.Combine(projectDirectory, GuideProjectLayout.ExportsDirectoryName));

        var store = new GuideProjectStore();
        await store.SaveAsync(new GuideProject
        {
            ProjectDirectory = projectDirectory,
            Document = document
        });
    }
}
