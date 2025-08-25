namespace SmartRx.Domain;

public record LoginRequest(string Username, string Password);
public record RegisterRequest(string Username, string Password, string Role);
public record LoginResponse(string Token, string Username, string Role);
