namespace DMS.API.Requests
{
    public class CreateCollectionRequest
    {
        public string Name { get; init; } = string.Empty;

        public string? Description { get; init; }
    }
}
