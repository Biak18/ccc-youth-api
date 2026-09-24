namespace CityYouth.Domain.Common;

// API labels are lowercase member names (admin, leader),
// matching the Supabase values. Persisted as text via an EF value converter.
public enum UserRole
{
    Admin,

    Leader,
}

public static class UserRoleExtensions
{
    public static string ToApiString(this UserRole role) =>
        role switch
        {
            UserRole.Admin => "admin",
            UserRole.Leader => "leader",
            _ => throw new ArgumentOutOfRangeException(nameof(role)),
        };

    public static bool TryParseApiString(string? value, out UserRole role)
    {
        role = UserRole.Leader;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        foreach (var candidate in Enum.GetValues<UserRole>())
        {
            if (string.Equals(candidate.ToApiString(), value.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                role = candidate;
                return true;
            }
        }

        return false;
    }

    public static UserRole ParseApiString(string value) =>
        TryParseApiString(value, out var role)
            ? role
            : throw new InvalidOperationException($"Unknown user role '{value}'.");
}
