using CareerConnect.Server.DTOs;
using CareerConnect.Server.Models;
using CareerConnect.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace CareerConnect.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login/initiate")]
        public async Task<ActionResult<PendingVerificationDto>> InitiateLogin([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var response = await _authService.InitiateLoginAsync(loginDto, ipAddress);
            return Ok(response);
        }

        [HttpPost("login/complete")]
        public async Task<ActionResult<AuthResponseDto>> CompleteLogin([FromBody] VerifyCodeDto verifyCodeDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _authService.CompleteLoginAsync(verifyCodeDto);
            return Ok(response);
        }

        [HttpPost("register/initiate")]
        public async Task<ActionResult<PendingVerificationDto>> InitiateRegister([FromBody] CreateUserDto createUserDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var response = await _authService.InitiateRegisterAsync(createUserDto, ipAddress);
            return Ok(response);
        }

        [HttpPost("register/finalize")]
        public async Task<ActionResult<AuthResponseDto>> FinalizeRegister([FromBody] CreateUserWithCodeDto createUserWithCodeDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _authService.FinalizeRegisterWithVerificationAsync(createUserWithCodeDto);
            return CreatedAtAction(nameof(FinalizeRegister), new { id = response.User.Id }, response);
        }

        [HttpPost("resend-code")]
        public async Task<IActionResult> ResendCode([FromBody] ResendCodeDto resendCodeDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _authService.ResendVerificationCodeAsync(resendCodeDto, ipAddress);

            return Ok(new { message = "Codul de verificare a fost retrimis cu succes" });
        }

        [HttpPost("google-login")]
        public async Task<ActionResult<AuthResponseDto>> GoogleLogin([FromBody] GoogleLoginDto googleLoginDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _authService.GoogleLoginAsync(googleLoginDto);
            return Ok(response);
        }

        [HttpPost("linkedin-login")]
        public async Task<ActionResult<AuthResponseDto>> LinkedInLogin([FromBody] LinkedInLoginDto linkedInLoginDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _authService.LinkedInLoginAsync(linkedInLoginDto);
            return Ok(response);
        }

        [HttpPost("social-login")]
        public async Task<ActionResult<AuthResponseDto>> SocialLogin([FromBody] SocialLoginDto dto)
        {
            var response = await _authService.SocialLoginAsync(dto);
            return Ok(response);
        }

        // New endpoints for forgot password
        [HttpPost("forgot-password")]
        public async Task<ActionResult<PendingVerificationDto>> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var response = await _authService.InitiateForgotPasswordAsync(forgotPasswordDto, ipAddress);
            return Ok(response);
        }

        [HttpPost("verify-reset-code")]
        public async Task<IActionResult> VerifyResetCode([FromBody] VerifyResetCodeDto verifyResetCodeDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var isValid = await _authService.VerifyResetCodeAsync(verifyResetCodeDto);
            return Ok(new { valid = isValid, message = "Codul este valid" });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var success = await _authService.ResetPasswordAsync(resetPasswordDto);
            return Ok(new { success, message = "Parola a fost resetată cu succes" });
        }
    }
}