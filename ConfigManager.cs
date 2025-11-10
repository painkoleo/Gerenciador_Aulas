using System;
using System.IO;
using System.Text.Json;
using GerenciadorAulas.Services;

namespace GerenciadorAulas
{
    public static class ConfigManager
    {
        private static readonly string arquivoConfig = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GerenciadorAulas", "config.json");

        public static void Salvar(Configuracoes config)
        {
            try
            {
                // Carregar a configuração existente para comparar
                Configuracoes oldConfig = Carregar();

                // Logar as mudanças
                if (config.PastaPadrao != oldConfig.PastaPadrao)
                {
                    LogService.Log($"Configuração alterada: PastaPadrao de '{oldConfig.PastaPadrao}' para '{config.PastaPadrao}'");
                }
                if (config.ReproducaoContinua != oldConfig.ReproducaoContinua)
                {
                    LogService.Log($"Configuração alterada: ReproducaoContinua de '{oldConfig.ReproducaoContinua}' para '{config.ReproducaoContinua}'");
                }
                if (config.LogDirectory != oldConfig.LogDirectory)
                {
                    LogService.Log($"Configuração alterada: LogDirectory de '{oldConfig.LogDirectory}' para '{config.LogDirectory}'");
                }

                // Logar as mudanças nas propriedades da janela
                if (config.WindowLeft != oldConfig.WindowLeft)
                {
                    LogService.Log($"Configuração alterada: WindowLeft de '{oldConfig.WindowLeft}' para '{config.WindowLeft}'");
                }
                if (config.WindowTop != oldConfig.WindowTop)
                {
                    LogService.Log($"Configuração alterada: WindowTop de '{oldConfig.WindowTop}' para '{config.WindowTop}'");
                }
                if (config.WindowWidth != oldConfig.WindowWidth)
                {
                    LogService.Log($"Configuração alterada: WindowWidth de '{oldConfig.WindowWidth}' para '{config.WindowWidth}'");
                }
                if (config.WindowHeight != oldConfig.WindowHeight)
                {
                    LogService.Log($"Configuração alterada: WindowHeight de '{oldConfig.WindowHeight}' para '{config.WindowHeight}'");
                }
                if (config.WindowState != oldConfig.WindowState)
                {
                    LogService.Log($"Configuração alterada: WindowState de '{oldConfig.WindowState}' para '{config.WindowState}'");
                }

                string dir = Path.GetDirectoryName(arquivoConfig)!;
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(arquivoConfig, json);
            }
            catch (UnauthorizedAccessException ex)
            {
                LogService.LogError($"Erro de permissão ao salvar configurações: {ex.Message}", ex);
            }
            catch (IOException ex)
            {
                LogService.LogError($"Erro de I/O ao salvar configurações: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                LogService.LogError($"Erro de serialização JSON ao salvar configurações: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                LogService.LogError($"Erro inesperado ao salvar configurações: {ex.Message}", ex);
            }
        }

        public static Configuracoes Carregar()
        {
            try
            {
                if (!File.Exists(arquivoConfig))
                    return new Configuracoes();

                var json = File.ReadAllText(arquivoConfig);
                var config = JsonSerializer.Deserialize<Configuracoes>(json) ?? new Configuracoes();

                if (config.VideoExtensions == null || !config.VideoExtensions.Any())
                {
                    config.VideoExtensions = new List<string> { ".mp4", ".mkv", ".avi", ".mov" };
                }

                return config;
            }
            catch (FileNotFoundException ex)
            {
                LogService.LogError($"Arquivo de configuração não encontrado: {ex.Message}", ex);
                return new Configuracoes();
            }
            catch (UnauthorizedAccessException ex)
            {
                LogService.LogError($"Erro de permissão ao carregar configurações: {ex.Message}", ex);
                return new Configuracoes();
            }
            catch (IOException ex)
            {
                LogService.LogError($"Erro de I/O ao carregar configurações: {ex.Message}", ex);
                return new Configuracoes();
            }
            catch (JsonException ex)
            {
                LogService.LogError($"Erro de desserialização JSON ao carregar configurações: {ex.Message}", ex);
                return new Configuracoes();
            }
            catch (Exception ex)
            {
                LogService.LogError($"Erro inesperado ao carregar configurações: {ex.Message}", ex);
                return new Configuracoes();
            }
        }
    }
}
