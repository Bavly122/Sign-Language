namespace EnTouch.API.Services
{
    public interface IEmailService
    {
        Task SendPasswordResetOtpAsync(string toEmail, string fullName, string otp);
    }
}