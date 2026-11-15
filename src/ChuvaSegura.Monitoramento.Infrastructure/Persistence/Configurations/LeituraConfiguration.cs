using ChuvaSegura.Monitoramento.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChuvaSegura.Monitoramento.Infrastructure.Persistence.Configurations;

public class LeituraConfiguration : IEntityTypeConfiguration<Leitura>
{
    public void Configure(EntityTypeBuilder<Leitura> builder)
    {
        builder.ToTable("leituras");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Risco).HasConversion<string>().HasMaxLength(20);
        builder.Property(l => l.MotivoRejeicao).HasMaxLength(200);

        builder.HasIndex(l => new { l.PontoId, l.DataHora });

        builder
            .HasOne<Ponto>()
            .WithMany(p => p.Leituras)
            .HasForeignKey(l => l.PontoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
