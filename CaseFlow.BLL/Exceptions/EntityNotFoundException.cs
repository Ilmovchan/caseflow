namespace CaseFlow.BLL.Exceptions;

public class EntityNotFoundException : Exception
{
    public EntityNotFoundException(string entityName, int id)
        : base($"Не знайдено {entityName}, id={id}.") { }

    public EntityNotFoundException(Type entityType, int id)
        : base($"Не знайдено {entityType.Name}, id={id}.") { }
}