using CaseFlow.DAL.Enums;

namespace CaseFlow.BLL.Dto.Detective;

public class DetectiveDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? FatherName { get; set; }
    public string PhoneNumber { get; set; } = null!;
    public string Email { get; set; } = null!;
    public DateOnly DateOfBirth { get; set; }
    public string Region { get; set; } = null!;
    public string City { get; set; } = null!;
    public string Street { get; set; } = null!;
    public string BuildingNumber { get; set; } = null!;
    public int? ApartmentNumber { get; set; }
    public DateTime HireDate { get; set; }
    public decimal Salary { get; set; }
    public string? PersonalNotes { get; set; }
    public DetectiveStatus Status { get; set; }
    
    public string FullName => $"{LastName} {FirstName}" + (FatherName != null ? $" {FatherName}" : "");
}