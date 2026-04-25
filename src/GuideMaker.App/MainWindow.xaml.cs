using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
    private readonly ObservableCollection<EditableStep> steps = [];
    private readonly ObservableCollection<EditableAsset> selectedStepAssets = [];

    private GuideProject? currentProject;
    private GuideMetadata? currentMetadata;
    private List<GuideAsset> currentAssets = [];
    private bool isDarkMode;
    private bool isDirty;
    private bool isUpdatingUi;
    private bool closeAlreadyConfirmed;

    public MainWindow()
    {
        InitializeComponent();

        StepsListBox.ItemsSource = steps;
        StepAssetsListBox.ItemsSource = selectedStepAssets;
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

        await RunUiActionAsync(async () =>
        {
            var result = await exportWriter.ExportAsync(currentProject.ProjectDirectory, BuildDocumentFromUi())
                .ConfigureAwait(true);
            SetStatus($"Exported Markdown and HTML to {Path.GetDirectoryName(result.MarkdownPath)}.");
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
        if (selectedStep is null || StepAssetsListBox.SelectedItem is not EditableAsset selectedAsset)
        {
            return;
        }

        selectedStep.AssetIds.Remove(selectedAsset.Id);
        RefreshSelectedStepAssets();
        MarkDirty();
        SetStatus("Image detached from selected step.");
    }

    private void InsertImageReferenceButton_Click(object sender, RoutedEventArgs e)
    {
        if (StepAssetsListBox.SelectedItem is not EditableAsset selectedAsset)
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
        LoadSelectedStepIntoEditor();
        RefreshSelectedStepAssets();
        UpdateUiState();
    }

    private void StepAssetsListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        UpdateUiState();
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

        GuideTitleTextBox.Text = project.Document.Metadata.Title;
        steps.Clear();

        foreach (var step in project.Document.Steps.OrderBy(step => step.Order))
        {
            steps.Add(EditableStep.FromGuideStep(step));
        }

        StepsListBox.SelectedIndex = steps.Count > 0 ? 0 : -1;
        LoadSelectedStepIntoEditor();
        RefreshSelectedStepAssets();

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
        UpdateUiState();
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
        ExportGuideButton.IsEnabled = isEnabled;
        AddStepButton.IsEnabled = isEnabled;
        DeleteStepButton.IsEnabled = isEnabled;
        ImportImageButton.IsEnabled = isEnabled;
        PasteImageButton.IsEnabled = isEnabled;
        CaptureScreenshotButton.IsEnabled = isEnabled;
        InsertImageReferenceButton.IsEnabled = isEnabled;
        RemoveImageButton.IsEnabled = isEnabled;
    }

    private void UpdateUiState()
    {
        var hasProject = currentProject is not null && currentMetadata is not null;
        var hasSelectedStep = StepsListBox.SelectedItem is EditableStep;
        var hasSelectedAsset = StepAssetsListBox.SelectedItem is EditableAsset;

        SaveGuideButton.IsEnabled = hasProject;
        ExportGuideButton.IsEnabled = hasProject;
        AddStepButton.IsEnabled = hasProject;
        DeleteStepButton.IsEnabled = hasProject && hasSelectedStep;
        ImportImageButton.IsEnabled = hasProject && hasSelectedStep;
        PasteImageButton.IsEnabled = hasProject && hasSelectedStep;
        CaptureScreenshotButton.IsEnabled = hasProject && hasSelectedStep;
        InsertImageReferenceButton.IsEnabled = hasProject && hasSelectedStep && hasSelectedAsset;
        RemoveImageButton.IsEnabled = hasProject && hasSelectedStep && hasSelectedAsset;
        GuideTitleTextBox.IsEnabled = hasProject;
        StepTitleTextBox.IsEnabled = hasSelectedStep;
        StepBodyTextBox.IsEnabled = hasSelectedStep;
        DirtyIndicatorTextBlock.Text = isDirty ? "Unsaved changes" : "Saved";
    }

    private EditableStep? GetSelectedStep()
    {
        return StepsListBox.SelectedItem as EditableStep;
    }

    private void AttachAssetToStep(EditableStep step, GuideAsset asset)
    {
        currentAssets.Add(asset);
        step.AssetIds.Add(asset.Id);
        RefreshSelectedStepAssets();
        MarkDirty();
    }

    private void RefreshSelectedStepAssets()
    {
        selectedStepAssets.Clear();

        if (currentProject is null || GetSelectedStep() is not { } selectedStep)
        {
            return;
        }

        foreach (var assetId in selectedStep.AssetIds)
        {
            var asset = currentAssets.FirstOrDefault(candidate => candidate.Id == assetId);
            if (asset is null)
            {
                continue;
            }

            selectedStepAssets.Add(EditableAsset.FromGuideAsset(currentProject.ProjectDirectory, asset));
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

        public static EditableAsset FromGuideAsset(string projectDirectory, GuideAsset asset)
        {
            return new EditableAsset
            {
                Id = asset.Id,
                Caption = string.IsNullOrWhiteSpace(asset.Caption) ? Path.GetFileName(asset.RelativePath) : asset.Caption,
                RelativePath = asset.RelativePath,
                FullPath = Path.Combine(projectDirectory, asset.RelativePath.Replace('/', Path.DirectorySeparatorChar))
            };
        }
    }
}
