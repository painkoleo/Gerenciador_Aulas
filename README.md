# Gerenciador de Aulas

---

## Índice
1. [Sobre o Programa](#sobre-o-programa)
2. [Features](#features)
3. [Tecnologias Utilizadas](#tecnologias-utilizadas)
4. [Documentação Técnica](#documentação-técnica)
   - [1. ConfigManager.cs](#1-configmanagercs)
   - [2. Configuracoes.cs](#2-configuracoescs)
   - [3. ConfigWindow.xaml / ConfigWindow.xaml.cs](#3-configwindowxaml--configwindowxamlcs)
   - [4. FolderItem.cs](#4-folderitemcs)
   - [5. VideoItem.cs](#5-videoitemcs)
   - [6. MainWindow.xaml](#6-mainwindowxaml)
   - [7. MainWindow.xaml.cs](#7-mainwindowxamlcs)
   - [8. Observações Gerais](#8-observações-gerais)

---

## Sobre o Programa
O **Gerenciador de Aulas** é uma ferramenta para organizar e acompanhar vídeos de cursos ou aulas.
Permite selecionar pastas de vídeos, marcar aulas assistidas, reproduzir vídeos com MPV, e acompanhar o progresso de forma automática.

---
## Features
- 📂 **Seleção de pastas** – Adicione a pasta principal com todas as aulas.
- ✅ **Marcar vídeos assistidos** – Cada vídeo possui um checkbox.
- 🔄 **Reprodução contínua** – Avança automaticamente para o próximo vídeo (configurável).
- 🎬 **Integração com MPV** – Reproduz vídeos dentro ou fora da tela cheia.
- 📊 **Progresso geral** – Barra e contador mostram quantos vídeos foram assistidos.
- 🔍 **Drag & Drop** – Arraste pastas ou vídeos diretamente para o programa.
- ⚙️ **Configurações personalizáveis** – Pasta padrão, fullscreen, caminho do MPV.
- 🔄 **Atualizar lista** – Atualiza TreeView sem precisar reiniciar.
- ❌ **Remover pasta principal** – Limpa toda a lista de vídeos e registros.

---

## Tecnologias Utilizadas
O Gerenciador de Aulas foi desenvolvido com as seguintes tecnologias:

- 🇨🇳 **C#** – Linguagem principal do programa.
- 🖥️ **WPF (Windows Presentation Foundation)** – Framework para criar a interface gráfica moderna e responsiva.
- 🏗️ **MVVM parcial** – Para organizar o código e facilitar o binding da interface.
- 📄 **JSON** – Para salvar configurações e estado do usuário (vídeos assistidos, última pasta, último vídeo).
- 🎬 **MPV** – Reprodutor de vídeo integrado ao programa, com suporte a tela cheia.
- 🔄 **NuGet Packages:**
  - `Newtonsoft.Json` – Serialização e desserialização de JSON.
  - `Ookii.Dialogs.Wpf` – Para diálogos de seleção de pasta com aparência moderna.

---

## Documentação Técnica

### 1. ConfigManager.cs
**Namespace:**
```csharp
namespace GerenciadorAulas
```
**Descrição:** Classe estática responsável por salvar e carregar as configurações do aplicativo em JSON no diretório AppData.

**Campo:**
```csharp
private static readonly string arquivoConfig;
```
Caminho completo para `%AppData%\GerenciadorAulas\config.json`.

**Métodos:**
```csharp
public static void Salvar(Configuracoes config)
```
Serializa o objeto Configuracoes e grava no arquivo JSON. Garante criação da pasta se não existir. Tratar exceções silenciosamente.

```csharp
public static Configuracoes Carregar()
```
Lê o arquivo JSON e retorna Configuracoes. Se não existir ou houver erro, retorna nova instância com valores padrão.

**Exemplo de uso:**
```csharp
var config = ConfigManager.Carregar();
config.PastaPadrao = @"D:\Aulas";
ConfigManager.Salvar(config);
```

### 2. Configuracoes.cs
Classe que contém as configurações do aplicativo.

**Propriedades:**

| Propriedade          | Tipo   | Valor padrão                  | Descrição                                      |
|---------------------|--------|------------------------------|-----------------------------------------------|
| PastaPadrao         | string | ""                           | Pasta principal de vídeos.                    |
| ReproducaoContinua  | bool   | true                         | Reproduz automaticamente o próximo vídeo.    |
| MPVFullscreen       | bool   | true                         | MPV abre em tela cheia.                       |
| MPVPath             | string | C:\Program Files (x86)\mpv\mpv.exe | Caminho do executável MPV.        |

**Exemplo de uso:**
```csharp
var config = new Configuracoes
{
    PastaPadrao = @"D:\Aulas",
    ReproducaoContinua = false,
    MPVFullscreen = true,
    MPVPath = @"C:\mpv\mpv.exe"
};
```

### 3. ConfigWindow.xaml / ConfigWindow.xaml.cs
Janela de configuração do aplicativo.

**Funcionalidades:**
- Alterar pasta padrão (txtPastaPadrao)
- Ativar/desativar reprodução contínua (chkReproducaoContinua)
- Ativar/desativar fullscreen do MPV (chkFullscreenMPV)
- Alterar caminho do MPV (txtMPVPath)

**Botões:**
- Salvar: Atualiza Configuracoes via ConfigManager.Salvar
- Cancelar: Fecha a janela sem salvar

**Exemplo de inicialização:**
```csharp
var configWindow = new ConfigWindow(configuracoes);
configWindow.ShowDialog();
```

### 4. FolderItem.cs
Representa uma pasta de vídeos na TreeView.

**Propriedades:**
```csharp
public string Name { get; set; }
public string DisplayName { get; set; }
public string FullPath { get; set; }
public FolderItem? ParentFolder { get; set; }
public ObservableCollection<object> Children { get; set; }
public bool? IsChecked { get; set; }
```

**Método principal:**
```csharp
public void MarcarFilhos(bool marcar)
```
Marca/desmarca todos os filhos recursivamente (VideoItem ou FolderItem).

### 5. VideoItem.cs
Representa um vídeo individual.

**Propriedades:**
```csharp
public string Name { get; set; }
public string FullPath { get; set; }
public FolderItem? ParentFolder { get; set; }
public bool IsChecked { get; set; }
```
Implementa INotifyPropertyChanged para atualizar UI automaticamente.

### 6. MainWindow.xaml
Interface principal do aplicativo.

**Layout:**
- Linha 0: Seletor de pasta (BtnSelectFolder, txtFolderPath)
- Linha 1: Botões de controle (Play, Next, Stop, Config, Refresh, Remove)
- Linha 2: TreeView (treeModules) com templates FolderItem e VideoItem
- Linha 3: Rodapé com ProgressBar e lblVideoAtual

**Recursos:**
- AlternatingRowBrushConverter → efeito zebra para linhas do TreeView

### 7. MainWindow.xaml.cs
Código-behind com toda lógica.

**Funcionalidades principais:**
- Inicialização: Carrega configurações, última pasta e estado de vídeos assistidos; inicializa TreeRoot e PlayCommand
- Carregamento de pastas e vídeos: Cria FolderItem e VideoItem recursivamente, com ordenação numérica
- Drag & Drop: Suporta arrastar pastas ou vídeos
- Checkboxes e progresso: Atualiza IsChecked, DisplayName e ProgressBar
- Reprodução de vídeos: PlayCommand e ReproduzirVideosAsync com MPV
- Próxima aula: BtnNextVideo_Click
- Parar reprodução: BtnStop_Click
- Atualizar lista: BtnRefresh_Click
- Configurações: BtnConfig_Click
- Remover pasta: BtnRemoveFolder_Click
- Persistência: Salva vídeos assistidos, última pasta e último vídeo em JSON no AppData

### 8. Observações Gerais
- Arquitetura: WPF + MVVM parcial
- Persistência: JSON no AppData
- Hierarquia: TreeView exibe FolderItem e VideoItem
- Recursos visuais: Tema escuro, ícones consistentes, efeito zebra
- Reprodução de vídeo: Integrada com MPV, suporta fullscreen
- Extensibilidade: Fácil de adicionar novos arquivos ou funcionalidades

[voltar ao índice](#índice)
