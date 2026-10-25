using System.ComponentModel.DataAnnotations;

namespace PollSystem.Models.DTOs
{
    public sealed class RegisterDto
    {
        [Required, StringLength(40, MinimumLength = 1)]
        public string Username { get; init; } = string.Empty;

        [Required, MinLength(6)]
        public string Password { get; init; } = string.Empty;
    }
}
