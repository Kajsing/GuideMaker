using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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
    private readonly GuideProjectStore projectStore = new();
    private readonly GuideAssetFileStore assetFileStore = new();
    private readonly GuideExportWriter exportWriter = new();
    private readonly HtmlGuideExporter previewExporter = new();
    private readonly ObservableCollection<EditableStep> steps = [];
    private readonly ObservableCollection<EditableAsset> selectedStepAssets = [];
    private readonly ObservableCollection<EditableAnnotation> selectedAssetAnnotations = [];
    private readonly DispatcherTimer guidePreviewRefreshTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(450)
    };

    private GuideProject? currentProject;
    private GuideMetadata? currentMetadata;
    private List<GuideAsset> currentAssets = [];
    private Guid? selectedAssetId;
    private Guid? selectedAnnotationId;
    private bool isDarkMode;
    private bool isDirty;
    private bool isUpdatingUi;
    private bool isChangingAssetSelection;
    private bool isRefreshingAssets;
    private bool isRefreshingAnnotations;
    private bool closeAlreadyConfirmed;

    public MainWindow()
    {
        InitializeComponent();

        StepsListBox.ItemsSource = steps;
        StepAssetsListBox.ItemsSource = selectedStepAssets;
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
            var previewPath = await WritePreviewAsync().ConfigureAwait(true);
            GuidePreviewBrowser.Navigate(new Uri(previewPath));
            PreviewPathTextBlock.Text = previewPath;
            WorkspaceTabControl.SelectedItem = GuidePreviewTab;
            SetStatus("Preview updated.");
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
                ? "Image imported and attached to selected step."
                : $"{dialog.FileNames.Length} images imported and attached to selected step.");
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
            SetStatus("Clipboard image attached to selected step.");
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

                SetStatus("Screenshot captured and attached to selected step.");
            }
            finally
            {
                WindowState = WindowState.Normal;
                Activate();
            }
        }).ConfigureAwait(true);
    }

    private void RemoveImageButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedStep = GetSelectedStep();
        var selectedAsset = GetSelectedEditableAsset();
        if (selectedStep is null || selectedAsset is null)
        {
            return;
        }

        selectedStep.AssetIds.Remove(selectedAsset.Id);
        selectedAssetId = selectedStep.AssetIds.Count > 0 ? selectedStep.AssetIds[0] : null;
        selectedAnnotationId = null;
        RefreshSelectedStepAssets();
        RefreshSelectedAssetAnnotations();
        MarkDirty();
        SetStatus("Image detached from selected step.");
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

        var annotation = selectedAnnotationId is { } annotationId
            ? selectedStep.Annotations.FirstOrDefault(candidate => candidate.Id == annotationId)
            : selectedStep.Annotations.LastOrDefault(candidate => candidate.AssetId == selectedAsset.Id);
        if (annotation is null)
        {
            return;
        }

        selectedStep.Annotations.Remove(annotation);
        selectedAnnotationId = null;
        RefreshSelectedStepAssets(selectedAsset.Id);
        RefreshSelectedAssetAnnotations();
        MarkDirty();
        SetStatus("Annotation removed from selected image.");
    }

    private void InsertImageReferenceButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedAsset = GetSelectedEditableAsset();
        if (selectedAsset is null)
        {
            return;
        }

        var token = $"[[image:{selectedAsset.RelativePath}]]";
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
        selectedAnnotationId = null;
        LoadSelectedStepIntoEditor();
        RefreshSelectedStepAssets();
        RefreshSelectedAssetAnnotations();
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

        if (selectedAssetId == asset.Id)
        {
            e.Handled = true;
            return;
        }

        SelectAsset(asset.Id);
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
        }
        else
        {
            selectedAssetId = null;
        }

        selectedAnnotationId = null;

        RefreshSelectedAssetAnnotations();
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

    private async void Window_Closing(object? sender, CancelEventArgs e)
    {
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
        await File.WriteAllTextAsync(previewPath, previewExporter.Export(BuildDocumentFromUi(), "../"), cancellationToken)
            .ConfigureAwait(true);

        return previewPath;
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
        selectedAnnotationId = null;

        GuideTitleTextBox.Text = project.Document.Metadata.Title;
        steps.Clear();

        foreach (var step in project.Document.Steps.OrderBy(step => step.Order))
        {
            steps.Add(EditableStep.FromGuideStep(step));
        }

        StepsListBox.SelectedIndex = steps.Count > 0 ? 0 : -1;
        LoadSelectedStepIntoEditor();
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
            var previewPath = await WritePreviewAsync().ConfigureAwait(true);
            GuidePreviewBrowser.Navigate(new Uri(previewPath));
            PreviewPathTextBlock.Text = previewPath;
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

    private void SetButtonsEnabled(bool isEnabled)
    {
        NewGuideButton.IsEnabled = isEnabled;
        OpenGuideButton.IsEnabled = isEnabled;
        SaveGuideButton.IsEnabled = isEnabled;
        PreviewGuideButton.IsEnabled = isEnabled;
        ExportGuideButton.IsEnabled = isEnabled;
        AddStepButton.IsEnabled = isEnabled;
        DeleteStepButton.IsEnabled = isEnabled;
        ImportImageButton.IsEnabled = isEnabled;
        PasteImageButton.IsEnabled = isEnabled;
        CaptureScreenshotButton.IsEnabled = isEnabled;
        InsertImageReferenceButton.IsEnabled = isEnabled;
        RemoveImageButton.IsEnabled = isEnabled;
        AddHighlightButton.IsEnabled = isEnabled;
        AddLabelButton.IsEnabled = isEnabled;
        AddArrowButton.IsEnabled = isEnabled;
        AddRedactButton.IsEnabled = isEnabled;
        RemoveAnnotationButton.IsEnabled = isEnabled;
        AnnotationListBox.IsEnabled = isEnabled;
        AnnotationTextBox.IsEnabled = isEnabled;
        AnnotationXSlider.IsEnabled = isEnabled;
        AnnotationYSlider.IsEnabled = isEnabled;
        AnnotationWidthSlider.IsEnabled = isEnabled;
        AnnotationHeightSlider.IsEnabled = isEnabled;
    }

    private void UpdateUiState()
    {
        var hasProject = currentProject is not null && currentMetadata is not null;
        var hasSelectedStep = StepsListBox.SelectedItem is EditableStep;
        var hasSelectedAsset = selectedAssetId.HasValue && GetSelectedEditableAsset() is not null;
        var hasSelectedAnnotation = selectedAnnotationId.HasValue && GetSelectedAnnotation() is not null;

        SaveGuideButton.IsEnabled = hasProject;
        PreviewGuideButton.IsEnabled = hasProject;
        ExportGuideButton.IsEnabled = hasProject;
        AddStepButton.IsEnabled = hasProject;
        DeleteStepButton.IsEnabled = hasProject && hasSelectedStep;
        ImportImageButton.IsEnabled = hasProject && hasSelectedStep;
        PasteImageButton.IsEnabled = hasProject && hasSelectedStep;
        CaptureScreenshotButton.IsEnabled = hasProject && hasSelectedStep;
        InsertImageReferenceButton.IsEnabled = hasProject && hasSelectedStep && hasSelectedAsset;
        RemoveImageButton.IsEnabled = hasProject && hasSelectedStep && hasSelectedAsset;
        AddHighlightButton.IsEnabled = hasProject && hasSelectedStep && hasSelectedAsset;
        AddLabelButton.IsEnabled = hasProject && hasSelectedStep && hasSelectedAsset;
        AddArrowButton.IsEnabled = hasProject && hasSelectedStep && hasSelectedAsset;
        AddRedactButton.IsEnabled = hasProject && hasSelectedStep && hasSelectedAsset;
        RemoveAnnotationButton.IsEnabled = hasProject && hasSelectedStep && hasSelectedAsset && selectedAssetAnnotations.Count > 0;
        AnnotationListBox.IsEnabled = hasProject && hasSelectedStep && hasSelectedAsset;
        AnnotationTextBox.IsEnabled = hasProject && hasSelectedAnnotation && GetSelectedAnnotation()?.Kind == GuideAnnotationKind.Label;
        AnnotationXSlider.IsEnabled = hasProject && hasSelectedAnnotation;
        AnnotationYSlider.IsEnabled = hasProject && hasSelectedAnnotation;
        AnnotationWidthSlider.IsEnabled = hasProject && hasSelectedAnnotation;
        AnnotationHeightSlider.IsEnabled = hasProject && hasSelectedAnnotation;
        GuideTitleTextBox.IsEnabled = hasProject;
        StepTitleTextBox.IsEnabled = hasSelectedStep;
        StepBodyTextBox.IsEnabled = hasSelectedStep;
        DirtyIndicatorTextBlock.Text = isDirty ? "Unsaved changes" : "Saved";
    }

    private EditableStep? GetSelectedStep()
    {
        return StepsListBox.SelectedItem as EditableStep;
    }

    private EditableAsset? GetSelectedEditableAsset()
    {
        return selectedAssetId is { } assetId
            ? selectedStepAssets.FirstOrDefault(asset => asset.Id == assetId)
            : null;
    }

    private void SelectAsset(Guid? assetId)
    {
        isChangingAssetSelection = true;
        selectedAssetId = assetId;
        selectedAnnotationId = null;

        isRefreshingAssets = true;
        StepAssetsListBox.SelectedItem = selectedStepAssets.FirstOrDefault(asset => asset.Id == assetId);
        isRefreshingAssets = false;

        RefreshSelectedAssetAnnotations();
        UpdateUiState();

        Dispatcher.BeginInvoke(() => isChangingAssetSelection = false, DispatcherPriority.ContextIdle);
    }

    private void AttachAssetToStep(EditableStep step, GuideAsset asset)
    {
        currentAssets.Add(asset);
        step.AssetIds.Add(asset.Id);
        selectedAssetId = asset.Id;
        selectedAnnotationId = null;
        RefreshSelectedStepAssets();
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

        var annotation = new GuideAnnotation
        {
            Id = Guid.NewGuid(),
            Kind = kind,
            AssetId = selectedAsset.Id,
            Text = text,
            Bounds = CreateDefaultBounds(kind, selectedStep.Annotations.Count(annotation => annotation.AssetId == selectedAsset.Id))
        };

        selectedStep.Annotations.Add(annotation);
        selectedAnnotationId = annotation.Id;

        RefreshSelectedStepAssets(selectedAsset.Id);
        RefreshSelectedAssetAnnotations(annotation.Id);
        MarkDirty();
        SetStatus($"{kind} annotation added to selected image.");
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

    private void RefreshSelectedStepAssets(Guid? preferredAssetId = null)
    {
        var previousAssetId = selectedAssetId;
        var targetAssetId = preferredAssetId ?? selectedAssetId;
        isRefreshingAssets = true;
        selectedStepAssets.Clear();

        if (currentProject is null || GetSelectedStep() is not { } selectedStep)
        {
            selectedAssetId = null;
            selectedAnnotationId = null;
            StepAssetsListBox.SelectedItem = null;
            isRefreshingAssets = false;
            return;
        }

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

        var selectedAsset = selectedStepAssets.FirstOrDefault(asset => asset.Id == targetAssetId)
            ?? selectedStepAssets.FirstOrDefault();
        selectedAssetId = selectedAsset?.Id;
        if (selectedAssetId != previousAssetId)
        {
            selectedAnnotationId = null;
        }

        StepAssetsListBox.SelectedItem = selectedAsset;
        isRefreshingAssets = false;
    }

    private void RefreshSelectedAssetAnnotations(Guid? preferredAnnotationId = null, bool reloadEditor = true)
    {
        var targetAnnotationId = preferredAnnotationId ?? selectedAnnotationId;
        isRefreshingAnnotations = true;
        selectedAssetAnnotations.Clear();

        if (GetSelectedStep() is not { } selectedStep || selectedAssetId is not { } assetId)
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

        foreach (var annotation in selectedStep.Annotations.Where(annotation => annotation.AssetId == assetId))
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
        if (GetSelectedStep() is not { } selectedStep || selectedAnnotationId is not { } annotationId)
        {
            return null;
        }

        return selectedStep.Annotations.FirstOrDefault(annotation => annotation.Id == annotationId);
    }

    private void UpdateSelectedAnnotation(GuideAnnotation updatedAnnotation, bool reloadEditor = true, bool refreshAssets = true)
    {
        var selectedStep = GetSelectedStep();
        if (selectedStep is null)
        {
            return;
        }

        var index = selectedStep.Annotations.FindIndex(annotation => annotation.Id == updatedAnnotation.Id);
        if (index < 0)
        {
            return;
        }

        selectedStep.Annotations[index] = updatedAnnotation;
        selectedAnnotationId = updatedAnnotation.Id;
        if (refreshAssets && selectedAssetId == updatedAnnotation.AssetId)
        {
            RefreshSelectedStepAssets(updatedAnnotation.AssetId);
        }

        RefreshSelectedAssetAnnotations(updatedAnnotation.Id, reloadEditor);
        MarkDirty();
    }

    private void UpdateSelectedAnnotationQuietly(GuideAnnotation updatedAnnotation)
    {
        var selectedStep = GetSelectedStep();
        if (selectedStep is null)
        {
            return;
        }

        var index = selectedStep.Annotations.FindIndex(annotation => annotation.Id == updatedAnnotation.Id);
        if (index < 0)
        {
            return;
        }

        selectedStep.Annotations[index] = updatedAnnotation;
        selectedAnnotationId = updatedAnnotation.Id;
        if (selectedAssetId == updatedAnnotation.AssetId)
        {
            RefreshSelectedStepAssets(updatedAnnotation.AssetId);
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
        }

        isUpdatingUi = false;
    }

    private void ApplyAnnotationSliderRanges(AnnotationBounds bounds)
    {
        AnnotationXSlider.Maximum = Math.Max(0, (1 - bounds.Width) * 100);
        AnnotationYSlider.Maximum = Math.Max(0, (1 - bounds.Height) * 100);
        AnnotationWidthSlider.Maximum = Math.Max(1, (1 - bounds.X) * 100);
        AnnotationHeightSlider.Maximum = Math.Max(1, (1 - bounds.Y) * 100);
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
        using var bitmap = new Drawing.Bitmap(bounds.Width, bounds.Height);
        using var graphics = Drawing.Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size);

        var stream = new MemoryStream();
        bitmap.Save(stream, Drawing.Imaging.ImageFormat.Png);
        stream.Position = 0;
        return stream;
    }

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

        public List<GuideAnnotation> Annotations { get; init; } = [];

        public static EditableStep FromGuideStep(GuideStep step)
        {
            return new EditableStep
            {
                Id = step.Id,
                Order = step.Order,
                Title = step.Title,
                Body = step.Body,
                AssetIds = [.. step.AssetIds],
                Annotations = [.. step.Annotations]
            };
        }

        public GuideStep ToGuideStep()
        {
            return new GuideStep
            {
                Id = Id,
                Order = Order,
                Title = string.IsNullOrWhiteSpace(Title) ? "Untitled step" : Title.Trim(),
                Body = Body,
                AssetIds = [.. AssetIds],
                Annotations = [.. Annotations]
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

        public required string Caption { get; init; }

        public required string RelativePath { get; init; }

        public required string FullPath { get; init; }

        public required string AnnotationSummary { get; init; }

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

        public static EditableAsset FromGuideAsset(string projectDirectory, GuideAsset asset, IEnumerable<GuideAnnotation> annotations)
        {
            const double thumbFrameWidth = 96;
            const double thumbFrameHeight = 64;
            const double editorFrameWidth = 360;
            const double editorFrameHeight = 210;
            const double workspaceFrameWidth = 340;
            const double workspaceFrameHeight = 520;

            var annotationList = annotations.ToArray();
            var fullPath = Path.Combine(projectDirectory, asset.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            var imageSize = ReadImageSize(fullPath);
            var thumbGeometry = CalculateFitGeometry(thumbFrameWidth, thumbFrameHeight, imageSize.Width, imageSize.Height);
            var editorGeometry = CalculateFitGeometry(editorFrameWidth, editorFrameHeight, imageSize.Width, imageSize.Height);
            var workspaceGeometry = CalculateFitGeometry(workspaceFrameWidth, workspaceFrameHeight, imageSize.Width, imageSize.Height);

            return new EditableAsset
            {
                Id = asset.Id,
                Caption = string.IsNullOrWhiteSpace(asset.Caption) ? Path.GetFileName(asset.RelativePath) : asset.Caption,
                RelativePath = asset.RelativePath,
                FullPath = fullPath,
                AnnotationSummary = annotationList.Length == 0 ? "No annotations" : $"{annotationList.Length} annotation(s)",
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
                WorkspaceImageHeight = workspaceGeometry.Height
            };
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

        public required Thickness BorderThickness { get; init; }

        public string? Text { get; init; }

        public static EditableAnnotationPreview FromGuideAnnotation(
            GuideAnnotation annotation,
            PreviewGeometry thumbGeometry,
            PreviewGeometry editorGeometry,
            PreviewGeometry workspaceGeometry)
        {
            return new EditableAnnotationPreview
            {
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
                    GuideAnnotationKind.Label => new SolidColorBrush(System.Windows.Media.Color.FromArgb(230, 26, 115, 232)),
                    GuideAnnotationKind.Blur => new SolidColorBrush(System.Windows.Media.Color.FromArgb(245, 32, 33, 36)),
                    GuideAnnotationKind.Arrow => System.Windows.Media.Brushes.Red,
                    _ => new SolidColorBrush(System.Windows.Media.Color.FromArgb(45, 251, 188, 4))
                },
                BorderThickness = annotation.Kind switch
                {
                    GuideAnnotationKind.Arrow => new Thickness(0, 2, 0, 0),
                    GuideAnnotationKind.Blur => new Thickness(1),
                    _ => new Thickness(2)
                },
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
