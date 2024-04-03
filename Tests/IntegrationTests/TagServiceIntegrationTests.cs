using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace TaskApi_MediportaTests
{
    public class TagServiceIntegrationTest
    {
        private readonly TagService _tagService;
        private readonly IMemoryCache _memoryCache;
        private readonly IHttpClientFactory _httpClientFactory;

        public TagServiceIntegrationTest()
        {
            _httpClientFactory = new HttpClientFactoryStub();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _tagService = new TagService(_httpClientFactory, _memoryCache, new NullLogger<TagService>());
        }

        [Fact]
        public async Task ImportTagsAsync_TagsImportedSuccessfully()
        {
            // Act
            await _tagService.ImportTagsAsync();

            // Assert
            var tagsExistInCache = _memoryCache.TryGetValue("TagsList", out object tags);
            Assert.True(tagsExistInCache, "Tags were not imported successfully.");
            Assert.NotNull(tags);
        }
    }

    public class HttpClientFactoryStub : IHttpClientFactory
    {
        public HttpClient CreateClient(string name = null)
        {
            return new HttpClient();
        }
    }
}