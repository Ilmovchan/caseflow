using Npgsql;

namespace CaseFlow.BLL.Exceptions;

public static class ConstraintViolationMapper
{
    private static readonly Dictionary<string, string> ConstraintMessages = new()
    {
        // Name constraints
        { "name_format", "Names must contain only Ukrainian letters (А-Я, І, Ї, Є, а-я, і, ї, є)" },
        
        // Phone constraints
        { "phone_number_format", "Phone number must be in format: +380XXXXXXXXX (e.g., +380991234567)" },
        
        // Email constraints
        { "email_format", "Email must be in valid format (e.g., user@example.com)" },
        
        // Date constraints
        { "date_of_birth_format", "Date of birth cannot be in the future" },
        { "date_format", "Report date cannot be in the future" },
        { "detective_hire_date_format", "Hire date cannot be in the future" },
        { "collection_date_format", "Collection date cannot be in the future" },
        { "date_time_format", "Date and time cannot be in the future" },
        
        // Region/City/Street constraints
        { "region_format", "Region must contain only Ukrainian letters" },
        { "city_format", "City must contain only Ukrainian letters and hyphens" },
        { "street_format", "Street must contain only Ukrainian letters, spaces, and hyphens" },
        
        // Building/Apartment constraints
        { "building_number_format", "Building number must contain only numbers and slashes (e.g., 123 or 123/1)" },
        { "apartment_number_format", "Apartment number must be a positive number" },
        
        // Salary/Amount constraints
        { "detective_salary_format", "Salary must be a positive number" },
        { "amount_format", "Amount must be a positive number" },
        
        // Purpose constraints
        { "purpose_format", "Purpose contains invalid characters" },
        
        // Type constraints
        { "type_format", "Type contains invalid characters" },
        
        // Weight/Height constraints
        { "weight_height_format", "Weight and height must both be positive numbers" },
        
        // Deadline constraints
        { "deadline_format", "Deadline date must be after or equal to start date" },
        { "close_date_format", "Close date must be after or equal to start date" }
    };

    public static string GetUserFriendlyMessage(string constraintName)
    {
        return ConstraintMessages.TryGetValue(constraintName, out var message)
            ? message
            : $"Invalid data: {constraintName}";
    }

    public static ConstraintViolationException? TryExtractConstraintViolation(Exception ex)
    {
        if (ex is PostgresException pgEx && pgEx.SqlState == "23514")
        {
            // Extract constraint name from the error message
            // Format: "new row for relation "table_name" violates check constraint "constraint_name""
            var constraintMatch = System.Text.RegularExpressions.Regex.Match(
                pgEx.Message,
                @"violates check constraint ""([^""]+)""");

            if (constraintMatch.Success)
            {
                var constraintName = constraintMatch.Groups[1].Value;
                var userMessage = GetUserFriendlyMessage(constraintName);
                return new ConstraintViolationException(constraintName, userMessage);
            }
        }

        // Numeric overflow (e.g., numeric(10,2) too large)
        if (ex is PostgresException pgExOverflow && pgExOverflow.SqlState == "22003")
        {
            return new ConstraintViolationException(
                "numeric_overflow",
                "Числове значення завелике. Перевірте поля з сумою/ціною (макс: 99 999 999.99)."
            );
        }

        return null;
    }
}

