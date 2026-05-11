namespace CaseFlow.BLL.Exceptions;

public class EntityDeleteConflictException : Exception
{
    public EntityDeleteConflictException(string entityName, int entityId, IEnumerable<int> connectedEntitiesIds)
        : base($"Неможливо видалити {entityName} (id={entityId}): пов’язані справи {string.Join(", ", connectedEntitiesIds)}.") { }

    public EntityDeleteConflictException(string entityName, int entityId)
        : base($"Неможливо видалити {entityName} (id={entityId}): є зв’язки з іншими справами.") { }
    
    public EntityDeleteConflictException(string message)
        : base(message) { }
}

public class EntityUpdateConflictException : Exception
{
    public EntityUpdateConflictException(string entityName, int entityId)
        : base($"Неможливо оновити {entityName} (id={entityId}): конфлікт зв’язків зі справами.") { }

    public EntityUpdateConflictException(string message)
        : base(message) { }
}