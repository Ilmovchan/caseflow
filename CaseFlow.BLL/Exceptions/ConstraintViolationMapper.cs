using Npgsql;

namespace CaseFlow.BLL.Exceptions;

public static class ConstraintViolationMapper
{
    private static readonly Dictionary<string, string> ConstraintMessages = new()
    {
        { "name_format", "Прізвище, ім'я та по батькові — лише українські літери (А-Я, І, Ї, Є)." },
        { "phone_number_format", "Телефон у форматі +380XXXXXXXXX (наприклад, +380991234567)." },
        { "email_format", "Електронна пошта має бути у коректному форматі (наприклад, user@example.com)." },
        { "date_of_birth_format", "Дата народження не може бути в майбутньому." },
        { "date_format", "Дата звіту не може бути в майбутньому." },
        { "detective_hire_date_format", "Дата прийому на роботу не може бути в майбутньому." },
        { "collection_date_format", "Дата збору не може бути в майбутньому." },
        { "date_time_format", "Дата й час не можуть бути в майбутньому." },
        { "region_format", "Регіон: лише українські літери." },
        { "city_format", "Місто: лише українські літери та дефіси." },
        { "street_format", "Вулиця: лише українські літери, пробіли та дефіси." },
        { "building_number_format", "Номер будинку: лише цифри та слеші (наприклад, 126/1)." },
        { "apartment_number_format", "Номер квартири має бути додатним числом." },
        { "detective_salary_format", "Зарплата має бути додатним числом." },
        { "amount_format", "Сума має бути додатним числом." },
        { "purpose_format", "Призначення містить недопустимі символи." },
        { "type_format", "Тип містить недопустимі символи." },
        { "weight_height_format", "Зріст і вага: заповніть обидва додатні числа або залиште обидва поля порожніми." },
        { "deadline_format", "Дедлайн має бути не раніше за дату початку." },
        { "close_date_format", "Дата закриття має бути не раніше за дату початку." }
    };

    public static string GetUserFriendlyMessage(string constraintName)
    {
        return ConstraintMessages.TryGetValue(constraintName, out var message)
            ? message
            : $"Некоректні дані: {constraintName}";
    }

    public static ConstraintViolationException? TryExtractConstraintViolation(Exception ex)
    {
        for (var cur = ex; cur != null; cur = cur.InnerException)
        {
            if (cur is PostgresException pgEx && pgEx.SqlState == "23514")
            {
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

            if (cur is PostgresException pgOverflow && pgOverflow.SqlState == "22003")
            {
                return new ConstraintViolationException(
                    "numeric_overflow",
                    "Числове значення завелике. Перевірте поля з сумою/ціною (макс: 99 999 999.99)."
                );
            }
        }

        return null;
    }
}

