public class GetTagsResponseModel
{
    public IEnumerable<ResponseTag> Tags { get; set; }

}

public class ResponseTag
{
    public string Name { get; set; }
    public double Percentage { get; set; }
}