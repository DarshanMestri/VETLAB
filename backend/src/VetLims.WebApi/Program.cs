using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using Serilog;
using VetLims.Infrastructure;
using VetLims.Infrastructure.Identity;
using VetLims.Infrastructure.Seed;
using VetLims.WebApi.Common;
using VetLims.WebApi.Reporting;
using VetLims.WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = LicenseType.Community;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// 1) REGISTER SERVICES (everything goes here, BEFORE Build)
builder.Services.AddDbContext<VetLimsDbContext>(opts =>
{
    var cs = builder.Configuration.GetConnectionString("DefaultConnection")
             ?? "Host=localhost;Port=5432;Database=vet_lims;Username=vet_user;Password=p@ssw0rd";
    opts.UseNpgsql(cs);
});

builder.Services
    .AddIdentity<AppUser, IdentityRole<int>>()
    .AddEntityFrameworkStores<VetLimsDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "Bearer";
    options.DefaultChallengeScheme = "Bearer";
}).AddJwtBearer("Bearer", options =>
{
    var cfg = builder.Configuration.GetSection("Jwt");
    options.TokenValidationParameters = new()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ValidIssuer = cfg["Issuer"],
        ValidAudience = cfg["Audience"],
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(cfg["Key"] ?? "dev-key-change-me"))
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireManager", policy => policy.RequireRole("Admin", "Manager"));
});

builder.Services.AddCors(o => o.AddPolicy("Dev", p =>
    p.WithOrigins("http://localhost:4200", "https://localhost:4200")
     .AllowAnyHeader()
     .AllowAnyMethod()
     .AllowCredentials()
));

builder.Services.AddControllers();           // <-- must be BEFORE Build
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<DataSeeder>();    // <-- also BEFORE Build
builder.Services.AddScoped<VetLims.Infrastructure.Services.PricingService>();
builder.Services.AddScoped<VetLims.Infrastructure.Services.BarcodeService>();
builder.Services.AddScoped<VetLims.Infrastructure.Services.ReferenceRangeService>();
builder.Services.AddScoped<ResultReportService>();
builder.Services.AddScoped<IWhatsAppProvider, WhatsAppProvider>();
builder.Services.AddScoped<WhatsAppMessagingService>();



// 2) BUILD THE APP
var app = builder.Build();

// 3) MIDDLEWARE PIPELINE
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("Dev");
app.UseApiExceptionMiddleware();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 4) DB MIGRATION + SEED (AFTER Build, BEFORE Run)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<VetLimsDbContext>();
    await db.Database.MigrateAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    await seeder.SeedAsync();   // prints the “Default Admin user created” message
}

// 5) RUN
app.Run();
