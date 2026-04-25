namespace GuideMaker.Storage;

public sealed class GuideProjectFormatException : GuideProjectStorageException
{
    public GuideProjectFormatException(string guideFilePath, Exception innerException)
        : base($"Guide project file is not valid JSON: {guideFilePath}", innerException)
    {
        GuideFilePath = guideFilePath;
    }

    public string GuideFilePath { get; }
}
