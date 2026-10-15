using DMS.BLL.Dtos;

namespace DMS.BLL.Interfaces.Services;

public interface IOcrQueuePublisher
{
    Task PublishAsync(OcrRequest request, CancellationToken cancellationToken = default);
}
