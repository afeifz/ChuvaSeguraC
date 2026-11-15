using ChuvaSegura.Monitoramento.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChuvaSegura.Monitoramento.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Ponto> Pontos => Set<Ponto>();

    public DbSet<Leitura> Leituras => Set<Leitura>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
