using API.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
  public class DataContext : DbContext
  {
    public DataContext(DbContextOptions options) : base(options)
    {
      // no code...
    }
    public DbSet<AppUser> Users { get; set; }
  }
}