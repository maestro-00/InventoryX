namespace InventoryX.Application.Exceptions;

/// <summary>Raised when a requested entity does not exist for the caller's tenant. Surfaces as 404.</summary>
public class NotFoundException(string message) : Exception(message);
