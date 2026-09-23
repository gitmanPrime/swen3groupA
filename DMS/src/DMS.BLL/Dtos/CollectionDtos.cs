namespace DMS.BLL.Dtos;

public record CollectionDto(
    Guid Id,
    string Name,
    string? Description,
    DateTime CreatedAt,
    int DocumentCount);

public class CreateCollectionRequest
{
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }
}

public class RenameCollectionRequest
{
    public string Name { get; init; } = string.Empty;
}