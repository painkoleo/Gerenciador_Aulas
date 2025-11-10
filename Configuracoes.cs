using System;
using System.IO;
using System.Windows; // Adicionado para WindowState

public class Configuracoes
{
    public string PastaPadrao { get; set; } = "";
    public bool ReproducaoContinua { get; set; } = true;
    public bool MinimizeToTray { get; set; } = false;
    public List<string> VideoExtensions { get; set; } = new List<string> { ".mp4", ".mkv", ".avi", ".mov" };
    public string LogDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GerenciadorAulas", "logs");

    // Propriedades da Janela
    public double WindowLeft { get; set; } = 0;
    public double WindowTop { get; set; } = 0;
    public double WindowWidth { get; set; } = 900; // Valor padrão
    public double WindowHeight { get; set; } = 720; // Valor padrão
    public WindowState WindowState { get; set; } = WindowState.Normal;
}
