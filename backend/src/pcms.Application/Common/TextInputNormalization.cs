namespace pcms.Application.Common;

public static class TextInputNormalization
{
    public static string Required(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    public static string? Optional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
