namespace RpgEngine.Core.Content;

/// <summary>Thrown when content fails to load; carries every problem found, not just the first.</summary>
public sealed class ContentValidationException : Exception
{
    public ContentValidationException(IReadOnlyList<string> errors)
        : base(BuildMessage(errors))
    {
        Errors = errors;
    }

    public IReadOnlyList<string> Errors { get; }

    private static string BuildMessage(IReadOnlyList<string> errors) =>
        $"Content validation failed with {errors.Count} error(s):{Environment.NewLine}"
        + string.Join(Environment.NewLine, errors.Select(error => $" - {error}"));
}
