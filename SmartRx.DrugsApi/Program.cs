using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using SmartRx.Data;
using SmartRx.Domain;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var dataDir = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataDir);
var conn = builder.Configuration.GetConnectionString("Default")!.Replace("App_Data", dataDir);

builder.Services.AddDbContext<SmartRxDbContext>(opt => opt.UseSqlite(conn));

var jwtKey = builder.Configuration["Jwt:Key"]!;
var issuer = builder.Configuration["Jwt:Issuer"];
var audience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SmartRx Drugs API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SmartRxDbContext>();
    db.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/drugs", async (SmartRxDbContext db) =>
    await db.Drugs.AsNoTracking().ToListAsync());

app.MapGet("/api/drugs/{id:int}", async (int id, SmartRxDbContext db) =>
    await db.Drugs.FindAsync(id) is Drug d ? Results.Ok(d) : Results.NotFound());

app.MapPost("/api/drugs", [Authorize(Roles = "Admin")] async (Drug drug, SmartRxDbContext db) =>
{
    db.Drugs.Add(drug);
    await db.SaveChangesAsync();
    return Results.Created($"/api/drugs/{drug.Id}", drug);
});

app.MapPut("/api/drugs/{id:int}", [Authorize(Roles = "Admin")] async (int id, Drug updated, SmartRxDbContext db) =>
{
    var d = await db.Drugs.FindAsync(id);
    if (d is null) return Results.NotFound();
    d.BrandName = updated.BrandName;
    d.Manufacturer = updated.Manufacturer;
    d.Ingredients = updated.Ingredients;
    d.DosageInstruction = updated.DosageInstruction;
    d.ManufacturedDate = updated.ManufacturedDate;
    d.ExpiryDate = updated.ExpiryDate;
    d.Price = updated.Price;
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDelete("/api/drugs/{id:int}", [Authorize(Roles = "Admin")] async (int id, SmartRxDbContext db) =>
{
    var d = await db.Drugs.FindAsync(id);
    if (d is null) return Results.NotFound();
    db.Drugs.Remove(d);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// Search drugs (user/admin must be logged in)
app.MapGet("/api/drugs/search", [Authorize] async (string? query, SmartRxDbContext db) =>
{
    Console.WriteLine("🔎 Incoming query: " + (query ?? "<null>"));

    if (string.IsNullOrWhiteSpace(query))
    {
        var all = await db.Drugs.AsNoTracking().ToListAsync();
        Console.WriteLine($"Returned {all.Count} drugs (all records).");
        return Results.Ok(all);
    }

    var like = $"%{query.ToLower()}%";

    var results = await db.Drugs
        .Where(d =>
            EF.Functions.Like(d.BrandName.ToLower(), like) ||
            EF.Functions.Like(d.Manufacturer.ToLower(), like))
        .AsNoTracking()
        .ToListAsync();

    Console.WriteLine($"DB returned {results.Count} drugs before filtering Ingredients/Expiry.");

   

    Console.WriteLine($"Final result count = {results.Count}");

    return Results.Ok(results);
});





// Get full details of a drug (logged-in users)
app.MapGet("/api/drugs/{id:int}/details", [Authorize] async (int id, SmartRxDbContext db) =>
{
    var d = await db.Drugs.AsNoTracking().FirstOrDefaultAsync(drug => drug.Id == id);
    if (d is null) return Results.NotFound();

    return Results.Ok(new
    {
        d.Id,
        d.BrandName,
        d.Manufacturer,
        d.Ingredients,
        d.DosageInstruction,
        d.ManufacturedDate,
        d.ExpiryDate,
        d.Price
    });
});



app.Run();
