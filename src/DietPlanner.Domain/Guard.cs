namespace DietPlanner.Domain;

internal static class Guard
{
    public static Guid AgainstEmpty(Guid value, string paramName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Value cannot be empty.", paramName);
        }

        return value;
    }

    public static string AgainstBlank(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be blank.", paramName);
        }

        return value;
    }

    public static int AgainstNegative(int value, string paramName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, "Value cannot be negative.");
        }

        return value;
    }

    public static decimal AgainstNonPositive(decimal value, string paramName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, "Value must be greater than zero.");
        }

        return value;
    }
}
