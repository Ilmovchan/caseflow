namespace CaseFlow.BLL.Dto.Client;

public class ClientDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? FatherName { get; set; }
    public string Email { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public DateOnly DateOfBirth { get; set; }
    public string Region { get; set; } = null!;
    public string City { get; set; } = null!;
    public string Street { get; set; } = null!;
    public string BuildingNumber { get; set; } = null!;
    public int? ApartmentNumber { get; set; }
    public DateTime RegistrationDate { get; set; }
    
    // Computed property for full name
    public string FullName => $"{LastName} {FirstName}" + (FatherName != null ? $" {FatherName}" : "");
}