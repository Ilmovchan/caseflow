using System.ComponentModel.DataAnnotations;

namespace CaseFlow.BLL.Dto.CaseType;

public class CreateCaseTypeDto
{
    [Required(ErrorMessage = "Назва є обов'язковою")]
    [MaxLength(100, ErrorMessage = "Назва має бути не довше 100 символів")]
    public string Name { get; set; } = null!;

    [Range(typeof(decimal), "0.01", "99999999.99", ErrorMessage = "Ціна має бути в межах 0.01 - 99 999 999.99")]
    public decimal Price { get; set; }
}