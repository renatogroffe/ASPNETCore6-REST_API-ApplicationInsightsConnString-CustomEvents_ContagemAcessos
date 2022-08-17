using Microsoft.AspNetCore.Mvc;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using APIContagem.Models;
using APIContagem.Logging;

namespace APIContagem.Controllers;

[ApiController]
[Route("[controller]")]
public class ContadorController : ControllerBase
{
    private static readonly Contador _CONTADOR = new Contador();
    private readonly ILogger<ContadorController> _logger;
    private readonly IConfiguration _configuration;
    private readonly TelemetryConfiguration _telemetryConfig;

    public ContadorController(ILogger<ContadorController> logger,
        IConfiguration configuration,
        TelemetryConfiguration telemetryConfig)
    {
        _logger = logger;
        _configuration = configuration;
        _telemetryConfig = telemetryConfig;
    }

    [HttpGet]
    public ResultadoContador Get()
    {
        int valorAtualContador;

        lock (_CONTADOR)
        {
            _CONTADOR.Incrementar();
            valorAtualContador = _CONTADOR.ValorAtual;
        }

        _logger.LogValorAtual(valorAtualContador);

        #region Testes com Application Insights
        var telemetryClient = new TelemetryClient(_telemetryConfig);
        Dictionary<string, string> GetDictionaryValorAtual() => 
            new ()
            {
                { "Horario", DateTime.Now.ToString("HH:mm:ss") },
                { "ValorAtual", valorAtualContador.ToString() }
            };

        try
        {
            if (valorAtualContador % 4 == 0)
                throw new Exception("Simulacao de Falha");
            _logger.LogInformation("Gerando Custom Event do Application Insights...");
            telemetryClient.TrackEvent("ContagemAcessos", GetDictionaryValorAtual());
        }
        catch (Exception ex)
        {
            _logger.LogError($"Excecao - Mensagem: {ex.Message}");
            _logger.LogWarning("Registrando Exception com o Application Insights...");
            telemetryClient.TrackException(ex, GetDictionaryValorAtual());
        }
        #endregion

        return new()
        {
            ValorAtual = valorAtualContador,
            Producer = _CONTADOR.Local,
            Kernel = _CONTADOR.Kernel,
            Framework = _CONTADOR.Framework,
            Mensagem = _configuration["MensagemVariavel"]
        };
    }}
