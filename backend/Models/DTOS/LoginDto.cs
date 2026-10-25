using System.ComponentModel.DataAnnotations;

namespace PollSystem.Models.DTOs
{
    public sealed class LoginDto
    {
        [Required, StringLength(40, MinimumLength = 1)]
        public string Username { get; init; } = string.Empty;

        [Required]
        public string Password { get; init; } = string.Empty;
    }
}
