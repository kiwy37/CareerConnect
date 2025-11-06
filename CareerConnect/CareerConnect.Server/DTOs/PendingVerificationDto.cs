namespace CareerConnect.Server.DTOs
{
    public class PendingVerificationDto
    {
        public string Email { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool RequiresVerification { get; set; } = true;
    }
}
