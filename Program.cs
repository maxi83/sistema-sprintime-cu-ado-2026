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
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Events.OnRedirectToLogin = context =>
        {
            var isCyclistArea = context.Request.Path.StartsWithSegments("/Registration") ||
                context.Request.Path.StartsWithSegments("/Cyclist");
            var target = isCyclistArea ? "/Account/CyclistLogin" : "/Account/Login";
            context.Response.Redirect(target + "?returnUrl=" + Uri.EscapeDataString(context.Request.Path + context.Request.QueryString));
            return Task.CompletedTask;
        };
        // Un organizador autenticado no debe recibir "Acceso denegado" al intentar
        // inscribir un corredor: se le ofrece ingresar o crear una cuenta de ciclista.
        options.Events.OnRedirectToAccessDenied = context =>
        {
            var isCyclistArea = context.Request.Path.StartsWithSegments("/Registration") ||
                context.Request.Path.StartsWithSegments("/Cyclist");
            if (isCyclistArea)
            {
                var returnUrl = Uri.EscapeDataString(context.Request.Path + context.Request.QueryString);
                context.Response.Redirect("/Account/CyclistLogin?returnUrl=" + returnUrl + "&area=cyclist");
            }
            else
            {
                context.Response.Redirect("/Account/AccessDenied");
            }
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

try
{
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
            IF COL_LENGTH('dbo.Cyclists', 'SprintimeBib') IS NULL ALTER TABLE dbo.Cyclists ADD SprintimeBib nvarchar(20) NULL;
        END
        """);
    // Run in a separate SQL batch: SQL Server compiles CREATE INDEX only after the new column exists.
    await db.Database.ExecuteSqlRawAsync("""
        IF OBJECT_ID(N'dbo.Cyclists', N'U') IS NOT NULL
           AND COL_LENGTH('dbo.Cyclists', 'SprintimeBib') IS NOT NULL
           AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Cyclists') AND name = N'IX_Cyclists_SprintimeBib')
        BEGIN
            CREATE UNIQUE INDEX IX_Cyclists_SprintimeBib ON dbo.Cyclists(SprintimeBib) WHERE SprintimeBib IS NOT NULL;
        END
        """);
    await db.Database.ExecuteSqlRawAsync("""
        IF OBJECT_ID(N'dbo.SocialLinks', N'U') IS NULL
        BEGIN
            CREATE TABLE dbo.SocialLinks (
                Id int NOT NULL PRIMARY KEY,
                FacebookUrl nvarchar(max) NULL,
                InstagramUrl nvarchar(max) NULL,
                WhatsappGroupUrl nvarchar(max) NULL,
                FacebookEnabled bit NOT NULL,
                InstagramEnabled bit NOT NULL,
                WhatsappGroupEnabled bit NOT NULL,
                RankingEnabled bit NOT NULL DEFAULT 0
            );
        END
        """);
    await db.Database.ExecuteSqlRawAsync("""
        IF OBJECT_ID(N'dbo.SocialLinks', N'U') IS NOT NULL
            AND COL_LENGTH('dbo.SocialLinks', 'RankingEnabled') IS NULL
            ALTER TABLE dbo.SocialLinks ADD RankingEnabled bit NOT NULL CONSTRAINT DF_SocialLinks_RankingEnabled DEFAULT 0;
        IF OBJECT_ID(N'dbo.Events', N'U') IS NOT NULL
        BEGIN
            IF COL_LENGTH('dbo.Events', 'PhotoAlbumUrl') IS NULL ALTER TABLE dbo.Events ADD PhotoAlbumUrl nvarchar(500) NULL;
            IF COL_LENGTH('dbo.Events', 'GalleryImageUrls') IS NULL ALTER TABLE dbo.Events ADD GalleryImageUrls nvarchar(4000) NULL;
        END
        IF OBJECT_ID(N'dbo.HistoricPodiums', N'U') IS NULL
        BEGIN
            CREATE TABLE dbo.HistoricPodiums (
                Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                EventId int NOT NULL REFERENCES dbo.Events(Id),
                Category nvarchar(120) NOT NULL,
                Place int NOT NULL,
                CyclistName nvarchar(160) NOT NULL,
                CONSTRAINT CK_HistoricPodiums_Place CHECK (Place BETWEEN 1 AND 3)
            );
            CREATE UNIQUE INDEX IX_HistoricPodiums_EventId_Category_Place
            ON dbo.HistoricPodiums(EventId, Category, Place);
        END
        """);
    await SeedData.InitializeAsync(db);
}

}
catch (Exception startupError)
{
    // Persistent diagnostics for IIS/SmarterASP startup failures.
    // Log lives outside wwwroot so it cannot be downloaded publicly.
    try
    {
        var logsDirectory = Path.Combine(app.Environment.ContentRootPath, "logs");
        Directory.CreateDirectory(logsDirectory);
        var report = $"[{DateTimeOffset.UtcNow:O}] Sprintime startup failure:{Environment.NewLine}{startupError}{Environment.NewLine}{Environment.NewLine}";
        await File.AppendAllTextAsync(Path.Combine(logsDirectory, "sprintime-startup-error.txt"), report);
    }
    catch (Exception writeError)
    {
        Console.Error.WriteLine($"Could not write startup report: {writeError.Message}");
    }
    Console.Error.WriteLine(startupError);
    throw;
}

app.Run();
