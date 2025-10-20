namespace CaseFlow.BLL.Exceptions;

public class ConstraintViolationException : Exception
{
    public string ConstraintName { get; }
    public string UserFriendlyMessage { get; }

    public ConstraintViolationException(string constraintName, string userFriendlyMessage)
        : base($"Constraint violation: {constraintName}")
    {
        ConstraintName = constraintName;
        UserFriendlyMessage = userFriendlyMessage;
    }
}

