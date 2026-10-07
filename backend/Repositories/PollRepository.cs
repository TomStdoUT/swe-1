using System.Text.Json;
using PollSystem.Models.Entities;
using PollSystem.Repositories.Interfaces;

namespace PollSystem.Repositories
{
    public class PollRepository : IPollRepository
    {
        private readonly List<Poll> _polls = new();
        private readonly string _filePath = "poll_snapshot.json";

        public IEnumerable<Poll> GetAll() => _polls;
        
        public Poll? GetById(Guid id) => _polls.FirstOrDefault(p => p.Id == id);
        
        public void Add(Poll poll) => _polls.Add(poll);
        
        public void Remove(Poll poll) => _polls.Remove(poll);

        public async Task SaveStateAsync()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            await using var stream = new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await JsonSerializer.SerializeAsync(stream, _polls, options);
        }

        public async Task LoadStateAsync()
        {
            if (!File.Exists(_filePath)) return;
            await using var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var loaded = await JsonSerializer.DeserializeAsync<List<Poll>>(stream);
            if (loaded != null)
            {
                _polls.Clear();
                _polls.AddRange(loaded);
            }
        }
    }
}