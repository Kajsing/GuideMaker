namespace GuideMaker.Storage;

public sealed class GuideProjectValidationException : GuideProjectStorageException
{
    public GuideProjectValidationException(IEnumerable<string> errors)
        : base("Guide project data is invalid.")
    {
        Errors = errors.ToArray();
    }

    public IReadOnlyList<string> Errors { get; }
}
