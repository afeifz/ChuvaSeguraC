using ChuvaSegura.Monitoramento.Application.Options;
using ChuvaSegura.Monitoramento.Application.Services;
using ChuvaSegura.Monitoramento.Domain.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ChuvaSegura.Monitoramento.Application;

public static class DependencyInjection
{
    /// <summary>Registra os serviços de aplicação e as regras de risco.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddOptions<RegrasRiscoOptions>().BindConfiguration(RegrasRiscoOptions.Secao);

        services.AddSingleton(TimeProvider.System);

        services.AddSingleton<IClassificadorRisco>(sp =>
        {
            var opcoes = sp.GetRequiredService<IOptions<RegrasRiscoOptions>>().Value;
            return new ClassificadorRiscoPorLimites(opcoes.AtencaoCm, opcoes.AlertaCm);
        });

        services.AddScoped<IPontoService, PontoService>();
        services.AddScoped<ILeituraService, LeituraService>();
        services.AddScoped<IRiscoService, RiscoService>();

        return services;
    }
}
