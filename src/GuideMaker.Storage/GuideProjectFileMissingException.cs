namespace GuideMaker.Storage;

public sealed class GuideProjectFileMissingException : GuideProjectStorageException
{
    public GuideProjectFileMissingException(string guideFilePath)
        : base($"Guide project file was not found: {guideFilePath}")
    {
        GuideFilePath = guideFilePath;
    }

    public string GuideFilePath { get; }
}
