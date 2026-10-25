using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace PollSystem.Models.Entities
{
    public class Vote
    {
        [JsonIgnore]
        public Guid Id { get; set; } = Guid.NewGuid();

        [JsonIgnore]
        public Guid PollId { get; set; }
        public Guid UserId { get; set; }
        public int BettedPoints { get; set; }
        public bool IsEarlyBird { get; set; }
        [JsonIgnore]
        public List<VoteSelection> Selections { get; set; } = new();

        [NotMapped]
        public List<int> OptionIndices
        {
            get => Selections.OrderBy(selection => selection.Position)
                .Select(selection => selection.OptionIndex)
                .ToList();
            set => Selections = (value ?? new List<int>())
                .Select((optionIndex, position) => new VoteSelection
                {
                    VoteId = Id,
                    OptionIndex = optionIndex,
                    Position = position
                })
                .ToList();
        }

        public Vote() { }

        public Vote(Guid userId, List<int> optionIndices, int pointsBet = 0)
        {
            UserId = userId;
            BettedPoints = pointsBet;
            OptionIndices = optionIndices;
        }
    }

}
