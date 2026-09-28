using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace PollSystem.Domain
{
    public class Post
    {
        // Represents an individual post in system.
        public Guid Id { get; init; } = Guid.NewGuid();
        public Guid AuthorId { get; init; }
        public string AuthorName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

        // Needed for Json files
        public Post() { }

        // Check input before creating
        public Post(Guid authorId, string authorName, string content)
        {
            if (string.IsNullOrWhiteSpace(authorName))
            {
                throw new ArgumentException("Author name cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentException("Post content cannot be empty.");
            }

            AuthorId = authorId;
            AuthorName = authorName;
            Content = content;
        }
    }

    // Post creation, functionality.
    public class PostManager : IEnumerable<Post>
    {
        private List<Post> _posts = new();

        // Create, store, and return a new post.
        public Post CreatePost(Guid authorId, string authorName, string content)
        {
            var post = new Post(authorId, authorName, content);
            _posts.Add(post);
            return post;
        }

        // Retrieve specific users posts
        public IEnumerable<Post> GetUserPosts(Guid authorId)
        {
            return _posts
                .Where(p => p.AuthorId == authorId)
                .OrderByDescending(p => p.CreatedAt);
        }

        // Retrieve global feed, ordered
        public IEnumerable<Post> GetFeed()
        {
            return _posts.OrderByDescending(p => p.CreatedAt);
        }

        // Save posts to a JSON file
        public async Task SaveStateAsync(string filePath = "posts.json")
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            using FileStream stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await JsonSerializer.SerializeAsync(stream, _posts, options);
        }

        // Load posts from a JSON file if it exists
        public async Task LoadStateAsync(string filePath = "posts.json")
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            using FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var loadedPosts = await JsonSerializer.DeserializeAsync<List<Post>>(stream);
            if (loadedPosts != null)
            {
                _posts = loadedPosts;
            }
        }

        // Allows looping through postManager directly with a foreach loop
        public IEnumerator<Post> GetEnumerator() => _posts.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}