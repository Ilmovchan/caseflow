using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CaseFlow.DAL.Enums;
using Microsoft.EntityFrameworkCore;

namespace CaseFlow.DAL.Models;

[Table("detective")]
public class Detective
{
    [Column("id")]
    public int Id { get; set; }

    [Column("first_name")]
    [MaxLength(100)]
    public string FirstName { get; set; } = null!;

    [Column("last_name")]
    [MaxLength(100)]
    public string LastName { get; set; } = null!;

    [Column("father_name")]
    [MaxLength(100)]
    public string? FatherName { get; set; }

    [Column("phone_number")]
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = null!;

    [Column("email")]
    [MaxLength(100)]
    public string Email { get; set; } = null!;

    [Column("date_of_birth")]
    public DateOnly DateOfBirth { get; set; }

    [Column("region")]
    [MaxLength(30)]
    public string Region { get; set; } = null!;

    [Column("city")]
    [MaxLength(30)]
    public string City { get; set; } = null!;

    [Column("street")]
    [MaxLength(50)]
    public string Street { get; set; } = null!;

    [Column("building_number")]
    [MaxLength(30)]
    public string BuildingNumber { get; set; } = null!;

    [Column("apartment_number")]
    public int? ApartmentNumber { get; set; }

    [Column("hire_date")]
    public DateTime HireDate { get; set; } = DateTime.UtcNow;

    [Column("salary")]
    [Precision(10, 2)]
    public decimal Salary { get; set; }

    [Column("personal_notes")]
    public string? PersonalNotes { get; set; }

    [Column("status")] 
    public DetectiveStatus Status { get; set; } = DetectiveStatus.Active;

    /// <summary>Ім’я ролі LOGIN у PostgreSQL для цього детектива.</summary>
    [Column("postgres_login")]
    [MaxLength(100)]
    public string? PostgresLogin { get; set; }

    public ICollection<Case> Cases { get; set; } = new List<Case>();
}