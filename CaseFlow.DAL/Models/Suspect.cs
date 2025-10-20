using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CaseFlow.DAL.Enums;
using CaseFlow.DAL.Interfaces;

namespace CaseFlow.DAL.Models;

[Table("suspect")]
public class Suspect : IWorkflowEntity
{
    [Column("id")]
    public int Id { get; set; }

    [Column("first_name")]
    [MaxLength(100)]
    public string? FirstName { get; set; }

    [Column("last_name")]
    [MaxLength(100)]
    public string? LastName { get; set; }

    [Column("father_name")]
    [MaxLength(100)]
    public string? FatherName { get; set; }

    [Column("nickname")]
    [MaxLength(50)]
    public string? Nickname { get; set; }

    [Column("phone_number")]
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [Column("date_of_birth")]
    public DateOnly? DateOfBirth { get; set; }

    [Column("region")]
    [MaxLength(30)]
    public string? Region { get; set; }

    [Column("city")]
    [MaxLength(30)]
    public string? City { get; set; }

    [Column("street")]
    [MaxLength(50)]
    public string? Street { get; set; }

    [Column("building_number")]
    [MaxLength(30)]
    public string? BuildingNumber { get; set; }

    [Column("apartment_number")]
    public int? ApartmentNumber { get; set; }

    [Column("height")]
    public int? Height { get; set; }

    [Column("weight")]
    public int? Weight { get; set; }

    [Column("physical_description")]
    public string? PhysicalDescription { get; set; }

    [Column("prior_convictions")]
    public string? PriorConvictions { get; set; }

    [Column("approval_status")]
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Draft;

    public ICollection<CaseSuspect> CaseSuspects { get; set; } = new List<CaseSuspect>();
}