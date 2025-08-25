using Microsoft.EntityFrameworkCore;
using SmartRx.Data;
using SmartRx.Domain;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var dataDir = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataDir);

var conn = builder.Configuration.GetConnectionString("Default")!.Replace("App_Data", dataDir);

builder.Services.AddDbContext<SmartRxDbContext>(opt => opt.UseSqlite(conn));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SmartRx Auth API", Version = "v1" });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SmartRxDbContext>();
    db.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/api/auth/register", async (RegisterRequest req, SmartRxDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
        return Results.BadRequest("Username and password required");

    if (await db.Users.AnyAsync(u => u.Username == req.Username))
        return Results.Conflict("User exists");

    var salt = SimpleHasher.CreateSalt();
    var hash = SimpleHasher.Hash(req.Password, salt);

    var user = new User { Username = req.Username, Salt = salt, PasswordHash = hash, Role = string.IsNullOrWhiteSpace(req.Role) ? "User" : req.Role };
    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Created($"/api/users/{user.Id}", new { user.Id, user.Username, user.Role });
});

app.MapPost("/api/auth/login", async (LoginRequest req, SmartRxDbContext db) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == req.Username);
    if (user is null || !SimpleHasher.Verify(req.Password, user.Salt, user.PasswordHash))
        return Results.Unauthorized();

    var jwt = JwtTokens.Create(app.Configuration, user);
    return Results.Ok(new LoginResponse(jwt, user.Username, user.Role));
});

app.Run();

public static class JwtTokens
{
    public static string Create(IConfiguration cfg, User user)
    {
        var key = cfg["Jwt:Key"]!;
        var issuer = cfg["Jwt:Issuer"]!;
        var audience = cfg["Jwt:Audience"]!;
        var expiry = int.Parse(cfg["Jwt:ExpiryMinutes"]!);

        var secKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(key));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(secKey, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.Username),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, user.Role)
        };

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiry),
            signingCredentials: creds);

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }
}
