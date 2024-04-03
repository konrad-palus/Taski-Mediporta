using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System.IO.Compression;
using TaskApi_Mediporta.Models;
using TaskApi_Mediporta.Models.Responses;
using TaskApi_Mediporta.Services.Interfaces;

public class TagService : ITagService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<TagService> _logger;
    private const int _pageSize = 100;
    private const int _pages = 10;
    private const string _gzipEncoding = "gzip";
    private const string _baseUri = "https://api.stackexchange.com/2.3/tags";
    private const string _apiSiteParameter = "site=stackoverflow";
    private const string CacheKey = "TagsList";

    /// <summary>
    /// Initializes a new instance of the TagService.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="cache">The cache store.</param>
    /// <param name="logger">The logger.</param>

    public TagService(IHttpClientFactory httpClientFactory, IMemoryCache cache, ILogger<TagService> logger)
    {
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.BaseAddress = new Uri(_baseUri);
        _cache = cache;
        _logger = logger;
    }

    public async Task ImportTagsAsync()
    {
        _logger.LogInformation("Starting {Method}", nameof(ImportTagsAsync));

        try
        {
            if (_cache.TryGetValue(CacheKey, out _))
            {
                _cache.Remove(CacheKey);
            }

            var fetchTasks = Enumerable.Range(1, _pages)
                                       .Select(page => FetchTagsFromApi(_pageSize, page))
                                       .ToList();

            var fetchedTagsResponses = await Task.WhenAll(fetchTasks);

            var fetchedTags = fetchedTagsResponses
                                .Where(response => response?.Items?.Any() == true)
                                .SelectMany(response => response.Items)
                                .Select(tagItem => new Tag(tagItem))
                                .ToList();

            if (fetchedTags.Any())
            {
                _cache.Set(CacheKey, fetchedTags);
                _logger.LogInformation("{Method} completed successfully with {Count} tags imported.", nameof(ImportTagsAsync), fetchedTags.Count);
            }
            else
            {
                _logger.LogWarning("{Method} completed with no tags imported.", nameof(ImportTagsAsync));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Method} encountered an error.", nameof(ImportTagsAsync));
            throw new InvalidOperationException($"Error in {nameof(ImportTagsAsync)}: {ex.Message}", ex);
        }
    }

    private string BuildApiUrl(int pageSize, int page)
         => $"{_baseUri}?{_apiSiteParameter}&pagesize={pageSize}&page={page}&order=desc&sort=popular";

    /// <summary>
    /// Fetches tags from the Stack Exchange API based on the provided page size and page number.
    /// </summary>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="page">The page number.</param>
    private async Task<FetchTagsResponse> FetchTagsFromApi(int pageSize, int page)
    {
        var url = BuildApiUrl(pageSize, page);
        _logger.LogInformation("Starting {method} method for URL: {url}", nameof(FetchTagsFromApi), url);

        try
        {
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                _logger.LogError("Request to {url} failed with status code: {statusCode}, message: {message}", url, response.StatusCode, errorMessage);
                throw new HttpRequestException($"Request to Stack Overflow API failed for URL: {url} with status code: {response.StatusCode} and message: {errorMessage}");
            }

            var responseStream = await response.Content.ReadAsStreamAsync();
            if (response.Content.Headers.ContentEncoding.Contains(_gzipEncoding))
            {
                responseStream = new GZipStream(responseStream, CompressionMode.Decompress);
            }

            using var sr = new StreamReader(responseStream);
            using var jsonTextReader = new JsonTextReader(sr);
            var result = new JsonSerializer().Deserialize<FetchTagsResponse>(jsonTextReader);

            _logger.LogInformation("{method} completed successfully for URL: {url}", nameof(FetchTagsFromApi), url);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request error occurred while fetching tags from {url}.", url);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while fetching tags from {url}.", url);
            throw;
        }
    }

    /// <summary>
    /// Processes the fetched tags response and adds the tags to the cache.
    /// </summary>
    /// <param name="tagsResponse">The response containing the fetched tags.</param>
    /// <param name="fetchedTags">The list to which the tags will be added.</param>
    private void ProcessTagsResponse(FetchTagsResponse tagsResponse, List<Tag> fetchedTags)
    {
        _logger.LogInformation("Starting {MethodName}", nameof(ProcessTagsResponse));

        if (tagsResponse?.Items == null)
        {
            _logger.LogWarning("{MethodName} received a null or empty tags response, skipping processing.", nameof(ProcessTagsResponse));
            throw new ArgumentException("Tags response is null or empty", nameof(tagsResponse));
        }

        int processedCount = tagsResponse.Items.Count;
        _logger.LogInformation("{MethodName} processing {ItemCount} tags.", nameof(ProcessTagsResponse), processedCount);
        fetchedTags.AddRange(tagsResponse.Items.Select(tagItem => new Tag(tagItem)));

        _logger.LogInformation("{MethodName} completed successfully, processed {ProcessedCount} tags.", nameof(ProcessTagsResponse), processedCount);
    }


    private void SaveTagsToCache(List<Tag> fetchedTags)
    {
        _logger.LogInformation("Starting {MethodName}", nameof(SaveTagsToCache));
        try
        {
            _cache.Set(CacheKey, fetchedTags);
            _logger.LogInformation("{MethodName} successfully saved {Count} tags to cache.", nameof(SaveTagsToCache), fetchedTags.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{MethodName} encountered an error while saving tags to cache.", nameof(SaveTagsToCache));
            throw new InvalidOperationException($"Failed to save tags to cache in {nameof(SaveTagsToCache)}", ex);
        }
    }

    /// <summary>
    /// Retrieves paginated tags based on the request model, sorts them, and calculates their percentages.
    /// </summary>
    /// <param name="requestModel">The model containing the request parameters.</param>
    /// <returns>The paginated tags response model.</returns>
    public GetTagsResponseModel GetPaginatedTags(GetTagsRequestModel requestModel)
    {
        _logger.LogInformation("Starting {MethodName} with RequestModel: PageNumber={PageNumber}, PageSize={PageSize}",
            nameof(GetPaginatedTags), requestModel.PageNumber, requestModel.PageSize);

        var responseTags = CalculateTagPercentages();
        if (!responseTags.Any())
        {
            _logger.LogWarning("{MethodName} found no tags to paginate.", nameof(GetPaginatedTags));
            return new GetTagsResponseModel { Tags = Enumerable.Empty<ResponseTag>() };
        }

        var sortedTags = SortTags(responseTags, requestModel.SortBy, requestModel.SortOrder);
        var paginatedTags = PaginateTags(sortedTags, requestModel.PageNumber, requestModel.PageSize);

        _logger.LogInformation("{MethodName} completed successfully. Returned {Count} paginated tags.", nameof(GetPaginatedTags), paginatedTags.Count());
        return new GetTagsResponseModel { Tags = paginatedTags };
    }

    /// <summary>
    /// Sorts the tags based on the specified sort criteria and order.
    /// </summary>
    private IEnumerable<ResponseTag> SortTags(IEnumerable<ResponseTag> tags, SortByEnum sortBy, SortOrderEnum sortOrder)
    {
        _logger.LogInformation("Starting {MethodName} with SortBy: {SortBy}, SortOrder: {SortOrder}",
            nameof(SortTags), sortBy, sortOrder);

        if (!tags.Any())
        {
            _logger.LogWarning("{MethodName} received an empty collection of tags for sorting.", nameof(SortTags));
        }

        Func<ResponseTag, object> keySelector = sortBy == SortByEnum.Name ? (tag => tag.Name) : (tag => tag.Percentage);
        var sortedTags = sortOrder == SortOrderEnum.Asc ? tags.OrderBy(keySelector) : tags.OrderByDescending(keySelector);

        _logger.LogInformation("{MethodName} completed sorting.", nameof(SortTags));
        return sortedTags;
    }

    /// <summary>
    /// Paginates the sorted collection of tags based on the specified page number and page size.
    /// </summary>
    /// <param name="sortedTags">The sorted collection of tags.</param>
    /// <param name="pageNumber">The page number to retrieve.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A paginated collection of tags.</returns>
    private IEnumerable<ResponseTag> PaginateTags(IEnumerable<ResponseTag> sortedTags, int pageNumber, int pageSize)
    {
        _logger.LogInformation("Starting {MethodName} for PageNumber: {PageNumber}, PageSize: {PageSize}",
            nameof(PaginateTags), pageNumber, pageSize);

        var paginatedTags = sortedTags.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        _logger.LogInformation("{MethodName} completed. Paginated to {ResultCount} tags.", nameof(PaginateTags), paginatedTags.Count);
        return paginatedTags;
    }

    /// <summary>
    /// Calculates the percentage of each tag based on its occurrence relative to the total count of all tags.
    /// </summary>
    /// <returns>A list of tags with calculated percentages.</returns>
    private List<ResponseTag> CalculateTagPercentages()
    {
        _logger.LogInformation("Starting {MethodName}", nameof(CalculateTagPercentages));

        if (!_cache.TryGetValue(CacheKey, out List<Tag> tagsFromCache))
        {
            _logger.LogWarning("{MethodName} found no tags in cache.", nameof(CalculateTagPercentages));
            return new List<ResponseTag>();
        }

        var totalTagsCount = tagsFromCache.Sum(tag => tag.Count);
        if (totalTagsCount == 0)
        {
            _logger.LogWarning("{MethodName} found tags but the total count is 0, which could lead to division by zero.", nameof(CalculateTagPercentages));
        }

        var responseTags = tagsFromCache.Select(tag => new ResponseTag
        {
            Name = tag.Name,
            Percentage = Math.Round((double)tag.Count / totalTagsCount * 100, 5)
        }).ToList();

        _logger.LogInformation("{MethodName} calculated tag percentages for {TagCount} tags.", nameof(CalculateTagPercentages), responseTags.Count);
        return responseTags;
    }
}