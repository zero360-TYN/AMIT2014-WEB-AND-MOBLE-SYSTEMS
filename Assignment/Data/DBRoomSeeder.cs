using Bogus;
using Assignment.Models;

namespace Assignment.Data;

public static class DBRoomSeeder
{
    public static void Initialize(DB db)
    {
        // If rooms already exist, we assume data is already seeded
        if (db.Rooms.Any()) return;

        // 1. Seed Service Categories (Required for RoomType)
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

        // 2. Seed Room Types (Required for Room)
        if (!db.RoomTypes.Any())
        {
            var roomTypeFaker = new Faker<RoomType>()
                .RuleFor(rt => rt.ServiceCategoryId, f => f.PickRandom(categoryIds))
                .RuleFor(rt => rt.Name, f => f.Commerce.ProductName() + " Room")
                .RuleFor(rt => rt.Description, f => f.Lorem.Paragraph())
                .RuleFor(rt => rt.BasePrice, f => Math.Round(f.Random.Decimal(50m, 500m), 2))
                .RuleFor(rt => rt.CreatedAt, f => f.Date.Past(1));

            var fakeRoomTypes = roomTypeFaker.Generate(10);
            db.RoomTypes.AddRange(fakeRoomTypes);
            db.SaveChanges();
        }

        var roomTypeIds = db.RoomTypes.Select(rt => rt.Id).ToList();

        // 3. Seed Rooms
        var roomNumberCounter = 101;
        var roomFaker = new Faker<Room>()
            .RuleFor(r => r.RoomTypeId, f => f.PickRandom(roomTypeIds))
            .RuleFor(r => r.RoomNumber, f => (roomNumberCounter++).ToString())
            .RuleFor(r => r.IsDeleted, f => f.Random.Bool(0.1f));

        var fakeRooms = roomFaker.Generate(30);
        db.Rooms.AddRange(fakeRooms);
        db.SaveChanges();
    }
}
