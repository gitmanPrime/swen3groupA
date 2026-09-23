using DMS.BLL.Dtos;
using DMS.BLL.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace DMS.API.Controllers;

[ApiController]
[Route("api/collections")]
public class CollectionsController(ICollectionService collections) : ControllerBase
{
    /// <summary>Creates a new collection.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CollectionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CollectionDto>> Create(CreateCollectionRequest request, CancellationToken cancellationToken)
    {
        var collection = await collections.CreateAsync(request.Name, request.Description, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = collection.Id }, collection);
    }

    /// <summary>Lists all collections.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CollectionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CollectionDto>>> List(CancellationToken cancellationToken)
    {
        return Ok(await collections.ListAsync(cancellationToken));
    }

    /// <summary>Retrieves the details of a collection.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CollectionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CollectionDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await collections.GetByIdAsync(id, cancellationToken));
    }

    /// <summary>Renames a collection.</summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(CollectionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CollectionDto>> Rename(Guid id, RenameCollectionRequest request, CancellationToken cancellationToken)
    {
        return Ok(await collections.RenameAsync(id, request.Name, cancellationToken));
    }

    /// <summary>Deletes an empty collection. Non-empty collections cannot be deleted.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await collections.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}