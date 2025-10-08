using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CaseFlow.DAL.Models;

[Table("Users")]
public class User
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Column("Username")]
    [MaxLength(100)]
    [Required]
    public string Username { get; set; } = null!;

    [Column("Email")]
    [MaxLength(100)]
    [EmailAddress]
    [Required]
    public string Email { get; set; } = null!;

    [Column("PasswordHash")]
    [MaxLength(255)]
    [Required]
    public string PasswordHash { get; set; } = null!;

    [Column("Role")]
    [MaxLength(50)]
    [Required]
    public string Role { get; set; } = null!;

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("LastLoginAt")]
    public DateTime? LastLoginAt { get; set; }

    [Column("IsActive")]
    public bool IsActive { get; set; } = true;
}