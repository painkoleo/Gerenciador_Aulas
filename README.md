# 🎬 Gerenciador de Aulas

Um aplicativo em **WPF** para gerenciar, assistir e acompanhar o progresso de vídeos de aulas organizados em pastas. Permite marcar vídeos assistidos, acompanhar o progresso por módulo e reproduzir vídeos via **MPV**.

---

## ✨ Funcionalidades

1. **Selecionar pasta principal**  
   - Escolha a pasta que contém os módulos/disciplinas com os vídeos.

2. **TreeView com módulos e vídeos**  
   - Exibe pastas e arquivos de vídeo de forma hierárquica.  
   - Nomes de módulos exibem o progresso, por exemplo:  
     `Módulo 1 (3/5)`.

3. **Checkbox de progresso**  
   - Marque vídeos assistidos.  
   - Marcação automática de todos os vídeos dentro de um módulo.  
   - Progresso atualizado em tempo real.

4. **Reprodução de vídeos**  
   - Abra vídeos diretamente pelo **MPV**.  
   - Reprodução contínua opcional de vídeos do mesmo módulo.

5. **Barra de progresso**  
   - Mostra o percentual total de vídeos assistidos.

6. **Persistência do progresso**  
   - As marcações são salvas em `videos_assistidos.json`.  
   - Estado restaurado automaticamente ao abrir o aplicativo.

---

## 🛠 Tecnologias utilizadas

- C# (.NET 9.0)  
- WPF (Windows Presentation Foundation)  
- [Newtonsoft.Json](https://www.newtonsoft.com/json) → salvar o estado dos vídeos  
- [Ookii.Dialogs.Wpf](https://github.com/ookii-dialogs/ookii-dialogs-wpf) → seleção de pastas  

---

## 📂 Estrutura do projeto

GerenciadorAulas/
│
├─ MainWindow.xaml / MainWindow.xaml.cs → Interface principal e lógica do TreeView
├─ FolderItem.cs → Modelo de pasta/módulo
├─ VideoItem.cs → Modelo de vídeo
├─ Resources/ → Ícones de pasta, vídeo e play
└─ videos_assistidos.json → Armazena o progresso dos vídeos


---

## 🚀 Como usar

1. Execute o aplicativo.  
2. Clique em **Selecionar Pasta** e escolha a pasta principal com os vídeos.  
3. Use a TreeView para marcar vídeos assistidos.  
4. Clique no botão de **play** para abrir o vídeo no **MPV**.  
5. O progresso de cada módulo e o total será atualizado automaticamente.

---

## ⚠️ Observações

- O **MPV** deve estar instalado em:  
  `C:\Program Files (x86)\mpv\mpv.exe`  
- Suporta vídeos nos formatos: `.mp4`, `.mkv`, `.avi`, `.mov`.  
- O arquivo `videos_assistidos.json` é gerado automaticamente na mesma pasta do executável.  

---

## 📌Mmelhorias futuras

- Suporte a múltiplos players de vídeo.  
- Configuração de atalhos de teclado para avançar vídeos.  
- Pesquisa e filtro de módulos ou vídeos.  
- Estatísticas detalhadas de progresso.

---

