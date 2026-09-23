using DMS.BLL.Dtos;
using DMS.BLL.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace DMS.API.Controllers;

[ApiController]
[Route("api/tags")]
public class TagsController(ITagService tags) : ControllerBase
{
    /// <summary>Creates a new tag.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(TagDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TagDto>> Create(CreateTagRequest request, CancellationToken cancellationToken)
    {
        var tag = await tags.CreateAsync(request.Name, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, tag);
    }

    /// <summary>Lists all tags.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TagDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TagDto>>> List(CancellationToken cancellationToken)
    {
        return Ok(await tags.ListAsync(cancellationToken));
    }
}