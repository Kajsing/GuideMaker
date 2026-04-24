using GuideMaker.Core;

namespace GuideMaker.Export;

public sealed class PdfGuideExporter
{
    public byte[] Export(GuideDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        throw new NotImplementedException("PDF export is part of MVP scope, but the implementation package must be chosen during the export milestone.");
    }
}
