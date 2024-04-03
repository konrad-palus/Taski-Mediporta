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
    private const int _pageSize = 100;
    private const int _pages = 10;
    private const string _gzipEncoding = "gzip";
    private const string _baseUri = "https://api.stackexchange.com/2.3/tags";
    private const string _apiSiteParameter = "site=stackoverflow";
    private const string CacheKey = "TagsList";

    public TagService(IHttpClientFactory httpClientFactory, IMemoryCache cache)
    {
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.BaseAddress = new Uri(_baseUri);
        _cache = cache;
    }

    //przekminić to
    public async Task ImportTagsAsync()
    {
        if (_cache.TryGetValue(CacheKey, out List<Tag> existingTags))
        {
            _cache.Remove(CacheKey);
        }

        var fetchedTags = new List<Tag>();

        for (int i = 1; i <= _pages; i++)
        {
            var tagsResponse = await FetchTagsFromApi(_pageSize, i);
            ProcessTagsResponse(tagsResponse, fetchedTags);

            if (tagsResponse.Items.Count == 0)
            {
                break;
            }
        }

        SaveTagsToCache(fetchedTags);
    }

    private string BuildApiUrl(int pageSize, int page)
    {
        //urlbuilder przekminić czy nie lepiej
        return $"{_baseUri}?{_apiSiteParameter}&{nameof(pageSize)}={pageSize}&{nameof(page)}={page}&order=desc&sort=popular";
    }

    private async Task<FetchTagsResponse> FetchTagsFromApi(int pageSize, int page)
    {
        var response = await _httpClient.GetAsync(BuildApiUrl(pageSize, page));

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Request to Stack Overflow API failed with status code: {response.StatusCode} and message: {await response.Content.ReadAsStringAsync()}");
        }

        var responseStream = await response.Content.ReadAsStreamAsync();

        if (response.Content.Headers.ContentEncoding.Contains(_gzipEncoding))
        {
            responseStream = new GZipStream(responseStream, CompressionMode.Decompress);
        }

        using var sr = new StreamReader(responseStream);
        using var jsonTextReader = new JsonTextReader(sr);

        return new JsonSerializer().Deserialize<FetchTagsResponse>(jsonTextReader);
    }

    private void ProcessTagsResponse(FetchTagsResponse tagsResponse, List<Tag> fetchedTags)
    {
        if (tagsResponse?.Items == null)
        {
            return;
        }

        fetchedTags.AddRange(tagsResponse.Items.Select(tagItem => new Tag(tagItem)));
    }

    private void SaveTagsToCache(List<Tag> fetchedTags)
    {
        _cache.Set(CacheKey, fetchedTags);
    }

    public GetTagsResponseModel GetPaginatedTags(GetTagsRequestModel requestModel)
    {
        var sortedTags = SortTags(CalculateTagPercentages(), requestModel.SortBy, requestModel.SortOrder);

        return new GetTagsResponseModel
        {
            Tags = PaginateTags(sortedTags, requestModel.PageNumber, requestModel.PageSize)
        };
    }

    private IEnumerable<ResponseTag> SortTags(IEnumerable<ResponseTag> tags, SortByEnum sortBy, SortOrderEnum sortOrder)
    {
        Func<ResponseTag, object> key = sortBy == SortByEnum.Name ? (tag => tag.Name) : (tag => tag.Percentage);

        return sortOrder == SortOrderEnum.Asc ?
            tags.OrderBy(key) :
            tags.OrderByDescending(key);
    }

    private IEnumerable<ResponseTag> PaginateTags(IEnumerable<ResponseTag> sortedTags, int pageNumber, int pageSize)
    {
        return sortedTags
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);
    }

    private List<ResponseTag> CalculateTagPercentages()
    {
        if (!_cache.TryGetValue(CacheKey, out List<Tag> tagsFromCache))
        {
            return new List<ResponseTag>();
        }

        var totalTagsCount = tagsFromCache.Sum(tag => tag.Count);

        var responseTags = tagsFromCache.Select(tag => new ResponseTag
        {
            Name = tag.Name,
            Percentage = Math.Round((double)tag.Count / totalTagsCount * 100, 5)
        }).ToList();

        return responseTags;
    }
}