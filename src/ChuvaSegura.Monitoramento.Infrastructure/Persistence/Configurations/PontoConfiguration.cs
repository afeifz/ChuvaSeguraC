using ChuvaSegura.Monitoramento.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChuvaSegura.Monitoramento.Infrastructure.Persistence.Configurations;

public class PontoConfiguration : IEntityTypeConfiguration<Ponto>
{
    public void Configure(EntityTypeBuilder<Ponto> builder)
    {
        builder.ToTable("pontos");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Nome).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Bairro).IsRequired().HasMaxLength(100);

        // Enum gravado como texto ("Normal", "Atencao", "Alerta"): o banco fica legível.
        builder.Property(p => p.RiscoAtual).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(p => p.Nome).IsUnique();
        builder.HasIndex(p => p.Bairro);
    }
}
