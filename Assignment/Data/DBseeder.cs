using Bogus;
using Assignment.Models;

namespace Assignment.Data;

public static class DbSeeder
{
    public static void Initialize(DB db)
    {
        if (db.Accounts.Any()) return;

        var staffRole = db.Roles.FirstOrDefault(r => r.RoleName == "Staff");
        if (staffRole == null)
        {
            staffRole = new Role { RoleName = "Staff" };
            db.Roles.Add(staffRole);
        }

        var memberRole = db.Roles.FirstOrDefault(r => r.RoleName == "Member");
        if (memberRole == null)
        {
            memberRole = new Role { RoleName = "Member" };
            db.Roles.Add(memberRole);
        }

        db.SaveChanges();
        //get role id
        int staffId = db.Roles.First(r => r.RoleName == "Staff").Id;
        int memberId = db.Roles.First(r => r.RoleName == "Member").Id;
        var staffAccountFaker = new Faker<Account>()
            .RuleFor(a => a.Provider, f => f.PickRandom<Provider>())
            .RuleFor(a => a.Email, f => f.Internet.Email())
            .RuleFor(a => a.PasswordHash, f => f.Internet.Password(10))
            .RuleFor(a => a.GoogleId, (f, a) => a.Provider != Provider.Local ? f.Random.AlphaNumeric(20) : null)
            .RuleFor(a => a.AccountDetail, (f, a) => new AccountDetail
            {
                RoleId = staffRole.Id,
                Username = f.Internet.UserName(),
                AvatarIcon = f.Internet.Avatar(),
                CreatedAt = f.Date.Past(1)
            })
            .RuleFor(a => a.AccountStatus, (f, a) => new AccountStatus
            {
                Status = f.PickRandom<AccountStatusType>(),
                BlockingReason = f.Random.Bool(0.2f) ? f.Lorem.Sentence() : null
            })
            .RuleFor(a => a.Staff, (f, a) => new Staff());

        var memberAccountFaker = new Faker<Account>()
            .RuleFor(a => a.Provider, f => f.PickRandom<Provider>())
            .RuleFor(a => a.Email, f => f.Internet.Email())
            .RuleFor(a => a.PasswordHash, f => f.Internet.Password(10))
            .RuleFor(a => a.GoogleId, (f, a) => a.Provider != Provider.Local ? f.Random.AlphaNumeric(20) : null)
            .RuleFor(a => a.AccountDetail, (f, a) => new AccountDetail
            {
                RoleId = memberId,
                Username = f.Internet.UserName(),
                AvatarIcon = f.Internet.Avatar(),
                CreatedAt = f.Date.Past(1)
            })
            .RuleFor(a => a.AccountStatus, (f, a) => new AccountStatus
            {
                Status = f.PickRandom<AccountStatusType>(),
                BlockingReason = f.Random.Bool(0.2f) ? f.Lorem.Sentence() : null
            });

        var fakeAccounts = staffAccountFaker.Generate(20);
        var fakeMemberAccounts = memberAccountFaker.Generate(30);

        db.Accounts.AddRange(fakeAccounts);
        db.Accounts.AddRange(fakeMemberAccounts);
        db.SaveChanges();
    }
}