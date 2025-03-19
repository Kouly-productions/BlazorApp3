using System.ComponentModel.DataAnnotations;

namespace BlazorApp3.Data
{
    public class CprRecord
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        [StringLength(10)]
        public string CprNumber { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }

    public class TodoItem
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        public string Description { get; set; }

        public bool IsCompleted { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}