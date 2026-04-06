using System.ComponentModel.DataAnnotations;

namespace CaseFlow.BLL.Dto.CaseType;

public class UpdateCaseTypeDto
{
    [MaxLength(100, ErrorMessage = "Назва має бути не довше 100 символів")]
    public string? Name { get; set; }

    [Range(typeof(decimal), "0.01", "99999999.99", ErrorMessage = "Ціна має бути в межах 0.01 - 99 999 999.99")]
    public decimal? Price { get; set; }
}