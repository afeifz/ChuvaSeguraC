using ChuvaSegura.Monitoramento.Domain.Enums;

namespace ChuvaSegura.Monitoramento.Domain.Services;

/// <summary>
/// Contrato para classificar o risco a partir do nível da água.
/// Permite trocar a regra (por limites, por previsão de ML etc.) sem alterar o restante do sistema.
/// </summary>
public interface IClassificadorRisco
{
    NivelRisco Classificar(double nivelAguaCm);
}
