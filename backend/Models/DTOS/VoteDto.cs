using System.ComponentModel.DataAnnotations;

namespace PollSystem.Models.DTOs
{
    public sealed class VoteDto
    {
        [Required, MinLength(1)]
        public List<int> OptionIndices { get; init; } = new();

        [Range(0, int.MaxValue)]
        public int BettedPoints { get; init; }
    }
}
