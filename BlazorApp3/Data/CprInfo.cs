using System.ComponentModel.DataAnnotations;

namespace BlazorApp3.Data;

public class CprInfo
{
    [Key]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "CPR number must be exactly 10 digits")]
    public string CprNumber { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}