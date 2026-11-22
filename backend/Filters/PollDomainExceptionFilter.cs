using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PollSystem.Models.Exceptions;

namespace PollSystem.Filters
{
    public sealed class PollDomainExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<PollDomainExceptionFilter> _logger;

        public PollDomainExceptionFilter(ILogger<PollDomainExceptionFilter> logger)
        {
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            if (context.Exception is not PollDomainException domainException)
                return;

            _logger.LogWarning(
                domainException,
                "Poll domain rule violated ({ErrorCode}) for poll {PollId}: {Message}",
                domainException.ErrorCode,
                domainException.PollId,
                domainException.Message);

            context.Result = new BadRequestObjectResult(new
            {
                error = domainException.Message,
                errorCode = domainException.ErrorCode,
                pollId = domainException.PollId
            });

            context.ExceptionHandled = true;
        }
    }
}