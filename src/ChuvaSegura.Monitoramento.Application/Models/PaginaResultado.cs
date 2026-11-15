namespace ChuvaSegura.Monitoramento.Application.Models;

/// <summary>Página de resultados de uma listagem.</summary>
public sealed record PaginaResultado<T>(IReadOnlyList<T> Itens, int Pagina, int TamanhoPagina, int Total)
{
    public int TotalPaginas => TamanhoPagina <= 0 ? 0 : (int)Math.Ceiling(Total / (double)TamanhoPagina);

    public PaginaResultado<TSaida> Mapear<TSaida>(Func<T, TSaida> conversor)
    {
        return new PaginaResultado<TSaida>(Itens.Select(conversor).ToList(), Pagina, TamanhoPagina, Total);
    }
}
