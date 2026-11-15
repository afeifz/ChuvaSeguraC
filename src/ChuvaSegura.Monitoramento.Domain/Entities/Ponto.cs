using ChuvaSegura.Monitoramento.Domain.Enums;
using ChuvaSegura.Monitoramento.Domain.Exceptions;
using ChuvaSegura.Monitoramento.Domain.Services;

namespace ChuvaSegura.Monitoramento.Domain.Entities;

/// <summary>
/// Ponto de monitoramento: o local físico (ex.: uma rua ou córrego) onde um sensor ESP32 mede o nível da água.
/// Concentra as regras de negócio: validação dos dados, cálculo do nível e mudança de risco.
/// </summary>
public class Ponto
{
    // Construtor exigido pelo EF Core.
    private Ponto()
    {
    }

    public Ponto(string nome, string bairro, double latitude, double longitude, double alturaInstalacaoCm, DateTime criadoEmUtc)
    {
        Aplicar(nome, bairro, latitude, longitude, alturaInstalacaoCm);
        CriadoEm = criadoEmUtc;
    }

    public int Id { get; private set; }

    public string Nome { get; private set; } = string.Empty;

    public string Bairro { get; private set; } = string.Empty;

    public double Latitude { get; private set; }

    public double Longitude { get; private set; }

    /// <summary>Altura (cm) do sensor em relação ao fundo/solo. Nível da água = altura − distância medida.</summary>
    public double AlturaInstalacaoCm { get; private set; }

    public bool Ativo { get; private set; } = true;

    /// <summary>Risco da última leitura válida (começa em Normal).</summary>
    public NivelRisco RiscoAtual { get; private set; } = NivelRisco.Normal;

    public double? UltimoNivelAguaCm { get; private set; }

    public DateTime? UltimaLeituraEm { get; private set; }

    public DateTime CriadoEm { get; private set; }

    public ICollection<Leitura> Leituras { get; private set; } = new List<Leitura>();

    public void Atualizar(string nome, string bairro, double latitude, double longitude, double alturaInstalacaoCm)
    {
        Aplicar(nome, bairro, latitude, longitude, alturaInstalacaoCm);
    }

    public void Ativar() => Ativo = true;

    public void Desativar() => Ativo = false;

    /// <summary>
    /// Processa a distância medida pelo sensor. Se estiver fora da faixa do sensor, devolve uma leitura rejeitada
    /// e não altera o risco. Caso contrário calcula o nível, classifica o risco e atualiza o estado do ponto.
    /// </summary>
    public ResultadoLeitura RegistrarLeitura(double distanciaCm, IClassificadorRisco classificador, DateTime agoraUtc)
    {
        if (!Ativo)
        {
            throw new ConflitoException("O ponto de monitoramento está inativo e não aceita leituras.");
        }

        var foraDaFaixa = double.IsNaN(distanciaCm)
            || double.IsInfinity(distanciaCm)
            || distanciaCm < FaixaSensor.DistanciaMinimaCm
            || distanciaCm > FaixaSensor.DistanciaMaximaCm;

        if (foraDaFaixa)
        {
            var motivo = $"Distância fora da faixa do sensor ({FaixaSensor.DistanciaMinimaCm} a {FaixaSensor.DistanciaMaximaCm} cm).";
            var rejeitada = Leitura.Rejeitada(Id, distanciaCm, motivo, agoraUtc);
            return new ResultadoLeitura(rejeitada, false, RiscoAtual);
        }

        // Se o sensor medir além da altura de instalação, não há água: o nível é 0.
        var nivel = Math.Max(0, AlturaInstalacaoCm - distanciaCm);
        var novoRisco = classificador.Classificar(nivel);
        var riscoAnterior = RiscoAtual;

        RiscoAtual = novoRisco;
        UltimoNivelAguaCm = nivel;
        UltimaLeituraEm = agoraUtc;

        var aceita = Leitura.Aceita(Id, distanciaCm, nivel, novoRisco, agoraUtc);
        return new ResultadoLeitura(aceita, novoRisco != riscoAnterior, riscoAnterior);
    }

    private void Aplicar(string nome, string bairro, double latitude, double longitude, double alturaInstalacaoCm)
    {
        var nomeValido = ValidarTexto(nome, "Nome", 3, 100);
        var bairroValido = ValidarTexto(bairro, "Bairro", 2, 100);
        var latitudeValida = ValidarFaixa(latitude, "Latitude", -90, 90);
        var longitudeValida = ValidarFaixa(longitude, "Longitude", -180, 180);
        var alturaValida = ValidarFaixa(alturaInstalacaoCm, "Altura de instalação (cm)", 10, FaixaSensor.DistanciaMaximaCm);

        Nome = nomeValido;
        Bairro = bairroValido;
        Latitude = latitudeValida;
        Longitude = longitudeValida;
        AlturaInstalacaoCm = alturaValida;
    }

    private static string ValidarTexto(string? valor, string campo, int minimo, int maximo)
    {
        var texto = valor?.Trim() ?? string.Empty;
        if (texto.Length < minimo || texto.Length > maximo)
        {
            throw new DominioException($"{campo} deve ter entre {minimo} e {maximo} caracteres.");
        }

        return texto;
    }

    private static double ValidarFaixa(double valor, string campo, double minimo, double maximo)
    {
        if (double.IsNaN(valor) || valor < minimo || valor > maximo)
        {
            throw new DominioException($"{campo} deve estar entre {minimo} e {maximo}.");
        }

        return valor;
    }
}
