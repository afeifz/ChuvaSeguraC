using ChuvaSegura.Monitoramento.Application.Dtos;
using ChuvaSegura.Monitoramento.Domain.Entities;

namespace ChuvaSegura.Monitoramento.Application;

/// <summary>Conversões entre entidades do domínio e DTOs de resposta.</summary>
public static class Mapeamentos
{
    public static PontoResponse ToResponse(this Ponto p)
    {
        return new PontoResponse(
            p.Id,
            p.Nome,
            p.Bairro,
            p.Latitude,
            p.Longitude,
            p.AlturaInstalacaoCm,
            p.Ativo,
            p.RiscoAtual,
            p.UltimoNivelAguaCm,
            p.UltimaLeituraEm.HasValue ? ComoUtc(p.UltimaLeituraEm.Value) : null);
    }

    public static LeituraResponse ToResponse(this Leitura l)
    {
        return new LeituraResponse(
            l.Id,
            l.PontoId,
            l.DistanciaCm,
            l.NivelAguaCm,
            l.Risco,
            l.Valida,
            l.MotivoRejeicao,
            ComoUtc(l.DataHora));
    }

    /// <summary>O SQLite devolve datas sem "Kind"; aqui marcamos como UTC para a API serializar com "Z".</summary>
    public static DateTime ComoUtc(DateTime data)
    {
        return data.Kind switch
        {
            DateTimeKind.Utc => data,
            DateTimeKind.Local => data.ToUniversalTime(),
            _ => DateTime.SpecifyKind(data, DateTimeKind.Utc)
        };
    }
}
