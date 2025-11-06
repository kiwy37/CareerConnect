using CareerConnect.Server.Services;

namespace CareerConnect.Server.Services
{
    public class VerificationCodeCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<VerificationCodeCleanupService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromHours(1); // Rulează la fiecare oră

        public VerificationCodeCleanupService(
            IServiceProvider serviceProvider,
            ILogger<VerificationCodeCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Verification Code Cleanup Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupExpiredCodesAsync();
                    await Task.Delay(_interval, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while cleaning up expired verification codes");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }

            _logger.LogInformation("Verification Code Cleanup Service stopped");
        }

        private async Task CleanupExpiredCodesAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var verificationService = scope.ServiceProvider.GetRequiredService<IVerificationService>();

            await verificationService.CleanupExpiredCodesAsync();
        }
    }
}