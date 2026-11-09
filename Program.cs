using lucho_sprintime.Data;
using lucho_sprintime.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "SprinTime.Auth";
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            var target = context.Request.Path.StartsWithSegments("/Registration") ||
                context.Request.Path.StartsWithSegments("/Cyclist")
                ? "/Account/CyclistLogin" : "/Account/Login";
            context.Response.Redirect(target + "?returnUrl=" + Uri.EscapeDataString(context.Request.Path + context.Request.QueryString));
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddScoped<IPasswordHasher<lucho_sprintime.Models.Cyclist>, PasswordHasher<lucho_sprintime.Models.Cyclist>>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IMercadoPagoService, MercadoPagoService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<IQrCodeService, QrCodeService>();
builder.Services.AddScoped<IRankingService, RankingService>();
builder.Services.AddHostedService<RegistrationClosureHostedService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// TEMPORAL: desactivar redirección HTTPS hasta emitir certificado SSL para sprintime.online.
// app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.EnsureCreatedAsync();
    // EnsureCreated no actualiza tablas existentes: agregar solamente las columnas faltantes.
    await db.Database.ExecuteSqlRawAsync("""
        IF OBJECT_ID(N'dbo.Cyclists', N'U') IS NOT NULL
        BEGIN
            IF COL_LENGTH('dbo.Cyclists', 'PasswordHash') IS NULL ALTER TABLE dbo.Cyclists ADD PasswordHash nvarchar(500) NULL;
            IF COL_LENGTH('dbo.Cyclists', 'BloodType') IS NULL ALTER TABLE dbo.Cyclists ADD BloodType nvarchar(10) NULL;
            IF COL_LENGTH('dbo.Cyclists', 'HasInsurance') IS NULL ALTER TABLE dbo.Cyclists ADD HasInsurance bit NOT NULL CONSTRAINT DF_Cyclists_HasInsurance DEFAULT 0;
            IF COL_LENGTH('dbo.Cyclists', 'InsuranceNumber') IS NULL ALTER TABLE dbo.Cyclists ADD InsuranceNumber nvarchar(120) NULL;
            IF COL_LENGTH('dbo.Cyclists', 'SecondEmergencyPhone') IS NULL ALTER TABLE dbo.Cyclists ADD SecondEmergencyPhone nvarchar(40) NULL;
            IF COL_LENGTH('dbo.Cyclists', 'MedicalConditions') IS NULL ALTER TABLE dbo.Cyclists ADD MedicalConditions nvarchar(1000) NULL;
            IF COL_LENGTH('dbo.Cyclists', 'MedicalNotes') IS NULL ALTER TABLE dbo.Cyclists ADD MedicalNotes nvarchar(1000) NULL;
            IF COL_LENGTH('dbo.Cyclists', 'MedicalConsent') IS NULL ALTER TABLE dbo.Cyclists ADD MedicalConsent bit NOT NULL CONSTRAINT DF_Cyclists_MedicalConsent DEFAULT 0;
        END
        """);
    await SeedData.InitializeAsync(db);
}

app.Run();
