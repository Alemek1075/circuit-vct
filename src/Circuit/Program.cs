using Circuit.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();

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
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();

public partial class Program;
