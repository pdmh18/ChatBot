namespace Application.Common.DTOs;

public record LoginRequest(string Email, string Password);
public record RegisterRequest(string Username, string Email, string Password);
public record AuthResponse(string Token, string RefreshToken, UserDto User);
