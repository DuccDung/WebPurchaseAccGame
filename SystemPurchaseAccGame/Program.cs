using Microsoft.EntityFrameworkCore;
using System;
using SystemPurchaseAccGame.Models;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<GameAccShopContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("sql_connection")));
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();
app.UseStaticFiles();
app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=HomePage}/{id?}")
    .WithStaticAssets();


app.Run();
