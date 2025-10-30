using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace GerenciadorAulas.Services
{
    // Classe estática para log centralizado
    public static class LogService
    {
        // O caminho inicial é um fallback (por exemplo, na pasta do executável) caso o Initialize falhe ou não seja chamado.
        private static string LogFilePath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? AppDomain.CurrentDomain.BaseDirectory,
            "log_fallback.txt");

        private static bool isInitialized = false;

        /// <summary>
        /// Define o caminho base onde o arquivo log.txt será salvo. Deve ser chamado na inicialização do app.
        /// </summary>
        /// <param name="basePath">O caminho da pasta, que deve ser a pasta AppData do seu aplicativo.</param>
        public static void Initialize(string basePath)
        {
            if (isInitialized) return;

            try
            {
                // Garante que o diretório exista antes de definir o caminho do log.
                if (!Directory.Exists(basePath))
                {
                    Directory.CreateDirectory(basePath);
                }

                // Define o caminho final (agora na pasta AppData)
                LogFilePath = Path.Combine(basePath, "log.txt");
                isInitialized = true;

                // Registra a inicialização no novo local
                Log($"Serviço de Log inicializado. O log está sendo salvo em: {LogFilePath}");
            }
            catch (Exception ex)
            {
                // Se falhar, registra o erro no console de debug e mantém o LogFilePath de fallback
                Debug.WriteLine($"ERRO FATAL ao inicializar o LogService: {ex.Message}");
            }
        }

        // Grava a mensagem com data e hora
        public static void Log(string message)
        {
            var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";

            try
            {
                // Garante segurança de thread ao escrever no arquivo
                lock (typeof(LogService))
                {
                    File.AppendAllText(LogFilePath, logMessage);
                }
            }
            catch (Exception ex)
            {
                // Se o log falhar (por exemplo, permissão), registra no console de Debug
                System.Diagnostics.Debug.WriteLine($"ERRO NO LOG: {ex.Message} - Mensagem original: {message}");
            }
        }
    }
}
