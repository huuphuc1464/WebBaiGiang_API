namespace WebBaiGiangAPI.Models
{
    public interface IJwtService
    {
        Dictionary<string, string> GetTokenInfoFromToken(string token);
        string GetToken (string token);
        public bool IsValidToken(string token);
    }
}
