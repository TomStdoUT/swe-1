using Microsoft.AspNetCore.Mvc;
using PollSystem.Domain;

namespace swe_1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PostsController : ControllerBase
    {
        private readonly PostManager _manager;
        private readonly UserManager _users;

        public PostsController(PostManager manager, UserManager users)
        {
            _manager = manager;
            _users = users;
        }

        [HttpGet]
        public IActionResult GetFeed()
        {
            return Ok(_manager.GetFeed());
        }

        [HttpPost]
        public IActionResult CreatePost(
            [FromHeader(Name = "X-Username")] string? username,
            [FromBody] PostCreateDto dto
        )
        {
            var currentUser = _users.Find(username);
            if (currentUser is null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.Content))
            {
                return BadRequest(new { error = "Post content cannot be empty." });
            }

            var post = _manager.CreatePost(currentUser.Id, currentUser.Username, dto.Content.Trim());
            
            return Ok(post);
        }
        [HttpPost("{id}/comments")]
        public IActionResult AddComment(Guid id, [FromBody] CommentCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Content))
            {
                return BadRequest(new { error = "Comment content cannot be empty." });
            }

            try
            {
                var comment = _manager.AddComment(id, _currentUser.Id, _currentUser.Username, dto.Content.Trim());
                return Ok(comment);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }
    }
}
