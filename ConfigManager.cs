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
                string dir = Path.GetDirectoryName(arquivoConfig)!;
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(arquivoConfig, json);
            }
            catch (Exception ex)
            {
                LogService.Log($"Erro ao salvar configuracoes: {ex.Message}");
            }
        }

        public static Configuracoes Carregar()
        {
            try
            {
                if (!File.Exists(arquivoConfig))
                    return new Configuracoes();

                var json = File.ReadAllText(arquivoConfig);
                return JsonSerializer.Deserialize<Configuracoes>(json) ?? new Configuracoes();
            }
            catch (Exception ex)
            {
                LogService.Log($"Erro ao carregar configuracoes: {ex.Message}");
                return new Configuracoes();
            }
        }
    }
}
