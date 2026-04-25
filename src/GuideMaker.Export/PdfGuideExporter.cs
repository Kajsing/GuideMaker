using GuideMaker.Core;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace GuideMaker.Export;

public sealed class PdfGuideExporter
{
    private const double PageMargin = 48;
    private const double ParagraphGap = 8;

    public byte[] Export(GuideDocument document, string projectDirectory)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectDirectory);
        EnsureFontResolver();

        using var pdf = new PdfDocument();
        pdf.Info.Title = document.Metadata.Title;
        pdf.Info.Author = document.Metadata.Author;

        var layout = new PdfLayout(pdf);
        var titleFont = new XFont(GuideMakerFontResolver.FontFamilyName, 22, XFontStyleEx.Bold);
        var headingFont = new XFont(GuideMakerFontResolver.FontFamilyName, 15, XFontStyleEx.Bold);
        var bodyFont = new XFont(GuideMakerFontResolver.FontFamilyName, 11, XFontStyleEx.Regular);
        var captionFont = new XFont(GuideMakerFontResolver.FontFamilyName, 9, XFontStyleEx.Italic);

        layout.DrawWrappedText(document.Metadata.Title, titleFont, XBrushes.Black, 28);

        if (!string.IsNullOrWhiteSpace(document.Metadata.Description))
        {
            layout.DrawWrappedText(document.Metadata.Description, bodyFont, XBrushes.DimGray, 18);
        }

        foreach (var step in document.Steps.OrderBy(step => step.Order))
        {
            layout.EnsureSpace(80);
            layout.DrawWrappedText($"{step.Order}. {step.Title}", headingFont, XBrushes.Black, 16);

            var renderedAssetIds = new HashSet<Guid>();
            if (!string.IsNullOrWhiteSpace(step.Body))
            {
                RenderBody(layout, document, step, bodyFont, projectDirectory, renderedAssetIds);
            }

            foreach (var assetId in step.AssetIds)
            {
                if (renderedAssetIds.Contains(assetId))
                {
                    continue;
                }

                var asset = document.Assets.FirstOrDefault(candidate => candidate.Id == assetId);
                if (asset is not null)
                {
                    RenderAsset(layout, asset, step.Annotations, projectDirectory, bodyFont, captionFont);
                }
            }
        }

        using var stream = new MemoryStream();
        pdf.Save(stream, closeStream: false);
        return stream.ToArray();
    }

    private static void RenderBody(
        PdfLayout layout,
        GuideDocument document,
        GuideStep step,
        XFont bodyFont,
        string projectDirectory,
        ISet<Guid> renderedAssetIds)
    {
        var lines = step.Body.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("[[image:", StringComparison.OrdinalIgnoreCase) &&
                trimmed.EndsWith("]]", StringComparison.Ordinal))
            {
                var relativePath = trimmed["[[image:".Length..^2].Trim().Replace('\\', '/');
                var asset = document.Assets.FirstOrDefault(candidate =>
                    string.Equals(candidate.RelativePath.Replace('\\', '/'), relativePath, StringComparison.OrdinalIgnoreCase));

                if (asset is not null)
                {
                    RenderAsset(layout, asset, step.Annotations, projectDirectory, bodyFont, bodyFont);
                    renderedAssetIds.Add(asset.Id);
                    continue;
                }
            }

            layout.DrawWrappedText(line, bodyFont, XBrushes.Black, ParagraphGap);
        }
    }

    private static void RenderAsset(
        PdfLayout layout,
        GuideAsset asset,
        IReadOnlyCollection<GuideAnnotation> annotations,
        string projectDirectory,
        XFont bodyFont,
        XFont captionFont)
    {
        var assetPath = Path.Combine(projectDirectory, asset.RelativePath.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(assetPath))
        {
            layout.DrawWrappedText($"[Missing image: {asset.RelativePath}]", bodyFont, XBrushes.Firebrick, 12);
            return;
        }

        try
        {
            using var image = XImage.FromFile(assetPath);
            var maxWidth = layout.ContentWidth;
            var maxHeight = 330d;
            var scale = Math.Min(maxWidth / image.PixelWidth, maxHeight / image.PixelHeight);
            scale = Math.Min(scale, 1d);

            var width = image.PixelWidth * scale;
            var height = image.PixelHeight * scale;
            layout.EnsureSpace(height + 48);

            var x = PageMargin;
            var y = layout.CurrentY;
            layout.Graphics.DrawImage(image, x, y, width, height);

            foreach (var annotation in annotations.Where(annotation => annotation.AssetId == asset.Id))
            {
                RenderAnnotation(layout.Graphics, annotation, x, y, width, height);
            }

            layout.CurrentY += height + ParagraphGap;
        }
        catch (InvalidOperationException)
        {
            layout.DrawWrappedText($"[Unreadable image: {asset.RelativePath}]", bodyFont, XBrushes.Firebrick, 12);
        }

        if (!string.IsNullOrWhiteSpace(asset.Caption))
        {
            layout.DrawWrappedText(asset.Caption, captionFont, XBrushes.DimGray, 14);
        }
    }

    private static void RenderAnnotation(XGraphics graphics, GuideAnnotation annotation, double imageX, double imageY, double imageWidth, double imageHeight)
    {
        var bounds = annotation.Bounds;
        var x = imageX + bounds.X * imageWidth;
        var y = imageY + bounds.Y * imageHeight;
        var width = bounds.Width * imageWidth;
        var height = bounds.Height * imageHeight;

        switch (annotation.Kind)
        {
            case GuideAnnotationKind.Rectangle:
                graphics.DrawRectangle(
                    new XSolidBrush(XColor.FromArgb(45, 251, 188, 4)),
                    x,
                    y,
                    width,
                    height);
                graphics.DrawRectangle(new XPen(XColor.FromArgb(251, 188, 4), 3), x, y, width, height);
                break;

            case GuideAnnotationKind.Blur:
                graphics.DrawRectangle(new XSolidBrush(XColor.FromArgb(210, 32, 33, 36)), x, y, width, height);
                break;

            case GuideAnnotationKind.Label:
                graphics.DrawRectangle(new XSolidBrush(XColor.FromArgb(26, 115, 232)), x, y, width, height);
                if (!string.IsNullOrWhiteSpace(annotation.Text))
                {
                    var font = new XFont(GuideMakerFontResolver.FontFamilyName, 9, XFontStyleEx.Bold);
                    graphics.DrawString(annotation.Text, font, XBrushes.White, new XRect(x + 4, y + 2, Math.Max(1, width - 8), Math.Max(1, height - 4)), XStringFormats.TopLeft);
                }

                break;

            case GuideAnnotationKind.Arrow:
                var pen = new XPen(XColor.FromArgb(234, 67, 53), 4);
                var start = new XPoint(x, y + height / 2);
                var end = new XPoint(x + width, y + height / 2);
                graphics.DrawLine(pen, start, end);
                graphics.DrawLine(pen, end, new XPoint(end.X - 10, end.Y - 7));
                graphics.DrawLine(pen, end, new XPoint(end.X - 10, end.Y + 7));
                break;
        }
    }

    private sealed class PdfLayout
    {
        private readonly PdfDocument document;

        public PdfLayout(PdfDocument document)
        {
            this.document = document;
            AddPage();
        }

        public XGraphics Graphics { get; private set; } = null!;

        public double CurrentY { get; set; }

        public double ContentWidth => Graphics.PageSize.Width - PageMargin * 2;

        public void EnsureSpace(double requiredHeight)
        {
            if (CurrentY + requiredHeight <= Graphics.PageSize.Height - PageMargin)
            {
                return;
            }

            AddPage();
        }

        public void DrawWrappedText(string value, XFont font, XBrush brush, double bottomGap)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                CurrentY += bottomGap;
                return;
            }

            var lineHeight = font.GetHeight() + 3;
            foreach (var paragraph in value.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
            {
                foreach (var line in WrapLine(paragraph, font))
                {
                    EnsureSpace(lineHeight + bottomGap);
                    Graphics.DrawString(line, font, brush, new XRect(PageMargin, CurrentY, ContentWidth, lineHeight), XStringFormats.TopLeft);
                    CurrentY += lineHeight;
                }
            }

            CurrentY += bottomGap;
        }

        private IEnumerable<string> WrapLine(string value, XFont font)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                yield break;
            }

            var words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var line = string.Empty;

            foreach (var word in words)
            {
                var candidate = string.IsNullOrEmpty(line) ? word : $"{line} {word}";
                if (Graphics.MeasureString(candidate, font).Width <= ContentWidth)
                {
                    line = candidate;
                    continue;
                }

                if (!string.IsNullOrEmpty(line))
                {
                    yield return line;
                }

                line = word;
            }

            if (!string.IsNullOrEmpty(line))
            {
                yield return line;
            }
        }

        private void AddPage()
        {
            var page = document.AddPage();
            page.Size = PdfSharp.PageSize.A4;
            Graphics = XGraphics.FromPdfPage(page);
            CurrentY = PageMargin;
        }
    }

    private static void EnsureFontResolver()
    {
        if (GlobalFontSettings.FontResolver is null)
        {
            GlobalFontSettings.FontResolver = new GuideMakerFontResolver();
        }
    }

    private sealed class GuideMakerFontResolver : IFontResolver
    {
        public const string FontFamilyName = "GuideMakerSans";

        private const string RegularFaceName = "GuideMakerSans#Regular";
        private const string BoldFaceName = "GuideMakerSans#Bold";
        private const string ItalicFaceName = "GuideMakerSans#Italic";

        public byte[] GetFont(string faceName)
        {
            var fileName = faceName switch
            {
                BoldFaceName => "arialbd.ttf",
                ItalicFaceName => "ariali.ttf",
                _ => "arial.ttf"
            };

            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), fileName);
            return File.ReadAllBytes(path);
        }

        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            if (!string.Equals(familyName, FontFamilyName, StringComparison.OrdinalIgnoreCase))
            {
                return new FontResolverInfo(RegularFaceName);
            }

            if (isBold)
            {
                return new FontResolverInfo(BoldFaceName);
            }

            if (isItalic)
            {
                return new FontResolverInfo(ItalicFaceName);
            }

            return new FontResolverInfo(RegularFaceName);
        }
    }
}
