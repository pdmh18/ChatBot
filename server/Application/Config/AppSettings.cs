namespace Application.Config;

public class AppSettings
{
    public JwtSettings Jwt { get; set; } = new();
    public AiServerSettings AiServer { get; set; } = new();
}

public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 60;
}

public class AiServerSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ChatEndpoint { get; set; } = "/api/chat";
    public string RiskEndpoint { get; set; } = "/api/analyze-risk";
}
