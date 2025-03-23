using System.ComponentModel.DataAnnotations;

namespace BlazorApp3.Data
{
    public class TodoItemViewModel
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(100, ErrorMessage = "Title cannot be longer than 100 characters")]
        public string Title { get; set; } = "";

        public string? Description { get; set; }
    }
}