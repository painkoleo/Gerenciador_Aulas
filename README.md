# Documentação do Projeto: Gerenciador de Aulas 2.0

## 📝 Índice

1. [Visão Geral do Sistema](#1-visão-geral-do-sistema)
    * [1.1. Tecnologias e Padrões](#11-tecnologias-e-padrões)
2. [Guia do Usuário: Como Usar o Gerenciador de Aulas](#2-guia-do-usuário-como-usar-o-gerenciador-de-aulas)
    * [2.1. Adicionar Aulas](#21-adicionar-aulas)
    * [2.2. Rastreamento de Progresso](#22-rastreamento-de-progresso)
    * [2.3. Controles de Reprodução](#23-controles-de-reprodução)
    * [2.4. Configurações (Player MPV)](#24-configurações-player-mpv)
3. [Arquitetura do Sistema (MVVM)](#3-arquitetura-do-sistema-mvvm)
    * [3.1. Componentes Principais](#31-componentes-principais)
4. [Detalhes do ViewModel (`MainWindowViewModel.cs`)](#4-detalhes-do-viewmodel-mainwindowviewmodelcs)
    * [4.1. Propriedades Observáveis (Data Binding)](#41-propriedades-observáveis-data-binding)
    * [4.2. Comandos Principais](#42-comandos-principais)
    * [4.3. Mecanismo de Reprodução de Mídia](#43-mecanismo-de-reprodução-de-mídia)
5. [Gerenciamento e Persistência de Estado](#5-gerenciamento-e-persistência-de-estado)
    * [5.1. Arquivos de Persistência](#51-arquivos-de-persistência)
    * [5.2. Rastreamento de Progresso](#52-rastreamento-de-progresso)
6. [Serviços e Injeção de Dependência](#6-serviços-e-injeção-de-dependência)
    * [6.1. LogService (`LogService.cs`)](#61-logservice-logservicecs)
    * [6.2. IWindowManager (Gerenciamento de Janelas)](#62-iwindowmanager-gerenciamento-de-janelas)
    * [6.3. IPersistenceService (Gerenciamento de Persistência)](#63-ipersistenceservice-gerenciamento-de-persistência)

---

## 1. Visão Geral do Sistema

O **Gerenciador de Aulas** é uma aplicação de desktop desenvolvida em **WPF (.NET/C#)** cujo objetivo principal é organizar, rastrear o progresso e reproduzir coleções de aulas em vídeo.

O sistema permite que o usuário adicione pastas de aulas, visualize o conteúdo em uma estrutura de árvore hierárquica (`TreeView`), marque vídeos como assistidos, e utilize um *player* de mídia externo (`mpv.exe`) para a reprodução. O estado de progresso é salvo automaticamente, permitindo que o usuário retome suas atividades a qualquer momento.

- **Tratamento de Erros Robusto:** A aplicação agora inclui um sistema de tratamento de exceções global para capturar e registrar erros inesperados, melhorando a estabilidade e a experiência do usuário.

### 1.1. Tecnologias e Padrões

* **Linguagem de Programação:** C#
* **Framework:** WPF (.NET)
* **Padrão de Design:** MVVM (Model-View-ViewModel)
* **Persistência de Dados:** Serialização JSON (Newtonsoft.Json)
* **Mídia:** Reprodução via processo externo (`mpv.exe`)

---

## 2. Guia do Usuário: Como Usar o Gerenciador de Aulas

Esta seção explica as principais funcionalidades do aplicativo e como o usuário pode interagir com o sistema para gerenciar suas coleções de vídeos.

### 2.1. Adicionar Aulas

Existem duas maneiras principais de adicionar conteúdo à sua biblioteca:

1.  **Arrastar e Soltar (Drag & Drop):** Arraste uma pasta contendo suas aulas (ou um arquivo de vídeo individual) diretamente para a área da lista principal do aplicativo. O sistema fará a leitura e a organização automática em árvore.
2.  **Botão "Adicionar Pasta":** Use o botão com ícone de pasta na barra de ferramentas superior para abrir a caixa de diálogo e selecionar o diretório raiz das suas aulas.

> **Nota:** O sistema filtra automaticamente arquivos que não são de vídeo (`.mp4`, `.mkv`, `.avi`, `.mov`, etc.) para manter a lista limpa.

### 2.2. Rastreamento de Progresso

O progresso é rastreado através da `TreeView` e da caixa de seleção (checkbox) ao lado de cada item.

* **Marcar como Assistido:** Clique na **checkbox** ao lado de um vídeo para marcá-lo como assistido. O sistema salva esse estado automaticamente.
* **Progresso em Pasta:** Quando um vídeo é marcado/desmarcado, o sistema propaga a mudança para a pasta pai, atualizando o progresso exibido no nome da pasta (ex: `Módulo 1 (5/10)`).
* **Checkbox Indeterminada:** Se uma pasta contém alguns vídeos assistidos e outros não, a checkbox da pasta ficará em um **estado misto (hífen)**.

### 2.3. Controles de Reprodução

Os controles de mídia na barra de ferramentas permitem gerenciar a reprodução de vídeos:

| Botão | Função | Comportamento |
| :--- | :--- | :--- |
| **Play** | Iniciar / Tocar | Se um vídeo estiver selecionado, ele toca. Se uma pasta estiver selecionada, toca o **primeiro vídeo não assistido** dentro dela. |
| **Stop** | Parar | Finaliza o player MPV e encerra a reprodução contínua. |
| **Atualizar** | Recarregar Lista | Recarrega toda a estrutura de pastas e vídeos, restaurando o estado de progresso salvo no disco. Use se houver mudanças nos arquivos externos. |

### 2.4. Configurações (Player MPV)

É essencial configurar o caminho do player MPV para que a reprodução funcione:

1.  Clique no botão de **Configurações** (engrenagem) na barra de ferramentas.
2.  Defina o **Caminho do Executável MPV**: Indique o local do arquivo `mpv.exe` na sua máquina.
3.  **Reprodução Contínua:** Marque esta opção se desejar que o sistema inicie o próximo vídeo automaticamente após o término do vídeo atual.
4.  **Tela Cheia (Fullscreen):** Marque para que o MPV sempre inicie em modo tela cheia.

---

## 3. Arquitetura do Sistema (MVVM)

A aplicação segue rigorosamente o padrão **Model-View-ViewModel (MVVM)**, garantindo a separação de preocupações, alta manutenibilidade e testabilidade.

### 3.1. Componentes Principais

| Componente | Classes Relacionadas | Responsabilidade |
| :--- | :--- | :--- |
| **ViewModel** | `MainWindowViewModel`, `ViewModelBase` | Contém toda a lógica de negócio, comandos, gerenciamento de estado e preparação dos dados para a View. É a camada de comunicação entre a View e o Model. |
| **Model** | `VideoItem`, `FolderItem`, `Configuracoes` | Estruturas de dados que representam a hierarquia de arquivos (`VideoItem`, `FolderItem`) e os dados de configuração. |
| **View** | `MainWindow.xaml`, `FolderProgressWindow.xaml` | Responsável pela interface gráfica, pelo *Data Binding* e pela manipulação de eventos de UI, como *Drag & Drop*. |
| **Services** | `IWindowManager`, `IPersistenceService`, `LogService`, `ConfigManager` | Abstrai dependências externas, facilitando a injeção de dependência e a testabilidade. |

## 4. Detalhes do ViewModel (`MainWindowViewModel.cs`)

Esta é a classe central da aplicação, onde toda a lógica de estado e interação com o usuário é orquestrada.

### 4.1. Propriedades Observáveis (Data Binding)

As seguintes propriedades notificam a UI sobre mudanças de estado:

| Propriedade | Tipo | Uso |
| :--- | :--- | :--- |
| `TreeRoot` | `ObservableCollection<object>` | A fonte de dados principal para a `TreeView`. |
| `Configuracoes` | `Configuracoes` | Opções do aplicativo (ex: caminho do MPV, tela cheia, reprodução contínua). |
| `VideoAtual` | `string` | Exibe o nome do vídeo que está em reprodução. |
| `IsManuallyStopped` | `bool` | Flag para indicar se a reprodução foi interrompida pelo usuário. |
| `IsLoading` | `bool` | Indica que uma operação longa (como I/O de arquivos) está em andando. |

### 4.2. Comandos Principais

| Comando | Função |
| :--- | :--- |
| `PlaySelectedItemCommand` | Toca o item selecionado. Se for um vídeo, toca-o. Se for uma pasta, inicia o primeiro vídeo não assistido na pasta. |
| `StopPlaybackCommand` | Finaliza o processo `mpv.exe` e reseta o estado de reprodução. |
| `AddFoldersCommand` | Lida com a adição de novas pastas/arquivos de vídeo via *Drag & Drop* ou diálogo de seleção. |
| `RefreshListCommand` | Recarrega a estrutura da `TreeView` e restaura o estado de progresso salvo no disco. |
| `ClearSelectedFolderCommand` | Remove uma pasta raiz (e seu estado de progresso) do rastreamento do aplicativo. |

### 4.3. Mecanismo de Reprodução de Mídia

A reprodução é gerida pelo método `ReproduzirVideosAsync`, que utiliza o `System.Diagnostics.Process` para interagir com o `mpv.exe`.

1.  **Assincronicidade:** A reprodução é encapsulada em um `Task.Run` para garantir que o **Thread de UI** não seja bloqueado.
2.  **Controle de Fluxo:** Utiliza um `CancellationTokenSource` (`cts`) para permitir que o usuário interrompa o loop de reprodução contínua.
3.  **Processo MPV:** O método `PlayVideosLista` inicia o `mpv.exe` com o caminho do vídeo e argumentos de configuração (ex: `--fullscreen`). A aplicação espera a saída do processo (`mpvProcess.WaitForExit()`).
4.  **Reprodução Contínua:** Se a configuração estiver ativa, o sistema verifica a lista de vídeos para iniciar o próximo item após o término do vídeo atual.

## 5. Gerenciamento e Persistência de Estado

O estado do aplicativo é salvo em arquivos JSON na pasta de dados da aplicação (`AppData\GerenciadorAulas`), garantindo que o progresso do usuário seja mantido entre as sessões. Toda a lógica de leitura e escrita de arquivos é centralizada no `PersistenceService`, que é injetado no `MainWindowViewModel`.

### 5.2. Rastreamento de Progresso

* **Atualização em Cascata (`AtualizarPais`):** Quando a propriedade `IsChecked` de um `VideoItem` muda, a alteração é propagada recursivamente para seus pais (`FolderItem`).
* **Progresso de Pasta:** Cada `FolderItem` calcula dinamicamente seu progresso (ex: "Nome da Pasta (10/12)") com base no número de vídeos assistidos em seus filhos.
* **Estado Misto:** Um `FolderItem` utiliza o estado de *checkbox* **indeterminado** (ou `null`) quando alguns, mas não todos, os vídeos em sua hierarquia estão marcados.

## 6. Serviços e Injeção de Dependência

### 6.1. LogService (`LogService.cs`)

O `LogService` é uma classe estática utilizada para centralizar o registro de eventos e erros do sistema.

* **Função:** Escreve mensagens com *timestamp* no arquivo `log.txt`, localizado na mesma pasta do executável.
* **Segurança de Threads:** Utiliza `lock (typeof(LogService))` para garantir que a escrita no arquivo seja segura em um ambiente multi-thread.

### 6.2. IWindowManager (Gerenciamento de Janelas)

O padrão de Injeção de Dependência é utilizado para gerenciar a abertura de novas janelas (`ConfigWindow`, `FolderProgressWindow`) e caixas de diálogo do sistema.

* A interface `IWindowManager` abstrai as chamadas de UI, e a implementação `WindowManager` lida com a criação e exibição das janelas.
* O `MainWindowViewModel` recebe uma instância de `IWindowManager` em seu construtor, o que facilita a testabilidade da aplicação.

### 6.3. IPersistenceService (Gerenciamento de Persistência)

Para centralizar a lógica de leitura e escrita de dados, a aplicação utiliza o `IPersistenceService`.

* A interface `IPersistenceService` define um contrato para salvar e carregar o estado da aplicação (vídeos assistidos, estado da árvore, etc.).
* A implementação `PersistenceService` lida com a serialização e desserialização de objetos para arquivos JSON, localizados na pasta `AppData` do usuário.
* Assim como o `IWindowManager`, este serviço é injetado no `MainWindowViewModel` para manter o baixo acoplamento e a testabilidade.
