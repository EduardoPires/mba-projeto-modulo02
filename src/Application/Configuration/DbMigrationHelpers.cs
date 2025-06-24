﻿using Core.Data;
using Core.Domain.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Core.Configuration;

public static class DbMigrationHelperExtension
{
    public static void UseDbMigrationHelper(this WebApplication app)
    {
        DbMigrationHelpers.EnsureSeedData(app).Wait();
    }
}

public static class DbMigrationHelpers
{
    public static async Task EnsureSeedData(WebApplication serviceScope)
    {
        var services = serviceScope.Services.CreateScope().ServiceProvider;
        await EnsureSeedData(services);
    }

    public static async Task EnsureSeedData(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        if (env.IsDevelopment() || env.IsStaging())
        {
            await context.Database.MigrateAsync();
            await EnsureSeedProducts(context, userManager);
        }
    }

    private static async Task EnsureSeedProducts(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        if (context.Users.Any())
            return;

        var identityUser = new IdentityUser
        {
            Email = "dev@mail.com",
            UserName = "dev@mail.com"
        };

        var result = userManager.CreateAsync(identityUser, "Dev@123").GetAwaiter().GetResult();
        if (!result.Succeeded) return;

        var seller = new Seller
        {
            UserId = Guid.Parse(identityUser.Id)
        };
        await context.Sellers.AddAsync(seller);

        await context.SaveChangesAsync();

        if (context.Categories.Any())
            return;

        var category = new Categoria
        {
            Nome = "Flores",
            Descricao = "Categoria destinada para produtos do tipo flores"
        };

        await context.Categories.AddAsync(category);

        await context.SaveChangesAsync();

        if (context.Products.Any())
            return;

        await context.Products.AddAsync(new Product
        {
            Name = "Rosa",
            Description = "Produto do tipo flores ",
            Price = 25.80m,
            Stock = 100,
            Image = "21.png",

            SellerId = Guid.Parse(identityUser.Id),
            CategoryId = category.Id
        });

        await context.Products.AddAsync(new Product
        {
            Name = "Rosa variação",
            Description = "Produto do tipo flores",
            Price = 15.20m,
            Stock = 150,
            Image = "23.png",

            SellerId = Guid.Parse(identityUser.Id),
            CategoryId = category.Id
        });

        await context.SaveChangesAsync();
    }
}