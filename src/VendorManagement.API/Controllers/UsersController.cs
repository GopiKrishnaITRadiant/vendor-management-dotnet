using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VendorManagement.Application.Common.Interfaces;
using VendorManagement.Application.Features.Users.Dtos;
using VendorManagement.Domain.Enums;
using VendorManagement.Application.Common.Models;

namespace VendorManagement.API.Controllers;

[ApiController]
[Route("api/users")]
//[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ICurrentUserService _currentUser;

    public UsersController(IUserService userService, ICurrentUserService currentUser)
    {
        _userService = userService;
        _currentUser = currentUser;
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
    {
        var result = await _userService.CreateAsync(dto);
        return CreatedAtAction(nameof(FindById), new { id = result.Id }, result);
    }

    [HttpGet]
    public async Task<IActionResult> FindAll(
        [FromQuery] int page = 1, [FromQuery] int limit = 20,
        [FromQuery] string? search = null, [FromQuery] int? role = null,
        [FromQuery] UserStatus? status = null, [FromQuery] string? viewType = null)
    {
        var query = new FindUsersQuery(page, limit, search, role, status, viewType);
        var result = await _userService.FindAllAsync(query);
        return Ok(ApiResponse<object>.SuccessResponse(result, "Users Fetched Successfully"));
    }

    [HttpGet("status-counts")]
    public async Task<IActionResult> GetStatusCounts([FromQuery] string? roleName = null)
    {
        var result = await _userService.GetStatusCountsAsync(roleName);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> FindById(int id)
    {
        var result = await _userService.FindByIdAsync(id);
        return Ok(result);
    }

    [HttpPatch("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto dto)
    {
        var result = await _userService.UpdateAsync(id, dto, _currentUser.UserId, _currentUser.Role);
        return Ok(result);
    }

    [HttpPost("{id:int}/change-password")]
    public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordDto dto)
    {
        await _userService.ChangePasswordAsync(id, dto);
        return Ok(new { message = "Password changed successfully" });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Remove(int id)
    {
        await _userService.RemoveAsync(id);
        return NoContent();
    }

    [HttpPost("{id:int}/setup-vendor")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> SetupVendor(int id, [FromBody] SetupVendorDto dto)
    {
        var result = await _userService.SetupVendorAsync(id, dto);
        return Ok(result);
    }

    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification([FromQuery] string email)
    {
        await _userService.ResendVerificationAsync(email);
        return Ok(new { message = "If unverified, a new link has been sent." });
    }
}