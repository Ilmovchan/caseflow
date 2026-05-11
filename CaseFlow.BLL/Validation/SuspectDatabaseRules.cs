using System.Text.RegularExpressions;
using CaseFlow.BLL.Dto.Suspect;

namespace CaseFlow.BLL.Validation;

/// <summary>Перевірки полів підозрюваного, узгоджені з обмеженнями CHECK у PostgreSQL.</summary>
public static class SuspectDatabaseRules
{
    private static readonly Regex UkrainianName = new("^[А-ЯІЇЄа-яіїє]+$", RegexOptions.Compiled);
    private static readonly Regex PhoneUa = new(@"^\+380\d{9}$", RegexOptions.Compiled);
    private static readonly Regex RegionUa = new("^[А-ЯІЇЄа-яіїє]+$", RegexOptions.Compiled);
    private static readonly Regex CityUa = new("^[А-ЯІЇЄа-яіїє\\-]+$", RegexOptions.Compiled);
    private static readonly Regex StreetUa = new("^[А-ЯІЇЄа-яіїє\\s\\-]+$", RegexOptions.Compiled);
    private static readonly Regex BuildingNum = new("^[0-9/]+$", RegexOptions.Compiled);

    public static IReadOnlyList<(string PropertyName, string Message)> GetCreateViolations(CreateSuspectDto dto)
    {
        var errors = new List<(string, string)>();

        void NameField(string? value, string prop, string labelUa)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;
            var t = value.Trim();
            if (!UkrainianName.IsMatch(t))
                errors.Add((prop, $"{labelUa}: лише українські літери (А-Я, І, Ї, Є)."));
        }

        NameField(dto.FirstName, nameof(CreateSuspectDto.FirstName), "Ім'я");
        NameField(dto.LastName, nameof(CreateSuspectDto.LastName), "Прізвище");
        NameField(dto.FatherName, nameof(CreateSuspectDto.FatherName), "По батькові");

        if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
        {
            var p = dto.PhoneNumber.Trim();
            if (!PhoneUa.IsMatch(p))
                errors.Add((nameof(CreateSuspectDto.PhoneNumber), "Телефон у форматі +380XXXXXXXXX (наприклад, +380991234567)."));
        }

        if (dto.DateOfBirth.HasValue && dto.DateOfBirth.Value > DateOnly.FromDateTime(DateTime.Today))
            errors.Add((nameof(CreateSuspectDto.DateOfBirth), "Дата народження не може бути в майбутньому."));

        if (!string.IsNullOrWhiteSpace(dto.Region) && !RegionUa.IsMatch(dto.Region.Trim()))
            errors.Add((nameof(CreateSuspectDto.Region), "Регіон: лише українські літери."));

        if (!string.IsNullOrWhiteSpace(dto.City) && !CityUa.IsMatch(dto.City.Trim()))
            errors.Add((nameof(CreateSuspectDto.City), "Місто: українські літери та дефіси."));

        if (!string.IsNullOrWhiteSpace(dto.Street) && !StreetUa.IsMatch(dto.Street.Trim()))
            errors.Add((nameof(CreateSuspectDto.Street), "Вулиця: українські літери, пробіли та дефіси."));

        if (!string.IsNullOrWhiteSpace(dto.BuildingNumber) && !BuildingNum.IsMatch(dto.BuildingNumber.Trim()))
            errors.Add((nameof(CreateSuspectDto.BuildingNumber), "Номер будинку: лише цифри та слеші (наприклад, 126/1)."));

        if (dto.ApartmentNumber.HasValue && dto.ApartmentNumber.Value <= 0)
            errors.Add((nameof(CreateSuspectDto.ApartmentNumber), "Номер квартири має бути додатним числом."));

        var h = dto.Height;
        var w = dto.Weight;
        var oneSet = h.HasValue || w.HasValue;
        var bothSet = h.HasValue && w.HasValue;
        if (oneSet && !bothSet)
            errors.Add((nameof(CreateSuspectDto.Height), "Заповніть і зріст, і вагу (додатні числа), або залиште обидва поля порожніми."));
        else if (bothSet && (h!.Value <= 0 || w!.Value <= 0))
            errors.Add((nameof(CreateSuspectDto.Height), "Зріст і вага мають бути додатними числами."));

        return errors;
    }

    public static void TrimNullableStrings(CreateSuspectDto dto)
    {
        dto.FirstName = TrimOrNull(dto.FirstName);
        dto.LastName = TrimOrNull(dto.LastName);
        dto.FatherName = TrimOrNull(dto.FatherName);
        dto.Nickname = TrimOrNull(dto.Nickname);
        dto.PhoneNumber = TrimOrNull(dto.PhoneNumber);
        dto.Region = TrimOrNull(dto.Region);
        dto.City = TrimOrNull(dto.City);
        dto.Street = TrimOrNull(dto.Street);
        dto.BuildingNumber = TrimOrNull(dto.BuildingNumber);
        dto.PhysicalDescription = TrimOrNull(dto.PhysicalDescription);
        dto.PriorConvictions = TrimOrNull(dto.PriorConvictions);
    }

    private static string? TrimOrNull(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
