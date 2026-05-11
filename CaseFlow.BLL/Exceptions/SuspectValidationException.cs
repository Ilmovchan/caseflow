namespace CaseFlow.BLL.Exceptions;

/// <summary>Помилки перевірки полів підозрюваного до звернення до БД.</summary>
public sealed class SuspectValidationException : Exception
{
    public IReadOnlyList<(string PropertyName, string Message)> Errors { get; }

    public SuspectValidationException(IReadOnlyList<(string PropertyName, string Message)> errors)
        : base("Перевірка даних підозрюваного не пройдена.")
    {
        Errors = errors;
    }
}
