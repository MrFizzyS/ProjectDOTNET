namespace Catalog.Bll;

public sealed class EntityNotFoundException(string message) : Exception(message);
public sealed class BusinessConflictException(string message) : Exception(message);
