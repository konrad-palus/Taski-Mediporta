using Microsoft.Extensions.Caching.Memory;
using Moq;
using TaskApi_Mediporta.Models;
using Xunit;
namespace TaskApi_Mediporta.UnitTests
{
    public class TagServiceUnitTests
    {
        [Fact]
        public void GetPaginatedTags_ReturnsExpectedTag()
        {
            // Arrange
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var tags = new List<Tag>
            {
                new Tag { Name = "C#", Count = 2 },
                new Tag { Name = "Java", Count = 2 },
                new Tag { Name = "Sqlite", Count = 200 },
            };

            memoryCache.Set("TagsList", tags);

            var loggerMock = new Mock<ILogger<TagService>>();

            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            var httpClientMock = new Mock<HttpClient>();
            httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClientMock.Object);

            var service = new TagService(httpClientFactoryMock.Object, memoryCache, loggerMock.Object);

            var requestModel = new GetTagsRequestModel
            {
                PageNumber = 1,
                PageSize = 1,
                SortBy = SortByEnum.Name,
                SortOrder = SortOrderEnum.Desc
            };

            // Act
            var result = service.GetPaginatedTags(requestModel);

            // Assert
            Assert.Single(result.Tags);
            Assert.Equal("Sqlite", result.Tags.First().Name);
        }

        [Fact]
        public void GetPaginatedTags_WhenCacheIsEmpty_ReturnsEmptyTagsList()
        {
            // Arrange
            var memoryCache = new MemoryCache(new MemoryCacheOptions());

            var loggerMock = new Mock<ILogger<TagService>>();

            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            var httpClientMock = new Mock<HttpClient>();
            httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClientMock.Object);

            var service = new TagService(httpClientFactoryMock.Object, memoryCache, loggerMock.Object);

            var requestModel = new GetTagsRequestModel
            {
                PageNumber = 1,
                PageSize = 1,
                SortBy = SortByEnum.Name,
                SortOrder = SortOrderEnum.Asc
            };

            // Act
            var result = service.GetPaginatedTags(requestModel);

            // Assert
            Assert.Empty(result.Tags);
        }
    }
}