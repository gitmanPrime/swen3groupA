namespace DMS.BLL.Dtos;

public record CollectionDto(
    Guid Id,
    string Name,
    string? Description,
    DateTime CreatedAt,
    int DocumentCount
);
