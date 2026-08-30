using Bogus;
using Assignment.Models;

namespace Assignment.Data;

public static class DbServiceSeeder
{
    public static void Initialize(DB db)
    {
        // If services already exist, we assume data is already seeded
        if (db.Services.Any()) return;

        // 1. Ensure Service Categories exist
        if (!db.ServiceCategories.Any())
        {
            var categoryFaker = new Faker<ServiceCategory>()
                .RuleFor(c => c.Name, f => f.Commerce.Department())
                .RuleFor(c => c.Description, f => f.Lorem.Sentence());

            var fakeCategories = categoryFaker.Generate(5);
            db.ServiceCategories.AddRange(fakeCategories);
            db.SaveChanges();
        }

        var categoryIds = db.ServiceCategories.Select(c => c.Id).ToList();

        // 2. Seed Services
        var serviceFaker = new Faker<Service>()
            .RuleFor(s => s.ServiceCategoryId, f => f.PickRandom(categoryIds))
            .RuleFor(s => s.Name, f => f.Commerce.ProductName() + " Service")
            .RuleFor(s => s.Description, f => f.Lorem.Paragraph())
            .RuleFor(s => s.Price, f => Math.Round(f.Random.Decimal(20m, 300m), 2))
            .RuleFor(s => s.DurationMinutes, f => f.PickRandom(15, 30, 45, 60, 90, 120))
            .RuleFor(s => s.IsDeleted, f => f.Random.Bool(0.05f))
            .RuleFor(s => s.CreatedAt, f => f.Date.Past(1));

        var fakeServices = serviceFaker.Generate(25);
        db.Services.AddRange(fakeServices);
        db.SaveChanges();
    }
}
