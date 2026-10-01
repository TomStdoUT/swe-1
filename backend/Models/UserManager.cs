using System.Collections.Concurrent;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

namespace PollSystem.Domain
{
    public class UserManager
    {
        private readonly ConcurrentDictionary<string, User> _users =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly SemaphoreSlim _stateLock = new(1, 1);
        private readonly string _stateFilePath;

        public UserManager(string stateFilePath)
        {
            _stateFilePath = stateFilePath;
            GetOrCreate("Povilas");
        }

        public async Task LoadStateAsync()
        {
            if (!File.Exists(_stateFilePath))
            {
                return;
            }

            await _stateLock.WaitAsync();
            try
            {
                await using var stream = new FileStream(
                    _stateFilePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read
                );
                var loadedUsers = await JsonSerializer.DeserializeAsync<List<User>>(stream);
                if (loadedUsers is null)
                {
                    return;
                }

                _users.Clear();
                foreach (var user in loadedUsers)
                {
                    _users.TryAdd(user.Username, user);
                }

                if (_users.IsEmpty)
                {
                    GetOrCreate("Povilas");
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
                await using var stream = new FileStream(
                    _stateFilePath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None
                );
                await JsonSerializer.SerializeAsync(
                    stream,
                    GetAll().ToList(),
                    new JsonSerializerOptions { WriteIndented = true }
                );
            }
            finally
            {
                _stateLock.Release();
            }
        }

        public User GetOrCreate(string username)
        {
            var normalizedUsername = username.Trim();
            return _users.GetOrAdd(normalizedUsername, name => new User(name)
            {
                Id = CreateStableId(name),
            });
        }

        public User? Find(string? username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return null;
            }

            return _users.GetValueOrDefault(username.Trim());
        }

        public IEnumerable<User> GetAll() => _users.Values.OrderBy(user => user.Username);

        private static Guid CreateStableId(string username)
        {
            var normalizedUsername = username.ToUpperInvariant();
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedUsername));
            return new Guid(hash.AsSpan(0, 16));
        }
    }
}
