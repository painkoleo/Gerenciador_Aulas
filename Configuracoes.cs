public class Configuracoes
{
    public string PastaPadrao { get; set; } = "";
    public bool ReproducaoContinua { get; set; } = true;
    public bool MPVFullscreen { get; set; } = true;
    public string MPVPath { get; set; } = string.Empty;
    public List<string> VideoExtensions { get; set; } = new List<string> { ".mp4", ".mkv", ".avi", ".mov" };
}
