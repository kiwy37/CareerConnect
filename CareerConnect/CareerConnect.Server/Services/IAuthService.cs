using CareerConnect.Server.DTOs;
using CareerConnect.Server.Models;

namespace CareerConnect.Server.Services
{
    public interface IAuthService
    {
        Task<PendingVerificationDto> InitiateLoginAsync(LoginDto loginDto, string? ipAddress = null);
        Task<AuthResponseDto> CompleteLoginAsync(VerifyCodeDto verifyCodeDto);
        Task<PendingVerificationDto> InitiateRegisterAsync(CreateUserDto createUserDto, string? ipAddress = null);
        Task<AuthResponseDto> FinalizeRegisterWithVerificationAsync(CreateUserWithCodeDto createUserDto);
        Task<AuthResponseDto> GoogleLoginAsync(GoogleLoginDto googleLoginDto);
        Task ResendVerificationCodeAsync(ResendCodeDto resendCodeDto, string? ipAddress = null);
        Task<AuthResponseDto> SocialLoginAsync(SocialLoginDto socialLoginDto);
    }
}