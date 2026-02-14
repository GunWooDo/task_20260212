using Domain;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public DbSet<Employee> Employees { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Employee>(builder =>
        {
            // Primary Key 설정 (자동 증가)
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedOnAdd();
            
            // 필수 필드 설정
            builder.Property(e => e.Name).IsRequired();
            builder.Property(e => e.Email).IsRequired();
            builder.Property(e => e.Tel).IsRequired();
            
            // 중복 방지를 위한 유니크 인덱스 설정
            builder.HasIndex(e => e.Email).IsUnique();
            builder.HasIndex(e => e.Tel).IsUnique();
        });
    }
}
