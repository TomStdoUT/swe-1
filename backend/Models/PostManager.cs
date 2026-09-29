using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PollSystem.Domain
{
    public class Post
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public Guid AuthorId { get; init; }
        public string AuthorName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

        public Post() { }

        public Post(Guid authorId, string authorName, string content)
        {
            if (string.IsNullOrWhiteSpace(authorName)) throw new ArgumentException("Author name cannot be empty.");
            if (string.IsNullOrWhiteSpace(content)) throw new ArgumentException("Post content cannot be empty.");

            AuthorId = authorId;
            AuthorName = authorName;
            Content = content;
        }
    }

    public class PostManager : IEnumerable<Post>
    {
        private readonly List<Post> _posts = new();

        public Post CreatePost(Guid authorId, string authorName, string content)
        {
            var post = new Post(authorId, authorName, content);
            _posts.Add(post);
            return post;
        }

        public IEnumerable<Post> GetUserPosts(Guid authorId) =>
            _posts.Where(p => p.AuthorId == authorId).OrderByDescending(p => p.CreatedAt);

        public IEnumerable<Post> GetFeed() => 
            _posts.OrderByDescending(p => p.CreatedAt);

        public IEnumerator<Post> GetEnumerator() => _posts.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}