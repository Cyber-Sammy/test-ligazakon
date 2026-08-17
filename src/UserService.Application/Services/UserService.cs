using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;
using UserService.Application.Common;
using UserService.Application.Common.Exceptions;
using UserService.Application.Common.Extensions;
using UserService.Application.Common.Results;
using UserService.Application.Interfaces.Infrastructure;
using UserService.Application.Interfaces.IntegrationEvents;
using UserService.Application.Interfaces.Services;
using UserService.Application.Interfaces.UnitOfWork;
using UserService.Application.Models;
using UserService.Domain.Entities;
using UserService.Domain.Rules;

namespace UserService.Application.Services;

public class UserService : IUserService
{
    public UserService(
        IUserRepository userRepository,
        ILogger<UserService> logger,
        IUnitOfWork unitOfWork,
        IOutboxWriter outboxWriter,
        TimeProvider timeProvider)
    {
        _userRepository = userRepository;
        _logger = logger;
        _unitOfWork = unitOfWork;
        _outboxWriter = outboxWriter;
        _timeProvider = timeProvider;
    }

    public async Task<Result<int>> RegisterUserAsync(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        _logger.LogInformation(Constants.Logging.UserRegistrationStarted);

        if (!IsValid(command))
        {
            return Result<int>.Failure(
                ResultStatus.ValidationError,
                Constants.Results.InvalidRegistrationData);
        }

        var user = User.Create(
            command.FirstName,
            command.LastName,
            command.MiddleName,
            command.Email,
            command.PhoneNumber,
            _timeProvider.GetUtcNow());

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            _userRepository.Add(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var integrationEvent = new UserRegisteredIntegrationEvent(
                Guid.NewGuid(),
                _timeProvider.GetUtcNow(),
                user.Id,
                user.FirstName,
                user.LastName,
                user.MiddleName,
                user.Email,
                user.PhoneNumber);

            _outboxWriter.Add(integrationEvent);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(Constants.Logging.UserRegistrationCompleted, user.Id);

            return Result<int>.Success(user.Id);
        }
        catch (UserUniquenessConflictException exception)
        {
            await transaction.RollbackAsync(CancellationToken.None);

            _logger.LogWarning(Constants.Logging.UserAlreadyExists, user.Email, user.PhoneNumber);

            var message = exception.Conflict switch
            {
                UserUniquenessConflict.Email => string.Format(Constants.Results.EmailAlreadyExists, user.Email),
                UserUniquenessConflict.PhoneNumber => string.Format(Constants.Results.PhoneNumberAlreadyExists, user.PhoneNumber),
                _ => Constants.Results.UserAlreadyExists
            };

            return Result<int>.Failure(ResultStatus.Conflict, message);
        }
    }

    public async Task<Result<User>> GetUserByEmailAsync(string email, CancellationToken cancellationToken)
    {
        _logger.LogInformation(Constants.Logging.GettingUserByEmail, email);

        if (!IsValidEmail(email))
        {
            return Result<User>.Failure(
                ResultStatus.ValidationError,
                Constants.Results.InvalidEmail);
        }

        var normalizedEmail = UserEmail.Normalize(email);
        var retrievedUser = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (retrievedUser is null)
        {
            _logger.LogInformation(Constants.Logging.UserNotFound, normalizedEmail);

            return Result<User>.Failure(ResultStatus.NotFound, string.Format(Constants.Results.UserNotFound, normalizedEmail));
        }

        _logger.LogInformation(Constants.Logging.UserRetrieved, normalizedEmail);

        return Result<User>.Success(retrievedUser);
    }

    public async Task<Result<User>> GetUserByIdAsync(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation(Constants.Logging.GettingUserById, id);

        if (id <= 0)
        {
            return Result<User>.Failure(
                ResultStatus.ValidationError,
                Constants.Results.InvalidUserId);
        }

        var retrievedUser = await _userRepository.GetByIdAsync(id, cancellationToken);

        if (retrievedUser is null)
        {
            _logger.LogInformation(Constants.Logging.UserNotFoundById, id);

            return Result<User>.Failure(ResultStatus.NotFound, string.Format(Constants.Results.UserNotFoundById, id));
        }

        _logger.LogInformation(Constants.Logging.UserRetrievedById, id);

        return Result<User>.Success(retrievedUser);
    }

    public async Task<Result<IReadOnlyList<User>>> GetUsersAsync(int take, int skip, CancellationToken cancellationToken)
    {
        _logger.LogInformation(Constants.Logging.GettingUsers, take, skip);

        if (take is < Constants.Pagination.MinimumTake or > Constants.Pagination.MaximumTake
            || skip < Constants.Pagination.MinimumSkip)
        {
            return Result<IReadOnlyList<User>>.Failure(
                ResultStatus.ValidationError,
                string.Format(
                    Constants.Results.InvalidPagination,
                    Constants.Pagination.MinimumTake,
                    Constants.Pagination.MaximumTake));
        }

        var retrievedUsers = await _userRepository.GetAsync(take, skip, cancellationToken);

        _logger.LogInformation(Constants.Logging.UsersRetrieved, retrievedUsers.Count);

        return Result<IReadOnlyList<User>>.Success(retrievedUsers);
    }

    private static bool IsValid(RegisterUserCommand command) =>
        IsValidRequiredText(command.FirstName, UserRules.NameMaxLength)
        && IsValidRequiredText(command.LastName, UserRules.NameMaxLength)
        && IsValidOptionalText(command.MiddleName, UserRules.NameMaxLength)
        && IsValidEmail(command.Email)
        && IsValidRequiredText(command.PhoneNumber, UserRules.PhoneNumberMaxLength)
        && UserRules.IsValidPhoneNumber(command.PhoneNumber.Trim());

    private static bool IsValidEmail(string? email) =>
        IsValidRequiredText(email, UserRules.EmailMaxLength)
        && EmailAddressValidator.IsValid(email!.Trim());

    private static bool IsValidRequiredText(string? value, int maximumLength) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Trim().Length <= maximumLength;

    private static bool IsValidOptionalText(string? value, int maximumLength) =>
        string.IsNullOrWhiteSpace(value)
        || value.Trim().Length <= maximumLength;

    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserService> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxWriter _outboxWriter;
    private readonly TimeProvider _timeProvider;
    private static readonly EmailAddressAttribute EmailAddressValidator = new();
}
