using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;

namespace GerenciadorAulas.Services
{
    public class PersistenceService : IPersistenceService
    {
        private readonly string appDataDir;
        private readonly string estadoArquivo;
        private readonly string ultimoVideoArquivo;
        private readonly string estadoTreeArquivo;
        private readonly JsonSerializerOptions jsonOptions;

        public PersistenceService()
        {
            appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GerenciadorAulas");
            if (!Directory.Exists(appDataDir)) Directory.CreateDirectory(appDataDir);
            estadoArquivo = Path.Combine(appDataDir, "videos_assistidos.json");
            ultimoVideoArquivo = Path.Combine(appDataDir, "ultimo_video.json");
            estadoTreeArquivo = Path.Combine(appDataDir, "estadoTreeView.json");
            jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        }

        public HashSet<string> LoadWatchedVideos()
        {
            if (!File.Exists(estadoArquivo)) return new HashSet<string>();

            try
            {
                var json = File.ReadAllText(estadoArquivo);
                var lista = JsonSerializer.Deserialize<List<string>>(json);
                return lista != null ? new HashSet<string>(lista) : new HashSet<string>();
            }
            catch (Exception ex)
            {
                LogService.LogError($"Erro ao carregar estado de vídeos assistidos: {ex.Message}", ex);
                return new HashSet<string>();
            }
        }

        public void SaveWatchedVideos(HashSet<string> watchedVideos)
        {
            try
            {
                var json = JsonSerializer.Serialize(watchedVideos.ToList(), jsonOptions);
                File.WriteAllText(estadoArquivo, json);
            }
            catch (Exception ex)
            {
                LogService.LogError($"Erro ao salvar estado de vídeos assistidos: {ex.Message}", ex);
            }
        }

        public TreeViewEstado LoadTreeViewEstado()
        {
            if (!File.Exists(estadoTreeArquivo)) return new TreeViewEstado();

            try
            {
                var json = File.ReadAllText(estadoTreeArquivo);
                var estado = JsonSerializer.Deserialize<TreeViewEstado>(json);
                return estado ?? new TreeViewEstado();
            }
            catch (Exception ex)
            {
                LogService.LogError($"Erro ao carregar estado da TreeView: {ex.Message}", ex);
                return new TreeViewEstado();
            }
        }

        public void SaveTreeViewEstado(TreeViewEstado treeViewEstado)
        {
            try
            {
                var json = JsonSerializer.Serialize(treeViewEstado, jsonOptions);
                File.WriteAllText(estadoTreeArquivo, json);
            }
            catch (Exception ex)
            {
                LogService.LogError($"Erro ao salvar estado da TreeView: {ex.Message}", ex);
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
                LogService.LogError($"Erro ao salvar último vídeo reproduzido: {ex.Message}", ex);
            }
        }

        public void BackupData(string destinationFilePath)
        {
            try
            {
                if (File.Exists(destinationFilePath))
                {
                    File.Delete(destinationFilePath);
                }

                using (var zip = ZipFile.Open(destinationFilePath, ZipArchiveMode.Create))
                {
                    if (File.Exists(estadoArquivo))
                        zip.CreateEntryFromFile(estadoArquivo, Path.GetFileName(estadoArquivo));
                    if (File.Exists(ultimoVideoArquivo))
                        zip.CreateEntryFromFile(ultimoVideoArquivo, Path.GetFileName(ultimoVideoArquivo));
                    if (File.Exists(estadoTreeArquivo))
                        zip.CreateEntryFromFile(estadoTreeArquivo, Path.GetFileName(estadoTreeArquivo));
                }
            }
            catch (Exception ex)
            {
                LogService.LogError($"Erro ao criar backup: {ex.Message}", ex);
                throw;
            }
        }

        public void RestoreData(string sourceFilePath)
        {
            try
            {
                if (!File.Exists(sourceFilePath))
                {
                    throw new FileNotFoundException("Arquivo de backup não encontrado.", sourceFilePath);
                }

                ZipFile.ExtractToDirectory(sourceFilePath, appDataDir, true);
            }
            catch (Exception ex)
            {
                LogService.LogError($"Erro ao restaurar backup: {ex.Message}", ex);
                throw;
            }
        }
    }
}