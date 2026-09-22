namespace CityYouth.Api.Controllers;

/// <summary>Small HTTP-only bodies (commands stay free of HTTP concerns, §13).</summary>
public sealed record StatusRequest(string Status);
public sealed record PinRequest(bool IsPinned);
public sealed record RoleRequest(string Role);
