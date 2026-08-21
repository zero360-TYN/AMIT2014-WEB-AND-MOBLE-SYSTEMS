global using Assignment.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Assignment.Data;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddSqlServer<DB>($@"
    Data Source=(LocalDB)\MSSQLLocalDB;
    AttachDbFilename={builder.Environment.ContentRootPath}\pokemonDB.mdf;
    Initial Catalog=AssignmentDB_v2;
    Integrated Security=True;
");

builder.Services
    .AddAuthentication(options =>
     {
         options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme; // used to save the cookies of the user to keep login in the system
         options.DefaultChallengeScheme = "Google";
     })
    .AddCookie()
    .AddGoogle("Google", options =>
     {
         options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
         options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
     });

var app = builder.Build();
//DbSeeder 
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DB>();
    DbSeeder.Initialize(db);
}
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultControllerRoute();

app.Run();
