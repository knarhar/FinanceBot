using Microsoft.EntityFrameworkCore;

namespace FinanceBot.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
}