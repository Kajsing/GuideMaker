namespace GuideMaker.Storage;

public class GuideProjectStorageException : Exception
{
    public GuideProjectStorageException(string message)
        : base(message)
    {
    }

    public GuideProjectStorageException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
