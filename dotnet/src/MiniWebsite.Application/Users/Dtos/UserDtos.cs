namespace MiniWebsite.Application.Users.Dtos;

public record UserDto(
    int Id,
    string Role,
    string Email,
    string? Phone,
    string FirstName,
    string LastName,
    string Name,
    string Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string? ReferralCode,
    string? ReferredBy,
    string? District,
    string? State,
    string? Department,
    string? ProfileImage,
    string CollaborationEnabled,
    string SaleskitEnabled,
    string Influencer,
    string? WalletBalance,
    string? SelectService,
    int? MwReferralId,
    int? SenderUserId);

public record UpdateUserRequest(
    string? FirstName,
    string? LastName,
    string? Phone,
    string? State,
    string? District,
    string? Status,
    string? Department);

public record CreateUserRequest(
    string FirstName,
    string? LastName,
    string Email,
    string Phone,
    string Password,
    string Role,
    string? State = null,
    string? District = null);
