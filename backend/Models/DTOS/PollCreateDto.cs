using System.ComponentModel.DataAnnotations;

namespace PollSystem.Models.DTOs
{
    public sealed class PollCreateDto
    {
        [Required, StringLength(200, MinimumLength = 1)]
        public string Question { get; init; } = string.Empty;

        [Required, StringLength(50, MinimumLength = 1)]
        public string Category { get; init; } = string.Empty;

        [Required]
        public List<string> Tags { get; init; } = new();

        [Required, MinLength(2)]
        public List<string> Options { get; init; } = new();

        [Range(60, int.MaxValue)]
        public int DurationSeconds { get; init; }

        public bool AllowMultiple { get; init; }
    }
}
