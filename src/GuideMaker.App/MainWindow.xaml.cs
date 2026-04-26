using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using GuideMaker.Core;
using GuideMaker.Export;
using GuideMaker.Storage;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using WpfClipboard = System.Windows.Clipboard;
using WpfMessageBox = System.Windows.MessageBox;

namespace GuideMaker.App;

public partial class MainWindow : Window
{
    private const int WhMouseLl = 14;
    private const int WmLButtonUp = 0x0202;
    private const int WmRButtonUp = 0x0205;
    private const int WmMButtonUp = 0x0208;

    private readonly GuideProjectStore projectStore = new();
    private readonly GuideAssetFileStore assetFileStore = new();
    private readonly GuideExportWriter exportWriter = new();
    private readonly HtmlGuideExporter previewExporter = new();
    private readonly ExportAssetRenderer previewAssetRenderer = new();
    private readonly ObservableCollection<EditableStep> steps = [];
    private readonly ObservableCollection<EditableAsset> selectedStepAssets = [];
    private readonly ObservableCollection<EditableAsset> imagePoolAssets = [];
    private readonly ObservableCollection<EditableAnnotation> selectedAssetAnnotations = [];
    private readonly DispatcherTimer guidePreviewRefreshTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(1200)
    };

    private GuideProject? currentProject;
    private GuideMetadata? currentMetadata;
    private List<GuideAsset> currentAssets = [];
    private Guid? selectedAssetId;
    private Guid? selectedImageRefId;
    private Guid? selectedPoolAssetId;
    private Guid? selectedAnnotationId;
    private Guid? draggedAnnotationId;
    private System.Windows.Point annotationDragOffset;
    private System.Windows.Point cropDragStart;
    private bool isDarkMode;
    private bool isDirty;
    private bool isUpdatingUi;
    private bool isChangingAssetSelection;
    private bool isDraggingAnnotation;
    private bool isDraggingCrop;
    private bool isRefreshingAssets;
    private bool isRefreshingAnnotations;
    private bool isFollowAlongCaptureActive;
    private bool isFollowAlongCaptureSaving;
    private bool closeAlreadyConfirmed;
    private int followAlongCaptureCount;
    private IntPtr followAlongMouseHookHandle = IntPtr.Zero;
    private LowLevelMouseProc? followAlongMouseProc;
    private DateTimeOffset lastFollowAlongCaptureAt = DateTimeOffset.MinValue;

    public MainWindow()
    {
        InitializeComponent();

        StepsListBox.ItemsSource = steps;
        StepAssetsListBox.ItemsSource = selectedStepAssets;
        ImagePoolListBox.ItemsSource = imagePoolAssets;
        AnnotationListBox.ItemsSource = selectedAssetAnnotations;
        guidePreviewRefreshTimer.Tick += GuidePreviewRefreshTimer_Tick;
        ApplyTheme(isDarkMode);
        UpdateUiState();
    }

    private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
    {
        isDarkMode = ThemeToggleButton.IsChecked == true;
        ApplyTheme(isDarkMode);
        ThemeToggleButton.Content = isDarkMode ? "Light" : "Dark";
        SetStatus(isDarkMode ? "Dark mode enabled." : "Light mode enabled.");
    }

    private async void NewGuideButton_Click(object sender, RoutedEventArgs e)
    {
        if (!await ConfirmSaveIfDirtyAsync().ConfigureAwait(true))
        {
            return;
        }

        using var dialog = new Forms.FolderBrowserDialog
        {
            Description = "Choose a folder for the new guide project",
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog() != Forms.DialogResult.OK)
        {
            return;
        }

        var guideFilePath = Path.Combine(dialog.SelectedPath, GuideProjectLayout.GuideFileName);
        if (File.Exists(guideFilePath) && WpfMessageBox.Show(
                this,
                "This folder already contains a guide.json file. Overwrite it?",
                "GuideMaker",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        var document = GuideDocument.Create("Untitled guide", Environment.UserName) with
        {
            Steps =
            [
                GuideStep.Create(1, "New step", "Describe the action here.")
            ]
        };

        await RunUiActionAsync(async () =>
        {
            var project = await projectStore.CreateAsync(dialog.SelectedPath, document).ConfigureAwait(true);
            LoadProjectIntoUi(project);
            SetStatus("New guide project created.");
        }).ConfigureAwait(true);
    }

    private async void OpenGuideButton_Click(object sender, RoutedEventArgs e)
    {
        if (!await ConfirmSaveIfDirtyAsync().ConfigureAwait(true))
        {
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "Open GuideMaker project",
            Filter = "GuideMaker project (guide.json)|guide.json|JSON files (*.json)|*.json|All files (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        var projectDirectory = Path.GetDirectoryName(dialog.FileName);
        if (string.IsNullOrWhiteSpace(projectDirectory))
        {
            return;
        }

        await RunUiActionAsync(async () =>
        {
            var project = await projectStore.LoadAsync(projectDirectory).ConfigureAwait(true);
            LoadProjectIntoUi(project);
            SetStatus("Guide project opened.");
        }).ConfigureAwait(true);
    }

    private async void SaveGuideButton_Click(object sender, RoutedEventArgs e)
    {
        await SaveCurrentProjectAsync().ConfigureAwait(true);
    }

    private async void ExportGuideButton_Click(object sender, RoutedEventArgs e)
    {
        if (currentProject is null || currentMetadata is null)
        {
            return;
        }

        if (!ConfirmExportReview())
        {
            return;
        }

        await RunUiActionAsync(async () =>
        {
            var result = await exportWriter.ExportAsync(currentProject.ProjectDirectory, BuildDocumentFromUi())
                .ConfigureAwait(true);
            GuidePreviewBrowser.Navigate(new Uri(result.HtmlPath));
            PreviewPathTextBlock.Text = result.HtmlPath;
            SetStatus($"Exported Markdown, HTML, and PDF to {Path.GetDirectoryName(result.MarkdownPath)}.");
        }).ConfigureAwait(true);
    }

    private async void PreviewGuideButton_Click(object sender, RoutedEventArgs e)
    {
        if (currentProject is null || currentMetadata is null)
        {
            return;
        }

        await RunUiActionAsync(async () =>
        {
            await RefreshGuidePreviewAsync(selectGuideTab: true).ConfigureAwait(true);
            SetStatus("Preview updated.");
        }).ConfigureAwait(true);
    }

    private async void RefreshWorkspacePreviewButton_Click(object sender, RoutedEventArgs e)
    {
        if (currentProject is null || currentMetadata is null)
        {
            return;
        }

        guidePreviewRefreshTimer.Stop();
        await RunUiActionAsync(async () =>
        {
            await RefreshGuidePreviewAsync(selectGuideTab: false).ConfigureAwait(true);
            SetStatus("Workspace preview refreshed.");
        }).ConfigureAwait(true);
    }

    private void AddStepButton_Click(object sender, RoutedEventArgs e)
    {
        if (currentMetadata is null)
        {
            return;
        }

        var nextOrder = steps.Count == 0 ? 1 : steps.Max(step => step.Order) + 1;
        var step = EditableStep.FromGuideStep(GuideStep.Create(nextOrder, "New step", "Describe the action here."));
        steps.Add(step);
        StepsListBox.SelectedItem = step;
        MarkDirty();
        SetStatus("Step added.");
    }

    private void DeleteStepButton_Click(object sender, RoutedEventArgs e)
    {
        if (StepsListBox.SelectedItem is not EditableStep selectedStep)
        {
            return;
        }

        var selectedIndex = StepsListBox.SelectedIndex;
        steps.Remove(selectedStep);
        RenumberSteps();

        if (steps.Count > 0)
        {
            StepsListBox.SelectedIndex = Math.Min(selectedIndex, steps.Count - 1);
        }

        MarkDirty();
        SetStatus("Step deleted.");
    }

    private void MoveStepUpButton_Click(object sender, RoutedEventArgs e)
    {
        MoveSelectedStep(-1);
    }

    private void MoveStepDownButton_Click(object sender, RoutedEventArgs e)
    {
        MoveSelectedStep(1);
    }

    private async void ImportImageButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedStep = GetSelectedStep();
        if (selectedStep is null || currentProject is null)
        {
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "Import image",
            Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = true
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        await RunUiActionAsync(async () =>
        {
            foreach (var fileName in dialog.FileNames)
            {
                var asset = await assetFileStore.ImportImageAsync(currentProject, fileName).ConfigureAwait(true);
                AttachAssetToStep(selectedStep, asset);
            }

            SetStatus(dialog.FileNames.Length == 1
                ? "Image imported to pool and attached to selected step."
                : $"{dialog.FileNames.Length} images imported to pool and attached to selected step.");
        }).ConfigureAwait(true);
    }

    private async void PasteImageButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedStep = GetSelectedStep();
        if (selectedStep is null || currentProject is null)
        {
            return;
        }

        if (!WpfClipboard.ContainsImage())
        {
            WpfMessageBox.Show(this, "Clipboard does not contain an image.", "GuideMaker", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        await RunUiActionAsync(async () =>
        {
            var image = WpfClipboard.GetImage();
            if (image is null)
            {
                return;
            }

            await using var stream = EncodePng(image);
            var asset = await assetFileStore.SavePngAsync(currentProject, "clipboard image", stream).ConfigureAwait(true);
            AttachAssetToStep(selectedStep, asset);
            SetStatus("Clipboard image added to pool and attached to selected step.");
        }).ConfigureAwait(true);
    }

    private async void CaptureScreenshotButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedStep = GetSelectedStep();
        if (selectedStep is null || currentProject is null)
        {
            return;
        }

        await RunUiActionAsync(async () =>
        {
            try
            {
                WindowState = WindowState.Minimized;
                await Task.Delay(300).ConfigureAwait(true);

                await using var stream = CaptureVirtualScreenPng();
                var asset = await assetFileStore.SavePngAsync(currentProject, $"screenshot {DateTime.Now:yyyyMMdd-HHmmss}", stream)
                    .ConfigureAwait(true);
                AttachAssetToStep(selectedStep, asset);

                SetStatus("Screenshot captured to pool and attached to selected step.");
            }
            finally
            {
                WindowState = WindowState.Normal;
                Activate();
            }
        }).ConfigureAwait(true);
    }

    private void FollowAlongCaptureButton_Click(object sender, RoutedEventArgs e)
    {
        if (isFollowAlongCaptureActive)
        {
            StopFollowAlongCapture("Follow along capture stopped.");
            WindowState = WindowState.Normal;
            Activate();
            return;
        }

        StartFollowAlongCapture();
    }

    private void StartFollowAlongCapture()
    {
        if (currentProject is null || GetSelectedStep() is null)
        {
            SetStatus("Open a guide and select a step before starting follow along.");
            return;
        }

        followAlongMouseProc ??= FollowAlongMouseHookCallback;
        followAlongMouseHookHandle = SetWindowsHookEx(
            WhMouseLl,
            followAlongMouseProc,
            GetCurrentModuleHandle(),
            0);

        if (followAlongMouseHookHandle == IntPtr.Zero)
        {
            SetStatus("Could not start follow along capture.");
            return;
        }

        followAlongCaptureCount = 0;
        lastFollowAlongCaptureAt = DateTimeOffset.MinValue;
        isFollowAlongCaptureActive = true;
        FollowAlongCaptureButton.Content = "Stop follow";
        SetStatus("Follow along capture started. Restore GuideMaker to stop.");
        WindowState = WindowState.Minimized;
    }

    private void StopFollowAlongCapture(string status)
    {
        if (!isFollowAlongCaptureActive)
        {
            return;
        }

        if (followAlongMouseHookHandle != IntPtr.Zero)
        {
            UnhookWindowsHookEx(followAlongMouseHookHandle);
            followAlongMouseHookHandle = IntPtr.Zero;
        }

        isFollowAlongCaptureActive = false;
        isFollowAlongCaptureSaving = false;
        FollowAlongCaptureButton.Content = "Follow along";
        SetStatus($"{status} {followAlongCaptureCount} screenshot(s) captured.");
        UpdateUiState();
    }

    private IntPtr FollowAlongMouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && IsFollowAlongCaptureClick(wParam) && isFollowAlongCaptureActive)
        {
            var hookInfo = Marshal.PtrToStructure<LowLevelMouseHookStruct>(lParam);
            var screen = Forms.Screen.FromPoint(new Drawing.Point(hookInfo.Point.X, hookInfo.Point.Y));
            var delay = wParam == WmRButtonUp ? 250 : 140;
            Dispatcher.BeginInvoke(
                () => CaptureFollowAlongScreenshotAsync(screen, delay),
                DispatcherPriority.Background);
        }

        return CallNextHookEx(followAlongMouseHookHandle, nCode, wParam, lParam);
    }

    private static bool IsFollowAlongCaptureClick(IntPtr message)
    {
        return message == WmLButtonUp ||
            message == WmRButtonUp ||
            message == WmMButtonUp;
    }

    private async Task CaptureFollowAlongScreenshotAsync(Forms.Screen screen, int delayMilliseconds)
    {
        if (!isFollowAlongCaptureActive ||
            isFollowAlongCaptureSaving ||
            currentProject is null ||
            GetSelectedStep() is not { } selectedStep ||
            WindowState != WindowState.Minimized)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        if (now - lastFollowAlongCaptureAt < TimeSpan.FromMilliseconds(350))
        {
            return;
        }

        isFollowAlongCaptureSaving = true;
        lastFollowAlongCaptureAt = now;

        try
        {
            if (delayMilliseconds > 0)
            {
                await Task.Delay(delayMilliseconds).ConfigureAwait(true);
            }

            if (!isFollowAlongCaptureActive || currentProject is null || WindowState != WindowState.Minimized)
            {
                return;
            }

            await using var stream = CaptureScreenPng(screen);
            var asset = await assetFileStore.SavePngAsync(
                    currentProject,
                    $"follow {DateTime.Now:yyyyMMdd-HHmmss-fff}",
                    stream)
                .ConfigureAwait(true);
            AttachAssetToStep(selectedStep, asset);
            followAlongCaptureCount++;
            SetStatus($"Follow along captured {followAlongCaptureCount} screenshot(s). Restore GuideMaker to stop.");
        }
        catch
        {
            SetStatus("Follow along screenshot failed.");
        }
        finally
        {
            isFollowAlongCaptureSaving = false;
        }
    }

    private void RemoveImageButton_Click(object sender, RoutedEventArgs e)
    {
        if (IsPoolTabActive())
        {
            return;
        }

        var selectedStep = GetSelectedStep();
        var selectedAsset = GetSelectedEditableAsset();
        if (selectedStep is null || selectedAsset is null)
        {
            return;
        }

        if (selectedAsset.ImageRefId is { } imageRefId)
        {
            selectedStep.ImageRefs.RemoveAll(candidate => candidate.Id == imageRefId);
            selectedStep.AssetIds.Clear();
            selectedStep.AssetIds.AddRange(selectedStep.ImageRefs.Select(imageRef => imageRef.AssetId));
        }
        else
        {
            selectedStep.AssetIds.Remove(selectedAsset.Id);
            selectedStep.Annotations.RemoveAll(annotation => annotation.AssetId == selectedAsset.Id);
        }

        if (selectedStep.ImageRefs.Count > 0)
        {
            selectedStep.Annotations.Clear();
            selectedStep.Annotations.AddRange(selectedStep.ImageRefs.SelectMany(imageRef => imageRef.Annotations));
        }

        var nextImageRef = selectedStep.ImageRefs.FirstOrDefault();
        selectedImageRefId = nextImageRef?.Id;
        selectedAssetId = nextImageRef?.AssetId ?? (selectedStep.AssetIds.Count > 0 ? selectedStep.AssetIds[0] : null);
        selectedAnnotationId = null;
        RefreshSelectedStepAssets();
        RefreshSelectedAssetAnnotations();
        MarkDirty();
        SetStatus("Image detached from selected step.");
    }

    private void AttachPoolImageButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedStep = GetSelectedStep();
        if (selectedStep is null || selectedPoolAssetId is not { } assetId)
        {
            return;
        }

        var asset = currentAssets.FirstOrDefault(candidate => candidate.Id == assetId);
        if (asset is null)
        {
            return;
        }

        AttachAssetToStep(selectedStep, asset);
        SetStatus("Pool image attached to selected step.");
    }

    private void ScanAssetPoolButton_Click(object sender, RoutedEventArgs e)
    {
        if (currentProject is null || currentMetadata is null)
        {
            return;
        }

        var projectSnapshot = currentProject with
        {
            Document = BuildDocumentFromUi()
        };
        var missingAssetPaths = assetFileStore.FindMissingRegisteredImages(projectSnapshot);
        var discoveredAssets = assetFileStore.ScanUnregisteredImages(projectSnapshot);
        if (discoveredAssets.Count == 0)
        {
            SetStatus(missingAssetPaths.Count == 0
                ? "No unregistered image files found in assets."
                : $"No unregistered image files found. Missing registered assets: {missingAssetPaths.Count}.");
            return;
        }

        currentAssets.AddRange(discoveredAssets);
        RefreshImagePoolAssets(discoveredAssets[0].Id);
        RefreshSelectedStepAssets();
        MarkDirty();
        var registeredMessage = discoveredAssets.Count == 1
            ? "1 image file registered in the pool."
            : $"{discoveredAssets.Count} image files registered in the pool.";
        SetStatus(missingAssetPaths.Count == 0
            ? registeredMessage
            : $"{registeredMessage} Missing registered assets: {missingAssetPaths.Count}.");
    }

    private void AddHighlightButton_Click(object sender, RoutedEventArgs e)
    {
        AddAnnotationToSelectedImage(GuideAnnotationKind.Rectangle, "Highlight");
    }

    private void AddLabelButton_Click(object sender, RoutedEventArgs e)
    {
        AddAnnotationToSelectedImage(GuideAnnotationKind.Label, "Label");
    }

    private void AddArrowButton_Click(object sender, RoutedEventArgs e)
    {
        AddAnnotationToSelectedImage(GuideAnnotationKind.Arrow, null);
    }

    private void AddRedactButton_Click(object sender, RoutedEventArgs e)
    {
        AddAnnotationToSelectedImage(GuideAnnotationKind.Blur, null);
    }

    private void RemoveAnnotationButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedStep = GetSelectedStep();
        var selectedAsset = GetSelectedEditableAsset();
        if (selectedStep is null || selectedAsset is null)
        {
            return;
        }

        var annotations = GetSelectedAnnotationList();
        var annotation = selectedAnnotationId is { } annotationId
            ? annotations?.FirstOrDefault(candidate => candidate.Id == annotationId)
            : annotations?.LastOrDefault(candidate => candidate.AssetId == selectedAsset.Id);
        if (annotation is null)
        {
            return;
        }

        annotations?.Remove(annotation);
        SyncStepAnnotationsFromImageRefs(selectedStep);
        selectedAnnotationId = null;
        RefreshSelectedStepAssets(selectedAsset.Id, selectedAsset.ImageRefId);
        RefreshSelectedAssetAnnotations();
        MarkDirty();
        SetStatus("Annotation removed from selected image.");
    }

    private void MoveAnnotationUpButton_Click(object sender, RoutedEventArgs e)
    {
        MoveSelectedAnnotation(-1);
    }

    private void MoveAnnotationDownButton_Click(object sender, RoutedEventArgs e)
    {
        MoveSelectedAnnotation(1);
    }

    private void InsertImageReferenceButton_Click(object sender, RoutedEventArgs e)
    {
        if (IsPoolTabActive())
        {
            return;
        }

        var selectedAsset = GetSelectedEditableAsset();
        if (selectedAsset is null)
        {
            return;
        }

        var token = selectedAsset.ImageRefId is { } imageRefId
            ? $"[[image-ref:{imageRefId}]]"
            : $"[[image:{selectedAsset.RelativePath}]]";
        var prefix = StepBodyTextBox.CaretIndex > 0 && !StepBodyTextBox.Text[..StepBodyTextBox.CaretIndex].EndsWith(Environment.NewLine, StringComparison.Ordinal)
            ? Environment.NewLine
            : string.Empty;
        var suffix = Environment.NewLine;

        StepBodyTextBox.SelectedText = prefix + token + suffix;
        StepBodyTextBox.CaretIndex += prefix.Length + token.Length + suffix.Length;
        StepBodyTextBox.Focus();

        if (GetSelectedStep() is { } selectedStep)
        {
            selectedStep.Body = StepBodyTextBox.Text;
        }

        MarkDirty();
        SetStatus("Image reference inserted into step text.");
    }

    private void GuideTitleTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (!isUpdatingUi && currentMetadata is not null)
        {
            MarkDirty();
        }
    }

    private void StepTitleTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (isUpdatingUi || StepsListBox.SelectedItem is not EditableStep selectedStep)
        {
            return;
        }

        selectedStep.Title = StepTitleTextBox.Text;
        MarkDirty();
    }

    private void StepBodyTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (isUpdatingUi || StepsListBox.SelectedItem is not EditableStep selectedStep)
        {
            return;
        }

        selectedStep.Body = StepBodyTextBox.Text;
        MarkDirty();
    }

    private void StepsListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        selectedAssetId = null;
        selectedImageRefId = null;
        selectedAnnotationId = null;
        LoadSelectedStepIntoEditor();
        RefreshSelectedStepAssets();
        RefreshSelectedAssetAnnotations();
        LoadSelectedCropIntoEditor();
        UpdateUiState();
    }

    private void StepAssetsListBox_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (System.Windows.Controls.ItemsControl.ContainerFromElement(
                StepAssetsListBox,
                e.OriginalSource as DependencyObject) is not System.Windows.Controls.ListBoxItem { DataContext: EditableAsset asset })
        {
            return;
        }

        if (selectedAssetId == asset.Id && selectedImageRefId == asset.ImageRefId)
        {
            e.Handled = true;
            return;
        }

        SelectAsset(asset.Id, asset.ImageRefId);
        e.Handled = true;
    }

    private void StepAssetsListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (isRefreshingAssets)
        {
            return;
        }

        if (StepAssetsListBox.SelectedItem is EditableAsset selectedAsset)
        {
            selectedAssetId = selectedAsset.Id;
            selectedImageRefId = selectedAsset.ImageRefId;
        }
        else
        {
            selectedAssetId = null;
            selectedImageRefId = null;
        }

        selectedAnnotationId = null;

        RefreshWorkspaceImagePreview();
        RefreshSelectedAssetAnnotations();
        LoadSelectedCropIntoEditor();
        UpdateUiState();
    }

    private void ImagePoolListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        var selectedAsset = ImagePoolListBox.SelectedItem as EditableAsset;
        selectedPoolAssetId = selectedAsset?.Id;

        if (StepImagesTabControl.SelectedItem == PoolImagesTab)
        {
            RefreshWorkspaceImagePreview(selectedAsset);
        }

        UpdateUiState();
    }

    private void StepImagesTabControl_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (sender != StepImagesTabControl)
        {
            return;
        }

        if (IsPoolTabActive())
        {
            RefreshWorkspaceImagePreview(ImagePoolListBox.SelectedItem as EditableAsset);
            UpdateUiState();
            return;
        }

        RefreshWorkspaceImagePreview();
        UpdateUiState();
    }

    private void AnnotationListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (isRefreshingAnnotations)
        {
            return;
        }

        if (AnnotationListBox.SelectedItem is EditableAnnotation selectedAnnotation)
        {
            selectedAnnotationId = selectedAnnotation.Id;
        }
        else
        {
            selectedAnnotationId = null;
        }

        LoadSelectedAnnotationIntoEditor();
        LoadSelectedCropIntoEditor();
        UpdateUiState();
    }

    private void AnnotationTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (isUpdatingUi || isChangingAssetSelection || GetSelectedAnnotation() is not { } annotation)
        {
            return;
        }

        UpdateSelectedAnnotationQuietly(annotation with
        {
            Text = string.IsNullOrWhiteSpace(AnnotationTextBox.Text) ? null : AnnotationTextBox.Text
        });
    }

    private void AnnotationBoundsSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (isUpdatingUi || isChangingAssetSelection || GetSelectedAnnotation() is not { } annotation)
        {
            return;
        }

        var requestedWidth = AnnotationWidthSlider.Value / 100;
        var requestedHeight = AnnotationHeightSlider.Value / 100;
        var requestedX = AnnotationXSlider.Value / 100;
        var requestedY = AnnotationYSlider.Value / 100;
        var width = Math.Clamp(requestedWidth, 0.01, Math.Max(0.01, 1 - requestedX));
        var height = Math.Clamp(requestedHeight, 0.01, Math.Max(0.01, 1 - requestedY));
        var x = Math.Clamp(requestedX, 0, 1 - width);
        var y = Math.Clamp(requestedY, 0, 1 - height);

        UpdateSelectedAnnotation(annotation with
        {
            Bounds = new AnnotationBounds
            {
                X = x,
                Y = y,
                Width = width,
                Height = height
            }
        });
    }

    private void CropSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (isUpdatingUi || isChangingAssetSelection || GetSelectedImageRef() is not { } imageRef)
        {
            return;
        }

        var requestedWidth = CropWidthSlider.Value / 100;
        var requestedHeight = CropHeightSlider.Value / 100;
        var requestedX = CropXSlider.Value / 100;
        var requestedY = CropYSlider.Value / 100;
        var width = Math.Clamp(requestedWidth, 0.01, Math.Max(0.01, 1 - requestedX));
        var height = Math.Clamp(requestedHeight, 0.01, Math.Max(0.01, 1 - requestedY));
        var x = Math.Clamp(requestedX, 0, 1 - width);
        var y = Math.Clamp(requestedY, 0, 1 - height);

        UpdateSelectedImageRef(imageRef with
        {
            Crop = new ImageCropBounds
            {
                X = x,
                Y = y,
                Width = width,
                Height = height
            }
        });
    }

    private void ClearCropButton_Click(object sender, RoutedEventArgs e)
    {
        if (GetSelectedImageRef() is not { } imageRef)
        {
            return;
        }

        UpdateSelectedImageRef(imageRef with { Crop = null });
        LoadSelectedCropIntoEditor();
        SetStatus("Crop cleared for selected image.");
    }

    private void CropEditorExpander_Toggled(object sender, RoutedEventArgs e)
    {
        RefreshWorkspaceImagePreview();
        UpdateUiState();
    }

    private void LabelStyle_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (isUpdatingUi)
        {
            return;
        }

        UpdateSelectedLabelStyle(style => style with
        {
            FontSize = Math.Clamp(LabelFontSizeSlider.Value, 8, 36),
            BackgroundOpacity = Math.Clamp(LabelBackgroundOpacitySlider.Value / 100, 0.25, 1)
        });
    }

    private void LabelStyleToggle_Click(object sender, RoutedEventArgs e)
    {
        if (isUpdatingUi)
        {
            return;
        }

        UpdateSelectedLabelStyle(style => style with
        {
            IsBold = LabelBoldToggle.IsChecked == true,
            IsItalic = LabelItalicToggle.IsChecked == true
        });
    }

    private void LabelTextColorButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string color })
        {
            UpdateSelectedLabelStyle(style => style with { TextColor = color });
        }
    }

    private void LabelBackgroundColorButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string color })
        {
            UpdateSelectedLabelStyle(style => style with { BackgroundColor = color });
        }
    }

    private void WorkspaceAnnotation_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: EditableAnnotationPreview preview })
        {
            return;
        }

        var annotation = GetSelectedAnnotationList()?.FirstOrDefault(candidate => candidate.Id == preview.Id);
        if (annotation is null || selectedAssetId != annotation.AssetId)
        {
            return;
        }

        selectedAnnotationId = annotation.Id;
        RefreshSelectedAssetAnnotations(annotation.Id);
        LoadSelectedAnnotationIntoEditor();
        UpdateUiState();

        var pointer = e.GetPosition(WorkspaceImageSurface);
        annotationDragOffset = new System.Windows.Point(pointer.X - preview.WorkspaceX, pointer.Y - preview.WorkspaceY);
        draggedAnnotationId = annotation.Id;
        isDraggingAnnotation = true;
        WorkspaceImageSurface.CaptureMouse();
        e.Handled = true;
    }

    private void WorkspaceImageSurface_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (!IsCropWorkspaceModeActive() || GetSelectedEditableAsset() is not { } selectedAsset)
        {
            return;
        }

        var pointer = e.GetPosition(WorkspaceImageSurface);
        if (!IsPointInsideWorkspaceOriginalImage(pointer, selectedAsset))
        {
            return;
        }

        cropDragStart = ClampPointToWorkspaceOriginalImage(pointer, selectedAsset);
        isDraggingCrop = true;
        WorkspaceImageSurface.CaptureMouse();
        UpdateCropFromWorkspaceDrag(cropDragStart, selectedAsset);
        e.Handled = true;
    }

    private void WorkspaceImageSurface_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (isDraggingCrop)
        {
            if (e.LeftButton != System.Windows.Input.MouseButtonState.Pressed)
            {
                EndCropDrag(commit: true);
                return;
            }

            if (GetSelectedEditableAsset() is { } cropAsset)
            {
                UpdateCropFromWorkspaceDrag(e.GetPosition(WorkspaceImageSurface), cropAsset);
                e.Handled = true;
            }

            return;
        }

        if (!isDraggingAnnotation || draggedAnnotationId is not { } annotationId)
        {
            return;
        }

        if (e.LeftButton != System.Windows.Input.MouseButtonState.Pressed)
        {
            EndAnnotationDrag();
            return;
        }

        var selectedAsset = GetSelectedEditableAsset();
        var selectedStep = GetSelectedStep();
        var annotation = selectedStep?.Annotations.FirstOrDefault(candidate => candidate.Id == annotationId);
        if (selectedAsset is null || annotation is null || selectedAsset.WorkspaceImageWidth <= 0 || selectedAsset.WorkspaceImageHeight <= 0)
        {
            EndAnnotationDrag();
            return;
        }

        var pointer = e.GetPosition(WorkspaceImageSurface);
        var x = (pointer.X - annotationDragOffset.X - selectedAsset.WorkspaceImageX) / selectedAsset.WorkspaceImageWidth;
        var y = (pointer.Y - annotationDragOffset.Y - selectedAsset.WorkspaceImageY) / selectedAsset.WorkspaceImageHeight;

        var updatedAnnotation = annotation with
        {
            Bounds = new AnnotationBounds
            {
                X = Math.Clamp(x, 0, Math.Max(0, 1 - annotation.Bounds.Width)),
                Y = Math.Clamp(y, 0, Math.Max(0, 1 - annotation.Bounds.Height)),
                Width = annotation.Bounds.Width,
                Height = annotation.Bounds.Height
            }
        };

        UpdateSelectedAnnotation(updatedAnnotation, reloadEditor: false);
        LoadDraggedAnnotationPositionIntoEditor(updatedAnnotation.Bounds);

        e.Handled = true;
    }

    private void WorkspaceImageSurface_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (isDraggingCrop)
        {
            EndCropDrag(commit: true);
            e.Handled = true;
            return;
        }

        if (!isDraggingAnnotation)
        {
            return;
        }

        EndAnnotationDrag();
        e.Handled = true;
    }

    private void Window_StateChanged(object? sender, EventArgs e)
    {
        if (isFollowAlongCaptureActive && WindowState != WindowState.Minimized)
        {
            StopFollowAlongCapture("Follow along capture stopped.");
        }
    }

    private async void Window_Closing(object? sender, CancelEventArgs e)
    {
        StopFollowAlongCapture("Follow along capture stopped.");

        if (closeAlreadyConfirmed || !isDirty)
        {
            return;
        }

        e.Cancel = true;

        if (await ConfirmSaveIfDirtyAsync().ConfigureAwait(true))
        {
            closeAlreadyConfirmed = true;
            Close();
        }
    }

    private async Task<bool> ConfirmSaveIfDirtyAsync()
    {
        if (!isDirty)
        {
            return true;
        }

        var result = WpfMessageBox.Show(
            this,
            "Save changes before continuing?",
            "GuideMaker",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Cancel)
        {
            return false;
        }

        if (result == MessageBoxResult.No)
        {
            return true;
        }

        return await SaveCurrentProjectAsync().ConfigureAwait(true);
    }

    private async Task<bool> SaveCurrentProjectAsync()
    {
        if (currentProject is null || currentMetadata is null)
        {
            return false;
        }

        var success = false;

        await RunUiActionAsync(async () =>
        {
            await projectStore.SaveAsync(currentProject with
            {
                Document = BuildDocumentFromUi()
            }).ConfigureAwait(true);

            currentProject = await projectStore.LoadAsync(currentProject.ProjectDirectory).ConfigureAwait(true);
            LoadProjectIntoUi(currentProject);
            SetStatus("Guide project saved.");
            success = true;
        }).ConfigureAwait(true);

        return success;
    }

    private async Task<string> WritePreviewAsync(CancellationToken cancellationToken = default)
    {
        if (currentProject is null)
        {
            throw new InvalidOperationException("No guide project is open.");
        }

        var exportsDirectory = Path.Combine(currentProject.ProjectDirectory, GuideProjectLayout.ExportsDirectoryName);
        Directory.CreateDirectory(exportsDirectory);

        var previewPath = Path.Combine(exportsDirectory, "preview.html");
        var previewDocument = previewAssetRenderer.RenderExportAssets(
            currentProject.ProjectDirectory,
            exportsDirectory,
            BuildDocumentFromUi());
        await File.WriteAllTextAsync(previewPath, previewExporter.Export(previewDocument), cancellationToken)
            .ConfigureAwait(true);

        return previewPath;
    }

    private async Task RefreshGuidePreviewAsync(bool selectGuideTab, CancellationToken cancellationToken = default)
    {
        var previewPath = await WritePreviewAsync(cancellationToken).ConfigureAwait(true);
        GuidePreviewBrowser.Navigate(new Uri(previewPath));
        PreviewPathTextBlock.Text = previewPath;

        if (selectGuideTab)
        {
            WorkspaceTabControl.SelectedItem = GuidePreviewTab;
        }
    }

    private bool ConfirmExportReview()
    {
        var result = WpfMessageBox.Show(
            this,
            "Review the guide text, screenshots, and redactions before export. Exported files may contain sensitive or internal information.",
            "Review before export",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);

        return result == MessageBoxResult.OK;
    }

    private async Task RunUiActionAsync(Func<Task> action)
    {
        try
        {
            SetButtonsEnabled(false);
            await action().ConfigureAwait(true);
        }
        catch (GuideProjectValidationException exception)
        {
            WpfMessageBox.Show(this, string.Join(Environment.NewLine, exception.Errors), "Invalid guide project", MessageBoxButton.OK, MessageBoxImage.Warning);
            SetStatus("Guide project validation failed.");
        }
        catch (GuideProjectStorageException exception)
        {
            WpfMessageBox.Show(this, exception.Message, "Guide project error", MessageBoxButton.OK, MessageBoxImage.Warning);
            SetStatus("Guide project operation failed.");
        }
        catch (Exception exception)
        {
            WpfMessageBox.Show(this, exception.Message, "GuideMaker", MessageBoxButton.OK, MessageBoxImage.Error);
            SetStatus("Operation failed.");
        }
        finally
        {
            SetButtonsEnabled(true);
            UpdateUiState();
        }
    }

    private void LoadProjectIntoUi(GuideProject project)
    {
        isUpdatingUi = true;

        currentProject = project;
        currentMetadata = project.Document.Metadata;
        currentAssets = [.. project.Document.Assets];
        selectedAssetId = null;
        selectedImageRefId = null;
        selectedPoolAssetId = null;
        selectedAnnotationId = null;

        GuideTitleTextBox.Text = project.Document.Metadata.Title;
        steps.Clear();

        foreach (var step in project.Document.Steps.OrderBy(step => step.Order))
        {
            steps.Add(EditableStep.FromGuideStep(step));
        }

        StepsListBox.SelectedIndex = steps.Count > 0 ? 0 : -1;
        LoadSelectedStepIntoEditor();
        RefreshImagePoolAssets();
        RefreshSelectedStepAssets();
        RefreshSelectedAssetAnnotations();

        ProjectPathTextBlock.Text = project.ProjectDirectory;
        isDirty = false;
        isUpdatingUi = false;

        if (project.MissingAssetPaths.Count > 0)
        {
            SetStatus($"Opened guide. Missing assets: {project.MissingAssetPaths.Count}.");
        }

        UpdateUiState();
    }

    private void LoadSelectedStepIntoEditor()
    {
        isUpdatingUi = true;

        if (StepsListBox.SelectedItem is EditableStep selectedStep)
        {
            StepTitleTextBox.Text = selectedStep.Title;
            StepBodyTextBox.Text = selectedStep.Body;
            StepTitleTextBox.IsEnabled = true;
            StepBodyTextBox.IsEnabled = true;
        }
        else
        {
            StepTitleTextBox.Text = string.Empty;
            StepBodyTextBox.Text = string.Empty;
            StepTitleTextBox.IsEnabled = false;
            StepBodyTextBox.IsEnabled = false;
        }

        isUpdatingUi = false;
    }

    private GuideDocument BuildDocumentFromUi()
    {
        if (currentMetadata is null)
        {
            throw new InvalidOperationException("No guide project is open.");
        }

        var title = string.IsNullOrWhiteSpace(GuideTitleTextBox.Text)
            ? "Untitled guide"
            : GuideTitleTextBox.Text.Trim();

        return new GuideDocument
        {
            Metadata = currentMetadata with
            {
                Title = title,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            Steps = steps.Select(step => step.ToGuideStep()).ToList(),
            Assets = [.. currentAssets]
        };
    }

    private void MarkDirty()
    {
        isDirty = true;
        ScheduleGuidePreviewRefresh();
        UpdateUiState();
    }

    private void ScheduleGuidePreviewRefresh()
    {
        if (currentProject is null || currentMetadata is null)
        {
            return;
        }

        guidePreviewRefreshTimer.Stop();
        guidePreviewRefreshTimer.Start();
    }

    private async void GuidePreviewRefreshTimer_Tick(object? sender, EventArgs e)
    {
        guidePreviewRefreshTimer.Stop();

        if (currentProject is null || currentMetadata is null)
        {
            return;
        }

        try
        {
            await RefreshGuidePreviewAsync(selectGuideTab: false).ConfigureAwait(true);
        }
        catch
        {
            SetStatus("Preview refresh failed.");
        }
    }

    private void RenumberSteps()
    {
        for (var index = 0; index < steps.Count; index++)
        {
            steps[index].Order = index + 1;
        }
    }

    private void MoveSelectedStep(int direction)
    {
        var selectedIndex = StepsListBox.SelectedIndex;
        var targetIndex = selectedIndex + direction;
        if (selectedIndex < 0 || targetIndex < 0 || targetIndex >= steps.Count)
        {
            return;
        }

        steps.Move(selectedIndex, targetIndex);
        RenumberSteps();
        StepsListBox.SelectedIndex = targetIndex;
        MarkDirty();
        SetStatus(direction < 0 ? "Step moved up." : "Step moved down.");
        UpdateUiState();
    }

    private void SetButtonsEnabled(bool isEnabled)
    {
        NewGuideButton.IsEnabled = isEnabled;
        OpenGuideButton.IsEnabled = isEnabled;
        SaveGuideButton.IsEnabled = isEnabled;
        PreviewGuideButton.IsEnabled = isEnabled;
        RefreshWorkspacePreviewButton.IsEnabled = isEnabled;
        ExportGuideButton.IsEnabled = isEnabled;
        AddStepButton.IsEnabled = isEnabled;
        DeleteStepButton.IsEnabled = isEnabled;
        MoveStepUpButton.IsEnabled = isEnabled;
        MoveStepDownButton.IsEnabled = isEnabled;
        ImportImageButton.IsEnabled = isEnabled;
        PasteImageButton.IsEnabled = isEnabled;
        CaptureScreenshotButton.IsEnabled = isEnabled;
        FollowAlongCaptureButton.IsEnabled = isEnabled;
        AttachPoolImageButton.IsEnabled = isEnabled;
        ScanAssetPoolButton.IsEnabled = isEnabled;
        ImagePoolListBox.IsEnabled = isEnabled;
        InsertImageReferenceButton.IsEnabled = isEnabled;
        RemoveImageButton.IsEnabled = isEnabled;
        ClearCropButton.IsEnabled = isEnabled;
        CropXSlider.IsEnabled = isEnabled;
        CropYSlider.IsEnabled = isEnabled;
        CropWidthSlider.IsEnabled = isEnabled;
        CropHeightSlider.IsEnabled = isEnabled;
        AddHighlightButton.IsEnabled = isEnabled;
        AddLabelButton.IsEnabled = isEnabled;
        AddArrowButton.IsEnabled = isEnabled;
        AddRedactButton.IsEnabled = isEnabled;
        RemoveAnnotationButton.IsEnabled = isEnabled;
        MoveAnnotationUpButton.IsEnabled = isEnabled;
        MoveAnnotationDownButton.IsEnabled = isEnabled;
        AnnotationListBox.IsEnabled = isEnabled;
        AnnotationTextBox.IsEnabled = isEnabled;
        AnnotationXSlider.IsEnabled = isEnabled;
        AnnotationYSlider.IsEnabled = isEnabled;
        AnnotationWidthSlider.IsEnabled = isEnabled;
        AnnotationHeightSlider.IsEnabled = isEnabled;
        LabelStylePanel.IsEnabled = isEnabled;
        LabelFontSizeSlider.IsEnabled = isEnabled;
        LabelBoldToggle.IsEnabled = isEnabled;
        LabelItalicToggle.IsEnabled = isEnabled;
        LabelBackgroundOpacitySlider.IsEnabled = isEnabled;
    }

    private void UpdateUiState()
    {
        var hasProject = currentProject is not null && currentMetadata is not null;
        var hasSelectedStep = StepsListBox.SelectedItem is EditableStep;
        var hasSelectedAsset = selectedAssetId.HasValue && GetSelectedEditableAsset() is not null;
        var isPoolTabActive = IsPoolTabActive();
        var hasSelectedStepAsset = hasSelectedAsset && !isPoolTabActive;
        var hasSelectedAnnotation = selectedAnnotationId.HasValue && GetSelectedAnnotation() is not null;
        var hasSelectedPoolAsset = selectedPoolAssetId.HasValue && currentAssets.Any(asset => asset.Id == selectedPoolAssetId.Value);

        SaveGuideButton.IsEnabled = hasProject;
        PreviewGuideButton.IsEnabled = hasProject;
        RefreshWorkspacePreviewButton.IsEnabled = hasProject;
        ExportGuideButton.IsEnabled = hasProject;
        AddStepButton.IsEnabled = hasProject;
        DeleteStepButton.IsEnabled = hasProject && hasSelectedStep;
        MoveStepUpButton.IsEnabled = hasProject && StepsListBox.SelectedIndex > 0;
        MoveStepDownButton.IsEnabled = hasProject && StepsListBox.SelectedIndex >= 0 && StepsListBox.SelectedIndex < steps.Count - 1;
        ImportImageButton.IsEnabled = hasProject && hasSelectedStep;
        PasteImageButton.IsEnabled = hasProject && hasSelectedStep;
        CaptureScreenshotButton.IsEnabled = hasProject && hasSelectedStep;
        FollowAlongCaptureButton.IsEnabled = hasProject && hasSelectedStep;
        FollowAlongCaptureButton.Content = isFollowAlongCaptureActive ? "Stop follow" : "Follow along";
        AttachPoolImageButton.IsEnabled = hasProject && hasSelectedStep && hasSelectedPoolAsset;
        ScanAssetPoolButton.IsEnabled = hasProject;
        ImagePoolListBox.IsEnabled = hasProject;
        InsertImageReferenceButton.IsEnabled = hasProject && hasSelectedStep && hasSelectedStepAsset;
        RemoveImageButton.IsEnabled = hasProject && hasSelectedStep && hasSelectedStepAsset;
        ClearCropButton.IsEnabled = hasProject && hasSelectedStep && hasSelectedStepAsset && GetSelectedImageRef()?.Crop is not null;
        CropXSlider.IsEnabled = hasProject && hasSelectedStep && hasSelectedStepAsset;
        CropYSlider.IsEnabled = hasProject && hasSelectedStep && hasSelectedStepAsset;
        CropWidthSlider.IsEnabled = hasProject && hasSelectedStep && hasSelectedStepAsset;
        CropHeightSlider.IsEnabled = hasProject && hasSelectedStep && hasSelectedStepAsset;
        AddHighlightButton.IsEnabled = hasProject && hasSelectedStep && hasSelectedStepAsset;
        AddLabelButton.IsEnabled = hasProject && hasSelectedStep && hasSelectedStepAsset;
        AddArrowButton.IsEnabled = hasProject && hasSelectedStep && hasSelectedStepAsset;
        AddRedactButton.IsEnabled = hasProject && hasSelectedStep && hasSelectedStepAsset;
        RemoveAnnotationButton.IsEnabled = hasProject && hasSelectedStep && hasSelectedStepAsset && selectedAssetAnnotations.Count > 0;
        MoveAnnotationUpButton.IsEnabled = hasProject && hasSelectedStep && hasSelectedStepAsset && CanMoveSelectedAnnotation(-1);
        MoveAnnotationDownButton.IsEnabled = hasProject && hasSelectedStep && hasSelectedStepAsset && CanMoveSelectedAnnotation(1);
        AnnotationListBox.IsEnabled = hasProject && hasSelectedStep && hasSelectedStepAsset;
        AnnotationTextBox.IsEnabled = hasProject && hasSelectedStepAsset && hasSelectedAnnotation && GetSelectedAnnotation()?.Kind == GuideAnnotationKind.Label;
        AnnotationXSlider.IsEnabled = hasProject && hasSelectedStepAsset && hasSelectedAnnotation;
        AnnotationYSlider.IsEnabled = hasProject && hasSelectedStepAsset && hasSelectedAnnotation;
        AnnotationWidthSlider.IsEnabled = hasProject && hasSelectedStepAsset && hasSelectedAnnotation;
        AnnotationHeightSlider.IsEnabled = hasProject && hasSelectedStepAsset && hasSelectedAnnotation;
        LabelStylePanel.IsEnabled = hasProject && hasSelectedStepAsset && GetSelectedAnnotation()?.Kind == GuideAnnotationKind.Label;
        GuideTitleTextBox.IsEnabled = hasProject;
        StepTitleTextBox.IsEnabled = hasSelectedStep;
        StepBodyTextBox.IsEnabled = hasSelectedStep;
        DirtyIndicatorTextBlock.Text = isDirty ? "Unsaved changes" : "Saved";
    }

    private EditableStep? GetSelectedStep()
    {
        return StepsListBox.SelectedItem as EditableStep;
    }

    private bool IsPoolTabActive()
    {
        return StepImagesTabControl.SelectedItem == PoolImagesTab;
    }

    private EditableAsset? GetSelectedEditableAsset()
    {
        if (selectedImageRefId is { } imageRefId)
        {
            return selectedStepAssets.FirstOrDefault(asset => asset.ImageRefId == imageRefId);
        }

        return selectedAssetId is { } assetId
            ? selectedStepAssets.FirstOrDefault(asset => asset.Id == assetId)
            : null;
    }

    private void SelectAsset(Guid? assetId, Guid? imageRefId = null)
    {
        isChangingAssetSelection = true;
        selectedAssetId = assetId;
        selectedImageRefId = imageRefId;
        selectedAnnotationId = null;

        isRefreshingAssets = true;
        StepAssetsListBox.SelectedItem = imageRefId is { }
            ? selectedStepAssets.FirstOrDefault(asset => asset.ImageRefId == imageRefId)
            : selectedStepAssets.FirstOrDefault(asset => asset.Id == assetId);
        isRefreshingAssets = false;

        RefreshWorkspaceImagePreview();
        RefreshSelectedAssetAnnotations();
        LoadSelectedCropIntoEditor();
        UpdateUiState();

        Dispatcher.BeginInvoke(() => isChangingAssetSelection = false, DispatcherPriority.ContextIdle);
    }

    private void AttachAssetToStep(EditableStep step, GuideAsset asset)
    {
        if (!currentAssets.Any(candidate => candidate.Id == asset.Id))
        {
            currentAssets.Add(asset);
        }

        var imageRef = StepImageRef.Create(asset.Id);
        step.AssetIds.Add(asset.Id);
        step.ImageRefs.Add(imageRef);
        selectedAssetId = asset.Id;
        selectedImageRefId = imageRef.Id;
        selectedPoolAssetId = asset.Id;
        selectedAnnotationId = null;
        RefreshImagePoolAssets(asset.Id);
        RefreshSelectedStepAssets(preferredImageRefId: imageRef.Id);
        RefreshSelectedAssetAnnotations();
        MarkDirty();
    }

    private void AddAnnotationToSelectedImage(GuideAnnotationKind kind, string? text)
    {
        var selectedStep = GetSelectedStep();
        var selectedAsset = GetSelectedEditableAsset();
        if (selectedStep is null || selectedAsset is null)
        {
            return;
        }

        var annotations = GetSelectedAnnotationList();
        if (annotations is null)
        {
            return;
        }

        var annotation = new GuideAnnotation
        {
            Id = Guid.NewGuid(),
            Kind = kind,
            AssetId = selectedAsset.Id,
            Text = text,
            Style = kind == GuideAnnotationKind.Label ? new GuideAnnotationStyle() : null,
            Bounds = CreateDefaultBounds(kind, annotations.Count(annotation => annotation.AssetId == selectedAsset.Id))
        };

        annotations.Add(annotation);
        SyncStepAnnotationsFromImageRefs(selectedStep);
        selectedAnnotationId = annotation.Id;

        RefreshSelectedStepAssets(selectedAsset.Id, selectedAsset.ImageRefId);
        RefreshSelectedAssetAnnotations(annotation.Id);
        MarkDirty();
        SetStatus($"{kind} annotation added to selected image.");
    }

    private void UpdateSelectedLabelStyle(Func<GuideAnnotationStyle, GuideAnnotationStyle> update)
    {
        if (GetSelectedAnnotation() is not { Kind: GuideAnnotationKind.Label } annotation)
        {
            return;
        }

        UpdateSelectedAnnotation(annotation with
        {
            Style = update(GetEffectiveStyle(annotation))
        }, reloadEditor: false);
    }

    private bool CanMoveSelectedAnnotation(int direction)
    {
        if (selectedAssetId is not { } assetId ||
            selectedAnnotationId is not { } annotationId ||
            GetSelectedAnnotationList() is not { } annotations)
        {
            return false;
        }

        var assetAnnotations = annotations
            .Where(annotation => annotation.AssetId == assetId)
            .ToList();
        var selectedIndex = assetAnnotations.FindIndex(annotation => annotation.Id == annotationId);
        var targetIndex = selectedIndex + direction;
        return selectedIndex >= 0 && targetIndex >= 0 && targetIndex < assetAnnotations.Count;
    }

    private void MoveSelectedAnnotation(int direction)
    {
        if (selectedAssetId is not { } assetId ||
            selectedAnnotationId is not { } annotationId ||
            GetSelectedAnnotationList() is not { } annotations)
        {
            return;
        }

        var assetAnnotationIndexes = annotations
            .Select((annotation, index) => new { Annotation = annotation, Index = index })
            .Where(item => item.Annotation.AssetId == assetId)
            .ToList();
        var selectedIndex = assetAnnotationIndexes.FindIndex(item => item.Annotation.Id == annotationId);
        var targetIndex = selectedIndex + direction;
        if (selectedIndex < 0 || targetIndex < 0 || targetIndex >= assetAnnotationIndexes.Count)
        {
            return;
        }

        var sourceListIndex = assetAnnotationIndexes[selectedIndex].Index;
        var targetListIndex = assetAnnotationIndexes[targetIndex].Index;
        (annotations[sourceListIndex], annotations[targetListIndex]) =
            (annotations[targetListIndex], annotations[sourceListIndex]);

        SyncStepAnnotationsFromImageRefs(GetSelectedStep());

        RefreshSelectedStepAssets(assetId, selectedImageRefId);
        RefreshSelectedAssetAnnotations(annotationId);
        MarkDirty();
        SetStatus(direction < 0 ? "Annotation moved up." : "Annotation moved down.");
    }

    private static AnnotationBounds CreateDefaultBounds(GuideAnnotationKind kind, int existingAnnotationCount)
    {
        var offset = Math.Min(existingAnnotationCount * 0.05, 0.25);
        return kind switch
        {
            GuideAnnotationKind.Label => new AnnotationBounds { X = 0.08 + offset, Y = 0.08 + offset, Width = 0.24, Height = 0.1 },
            GuideAnnotationKind.Arrow => new AnnotationBounds { X = 0.12 + offset, Y = 0.45, Width = 0.35, Height = 0.04 },
            GuideAnnotationKind.Blur => new AnnotationBounds { X = 0.18 + offset, Y = 0.18 + offset, Width = 0.28, Height = 0.18 },
            _ => new AnnotationBounds { X = 0.12 + offset, Y = 0.12 + offset, Width = 0.32, Height = 0.22 }
        };
    }

    private static void SyncStepAnnotationsFromImageRefs(EditableStep? step)
    {
        if (step is null || step.ImageRefs.Count == 0)
        {
            return;
        }

        step.Annotations.Clear();
        step.Annotations.AddRange(step.ImageRefs.SelectMany(imageRef => imageRef.Annotations));
    }

    private void RefreshImagePoolAssets(Guid? preferredAssetId = null)
    {
        var targetAssetId = preferredAssetId ?? selectedPoolAssetId;
        imagePoolAssets.Clear();

        if (currentProject is null)
        {
            selectedPoolAssetId = null;
            ImagePoolListBox.SelectedItem = null;
            return;
        }

        foreach (var asset in currentAssets.OrderByDescending(asset => asset.CapturedAt))
        {
            imagePoolAssets.Add(EditableAsset.FromGuideAsset(
                currentProject.ProjectDirectory,
                asset,
                []));
        }

        var selectedAsset = imagePoolAssets.FirstOrDefault(asset => asset.Id == targetAssetId)
            ?? imagePoolAssets.FirstOrDefault();
        selectedPoolAssetId = selectedAsset?.Id;
        ImagePoolListBox.SelectedItem = selectedAsset;
    }

    private void RefreshSelectedStepAssets(Guid? preferredAssetId = null, Guid? preferredImageRefId = null)
    {
        var previousAssetId = selectedAssetId;
        var previousImageRefId = selectedImageRefId;
        var targetAssetId = preferredAssetId ?? selectedAssetId;
        var targetImageRefId = preferredImageRefId ?? selectedImageRefId;
        isRefreshingAssets = true;
        selectedStepAssets.Clear();

        if (currentProject is null || GetSelectedStep() is not { } selectedStep)
        {
            selectedAssetId = null;
            selectedImageRefId = null;
            selectedAnnotationId = null;
            StepAssetsListBox.SelectedItem = null;
            RefreshWorkspaceImagePreview();
            isRefreshingAssets = false;
            return;
        }

        if (selectedStep.ImageRefs.Count > 0)
        {
            var usageCounts = new Dictionary<Guid, int>();
            foreach (var imageRef in selectedStep.ImageRefs)
            {
                var asset = currentAssets.FirstOrDefault(candidate => candidate.Id == imageRef.AssetId);
                if (asset is null)
                {
                    continue;
                }

                usageCounts.TryGetValue(asset.Id, out var usageNumber);
                usageNumber++;
                usageCounts[asset.Id] = usageNumber;

                selectedStepAssets.Add(EditableAsset.FromGuideAsset(
                    currentProject.ProjectDirectory,
                    asset,
                    imageRef.Annotations,
                    imageRef.Crop,
                    imageRef.Id,
                    usageNumber));
            }
        }
        else
        {
            foreach (var assetId in selectedStep.AssetIds)
            {
                var asset = currentAssets.FirstOrDefault(candidate => candidate.Id == assetId);
                if (asset is null)
                {
                    continue;
                }

                selectedStepAssets.Add(EditableAsset.FromGuideAsset(
                    currentProject.ProjectDirectory,
                    asset,
                    selectedStep.Annotations.Where(annotation => annotation.AssetId == asset.Id)));
            }
        }

        var selectedAsset = targetImageRefId is { }
            ? selectedStepAssets.FirstOrDefault(asset => asset.ImageRefId == targetImageRefId)
            : null;
        selectedAsset ??= selectedStepAssets.FirstOrDefault(asset => asset.Id == targetAssetId)
            ?? selectedStepAssets.FirstOrDefault();
        selectedAssetId = selectedAsset?.Id;
        selectedImageRefId = selectedAsset?.ImageRefId;
        if (selectedAssetId != previousAssetId || selectedImageRefId != previousImageRefId)
        {
            selectedAnnotationId = null;
        }

        StepAssetsListBox.SelectedItem = selectedAsset;
        RefreshWorkspaceImagePreview();
        isRefreshingAssets = false;
    }

    private void RefreshWorkspaceImagePreview(EditableAsset? selectedAsset = null)
    {
        selectedAsset ??= GetSelectedEditableAsset();
        var useCropSurface = IsCropWorkspaceModeActive() && selectedAsset is not null;

        WorkspaceSelectedImage.Source = useCropSurface
            ? selectedAsset?.OriginalImageSource
            : selectedAsset?.DisplayImageSource;
        WorkspaceSelectedImage.Width = useCropSurface
            ? selectedAsset?.WorkspaceOriginalImageWidth ?? 0
            : selectedAsset?.WorkspaceImageWidth ?? 0;
        WorkspaceSelectedImage.Height = useCropSurface
            ? selectedAsset?.WorkspaceOriginalImageHeight ?? 0
            : selectedAsset?.WorkspaceImageHeight ?? 0;
        System.Windows.Controls.Canvas.SetLeft(
            WorkspaceSelectedImage,
            useCropSurface ? selectedAsset?.WorkspaceOriginalImageX ?? 0 : selectedAsset?.WorkspaceImageX ?? 0);
        System.Windows.Controls.Canvas.SetTop(
            WorkspaceSelectedImage,
            useCropSurface ? selectedAsset?.WorkspaceOriginalImageY ?? 0 : selectedAsset?.WorkspaceImageY ?? 0);

        WorkspaceAnnotationOverlay.ItemsSource = selectedAsset?.Annotations ?? Array.Empty<EditableAnnotationPreview>();
        WorkspaceAnnotationOverlay.Visibility = useCropSurface ? Visibility.Collapsed : Visibility.Visible;
        WorkspaceImageSurface.Cursor = useCropSurface
            ? System.Windows.Input.Cursors.Cross
            : System.Windows.Input.Cursors.Arrow;
        RefreshWorkspaceCropOverlay(selectedAsset);
    }

    private bool IsCropWorkspaceModeActive()
    {
        return CropEditorExpander.IsExpanded &&
            !IsPoolTabActive() &&
            GetSelectedImageRef() is not null;
    }

    private void RefreshWorkspaceCropOverlay(EditableAsset? selectedAsset)
    {
        if (!IsCropWorkspaceModeActive() ||
            selectedAsset is null ||
            selectedAsset.WorkspaceOriginalImageWidth <= 0 ||
            selectedAsset.WorkspaceOriginalImageHeight <= 0)
        {
            WorkspaceCropOverlayCanvas.Visibility = Visibility.Collapsed;
            WorkspaceCropOverlay.Width = 0;
            WorkspaceCropOverlay.Height = 0;
            return;
        }

        var crop = GetSelectedImageRef()?.Crop ?? CreateFullImageCrop();
        var left = selectedAsset.WorkspaceOriginalImageX + crop.X * selectedAsset.WorkspaceOriginalImageWidth;
        var top = selectedAsset.WorkspaceOriginalImageY + crop.Y * selectedAsset.WorkspaceOriginalImageHeight;
        var width = crop.Width * selectedAsset.WorkspaceOriginalImageWidth;
        var height = crop.Height * selectedAsset.WorkspaceOriginalImageHeight;

        WorkspaceCropOverlayCanvas.Visibility = Visibility.Visible;
        WorkspaceCropOverlay.Width = Math.Max(1, width);
        WorkspaceCropOverlay.Height = Math.Max(1, height);
        System.Windows.Controls.Canvas.SetLeft(WorkspaceCropOverlay, left);
        System.Windows.Controls.Canvas.SetTop(WorkspaceCropOverlay, top);
    }

    private void UpdateCropFromWorkspaceDrag(System.Windows.Point pointer, EditableAsset selectedAsset)
    {
        if (GetSelectedImageRef() is not { } imageRef)
        {
            return;
        }

        var clampedPointer = ClampPointToWorkspaceOriginalImage(pointer, selectedAsset);
        var startX = ToWorkspaceOriginalImageRatio(cropDragStart.X, selectedAsset.WorkspaceOriginalImageX, selectedAsset.WorkspaceOriginalImageWidth);
        var startY = ToWorkspaceOriginalImageRatio(cropDragStart.Y, selectedAsset.WorkspaceOriginalImageY, selectedAsset.WorkspaceOriginalImageHeight);
        var endX = ToWorkspaceOriginalImageRatio(clampedPointer.X, selectedAsset.WorkspaceOriginalImageX, selectedAsset.WorkspaceOriginalImageWidth);
        var endY = ToWorkspaceOriginalImageRatio(clampedPointer.Y, selectedAsset.WorkspaceOriginalImageY, selectedAsset.WorkspaceOriginalImageHeight);
        var x = Math.Min(startX, endX);
        var y = Math.Min(startY, endY);
        var width = Math.Max(0.01, Math.Abs(endX - startX));
        var height = Math.Max(0.01, Math.Abs(endY - startY));

        var crop = new ImageCropBounds
        {
            X = Math.Clamp(x, 0, 1 - width),
            Y = Math.Clamp(y, 0, 1 - height),
            Width = Math.Clamp(width, 0.01, 1),
            Height = Math.Clamp(height, 0.01, 1)
        };

        ApplySelectedImageRefCropQuietly(imageRef, crop);
        LoadCropIntoEditor(crop);
        RefreshWorkspaceCropOverlay(selectedAsset);
    }

    private void ApplySelectedImageRefCropQuietly(StepImageRef imageRef, ImageCropBounds crop)
    {
        var selectedStep = GetSelectedStep();
        if (selectedStep is null)
        {
            return;
        }

        var index = selectedStep.ImageRefs.FindIndex(candidate => candidate.Id == imageRef.Id);
        if (index < 0)
        {
            return;
        }

        selectedStep.ImageRefs[index] = imageRef with { Crop = crop };
        selectedAssetId = imageRef.AssetId;
        selectedImageRefId = imageRef.Id;
    }

    private void EndCropDrag(bool commit)
    {
        isDraggingCrop = false;
        WorkspaceImageSurface.ReleaseMouseCapture();

        if (!commit || GetSelectedImageRef() is not { } imageRef)
        {
            return;
        }

        RefreshSelectedStepAssets(imageRef.AssetId, imageRef.Id);
        LoadSelectedCropIntoEditor();
        MarkDirty();
        SetStatus("Crop updated for selected image.");
    }

    private static ImageCropBounds CreateFullImageCrop()
    {
        return new ImageCropBounds
        {
            X = 0,
            Y = 0,
            Width = 1,
            Height = 1
        };
    }

    private static bool IsPointInsideWorkspaceOriginalImage(System.Windows.Point point, EditableAsset selectedAsset)
    {
        return point.X >= selectedAsset.WorkspaceOriginalImageX &&
            point.X <= selectedAsset.WorkspaceOriginalImageX + selectedAsset.WorkspaceOriginalImageWidth &&
            point.Y >= selectedAsset.WorkspaceOriginalImageY &&
            point.Y <= selectedAsset.WorkspaceOriginalImageY + selectedAsset.WorkspaceOriginalImageHeight;
    }

    private static System.Windows.Point ClampPointToWorkspaceOriginalImage(System.Windows.Point point, EditableAsset selectedAsset)
    {
        return new System.Windows.Point(
            Math.Clamp(
                point.X,
                selectedAsset.WorkspaceOriginalImageX,
                selectedAsset.WorkspaceOriginalImageX + selectedAsset.WorkspaceOriginalImageWidth),
            Math.Clamp(
                point.Y,
                selectedAsset.WorkspaceOriginalImageY,
                selectedAsset.WorkspaceOriginalImageY + selectedAsset.WorkspaceOriginalImageHeight));
    }

    private static double ToWorkspaceOriginalImageRatio(double value, double imageOffset, double imageSize)
    {
        return imageSize <= 0
            ? 0
            : Math.Clamp((value - imageOffset) / imageSize, 0, 1);
    }

    private void RefreshSelectedAssetAnnotations(Guid? preferredAnnotationId = null, bool reloadEditor = true)
    {
        var targetAnnotationId = preferredAnnotationId ?? selectedAnnotationId;
        isRefreshingAnnotations = true;
        selectedAssetAnnotations.Clear();

        if (selectedAssetId is not { } assetId || GetSelectedAnnotationList() is not { } annotations)
        {
            selectedAnnotationId = null;
            AnnotationListBox.SelectedItem = null;
            isRefreshingAnnotations = false;
            if (reloadEditor)
            {
                LoadSelectedAnnotationIntoEditor();
            }

            return;
        }

        foreach (var annotation in annotations.Where(annotation => annotation.AssetId == assetId))
        {
            selectedAssetAnnotations.Add(EditableAnnotation.FromGuideAnnotation(annotation));
        }

        var selectedAnnotation = selectedAssetAnnotations.FirstOrDefault(annotation => annotation.Id == targetAnnotationId)
            ?? selectedAssetAnnotations.FirstOrDefault();
        selectedAnnotationId = selectedAnnotation?.Id;
        AnnotationListBox.SelectedItem = selectedAnnotation;
        isRefreshingAnnotations = false;
        if (reloadEditor)
        {
            LoadSelectedAnnotationIntoEditor();
        }
    }

    private GuideAnnotation? GetSelectedAnnotation()
    {
        if (selectedAnnotationId is not { } annotationId || GetSelectedAnnotationList() is not { } annotations)
        {
            return null;
        }

        return annotations.FirstOrDefault(annotation => annotation.Id == annotationId);
    }

    private List<GuideAnnotation>? GetSelectedAnnotationList()
    {
        if (GetSelectedStep() is not { } selectedStep)
        {
            return null;
        }

        if (selectedImageRefId is { } imageRefId)
        {
            return selectedStep.ImageRefs.FirstOrDefault(imageRef => imageRef.Id == imageRefId)?.Annotations;
        }

        return selectedStep.Annotations;
    }

    private StepImageRef? GetSelectedImageRef()
    {
        if (GetSelectedStep() is not { } selectedStep)
        {
            return null;
        }

        if (selectedImageRefId is { } imageRefId)
        {
            return selectedStep.ImageRefs.FirstOrDefault(imageRef => imageRef.Id == imageRefId);
        }

        return selectedAssetId is { } assetId
            ? selectedStep.ImageRefs.FirstOrDefault(imageRef => imageRef.AssetId == assetId)
            : null;
    }

    private void UpdateSelectedImageRef(StepImageRef updatedImageRef)
    {
        var selectedStep = GetSelectedStep();
        if (selectedStep is null)
        {
            return;
        }

        var index = selectedStep.ImageRefs.FindIndex(imageRef => imageRef.Id == updatedImageRef.Id);
        if (index < 0)
        {
            return;
        }

        selectedStep.ImageRefs[index] = updatedImageRef;
        selectedAssetId = updatedImageRef.AssetId;
        selectedImageRefId = updatedImageRef.Id;
        RefreshSelectedStepAssets(updatedImageRef.AssetId, updatedImageRef.Id);
        LoadSelectedCropIntoEditor();
        MarkDirty();
        SetStatus("Crop updated for selected image.");
    }

    private void UpdateSelectedAnnotation(GuideAnnotation updatedAnnotation, bool reloadEditor = true, bool refreshAssets = true)
    {
        if (GetSelectedAnnotationList() is not { } annotations)
        {
            return;
        }

        var index = annotations.FindIndex(annotation => annotation.Id == updatedAnnotation.Id);
        if (index < 0)
        {
            return;
        }

        annotations[index] = updatedAnnotation;
        SyncStepAnnotationsFromImageRefs(GetSelectedStep());

        selectedAnnotationId = updatedAnnotation.Id;
        if (refreshAssets && selectedAssetId == updatedAnnotation.AssetId)
        {
            RefreshSelectedStepAssets(updatedAnnotation.AssetId, selectedImageRefId);
        }

        RefreshSelectedAssetAnnotations(updatedAnnotation.Id, reloadEditor);
        MarkDirty();
    }

    private void UpdateSelectedAnnotationQuietly(GuideAnnotation updatedAnnotation)
    {
        if (GetSelectedAnnotationList() is not { } annotations)
        {
            return;
        }

        var index = annotations.FindIndex(annotation => annotation.Id == updatedAnnotation.Id);
        if (index < 0)
        {
            return;
        }

        annotations[index] = updatedAnnotation;
        SyncStepAnnotationsFromImageRefs(GetSelectedStep());

        selectedAnnotationId = updatedAnnotation.Id;
        if (selectedAssetId == updatedAnnotation.AssetId)
        {
            RefreshSelectedStepAssets(updatedAnnotation.AssetId, selectedImageRefId);
            RefreshSelectedAssetAnnotations(updatedAnnotation.Id, reloadEditor: false);
        }

        isDirty = true;
        ScheduleGuidePreviewRefresh();
        UpdateUiState();
    }

    private void LoadSelectedAnnotationIntoEditor()
    {
        isUpdatingUi = true;

        if (GetSelectedAnnotation() is { } annotation)
        {
            ApplyAnnotationSliderRanges(annotation.Bounds);
            AnnotationTextBox.Text = annotation.Text ?? string.Empty;
            AnnotationXSlider.Value = annotation.Bounds.X * 100;
            AnnotationYSlider.Value = annotation.Bounds.Y * 100;
            AnnotationWidthSlider.Value = annotation.Bounds.Width * 100;
            AnnotationHeightSlider.Value = annotation.Bounds.Height * 100;
            LoadLabelStyleIntoEditor(annotation);
        }
        else
        {
            AnnotationTextBox.Text = string.Empty;
            AnnotationXSlider.Value = 0;
            AnnotationYSlider.Value = 0;
            AnnotationWidthSlider.Value = 1;
            AnnotationHeightSlider.Value = 1;
            AnnotationXSlider.Maximum = 100;
            AnnotationYSlider.Maximum = 100;
            AnnotationWidthSlider.Maximum = 100;
            AnnotationHeightSlider.Maximum = 100;
            LoadLabelStyleIntoEditor(null);
        }

        isUpdatingUi = false;
    }

    private void LoadSelectedCropIntoEditor()
    {
        LoadCropIntoEditor(GetSelectedImageRef()?.Crop ?? CreateFullImageCrop());
    }

    private void LoadCropIntoEditor(ImageCropBounds crop)
    {
        isUpdatingUi = true;
        ApplyCropSliderRanges(crop);
        CropXSlider.Value = crop.X * 100;
        CropYSlider.Value = crop.Y * 100;
        CropWidthSlider.Value = crop.Width * 100;
        CropHeightSlider.Value = crop.Height * 100;
        isUpdatingUi = false;
    }

    private void ApplyAnnotationSliderRanges(AnnotationBounds bounds)
    {
        AnnotationXSlider.Maximum = Math.Max(0, (1 - bounds.Width) * 100);
        AnnotationYSlider.Maximum = Math.Max(0, (1 - bounds.Height) * 100);
        AnnotationWidthSlider.Maximum = Math.Max(1, (1 - bounds.X) * 100);
        AnnotationHeightSlider.Maximum = Math.Max(1, (1 - bounds.Y) * 100);
    }

    private void ApplyCropSliderRanges(ImageCropBounds crop)
    {
        CropXSlider.Maximum = Math.Max(0, (1 - crop.Width) * 100);
        CropYSlider.Maximum = Math.Max(0, (1 - crop.Height) * 100);
        CropWidthSlider.Maximum = Math.Max(1, (1 - crop.X) * 100);
        CropHeightSlider.Maximum = Math.Max(1, (1 - crop.Y) * 100);
    }

    private void LoadLabelStyleIntoEditor(GuideAnnotation? annotation)
    {
        var style = annotation?.Kind == GuideAnnotationKind.Label
            ? GetEffectiveStyle(annotation)
            : new GuideAnnotationStyle();
        LabelFontSizeSlider.Value = style.FontSize;
        LabelBoldToggle.IsChecked = style.IsBold;
        LabelItalicToggle.IsChecked = style.IsItalic;
        LabelBackgroundOpacitySlider.Value = style.BackgroundOpacity * 100;
    }

    private void LoadDraggedAnnotationPositionIntoEditor(AnnotationBounds bounds)
    {
        isUpdatingUi = true;
        AnnotationXSlider.Maximum = Math.Max(0, (1 - bounds.Width) * 100);
        AnnotationYSlider.Maximum = Math.Max(0, (1 - bounds.Height) * 100);
        AnnotationXSlider.Value = bounds.X * 100;
        AnnotationYSlider.Value = bounds.Y * 100;
        isUpdatingUi = false;
    }

    private void EndAnnotationDrag()
    {
        isDraggingAnnotation = false;
        draggedAnnotationId = null;
        WorkspaceImageSurface.ReleaseMouseCapture();
    }

    private static GuideAnnotationStyle GetEffectiveStyle(GuideAnnotation annotation)
    {
        return annotation.Style ?? new GuideAnnotationStyle();
    }

    private static SolidColorBrush CreateBrush(string color, double opacity = 1)
    {
        try
        {
            var parsedColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color);
            parsedColor.A = (byte)Math.Round(Math.Clamp(opacity, 0, 1) * 255);
            return new SolidColorBrush(parsedColor);
        }
        catch (FormatException)
        {
            return new SolidColorBrush(System.Windows.Media.Color.FromArgb(
                (byte)Math.Round(Math.Clamp(opacity, 0, 1) * 255),
                26,
                115,
                232));
        }
    }

    private static MemoryStream EncodePng(BitmapSource image)
    {
        var stream = new MemoryStream();
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        encoder.Save(stream);
        stream.Position = 0;
        return stream;
    }

    private static MemoryStream CaptureVirtualScreenPng()
    {
        var bounds = Forms.SystemInformation.VirtualScreen;
        return CaptureBoundsPng(bounds);
    }

    private static MemoryStream CaptureScreenPng(Forms.Screen screen)
    {
        return CaptureBoundsPng(screen.Bounds);
    }

    private static MemoryStream CaptureBoundsPng(Drawing.Rectangle bounds)
    {
        using var bitmap = new Drawing.Bitmap(bounds.Width, bounds.Height);
        using var graphics = Drawing.Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size);

        var stream = new MemoryStream();
        bitmap.Save(stream, Drawing.Imaging.ImageFormat.Png);
        stream.Position = 0;
        return stream;
    }

    private static IntPtr GetCurrentModuleHandle()
    {
        var moduleName = Process.GetCurrentProcess().MainModule?.ModuleName;
        return string.IsNullOrWhiteSpace(moduleName)
            ? IntPtr.Zero
            : GetModuleHandle(moduleName);
    }

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct NativePoint
    {
        public readonly int X;

        public readonly int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct LowLevelMouseHookStruct
    {
        public readonly NativePoint Point;

        public readonly int MouseData;

        public readonly int Flags;

        public readonly int Time;

        public readonly IntPtr ExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    private void SetStatus(string message)
    {
        StatusTextBlock.Text = message;
    }

    private void ApplyTheme(bool dark)
    {
        SetBrush("SurfaceBrush", dark ? "#1F1F1F" : "#FFFFFF");
        SetBrush("CanvasBrush", dark ? "#121212" : "#F8FAFD");
        SetBrush("PrimaryBrush", dark ? "#8AB4F8" : "#1A73E8");
        SetBrush("PrimaryDarkBrush", dark ? "#669DF6" : "#1558B0");
        SetBrush("TextBrush", dark ? "#E8EAED" : "#202124");
        SetBrush("MutedTextBrush", dark ? "#BDC1C6" : "#5F6368");
        SetBrush("BorderBrushSoft", dark ? "#3C4043" : "#DADCE0");
        SetBrush("ButtonBackgroundBrush", dark ? "#2B2C2F" : "#FFFFFF");
        SetBrush("ButtonHoverBrush", dark ? "#35363A" : "#F1F3F4");
        SetBrush("InputBackgroundBrush", dark ? "#202124" : "#FFFFFF");
    }

    private void SetBrush(string key, string color)
    {
        Resources[key] = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color));
    }

    public sealed class EditableStep : INotifyPropertyChanged
    {
        private int order;
        private string title = string.Empty;
        private string body = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        public required Guid Id { get; init; }

        public int Order
        {
            get => order;
            set
            {
                if (order == value)
                {
                    return;
                }

                order = value;
                OnPropertyChanged(nameof(Order));
            }
        }

        public string Title
        {
            get => title;
            set
            {
                if (title == value)
                {
                    return;
                }

                title = value;
                OnPropertyChanged(nameof(Title));
            }
        }

        public string Body
        {
            get => body;
            set
            {
                if (body == value)
                {
                    return;
                }

                body = value;
                OnPropertyChanged(nameof(Body));
            }
        }

        public List<Guid> AssetIds { get; init; } = [];

        public List<StepImageRef> ImageRefs { get; init; } = [];

        public List<GuideAnnotation> Annotations { get; init; } = [];

        public static EditableStep FromGuideStep(GuideStep step)
        {
            var imageRefs = step.ImageRefs.Count > 0
                ? step.ImageRefs.Select(CloneImageRef).ToList()
                : CreateImageRefsFromLegacyStep(step);
            var assetIds = imageRefs.Count > 0
                ? imageRefs.Select(imageRef => imageRef.AssetId).ToList()
                : [.. step.AssetIds];
            var annotations = imageRefs.Count > 0
                ? imageRefs.SelectMany(imageRef => imageRef.Annotations).ToList()
                : [.. step.Annotations];

            return new EditableStep
            {
                Id = step.Id,
                Order = step.Order,
                Title = step.Title,
                Body = step.Body,
                AssetIds = assetIds,
                ImageRefs = imageRefs,
                Annotations = annotations
            };
        }

        public GuideStep ToGuideStep()
        {
            var imageRefs = BuildImageRefs();
            var assetIds = imageRefs.Count > 0
                ? imageRefs.Select(imageRef => imageRef.AssetId).ToList()
                : [.. AssetIds];
            return new GuideStep
            {
                Id = Id,
                Order = Order,
                Title = string.IsNullOrWhiteSpace(Title) ? "Untitled step" : Title.Trim(),
                Body = Body,
                AssetIds = assetIds,
                ImageRefs = imageRefs,
                Annotations = imageRefs.Count > 0
                    ? imageRefs.SelectMany(imageRef => imageRef.Annotations).ToList()
                    : [.. Annotations]
            };
        }

        private List<StepImageRef> BuildImageRefs()
        {
            if (ImageRefs.Count > 0)
            {
                return ImageRefs.Select(CloneImageRef).ToList();
            }

            return AssetIds
                .Select(assetId => StepImageRef.Create(assetId) with
                {
                    Annotations = Annotations
                        .Where(annotation => annotation.AssetId == assetId)
                        .ToList()
                })
                .ToList();
        }

        private static List<StepImageRef> CreateImageRefsFromLegacyStep(GuideStep step)
        {
            return step.AssetIds
                .Select(assetId => StepImageRef.Create(assetId) with
                {
                    Annotations = step.Annotations
                        .Where(annotation => annotation.AssetId == assetId)
                        .ToList()
                })
                .ToList();
        }

        private static StepImageRef CloneImageRef(StepImageRef imageRef)
        {
            return imageRef with
            {
                Annotations = [.. imageRef.Annotations]
            };
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public sealed record EditableAsset
    {
        public required Guid Id { get; init; }

        public Guid? ImageRefId { get; init; }

        public required string Caption { get; init; }

        public required string RelativePath { get; init; }

        public required string FullPath { get; init; }

        public required ImageSource? DisplayImageSource { get; init; }

        public required ImageSource? OriginalImageSource { get; init; }

        public required string AnnotationSummary { get; init; }

        public required string CropSummary { get; init; }

        public required IReadOnlyList<EditableAnnotationPreview> Annotations { get; init; }

        public required double ThumbImageX { get; init; }

        public required double ThumbImageY { get; init; }

        public required double ThumbImageWidth { get; init; }

        public required double ThumbImageHeight { get; init; }

        public required double EditorImageX { get; init; }

        public required double EditorImageY { get; init; }

        public required double EditorImageWidth { get; init; }

        public required double EditorImageHeight { get; init; }

        public required double WorkspaceImageX { get; init; }

        public required double WorkspaceImageY { get; init; }

        public required double WorkspaceImageWidth { get; init; }

        public required double WorkspaceImageHeight { get; init; }

        public required double WorkspaceOriginalImageX { get; init; }

        public required double WorkspaceOriginalImageY { get; init; }

        public required double WorkspaceOriginalImageWidth { get; init; }

        public required double WorkspaceOriginalImageHeight { get; init; }

        public static EditableAsset FromGuideAsset(
            string projectDirectory,
            GuideAsset asset,
            IEnumerable<GuideAnnotation> annotations,
            ImageCropBounds? crop = null,
            Guid? imageRefId = null,
            int usageNumber = 0)
        {
            const double thumbFrameWidth = 96;
            const double thumbFrameHeight = 64;
            const double editorFrameWidth = 360;
            const double editorFrameHeight = 210;
            const double workspaceFrameWidth = 760;
            const double workspaceFrameHeight = 520;

            var annotationList = annotations.ToArray();
            var fullPath = Path.Combine(projectDirectory, asset.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            var originalImage = CreateDisplayImage(fullPath, crop: null);
            var displayImage = crop is null ? originalImage : CreateDisplayImage(fullPath, crop);
            var originalImageSize = GetImageSize(originalImage) ?? ReadImageSize(fullPath);
            var imageSize = GetImageSize(displayImage) ?? originalImageSize;
            var thumbGeometry = CalculateFitGeometry(thumbFrameWidth, thumbFrameHeight, imageSize.Width, imageSize.Height);
            var editorGeometry = CalculateFitGeometry(editorFrameWidth, editorFrameHeight, imageSize.Width, imageSize.Height);
            var workspaceGeometry = CalculateFitGeometry(workspaceFrameWidth, workspaceFrameHeight, imageSize.Width, imageSize.Height);
            var workspaceOriginalGeometry = CalculateFitGeometry(workspaceFrameWidth, workspaceFrameHeight, originalImageSize.Width, originalImageSize.Height);

            return new EditableAsset
            {
                Id = asset.Id,
                ImageRefId = imageRefId,
                Caption = CreateCaption(asset, usageNumber),
                RelativePath = asset.RelativePath,
                FullPath = fullPath,
                DisplayImageSource = displayImage,
                OriginalImageSource = originalImage,
                AnnotationSummary = annotationList.Length == 0 ? "No annotations" : $"{annotationList.Length} annotation(s)",
                CropSummary = crop is null ? "Full image" : $"Crop X {ToPercent(crop.X)}, Y {ToPercent(crop.Y)}, W {ToPercent(crop.Width)}, H {ToPercent(crop.Height)}",
                Annotations = annotationList.Select(annotation => EditableAnnotationPreview.FromGuideAnnotation(annotation, thumbGeometry, editorGeometry, workspaceGeometry)).ToArray(),
                ThumbImageX = thumbGeometry.X,
                ThumbImageY = thumbGeometry.Y,
                ThumbImageWidth = thumbGeometry.Width,
                ThumbImageHeight = thumbGeometry.Height,
                EditorImageX = editorGeometry.X,
                EditorImageY = editorGeometry.Y,
                EditorImageWidth = editorGeometry.Width,
                EditorImageHeight = editorGeometry.Height,
                WorkspaceImageX = workspaceGeometry.X,
                WorkspaceImageY = workspaceGeometry.Y,
                WorkspaceImageWidth = workspaceGeometry.Width,
                WorkspaceImageHeight = workspaceGeometry.Height,
                WorkspaceOriginalImageX = workspaceOriginalGeometry.X,
                WorkspaceOriginalImageY = workspaceOriginalGeometry.Y,
                WorkspaceOriginalImageWidth = workspaceOriginalGeometry.Width,
                WorkspaceOriginalImageHeight = workspaceOriginalGeometry.Height
            };
        }

        private static string CreateCaption(GuideAsset asset, int usageNumber)
        {
            var caption = string.IsNullOrWhiteSpace(asset.Caption) ? Path.GetFileName(asset.RelativePath) : asset.Caption;
            return usageNumber > 1 ? $"{caption} (use {usageNumber})" : caption;
        }

        private static BitmapSource? CreateDisplayImage(string fullPath, ImageCropBounds? crop)
        {
            if (!File.Exists(fullPath))
            {
                return null;
            }

            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = new Uri(fullPath);
                image.EndInit();
                image.Freeze();

                if (crop is null)
                {
                    return image;
                }

                var cropRect = CalculateCropPixelRect(image.PixelWidth, image.PixelHeight, crop);
                var croppedImage = new CroppedBitmap(image, cropRect);
                croppedImage.Freeze();
                return croppedImage;
            }
            catch
            {
                return null;
            }
        }

        private static PreviewGeometry? GetImageSize(BitmapSource? image)
        {
            return image is null
                ? null
                : new PreviewGeometry(0, 0, image.PixelWidth, image.PixelHeight);
        }

        private static Int32Rect CalculateCropPixelRect(int imageWidth, int imageHeight, ImageCropBounds crop)
        {
            var left = ClampToRange((int)Math.Round(crop.X * imageWidth), 0, imageWidth - 1);
            var top = ClampToRange((int)Math.Round(crop.Y * imageHeight), 0, imageHeight - 1);
            var right = ClampToRange((int)Math.Round((crop.X + crop.Width) * imageWidth), left + 1, imageWidth);
            var bottom = ClampToRange((int)Math.Round((crop.Y + crop.Height) * imageHeight), top + 1, imageHeight);
            return new Int32Rect(left, top, right - left, bottom - top);
        }

        private static int ClampToRange(int value, int min, int max)
        {
            return Math.Min(max, Math.Max(min, value));
        }

        private static string ToPercent(double value)
        {
            return $"{value * 100:0}%";
        }

        private static PreviewGeometry ReadImageSize(string fullPath)
        {
            if (!File.Exists(fullPath))
            {
                return new PreviewGeometry(0, 0, 1, 1);
            }

            try
            {
                using var image = Drawing.Image.FromFile(fullPath);
                return new PreviewGeometry(0, 0, image.Width, image.Height);
            }
            catch
            {
                return new PreviewGeometry(0, 0, 1, 1);
            }
        }

        private static PreviewGeometry CalculateFitGeometry(double frameWidth, double frameHeight, double imageWidth, double imageHeight)
        {
            if (imageWidth <= 0 || imageHeight <= 0)
            {
                return new PreviewGeometry(0, 0, frameWidth, frameHeight);
            }

            var scale = Math.Min(frameWidth / imageWidth, frameHeight / imageHeight);
            var width = imageWidth * scale;
            var height = imageHeight * scale;
            return new PreviewGeometry((frameWidth - width) / 2, (frameHeight - height) / 2, width, height);
        }
    }

    public sealed record PreviewGeometry(double X, double Y, double Width, double Height);

    public sealed record EditableAnnotation
    {
        public required Guid Id { get; init; }

        public required string Summary { get; init; }

        public required string PositionSummary { get; init; }

        public static EditableAnnotation FromGuideAnnotation(GuideAnnotation annotation)
        {
            var text = string.IsNullOrWhiteSpace(annotation.Text) ? string.Empty : $" - {annotation.Text}";
            var kind = annotation.Kind == GuideAnnotationKind.Blur ? "Redact" : annotation.Kind.ToString();
            return new EditableAnnotation
            {
                Id = annotation.Id,
                Summary = $"{kind}{text}",
                PositionSummary = $"X {ToPercent(annotation.Bounds.X)}, Y {ToPercent(annotation.Bounds.Y)}, W {ToPercent(annotation.Bounds.Width)}, H {ToPercent(annotation.Bounds.Height)}"
            };
        }

        private static string ToPercent(double value)
        {
            return $"{value * 100:0}%";
        }
    }

    public sealed record EditableAnnotationPreview
    {
        public required Guid Id { get; init; }

        public required double ThumbX { get; init; }

        public required double ThumbY { get; init; }

        public required double ThumbWidth { get; init; }

        public required double ThumbHeight { get; init; }

        public required double EditorX { get; init; }

        public required double EditorY { get; init; }

        public required double EditorWidthValue { get; init; }

        public required double EditorHeightValue { get; init; }

        public required double WorkspaceX { get; init; }

        public required double WorkspaceY { get; init; }

        public required double WorkspaceWidthValue { get; init; }

        public required double WorkspaceHeightValue { get; init; }

        public required System.Windows.Media.Brush BorderBrush { get; init; }

        public required System.Windows.Media.Brush Background { get; init; }

        public required System.Windows.Media.Brush TextBrush { get; init; }

        public required double ThumbFontSize { get; init; }

        public required double WorkspaceFontSize { get; init; }

        public required FontWeight TextFontWeight { get; init; }

        public required System.Windows.FontStyle TextFontStyle { get; init; }

        public required Thickness BorderThickness { get; init; }

        public required Visibility BoxVisibility { get; init; }

        public required Visibility ArrowVisibility { get; init; }

        public string? Text { get; init; }

        public static EditableAnnotationPreview FromGuideAnnotation(
            GuideAnnotation annotation,
            PreviewGeometry thumbGeometry,
            PreviewGeometry editorGeometry,
            PreviewGeometry workspaceGeometry)
        {
            var style = GetEffectiveStyle(annotation);
            var isArrow = annotation.Kind == GuideAnnotationKind.Arrow;
            return new EditableAnnotationPreview
            {
                Id = annotation.Id,
                ThumbX = thumbGeometry.X + annotation.Bounds.X * thumbGeometry.Width,
                ThumbY = thumbGeometry.Y + annotation.Bounds.Y * thumbGeometry.Height,
                ThumbWidth = annotation.Bounds.Width * thumbGeometry.Width,
                ThumbHeight = annotation.Bounds.Height * thumbGeometry.Height,
                EditorX = editorGeometry.X + annotation.Bounds.X * editorGeometry.Width,
                EditorY = editorGeometry.Y + annotation.Bounds.Y * editorGeometry.Height,
                EditorWidthValue = annotation.Bounds.Width * editorGeometry.Width,
                EditorHeightValue = annotation.Bounds.Height * editorGeometry.Height,
                WorkspaceX = workspaceGeometry.X + annotation.Bounds.X * workspaceGeometry.Width,
                WorkspaceY = workspaceGeometry.Y + annotation.Bounds.Y * workspaceGeometry.Height,
                WorkspaceWidthValue = annotation.Bounds.Width * workspaceGeometry.Width,
                WorkspaceHeightValue = annotation.Bounds.Height * workspaceGeometry.Height,
                BorderBrush = annotation.Kind switch
                {
                    GuideAnnotationKind.Arrow => System.Windows.Media.Brushes.Red,
                    GuideAnnotationKind.Label => System.Windows.Media.Brushes.DodgerBlue,
                    GuideAnnotationKind.Blur => System.Windows.Media.Brushes.DimGray,
                    _ => System.Windows.Media.Brushes.Gold
                },
                Background = annotation.Kind switch
                {
                    GuideAnnotationKind.Label => CreateBrush(style.BackgroundColor, style.BackgroundOpacity),
                    GuideAnnotationKind.Blur => new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 32, 33, 36)),
                    GuideAnnotationKind.Arrow => System.Windows.Media.Brushes.Red,
                    _ => new SolidColorBrush(System.Windows.Media.Color.FromArgb(45, 251, 188, 4))
                },
                TextBrush = annotation.Kind == GuideAnnotationKind.Label
                    ? CreateBrush(style.TextColor)
                    : System.Windows.Media.Brushes.White,
                ThumbFontSize = annotation.Kind == GuideAnnotationKind.Label ? Math.Max(6, style.FontSize * 0.7) : 10,
                WorkspaceFontSize = annotation.Kind == GuideAnnotationKind.Label ? style.FontSize : 13,
                TextFontWeight = annotation.Kind == GuideAnnotationKind.Label && style.IsBold
                    ? FontWeights.Bold
                    : FontWeights.Normal,
                TextFontStyle = annotation.Kind == GuideAnnotationKind.Label && style.IsItalic
                    ? FontStyles.Italic
                    : FontStyles.Normal,
                BorderThickness = annotation.Kind switch
                {
                    GuideAnnotationKind.Arrow => new Thickness(0),
                    GuideAnnotationKind.Blur => new Thickness(1),
                    _ => new Thickness(2)
                },
                BoxVisibility = isArrow ? Visibility.Collapsed : Visibility.Visible,
                ArrowVisibility = isArrow ? Visibility.Visible : Visibility.Collapsed,
                Text = annotation.Kind switch
                {
                    GuideAnnotationKind.Label => annotation.Text,
                    GuideAnnotationKind.Blur => "REDACT",
                    _ => null
                }
            };
        }
    }
}
