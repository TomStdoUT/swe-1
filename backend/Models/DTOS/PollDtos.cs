namespace PollSystem.Models.DTOs
{
    public record PollCreateDto(
        string Question, 
        string Category, 
        List<string> Tags, 
        List<string> Options, 
        int DurationSeconds, 
        bool AllowMultiple
    );
    
    public record VoteDto(List<int> OptionIndices, int BettedPoints);
    
    public record CommentCreateDto(string Content);
}