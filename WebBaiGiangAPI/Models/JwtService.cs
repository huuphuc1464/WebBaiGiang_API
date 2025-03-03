using Jose;
using System.Text;
namespace WebBaiGiangAPI.Models
{
    public class JwtService : IJwtService
    {
        private readonly string _secretKey; 
        private readonly IConfiguration _configuration;


        public JwtService(IConfiguration configuration)
        {
            _secretKey = configuration["Jwt:Key"] ?? throw new Exception("JWT Key không được tìm thấy!");
            _configuration = configuration;
        }

        public Dictionary<string, string> GetTokenInfoFromToken(string token)
        {
            try
            {
                var key = Encoding.UTF8.GetBytes(_secretKey);
                var payload = JWT.Decode<Dictionary<string, object>>(token, key, JwsAlgorithm.HS256);

                var tokenInfo = new Dictionary<string, string>();
                foreach (var kvp in payload)
                {
                    tokenInfo[kvp.Key] = kvp.Value?.ToString();
                }

                return tokenInfo;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi khi giải mã JWT: " + ex.Message);
                return null;
            }
        }
        public string GetToken(string tokenString)
        {
            if (string.IsNullOrEmpty(tokenString))
            {
                return null;
            }
            var token = tokenString.Split(" ").Last();
            if (!tokenString.StartsWith("Bearer ") || !IsValidToken(token))
            {
                return null;
            }
            return token;
        }
        public bool IsValidToken(string token)
        {
            try
            {
                var keyString = _configuration["Jwt:Key"];
                var key = Encoding.UTF8.GetBytes(keyString);

                var payload = JWT.Decode<Dictionary<string, object>>(token, key, JwsAlgorithm.HS256);

                string issuer = payload.ContainsKey("iss") ? payload["iss"].ToString() : null;
                string audience = payload.ContainsKey("aud") ? payload["aud"].ToString() : null;

                var validIssuer = _configuration["Jwt:Issuer"];
                var validAudience = _configuration["Jwt:Audience"];

                if (issuer != validIssuer || audience != validAudience)
                    return false;

                var now = DateTime.UtcNow;

                if (payload.ContainsKey("exp") && long.TryParse(payload["exp"].ToString(), out long expTime) &&
                    DateTimeOffset.FromUnixTimeSeconds(expTime) < now)
                    return false; // Token hết hạn

                if (payload.ContainsKey("nbf") && long.TryParse(payload["nbf"].ToString(), out long nbfTime) &&
                    DateTimeOffset.FromUnixTimeSeconds(nbfTime) > now)
                    return false; // Token chưa hợp lệ

                return true;
            }
            catch
            {
                return false;
            }
        }

    }
}
