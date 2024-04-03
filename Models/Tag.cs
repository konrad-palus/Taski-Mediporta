namespace TaskApi_Mediporta.Models
{
    public class Tag
    {
        public string Name { get; set; }
        public int Count { get; set; }

        public Tag() { }

        public Tag(Tag tag)
        {
            Name = tag.Name;
            Count = tag.Count;
        }
    }
}