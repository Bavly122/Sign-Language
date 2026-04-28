namespace EnTouch.API.Services
{
    public interface ISmsService
    {
        Task SendPasswordResetOtpAsync(string phoneNumber, string otp);
    }
}