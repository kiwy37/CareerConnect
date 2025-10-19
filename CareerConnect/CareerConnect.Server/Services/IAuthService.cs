using CareerConnect.Server.Models;

namespace CareerConnect.Server.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<AuthResponseDto> GoogleLoginAsync(GoogleLoginDto googleLoginDto);
        Task<AuthResponseDto> RegisterAsync(CreateUserDto createUserDto);
    }
}
