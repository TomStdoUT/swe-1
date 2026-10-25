using System.ComponentModel.DataAnnotations;

namespace PollSystem.Models.DTOs
{
    public sealed class CommentCreateDto
    {
        [Required, StringLength(500, MinimumLength = 1)]
        public string Content { get; init; } = string.Empty;
    }
}
