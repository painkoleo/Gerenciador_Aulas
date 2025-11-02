using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace GerenciadorAulas.Services
{
    public class LogServiceInitializationException : Exception
    {
        public LogServiceInitializationException(string message, Exception innerException) : base(message, innerException) { }
    }

    // Classe estática para log centralizado
    public static class LogService
    {
        // O caminho inicial é um fallback (por exemplo, na pasta do executável) caso o Initialize falhe ou não seja chamado.
        private static string _logDirectory = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? AppDomain.CurrentDomain.BaseDirectory,
            "Logs_Fallback");
        private static string _currentLogFilePath = Path.Combine(_logDirectory, "log_fallback.txt");
        private static bool isInitialized = false;

        /// <summary>
        /// Define o caminho base onde os arquivos de log serão salvos. Deve ser chamado na inicialização do app.
        /// </summary>
        /// <param name="logDirectory">O caminho da pasta onde os logs serão armazenados.</param>
        public static void Initialize(string logDirectory)
        {
            if (isInitialized) return;

            try
            {
                _logDirectory = logDirectory;

                // Garante que o diretório de logs exista.
                if (!Directory.Exists(_logDirectory))
                {
                    Directory.CreateDirectory(_logDirectory);
                }

                // Gera um nome de arquivo de log único para esta execução.
                _currentLogFilePath = Path.Combine(_logDirectory, $"Log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                isInitialized = true;

                // Registra a inicialização no novo local
                Log($"Serviço de Log inicializado. O log está sendo salvo em: {_currentLogFilePath}");
            }
            catch (Exception ex)
            {
                // Se falhar, registra o erro no console de debug e mantém o LogFilePath de fallback
                Debug.WriteLine($"ERRO FATAL ao inicializar o LogService: {ex.Message}");
                // Fallback to a temporary log file if initialization fails
                _currentLogFilePath = Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? AppDomain.CurrentDomain.BaseDirectory,
                    "log_fallback.txt");

                throw new LogServiceInitializationException($"Erro crítico ao inicializar o serviço de log: {ex.Message}. O log será direcionado para o console de depuração. Por favor, verifique as permissões de escrita.", ex);
            }
        }

        // Grava a mensagem com data e hora
        public static void Log(string message)
        {
            LogInternal("INFO", message);
        }

        public static void LogWarning(string message)
        {
            LogInternal("WARNING", message);
        }

        public static void LogError(string message, Exception? ex = (Exception?)null)
        {
            string errorMessage = message;
            if (ex != null)
            {
                errorMessage += $" Exception: {ex.Message} StackTrace: {ex.StackTrace}";
            }
            LogInternal("ERROR", errorMessage);
        }

        private static void LogInternal(string level, string message)
        {
            var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}{Environment.NewLine}";

            try
            {
                // Garante segurança de thread ao escrever no arquivo
                lock (typeof(LogService))
                {
                    File.AppendAllText(_currentLogFilePath, logMessage);
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
