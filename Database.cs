using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Token> Tokens => Set<Token>();

    public DbSet<CardEntry> CardEntries => Set<CardEntry>();

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<Token>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<CardEntry>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<Token>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId);

        modelBuilder.Entity<CardEntry>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId);
    }
}

public class User
{
    public int Id { get; set; }

    public string Username { get; set; } = "";

    public string Password { get; set; } = "";
}

public class Token
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Value { get; set; } = "";
}

public class CardEntry
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int CardId { get; set; } = 0;
}