namespace CityYouth.Domain.Common;

/// <summary>Base for domain rule violations (→ 400/422 via the exception handler).</summary>
public class DomainException(string message) : Exception(message);

/// <summary>Expected miss (→ 404).</summary>
public sealed class NotFoundException(string message) : DomainException(message);

/// <summary>Ownership/role denial inside a use case (→ 403).</summary>
public sealed class ForbiddenException(string message) : DomainException(message);
