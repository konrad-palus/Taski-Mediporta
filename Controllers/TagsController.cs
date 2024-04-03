using Microsoft.AspNetCore.Mvc;
using TaskApi_Mediporta.Services.Interfaces;

namespace TaskApi_Mediporta.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TagsController : ControllerBase
    {
        private readonly ITagService _soTagService;

        public TagsController(ITagService soTagService)
        {
            _soTagService = soTagService;
        }

        [HttpPost("FetchTags")]
        public async Task<IActionResult> FetchTags()
        {
            await _soTagService.ImportTagsAsync();
            return Ok("Import started.");
        }

        [HttpGet("GetTags")]
        public async Task<IActionResult> GetTags([FromQuery] GetTagsRequestModel requestModel)
        {
            var responseModel =  _soTagService.GetPaginatedTags(requestModel);
            return Ok(responseModel);
        }
    }
}
