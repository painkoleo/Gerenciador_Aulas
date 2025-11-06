using System;
using System.IO;

public class Configuracoes
{
    public string PastaPadrao { get; set; } = "";
    public bool ReproducaoContinua { get; set; } = true;
    public bool MPVFullscreen { get; set; } = true;
    public bool MinimizeToTray { get; set; } = false;
    public string MPVPath { get; set; } = string.Empty;
    public List<string> VideoExtensions { get; set; } = new List<string> { ".mp4", ".mkv", ".avi", ".mov" };
    public string LogDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GerenciadorAulas", "logs");
}
