namespace UserService.Api.DTOs;

public class GetUserDto
{
    public required string FullName { get; set; }

    public required string Email { get; set; }

    public required string PhoneNumber { get; set; }
}
