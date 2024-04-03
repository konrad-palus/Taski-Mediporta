namespace TaskApi_Mediporta.Services.Interfaces
{
    public interface ITagService
    {
        Task ImportTagsAsync();
        GetTagsResponseModel GetPaginatedTags(GetTagsRequestModel requestModel);
    }
}