using System.Windows;
using System;
using System.IO;
using GerenciadorAulas.Services; // Assuming LogService is in this namespace
using System.Windows.Threading;

namespace GerenciadorAulas
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Define o diretório para os logs na pasta de dados do aplicativo do usuário
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string logDirectory = Path.Combine(appDataPath, "GerenciadorAulas", "Logs");

            // Inicializa o serviço de log
            LogService.Initialize(logDirectory);
            LogService.Log("Aplicação iniciada.");

            // Configura o tratador de exceções para a UI thread
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;

            // Configura o tratador de exceções para todas as outras threads
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // Log da exceção
            LogService.LogError("Exceção não tratada na UI Thread.", e.Exception);

            // Informa o usuário
            MessageBox.Show($"Ocorreu um erro inesperado na aplicação: {e.Exception.Message}\n\nO erro foi registrado. Por favor, reinicie a aplicação.",
                            "Erro na Aplicação", MessageBoxButton.OK, MessageBoxImage.Error);

            // Marca a exceção como tratada para evitar que a aplicação feche imediatamente
            e.Handled = true;

            // Opcional: Fechar a aplicação após o erro crítico
            // Application.Current.Shutdown();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;

            // Log da exceção
            LogService.LogError("Exceção não tratada em thread de background.", ex);

            // Informa o usuário (pode ser necessário usar o Dispatcher para exibir na UI thread)
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show($"Ocorreu um erro inesperado em segundo plano: {ex.Message}\n\nO erro foi registrado. Por favor, reinicie a aplicação.",
                                "Erro em Segundo Plano", MessageBoxButton.OK, MessageBoxImage.Error);
            });

            // Se e.IsTerminating for true, a aplicação será encerrada de qualquer forma.
            // Podemos adicionar lógica adicional aqui se necessário.
        }
    }
}
