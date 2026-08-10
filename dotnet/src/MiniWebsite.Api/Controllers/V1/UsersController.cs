using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Application.Users;
using MiniWebsite.Application.Users.Dtos;
using MiniWebsite.Shared.Constants;

namespace MiniWebsite.Api.Controllers.V1;

/// <summary>
/// Fetches / manages rows from live <c>user_details</c> table.
/// Auth temporarily open — secure later.
/// </summary>
[ApiController]
[Route(ApiConstants.ApiRoutePrefix + "/users")]
[AllowAnonymous]
public class UsersController : ControllerBase
{
    private readonly IUserService _users;

    public UsersController(IUserService users)
    {
        _users = users;
    }

    /// <summary>List users from user_details (paged).</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResult<PagedResult<UserDto>>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? role = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var result = await _users.ListAsync(page, pageSize, role, search, ct);
        return Ok(result);
    }

    /// <summary>Get one user by id.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResult<UserDto>>> GetById(int id, CancellationToken ct)
    {
        var result = await _users.GetByIdAsync(id, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResult<UserDto>>> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var result = await _users.CreateAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResult<UserDto>>> Update(int id, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        var result = await _users.UpdateAsync(id, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResult>> Delete(int id, CancellationToken ct)
    {
        var result = await _users.SoftDeleteAsync(id, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
