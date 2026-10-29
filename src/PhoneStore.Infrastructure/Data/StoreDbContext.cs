using Microsoft.EntityFrameworkCore;

namespace PhoneStore.Infrastructure.Data;

public sealed class StoreDbContext(DbContextOptions<StoreDbContext> options) : DbContext(options);
