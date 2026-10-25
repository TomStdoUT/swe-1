namespace PollSystem.Data
{
    internal sealed class LegacyVoteData
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public List<int>? OptionIndices { get; set; }
        public int BettedPoints { get; set; }
        public bool IsEarlyBird { get; set; }
    }
}
