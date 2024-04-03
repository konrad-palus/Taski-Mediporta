public class GetTagsRequestModel
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public SortByEnum SortBy { get; set; } = SortByEnum.Name;
    public SortOrderEnum SortOrder { get; set; } = SortOrderEnum.Asc;
}