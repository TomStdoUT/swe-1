namespace PollSystem.Models.Exceptions
{
    public class PollDomainException : Exception
    {
        public PollDomainException(string message) : base(message) { }
    }
}