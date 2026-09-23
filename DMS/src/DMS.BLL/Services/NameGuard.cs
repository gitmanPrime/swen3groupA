using DMS.BLL.Exceptions;

namespace DMS.BLL.Services;

// shared input validation used by the application services
internal static class NameGuard
{
    public static string RequireName(string? value, string propertyName, int maxLength = 128)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ValidationException(propertyName, $"{propertyName} is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new ValidationException(propertyName, $"{propertyName} must not exceed {maxLength} characters.");
        }

        return trimmed;
    }
}