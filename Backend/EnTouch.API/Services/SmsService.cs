using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace EnTouch.API.Services
{
    public class SmsService : ISmsService
    {
        private readonly IConfiguration _config;

        public SmsService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendPasswordResetOtpAsync(string phoneNumber, string otp)
        {
            var settings = _config.GetSection("TwilioSettings");

            TwilioClient.Init(
                settings["AccountSid"],
                settings["AuthToken"]);

            await MessageResource.CreateAsync(
                body: $"EnTouch: You Verfication Code Is {otp}. Valid For 10 Mins Only.",
                from: new Twilio.Types.PhoneNumber(settings["FromPhone"]),
                to: new Twilio.Types.PhoneNumber(phoneNumber)
            );
        }
    }
}