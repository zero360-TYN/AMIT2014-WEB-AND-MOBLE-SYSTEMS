using Bogus;
using Microsoft.EntityFrameworkCore;
using Assignment.Models;

namespace Assignment.Data;

public static class DbBookingSeeder
{
    public static void Initialize(DB db)
    {
        // If bookings already exist, return
        if (db.Bookings.Any()) return;

        // Ensure prerequisite data is seeded
        DBUsersSeder.Initialize(db);
        DBRoomSeeder.Initialize(db);
        DbServiceSeeder.Initialize(db);

        var memberAccounts = db.Accounts
            .Include(a => a.AccountDetail)
                .ThenInclude(ad => ad.Role)
            .Include(a => a.AccountStatus)
            .Where(a => a.AccountDetail.Role.RoleName == "Member" && a.AccountStatus.Status != AccountStatusType.deleted)
            .ToList();

        var staffs = db.Staffs
            .Include(s => s.Account)
                .ThenInclude(a => a.AccountStatus)
            .Where(s => s.Account.AccountStatus.Status == AccountStatusType.active)
            .ToList();

        var rooms = db.Rooms
            .Include(r => r.RoomType)
            .Where(r => !r.IsDeleted)
            .ToList();

        var services = db.Services
            .Where(s => !s.IsDeleted)
            .ToList();

        if (!memberAccounts.Any() || !staffs.Any() || !rooms.Any() || !services.Any())
        {
            return;
        }

        var faker = new Faker();
        var pokemonList = new[]
        {
            "Pikachu", "Charmander", "Bulbasaur", "Squirtle", "Eevee",
            "Snorlax", "Gengar", "Lucario", "Jigglypuff", "Togepi",
            "Psyduck", "Meowth", "Vulpix", "Dragonite", "Mewtwo",
            "Gardevoir", "Chikorita", "Cyndaquil", "Totodile", "Piplup"
        };

        var notesList = new[]
        {
            "Requires gentle brushing.",
            "Allergic to standard fragrance shampoo.",
            "First time visit, might be nervous.",
            "Needs extra attention to paws.",
            "Prefers warm water bath.",
            null,
            null
        };

        var bookings = new List<Booking>();

        for (int i = 0; i < 25; i++)
        {
            var service = faker.PickRandom(services);

            // Match room with same ServiceCategoryId to maintain category consistency
            var compatibleRooms = rooms.Where(r => r.RoomType.ServiceCategoryId == service.ServiceCategoryId).ToList();
            var room = compatibleRooms.Any() ? faker.PickRandom(compatibleRooms) : faker.PickRandom(rooms);

            var account = faker.PickRandom(memberAccounts);
            var staff = faker.PickRandom(staffs);

            // Generate dates: half in the past 14 days, half in the next 14 days
            var isPast = i % 2 == 0;
            var startTime = isPast
                ? faker.Date.Recent(14)
                : faker.Date.Soon(14);

            // Round to the nearest hour for clean schedule
            startTime = new DateTime(startTime.Year, startTime.Month, startTime.Day, startTime.Hour, 0, 0);
            var endTime = startTime.AddMinutes(service.DurationMinutes);

            // Assign logical status based on date
            BookingStatus status;
            if (isPast)
            {
                status = faker.Random.Bool(0.85f) ? BookingStatus.completed : BookingStatus.cancelled;
            }
            else
            {
                status = faker.Random.Bool(0.7f) ? BookingStatus.confirmed : BookingStatus.pending;
            }

            var totalPrice = service.Price + (room.RoomType?.BasePrice ?? 0m);

            var booking = new Booking
            {
                AccountId = account.Id,
                StaffId = staff.Id,
                RoomId = room.Id,
                ServiceId = service.Id,
                StartTime = startTime,
                EndTime = endTime,
                Status = status,
                TotalPrice = totalPrice,
                CreatedAt = startTime.AddDays(-faker.Random.Int(1, 5)),
                BookingDetail = new BookingDetail
                {
                    PokemonName = faker.PickRandom(pokemonList),
                    Notes = faker.PickRandom(notesList)
                }
            };

            bookings.Add(booking);
        }

        db.Bookings.AddRange(bookings);
        db.SaveChanges();
    }
}
