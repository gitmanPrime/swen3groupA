using DMS.BLL.Dtos;
using DMS.BLL.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace DMS.API.Controllers;

[ApiController]
[Route("api/documents")]
public class DocumentsController(IDocumentService documents) : ControllerBase
{
    /// <summary>Uploads a new document using multipart/form-data.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DocumentDto>> Upload(
        [FromForm] IFormFile file,
        [FromForm] string? description,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Validation Failed",
                detail: "An empty file cannot be uploaded.");
        }

        await using var content = file.OpenReadStream();
        var document = await documents.UploadAsync(
            file.FileName,
            file.ContentType,
            file.Length,
            content,
            description,
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = document.Id }, document);
    }

    /// <summary>Lists documents, optionally filtered by collection and/or tag.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DocumentDto>>> List(
        [FromQuery] Guid? collectionId,
        [FromQuery] Guid? tagId,
        CancellationToken cancellationToken)
    {
        return Ok(await documents.ListAsync(collectionId, tagId, cancellationToken));
    }

    /// <summary>Retrieves the metadata of a document.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await documents.GetByIdAsync(id, cancellationToken));
    }

    /// <summary>Downloads the original file of a document.</summary>
    [HttpGet("{id:guid}/download")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        var download = await documents.DownloadAsync(id, cancellationToken);
        return File(download.Content, download.ContentType, download.FileName);
    }

    /// <summary>Updates the description and/or collection assignment of a document.</summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentDto>> Update(Guid id, UpdateDocumentRequest request, CancellationToken cancellationToken)
    {
        return Ok(await documents.UpdateAsync(id, request, cancellationToken));
    }

    /// <summary>Deletes a document and its stored file.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await documents.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Assigns a tag to a document.</summary>
    [HttpPost("{id:guid}/tags/{tagId:guid}")]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DocumentDto>> AssignTag(Guid id, Guid tagId, CancellationToken cancellationToken)
    {
        return Ok(await documents.AssignTagAsync(id, tagId, cancellationToken));
    }

    /// <summary>Removes a tag from a document.</summary>
    [HttpDelete("{id:guid}/tags/{tagId:guid}")]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentDto>> RemoveTag(Guid id, Guid tagId, CancellationToken cancellationToken)
    {
        return Ok(await documents.RemoveTagAsync(id, tagId, cancellationToken));
    }
}