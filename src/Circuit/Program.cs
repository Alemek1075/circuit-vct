using Circuit.Data;
using Circuit.Realtime;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Xml.Linq;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddSingleton<ScorePublisher>();

var connectionString = builder.Configuration.GetConnectionString("Circuit") ?? "Data Source=data/circuit.db";
if (connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
{
    var databaseFile = connectionString["Data Source=".Length..].Split(';')[0];
    var fullPath = Path.GetFullPath(databaseFile, builder.Environment.ContentRootPath);
    Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
    connectionString = $"Data Source={fullPath}";
}
builder.Services.AddDbContext<CircuitDbContext>(options => options.UseSqlite(connectionString));

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    await SeedData.ApplyAsync(scope.ServiceProvider.GetRequiredService<CircuitDbContext>());
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
if (!app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        if (context.Request.Path.StartsWithSegments("/api") &&
            !HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "Запис доступний лише в локальному режимі розробки." });
            return;
        }
        await next(context);
    });
}
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/robots.txt", (HttpContext context) =>
    Results.Text($"User-agent: *\nAllow: /\nDisallow: /studio\nSitemap: {context.Request.Scheme}://{context.Request.Host}/sitemap.xml\n", "text/plain"));
app.MapGet("/sitemap.xml", async (CircuitDbContext db, HttpContext context) =>
{
    var origin = $"{context.Request.Scheme}://{context.Request.Host}";
    XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
    var paths = new List<string> { "/" };
    paths.AddRange((await db.Tournaments.AsNoTracking().Where(t => t.Slug != "paris-2025")
        .Select(t => t.Slug).ToListAsync()).Select(slug => $"/?tournament={Uri.EscapeDataString(slug)}"));
    paths.AddRange((await db.Teams.AsNoTracking().Select(t => t.Id).ToListAsync()).Select(id => $"/team/{id}"));
    paths.AddRange((await db.Matches.AsNoTracking().Select(m => m.Id).ToListAsync()).Select(id => $"/match/{id}"));
    var document = new XDocument(new XElement(ns + "urlset",
        paths.Select(path => new XElement(ns + "url", new XElement(ns + "loc", origin + path)))));
    return Results.Text(document.ToString(SaveOptions.DisableFormatting), "application/xml", Encoding.UTF8);
});
app.MapHub<ScoreHub>("/score-hub");
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();

public partial class Program;
