using Microsoft.AspNetCore.Mvc;
using PollSystem.Domain;

namespace swe_1.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Automatically maps to /api/posts
    public class PostsController : ControllerBase
    {
        private readonly PostManager _manager;
        private readonly User _currentUser;

        public PostsController(PostManager manager, User currentUser)
        {
            _manager = manager;
            _currentUser = currentUser;
        }

        [HttpGet]
        public IActionResult GetFeed()
        {
            return Ok(_manager.GetFeed());
        }

        [HttpPost]
        public async Task<IActionResult> CreatePost([FromBody] PostCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Content))
            {
                return BadRequest(new { error = "Post content cannot be empty." });
            }

            var post = _manager.CreatePost(_currentUser.Id, _currentUser.Username, dto.Content.Trim());
            await _manager.SaveStateAsync();
            
            return Ok(post);
        }
    }
}