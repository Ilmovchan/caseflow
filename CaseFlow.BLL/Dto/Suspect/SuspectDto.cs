using CaseFlow.DAL.Enums;

namespace CaseFlow.BLL.Dto.Suspect;

public class SuspectDto
{
    public int Id { get; init; }
    public int? CaseId { get; set; }
    
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FatherName { get; set; }
    public string? Nickname { get; set; }
    public string? PhoneNumber { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Region { get; set; }
    public string? City { get; set; }
    public string? Street { get; set; }
    public string? BuildingNumber { get; set; }
    public int? ApartmentNumber { get; set; }
    public int? Height { get; set; }
    public int? Weight { get; set; }
    public string? PhysicalDescription { get; set; }
    public string? PriorConvictions { get; set; }
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Draft;
    
    public string? FullName => 
        !string.IsNullOrEmpty(LastName) && !string.IsNullOrEmpty(FirstName) 
            ? $"{LastName} {FirstName}" + (FatherName != null ? $" {FatherName}" : "")
            : null;
}