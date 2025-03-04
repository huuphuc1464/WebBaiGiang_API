using Microsoft.Extensions.Caching.Memory;
using System;

public class OtpService
{
    private readonly IMemoryCache _cache;

    public OtpService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public void StoreOtp(string email, string otp)
    {
        _cache.Set(email, otp, TimeSpan.FromMinutes(5)); // OTP có hiệu lực 5 phút
    }

    public bool ValidateOtp(string email, string otp)
    {
        return _cache.TryGetValue(email, out string storedOtp) && storedOtp == otp;
    }
    public void RemoveOtp(string email)
    {
        _cache.Remove(email);
    }

}
