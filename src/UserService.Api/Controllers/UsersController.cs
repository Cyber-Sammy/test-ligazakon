using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using UserService.Api.DTOs;
using UserService.Api.Extensions;
using UserService.Application.Common;
using UserService.Application.Interfaces.Services;
using UserService.Application.Models;
using UserService.Domain.Rules;

namespace UserService.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    [HttpPost(Name = nameof(RegisterUserAsync))]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RegisterUserAsync(
        [FromServices]IUserService userService, 
        [FromBody] CreateUserDto userToRegister, 
        CancellationToken cancellationToken)
    {
        var command = new RegisterUserCommand(
            userToRegister.FirstName,
            userToRegister.LastName,
            userToRegister.MiddleName,
            userToRegister.Email,
            userToRegister.PhoneNumber);

        var result = await userService.RegisterUserAsync(command, cancellationToken);

        return result.ToActionResult(this, userId 
            => CreatedAtRoute(nameof(GetUserByIdAsync), new { id = userId }, new { id = userId }));
    }

    [HttpGet("{id:int:min(1)}", Name = nameof(GetUserByIdAsync))]
    [ProducesResponseType(typeof(GetUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUserByIdAsync(
        [FromServices] IUserService userService,
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var result = await userService.GetUserByIdAsync(id, cancellationToken);

        return result.ToActionResult(this, user => Ok(user.ToGetUserDto()));
    }

    [HttpGet("by-email/{email}", Name = nameof(GetUserByEmailAsync))]
    [ProducesResponseType(typeof(GetUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUserByEmailAsync(
        [FromServices] IUserService userService,
        [FromRoute]
        [Required]
        [EmailAddress]
        [StringLength(UserRules.EmailMaxLength)]
        string email,
        CancellationToken cancellationToken)
    {
        var result = await userService.GetUserByEmailAsync(email, cancellationToken);

        return result.ToActionResult(this, user => Ok(user.ToGetUserDto()));
    }

    [HttpGet(Name = nameof(GetUsersAsync))]
    [ProducesResponseType(typeof(IReadOnlyList<GetUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUsersAsync(
        [FromServices] IUserService userService,
        [FromQuery, BindRequired, Range(Constants.Pagination.MinimumTake, Constants.Pagination.MaximumTake)] int take,
        [FromQuery, Range(Constants.Pagination.MinimumSkip, int.MaxValue)] int skip,
        CancellationToken cancellationToken)
    {
        var result = await userService.GetUsersAsync(take, skip, cancellationToken);

        return result.ToActionResult(this, users => Ok(users.ToGetUserDtos()));
    }
}
