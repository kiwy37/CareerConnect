namespace CareerConnect.Server.Services
{
    public interface IEmailService
    {
        Task SendVerificationCodeAsync(string email, string code, string verificationType);
    }
}