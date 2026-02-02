using ECommerce.API.Data;
using ECommerce.API.Models;
using Microsoft.EntityFrameworkCore;

public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, IServiceProvider services)
    {
        if (await context.Users.AnyAsync()) return;

        // Seed Admin User
        var adminPassword = BCrypt.Net.BCrypt.HashPassword("Admin123!");
        var admin = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@ecommerce.com",
            PasswordHash = adminPassword,
            FirstName = "Admin",
            LastName = "User",
            Role = "Admin",
            IsActive = true
        };
        context.Users.Add(admin);

        // Seed Categories
        var categories = new[]
        {
            new Category { Id = Guid.NewGuid(), Name = "Electronics", Slug = "electronics", DisplayOrder = 1 },
            new Category { Id = Guid.NewGuid(), Name = "Clothing", Slug = "clothing", DisplayOrder = 2 },
            new Category { Id = Guid.NewGuid(), Name = "Books", Slug = "books", DisplayOrder = 3 },
            new Category { Id = Guid.NewGuid(), Name = "Home & Garden", Slug = "home-garden", DisplayOrder = 4 }
        };
        context.Categories.AddRange(categories);

        // Seed Products
        var products = new[]
        {
            new Product { Id = Guid.NewGuid(), Name = "Wireless Headphones", Slug = "wireless-headphones",
                Description = "High-quality Bluetooth headphones with noise cancellation",
                Price = 79.99m, StockQuantity = 50, CategoryId = categories[0].Id,
                ImageUrl = "https://picsum.photos/seed/headphones/400/400" },

            new Product { Id = Guid.NewGuid(), Name = "Gaming Mouse", Slug = "gaming-mouse",
                Description = "Ergonomic gaming mouse with RGB lighting",
                Price = 49.99m, StockQuantity = 100, CategoryId = categories[0].Id,
                ImageUrl = "https://picsum.photos/seed/mouse/400/400" },

            new Product { Id = Guid.NewGuid(), Name = "Cotton T-Shirt", Slug = "cotton-tshirt",
                Description = "Comfortable 100% cotton t-shirt",
                Price = 19.99m, StockQuantity = 200, CategoryId = categories[1].Id,
                ImageUrl = "https://picsum.photos/seed/tshirt/400/400" },

            new Product { Id = Guid.NewGuid(), Name = "Programming Book", Slug = "programming-book",
                Description = "Learn advanced C# programming techniques",
                Price = 39.99m, StockQuantity = 75, CategoryId = categories[2].Id,
                ImageUrl = "https://picsum.photos/seed/book/400/400" }
        };
        context.Products.AddRange(products);

        await context.SaveChangesAsync();
    }
}