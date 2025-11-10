using System.Windows;
using System;
using System.IO;
using GerenciadorAulas.Services;
using GerenciadorAulas.ViewModels;
using GerenciadorAulas.Views;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using LibVLCSharp.Shared;

namespace GerenciadorAulas
{
    public partial class App : Application
    {
        public static IServiceProvider? ServiceProvider { get; private set; }

        public App()
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider()!;

            // Registra o manipulador para FirstChanceException
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
        }

        private void CurrentDomain_FirstChanceException(object? sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            // Log de todas as exceções antes que sejam tratadas
            LogService.LogDebug($"FirstChanceException: {e.Exception.GetType().Name} - {e.Exception.Message}");
            // Opcional: Logar o StackTrace completo para depuração
            // LogService.LogDebug($"StackTrace: {e.Exception.StackTrace}");
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Initialize LibVLCSharp Core
            // LibVLCSharp.Shared.Core.Initialize();

            // Register services
            services.AddSingleton<IWindowManager>(provider => new WindowManager(provider));
            services.AddSingleton<IPersistenceService, PersistenceService>();
            services.AddSingleton<IMediaPlayerService, EmbeddedVlcPlayerUIService>();
            services.AddSingleton<ITreeViewDataService>(provider =>
                new TreeViewDataService(
                    provider.GetRequiredService<IWindowManager>(),
                    provider.GetRequiredService<IPersistenceService>(),
                    () => ConfigManager.Carregar() // Fornece a implementação para Func<Configuracoes>
                ));
            services.AddSingleton<ICloudStorageService, GoogleDriveService>();
            services.AddSingleton<Func<Configuracoes>>(() => ConfigManager.Carregar());

            // Register ViewModels
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<CloudBackupViewModel>();

            // Register Windows
            services.AddTransient<MainWindow>();
            services.AddTransient<CloudBackupWindow>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Define o diretório para os logs na pasta local do projeto
                string logDirectory = ConfigManager.Carregar().LogDirectory;

                System.Diagnostics.Debug.WriteLine($"LogDirectory: {logDirectory}"); // Adicionado para depuração

                // Inicializa o serviço de log
                LogService.Initialize(logDirectory);
                LogService.Log("Aplicação iniciada.");
            }
            catch (LogServiceInitializationException ex)
            {
                MessageBox.Show(ex.Message, "Erro de Inicialização de Log", MessageBoxButton.OK, MessageBoxImage.Error);
                // Application.Current.Shutdown(); // Descomente para fechar o aplicativo em caso de falha no log
            }

            // Configura o tratador de exceções para a UI thread
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;

            // Configura o tratador de exceções para todas as outras threads
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            try
            {
                var mainWindow = ServiceProvider!.GetRequiredService<MainWindow>();
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                LogService.LogError("Erro fatal ao iniciar a MainWindow.", ex);
                MessageBox.Show($"Ocorreu um erro fatal ao iniciar a aplicação: {ex.Message}\n\nConsulte os logs para mais detalhes.",
                                "Erro Fatal", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
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
