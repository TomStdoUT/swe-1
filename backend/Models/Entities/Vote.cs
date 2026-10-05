namespace PollSystem.Models.Entities
{
    public struct Vote
    {
        public Guid UserId { get; set; }
        public List<int> OptionIndices { get; set; }
        public int BettedPoints { get; set; }

        public Vote() 
        { 
            OptionIndices = new List<int>(); 
        }

        public Vote(Guid userId, List<int> optionIndices, int pointsBet = 0)
        {
            UserId = userId;
            OptionIndices = optionIndices ?? new List<int>();
            BettedPoints = pointsBet;
        }
    }
}