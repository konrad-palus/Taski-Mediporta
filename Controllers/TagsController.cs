using Microsoft.AspNetCore.Mvc;
using TaskApi_Mediporta.Services.Interfaces;

namespace TaskApi_Mediporta.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TagsController : ControllerBase
    {
        private readonly ITagService _soTagService;
        private readonly ILogger<TagsController> _logger;

        public TagsController(ITagService soTagService, ILogger<TagsController> logger)
        {
            _soTagService = soTagService;
            _logger = logger;
        }

        /// <summary>
        /// Initiates the import of tags.
        /// </summary>
        /// <returns>A status message.</returns>
        [HttpPost("FetchTags")]
        public async Task<IActionResult> FetchTags()
        {
            try
            {
                await _soTagService.ImportTagsAsync();
                return Ok("Import started.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while importing tags.");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Retrieves paginated tags based on request parameters.
        /// </summary>
        /// <param name="requestModel">The parameters for tag retrieval.</param>
        /// <returns>A paginated list of tags.</returns>
        [HttpGet("GetTags")]
        public IActionResult GetTags([FromQuery] GetTagsRequestModel requestModel)
        {
            try
            {
                var responseModel = _soTagService.GetPaginatedTags(requestModel);
                return Ok(responseModel);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid request parameters.");
                return BadRequest("Invalid request parameters.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving tags.");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }
    }
}