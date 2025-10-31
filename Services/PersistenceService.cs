using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace GerenciadorAulas.Services
{
    public class PersistenceService : IPersistenceService
    {
        private readonly string appDataDir;
        private readonly string estadoArquivo;
        private readonly string ultimoVideoArquivo;
        private readonly string estadoTreeArquivo;

        public PersistenceService()
        {
            appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GerenciadorAulas");
            if (!Directory.Exists(appDataDir)) Directory.CreateDirectory(appDataDir);
            estadoArquivo = Path.Combine(appDataDir, "videos_assistidos.json");
            ultimoVideoArquivo = Path.Combine(appDataDir, "ultimo_video.json");
            estadoTreeArquivo = Path.Combine(appDataDir, "estadoTreeView.json");
        }

        public HashSet<string> LoadWatchedVideos()
        {
            if (!File.Exists(estadoArquivo)) return new HashSet<string>();

            try
            {
                var lista = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(estadoArquivo));
                return lista != null ? new HashSet<string>(lista) : new HashSet<string>();
            }
            catch (Exception ex)
            {
                LogService.Log($"Erro ao carregar estado de vídeos assistidos: {ex.Message}");
                return new HashSet<string>();
            }
        }

        public void SaveWatchedVideos(HashSet<string> watchedVideos)
        {
            try
            {
                File.WriteAllText(estadoArquivo, JsonConvert.SerializeObject(watchedVideos.ToList()));
            }
            catch (Exception ex)
            {
                LogService.Log($"Erro ao salvar estado de vídeos assistidos: {ex.Message}");
            }
        }

        public TreeViewEstado LoadTreeViewEstado()
        {
            if (!File.Exists(estadoTreeArquivo)) return new TreeViewEstado();

            try
            {
                var estado = JsonConvert.DeserializeObject<TreeViewEstado>(File.ReadAllText(estadoTreeArquivo));
                return estado ?? new TreeViewEstado();
            }
            catch (Exception ex)
            {
                LogService.Log($"Erro ao carregar estado da TreeView: {ex.Message}");
                return new TreeViewEstado();
            }
        }

        public void SaveTreeViewEstado(TreeViewEstado treeViewEstado)
        {
            try
            {
                File.WriteAllText(estadoTreeArquivo, JsonConvert.SerializeObject(treeViewEstado, Formatting.Indented));
            }
            catch (Exception ex)
            {
                LogService.Log($"Erro ao salvar estado da TreeView: {ex.Message}");
            }
        }

        public void SaveLastPlayedVideo(string videoPath)
        {
            try
            {
                File.WriteAllText(ultimoVideoArquivo, videoPath);
            }
            catch (Exception ex)
            {
                LogService.Log($"Erro ao salvar último vídeo reproduzido: {ex.Message}");
            }
        }
    }
}