namespace FuknWeather.Api.Utilities;

/// <summary>
/// Validation utilities for input data.
/// </summary>
public static class ValidationHelper
{
    /// <summary>
    /// Validates a US zip code format.
    /// </summary>
    /// <param name="zipCode">The zip code to validate.</param>
    /// <param name="errorMessage">The error message if validation fails.</param>
    /// <returns>True if valid, false otherwise.</returns>
    public static bool IsValidZipCode(string? zipCode, out string errorMessage)
    {
        if (string.IsNullOrEmpty(zipCode))
        {
            errorMessage = "Zip code cannot be null or empty.";
            return false;
        }

        if (zipCode.Length != 5)
        {
            errorMessage = "Invalid zip code format. Must be 5 digits.";
            return false;
        }

        if (!zipCode.All(char.IsDigit))
        {
            errorMessage = "Invalid zip code format. Must be 5 digits.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }
}
