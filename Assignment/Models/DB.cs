using Microsoft.EntityFrameworkCore;

namespace Assignment.Models;

#nullable disable warnings
public class DB(DbContextOptions options) : DbContext(options)
{

}
