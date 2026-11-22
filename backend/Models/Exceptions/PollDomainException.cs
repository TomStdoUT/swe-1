namespace PollSystem.Models.Exceptions
{
    public class PollDomainException : Exception
    {
        public string ErrorCode { get; }
        public Guid? PollId { get; }

        public PollDomainException(string message, string errorCode = "POLL_DOMAIN_ERROR", Guid? pollId = null)
            : base(message)
        {
            ErrorCode = errorCode;
            PollId = pollId;
        }
    }
}
