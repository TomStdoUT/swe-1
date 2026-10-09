using System.Collections.Concurrent;
using System.Text.Json;
using PollSystem.Models.Entities;
using PollSystem.Repositories.Interfaces;

namespace PollSystem.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ConcurrentDictionary<string, User> _users = new(StringComparer.OrdinalIgnoreCase);
        private readonly SemaphoreSlim _stateLock = new(1, 1);
        private readonly string _stateFilePath;

        public UserRepository(string stateFilePath)
        {
            _stateFilePath = stateFilePath;
        }

        public async Task LoadStateAsync()
        {
            if (!File.Exists(_stateFilePath)) return;

            await _stateLock.WaitAsync();
            try
            {
                await using var stream = new FileStream(_stateFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var loadedUsers = await JsonSerializer.DeserializeAsync<List<User>>(stream);
                
                if (loadedUsers is null) return;

                _users.Clear();
                foreach (var user in loadedUsers)
                {
                    _users.TryAdd(user.Username, user);
                }
            }
            finally
            {
                _stateLock.Release();
            }
        }

        public async Task SaveStateAsync()
        {
            await _stateLock.WaitAsync();
            try
            {
                await using var stream = new FileStream(_stateFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
                await JsonSerializer.SerializeAsync(stream, GetAll().ToList(), new JsonSerializerOptions { WriteIndented = true });
            }
            finally
            {
                _stateLock.Release();
            }
        }

        public void Add(User user)
        {
            _users.TryAdd(user.Username, user);
        }

        public User? FindByUsername(string? username)
        {
            if (string.IsNullOrWhiteSpace(username)) return null;
            return _users.GetValueOrDefault(username.Trim());
        }

        public IEnumerable<User> GetAll() => _users.Values.OrderBy(user => user.Username);
    }
}