using DMS.Domain.Entities;

namespace DMS.BLL.Dtos;

public record DocumentDto(
    Guid Id,
    string FileName,
    string ContentType,
    long FileSize,
    DateTime UploadedAt,
    string? Description,
    DocumentStatus Status,
    IReadOnlyList<Guid> CollectionIds,
    IReadOnlyList<Guid> TagIds);