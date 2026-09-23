namespace DMS.BLL.Dtos;

public record TagDto(Guid Id, string Name);

public class CreateTagRequest
{
    public string Name { get; init; } = string.Empty;
}