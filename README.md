# Documentação do Projeto: Gerenciador de Aulas 2.0

## 📝 Índice

1. [Visão Geral do Sistema](#1-visão-geral-do-sistema)
    * [1.1. Tecnologias e Padrões](#11-tecnologias-e-padrões)
    * [1.2. Guia de Instalação e Desenvolvimento](#12-guia-de-instalação-e-desenvolvimento)
2. [Guia do Usuário: Como Usar o Gerenciador de Aulas](#2-guia-do-usuário-como-usar-o-gerenciador-de-aulas)
    * [2.1. Adicionar Aulas](#21-adicionar-aulas)
    * [2.2. Rastreamento de Progresso](#22-rastreamento-de-progresso)
    * [2.3. Controles de Reprodução](#23-controles-de-reprodução)
    * [2.4. Configurações (Player MPV)](#24-configurações-player-mpv)
    * [2.5. Funcionalidades Adicionais](#25-funcionalidades-adicionais)
    * [2.6. Backup e Restauração de Dados](#26-backup-e-restauração-de-dados)
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
* **Injeção de Dependência:** Utilização extensiva para desacoplamento e testabilidade.
* **Persistência de Dados:** Serialização JSON (Newtonsoft.Json), Backup na nuvem via Google Drive API (OAuth2)
* **Mídia:** Reprodução via serviço abstrato (`IMediaPlayerService`), com implementação padrão usando processo externo (`mpv.exe`).
* **Comandos Assíncronos:** Implementação de `AsyncRelayCommand` para operações não bloqueantes na UI.

---

## 1.2. Guia de Instalação e Desenvolvimento

Para configurar o ambiente de desenvolvimento e executar o projeto, siga os passos abaixo:

### Pré-requisitos

*   **Visual Studio:** Recomenda-se o Visual Studio 2022 ou superior com a carga de trabalho ".NET desktop development" instalada.
*   **SDK do .NET:** Certifique-se de ter o SDK do .NET 6.0 ou superior instalado.
*   **Git:** Para clonar o repositório.

### Configuração do Ambiente

1.  **Clonar o Repositório:**
    ```bash
    git clone https://github.com/seu-usuario/Gerenciador_Aulas.git
    cd Gerenciador_Aulas/Gerenciador_Aulas
    ```
    (Substitua `https://github.com/seu-usuario/Gerenciador_Aulas.git` pelo URL real do repositório, se diferente).

    > **Nota sobre `client_secret.json`:** Para a funcionalidade de backup no Google Drive, é necessário um arquivo `client_secret.json`. Este arquivo contém credenciais sensíveis e **não deve ser versionado em repositórios públicos**. Para desenvolvimento local, você precisará criar seu próprio projeto no Google Cloud Console, habilitar a Google Drive API e baixar seu `client_secret.json`, colocando-o na raiz do projeto.

2.  **Abrir no Visual Studio:**
    *   Abra o arquivo de solução `GerenciadorAulas.sln` no Visual Studio.

3.  **Restaurar Pacotes NuGet:**
    *   O Visual Studio deve restaurar automaticamente os pacotes NuGet. Caso contrário, clique com o botão direito na solução no "Gerenciador de Soluções" e selecione "Restaurar Pacotes NuGet".

4.  **Compilar o Projeto:**
    *   No Visual Studio, vá em `Build > Build Solution` (ou pressione `Ctrl+Shift+B`).

5.  **Executar a Aplicação:**
    *   Pressione `F5` no Visual Studio para iniciar a aplicação em modo de depuração, ou `Ctrl+F5` para iniciar sem depurar.

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

Os controles de mídia na barra de ferramentas e através de menus de contexto permitem gerenciar a reprodução de vídeos:

| Botão | Função | Comportamento |
| :--- | :--- | :--- |
| **Play** | Iniciar / Tocar | Se um vídeo estiver selecionado, ele toca. Se uma pasta estiver selecionada, toca o **primeiro vídeo não assistido** dentro dela. |
| **Stop** | Parar | Finaliza o player MPV e encerra a reprodução contínua. |
| **Atualizar** | Recarregar Lista | Recarrega toda a estrutura de pastas e vídeos, restaurando o estado de progresso salvo no disco. Use se houver mudanças nos arquivos externos. |

* **Menu de Contexto (Play):** Clique com o botão direito em qualquer pasta ou vídeo na lista para abrir um menu de contexto com a opção "Play". Esta é uma forma rápida de iniciar a reprodução do item desejado.

### 2.4. Configurações (Player MPV)

É essencial configurar o caminho do player MPV para que a reprodução funcione:

1.  Clique no botão de **Configurações** (engrenagem) na barra de ferramentas.
2.  Defina o **Caminho do Executável MPV**: Indique o local do arquivo `mpv.exe` na sua máquina.
3.  **Reprodução Contínua:** Marque esta opção se desejar que o sistema inicie o próximo vídeo automaticamente após o término do vídeo atual.
4.  **Tela Cheia (Fullscreen):** Marque para que o MPV sempre inicie em modo tela cheia.

### 2.5. Funcionalidades Adicionais

#### 2.5.1. Melhorias na Interação com a TreeView

*   **Seleção Múltipla:** Agora é possível selecionar múltiplos itens na `TreeView` utilizando a tecla `Ctrl`.
*   **Botões "Marcar Selecionados" e "Desmarcar Selecionados":** Dois novos botões foram adicionados na parte superior da `TreeView` para marcar ou desmarcar rapidamente todos os itens atualmente selecionados.
*   **Dicas de Ferramenta (Tooltips):** Todos os botões da interface agora exibem uma dica de ferramenta ao passar o mouse, descrevendo sua função.
*   **Remoção do Menu de Contexto "Marcar Selecionados":** Devido a problemas de funcionalidade, a opção "Marcar Selecionados" foi removida do menu de contexto da `TreeView`. A funcionalidade equivalente está disponível através do botão dedicado.

#### Minimizar para a Bandeja

Quando a opção "Minimizar para bandeja ao fechar" está ativada nas configurações, o comportamento do botão de fechar da janela principal é alterado. Ao invés de fechar a aplicação, a janela será escondida e um ícone será exibido na bandeja do sistema (próximo ao relógio).

#### Menu de Contexto da Bandeja

Ao clicar com o botão direito no ícone do Gerenciador de Aulas na bandeja do sistema, um menu de contexto será exibido com as seguintes opções:

*   **Restaurar:** Torna a janela principal do aplicativo visível novamente.
*   **Fechar:** Encerra completamente a aplicação.

Um duplo clique no ícone da bandeja também restaura a janela principal.

### 2.6. Backup e Restauração de Dados

Para garantir a segurança dos seus dados de progresso, a aplicação agora conta com uma funcionalidade de backup e restauração.

1.  **Backup e Restauração na Nuvem (Google Drive):**
    *   **Salvar no Google Drive:** Cria um backup dos dados da aplicação e o envia para uma pasta dedicada no seu Google Drive (`GerenciadorDeAulas_Backups`).
    *   **Restaurar do Google Drive:** Permite selecionar um backup previamente salvo no Google Drive e restaurá-lo para a aplicação. **Atenção:** A restauração substituirá todos os dados atuais da aplicação.
2.  **Backup Local:**
    *   **Acesse a Janela de Configurações:** Clique no ícone de engrenagem para abrir as configurações.
    *   **Use os Botões de Backup:**
        *   **Fazer Backup:** Ao clicar neste botão, o sistema irá gerar um único arquivo `.zip` contendo todos os dados da aplicação (vídeos assistidos, estado das pastas, último vídeo reproduzido). Você poderá escolher onde salvar este arquivo.
        *   **Restaurar Backup:** Ao clicar, você poderá selecionar um arquivo de backup (`.zip`) previamente salvo. **Atenção:** A restauração substituirá todos os dados atuais da aplicação pelos dados contidos no backup. Uma caixa de diálogo de confirmação será exibida antes da operação.

---

## 3. Arquitetura do Sistema (MVVM)

A aplicação segue rigorosamente o padrão **Model-View-ViewModel (MVVM)**, garantindo a separação de preocupações, alta manutenibilidade e testabilidade.

### 3.1. Componentes Principais

| Componente | Classes Relacionadas | Responsabilidade |
| :--- | :--- | :--- |
| **ViewModel** | `ViewModels/MainWindowViewModel.cs`, `ViewModels/ViewModelBase.cs` | Contém toda a lógica de negócio, comandos, gerenciamento de estado e preparação dos dados para a View. É a camada de comunicação entre a View e o Model. |
| **Model** | `Models/VideoItem.cs`, `Models/FolderItem.cs`, `Configuracoes.cs` | Estruturas de dados que representam a hierarquia de arquivos (`VideoItem`, `FolderItem`) e os dados de configuração. |
| **View** | `Views/MainWindow.xaml`, `Views/FolderProgressWindow.xaml` | Responsável pela interface gráfica, pelo *Data Binding* e pela manipulação de eventos de UI, como *Drag & Drop*. |
| **Services** | `Services/IWindowManager.cs`, `Services/IPersistenceService.cs`, `Services/IMediaPlayerService.cs`, `Services/ITreeViewDataService.cs`, `Services/LogService.cs` | Abstrai dependências externas, facilitando a injeção de dependência e a testabilidade. |

### 3.2. Estrutura de Pastas

Para melhor organização e aderência ao padrão MVVM, o projeto foi reestruturado nas seguintes pastas principais:

*   **`Commands/`**: Contém implementações de `ICommand`, como `RelayCommand` e `AsyncRelayCommand`, para desacoplar ações da UI.
*   **`Converters/`**: Armazena classes que implementam `IValueConverter` para transformações de dados na View.
*   **`Helpers/`**: Inclui classes auxiliares e extensões que fornecem funcionalidades diversas.
*   **`Models/`**: Define as classes de modelo de dados, como `VideoItem` e `FolderItem`.
*   **`Services/`**: Contém interfaces e implementações de serviços (ex: `IPersistenceService`, `IWindowManager`, `IMediaPlayerService`, `ITreeViewDataService`, `LogService`, `MpvPlayerService`, `TreeViewDataService`, e suas versões `Stub`).
*   **`ViewModels/`**: Abriga as classes ViewModel, como `MainWindowViewModel` e `ViewModelBase`, que expõem dados e comandos para as Views.
*   **`Views/`**: Contém os arquivos XAML e code-behind das janelas e controles de usuário da aplicação.

---

## 4. Detalhes do ViewModel (`MainWindowViewModel.cs`)

Esta é a classe central da aplicação, onde toda a lógica de estado e interação com o usuário é orquestrada.

### 4.1. Propriedades Observáveis (Data Binding)

As seguintes propriedades notificam a UI sobre mudanças de estado:

| Propriedade | Tipo | Uso |
| :--- | :--- | :--- |
| `TreeRoot` | `ObservableCollection<object>` | A fonte de dados principal para a `TreeView`, gerenciada pelo `ITreeViewDataService`. |
| `Configuracoes` | `Configuracoes` | Opções do aplicativo (ex: caminho do MPV, tela cheia, reprodução contínua). |
| `VideoAtual` | `string` | Exibe o nome do vídeo que está em reprodução. |
| `IsManuallyStopped` | `bool` | Flag para indicar se a reprodução foi interrompida pelo usuário. |
| `IsLoading` | `bool` | Indica que uma operação longa (como I/O de arquivos) está em andando. |

### 4.2. Comandos Principais

| Comando | Função |
| :--- | :--- |
| `PlaySelectedItemCommand` | Toca o item selecionado. Se for um vídeo, toca-o. Se for uma pasta, inicia o primeiro vídeo não assistido dentro dela. Este comando é assíncrono. |
| `StopPlaybackCommand` | Finaliza o processo `mpv.exe` (via `IMediaPlayerService`) e reseta o estado de reprodução. |
| `AddFoldersCommand` | Lida com a adição assíncrona de novas pastas/arquivos de vídeo via *Drag & Drop* ou diálogo de seleção. |
| `RefreshListCommand` | Recarrega a estrutura da `TreeView` (via `ITreeViewDataService`) e restaura o estado de progresso salvo no disco. |
| `ClearSelectedFolderCommand` | Remove uma pasta raiz (e seu estado de progresso) do rastreamento do aplicativo (via `ITreeViewDataService`). |
| `BrowseFoldersCommand` | Abre um diálogo para selecionar pastas e as adiciona assincronamente. |

### 4.3. Mecanismo de Reprodução de Mídia (via `IMediaPlayerService`)

A reprodução de 1mídia agora é abstraída através da interface `IMediaPlayerService`, que é injetada no `MainWindowViewModel`. A implementação padrão, `MpvPlayerService`, utiliza o `mpv.exe` como processo externo.

1.  **Abstração:** O ViewModel interage apenas com a interface `IMediaPlayerService`, sem conhecimento dos detalhes de implementação do player.
2.  **Assincronicidade:** Os métodos de reprodução são assíncronos, garantindo que a UI permaneça responsiva.
3.  **Controle de Fluxo:** O serviço gerencia o ciclo de vida do player externo, incluindo inicialização, reprodução, parada e tratamento de erros.
4.  **Reprodução Contínua:** A lógica de reprodução contínua é orquestrada pelo ViewModel, que solicita ao `IMediaPlayerService` para reproduzir o próximo vídeo após a conclusão do atual, se a configuração `ReproducaoContinua` estiver ativa.

## 5. Gerenciamento e Persistência de Estado

O estado do aplicativo é salvo em arquivos JSON na pasta de dados da aplicação (`AppData\GerenciadorAulas`), garantindo que o progresso do usuário seja mantido entre as sessões. Toda a lógica de leitura e escrita de arquivos é centralizada no `IPersistenceService`, que é utilizado pelo `ITreeViewDataService` para gerenciar o estado da `TreeView` e os vídeos assistidos.

### 5.1. Arquivos de Persistência

(Esta subseção não sofreu alterações significativas na sua descrição, mas a responsabilidade de uso foi movida para `ITreeViewDataService`.)

### 5.2. Rastreamento de Progresso (via `ITreeViewDataService`)

A lógica de rastreamento de progresso, que antes estava diretamente no `MainWindowViewModel`, agora é gerenciada pelo `ITreeViewDataService`.

*   **Atualização em Cascata:** Quando a propriedade `IsChecked` de um `VideoItem` muda, o `ITreeViewDataService` propaga a alteração recursivamente para seus pais (`FolderItem`).
*   **Progresso de Pasta:** Cada `FolderItem` calcula dinamicamente seu progresso (ex: "Nome da Pasta (10/12)") com base no número de vídeos assistidos em seus filhos, com a ajuda do `ITreeViewDataService`.
*   **Estado Misto:** Um `FolderItem` utiliza o estado de *checkbox* **indeterminado** (ou `null`) quando alguns, mas não todos, os vídeos em sua hierarquia estão marcados, também gerenciado pelo `ITreeViewDataService`.

## 6. Serviços e Injeção de Dependência

### 6.1. LogService (`LogService.cs`)



O `LogService` é uma classe estática utilizada para centralizar o registro de eventos e erros do sistema.



*   **Função:** Escreve mensagens com *timestamp* em arquivos `Log_YYYYMMDD_HHMMSS.txt`, localizados na pasta `logs` dentro do diretório de dados da aplicação do usuário (`%APPDATA%\GerenciadorAulas\logs`).

*   **Segurança de Threads:** Utiliza `lock (typeof(LogService))` para garantir que a escrita no arquivo seja segura em um ambiente multi-thread.



### 6.2. IWindowManager (Gerenciamento de Janelas)



O padrão de Injeção de Dependência é utilizado para gerenciar a abertura de novas janelas (`ConfigWindow`, `FolderProgressWindow`) e caixas de diálogo do sistema.



*   A interface `IWindowManager` abstrai as chamadas de UI, e a implementação `WindowManager` lida com a criação e exibição das janelas.

*   O `MainWindowViewModel` recebe uma instância de `IWindowManager` em seu construtor, o que facilita a testabilidade da aplicação.



### 6.3. IPersistenceService (Gerenciamento de Persistência)



Para centralizar a lógica de leitura e escrita de dados, a aplicação utiliza o `IPersistenceService`.



*   A interface `IPersistenceService` define um contrato para salvar e carregar o estado da aplicação (vídeos assistidos, estado da árvore, etc.).

*   A implementação `PersistenceService` lida com a serialização e desserialização de objetos para arquivos JSON, localizados na pasta `AppData` do usuário.

*   Assim como o `IWindowManager`, este serviço é injetado no `MainWindowViewModel` para manter o baixo acoplamento e a testabilidade.



### 6.4. IMediaPlayerService (Serviço de Reprodução de Mídia)



Esta nova interface abstrai a funcionalidade de reprodução de mídia, permitindo que o ViewModel seja independente da implementação específica do player.



*   A interface `IMediaPlayerService` define métodos como `PlayAsync` e `Stop` para controlar a reprodução.

*   A implementação `MpvPlayerService` utiliza o `mpv.exe` como player externo, encapsulando a lógica de inicialização e controle do processo.

*   `StubMediaPlayerService` é fornecido para fins de teste, permitindo que o ViewModel seja testado sem a necessidade de um player de mídia real.



### 6.5. ITreeViewDataService (Serviço de Dados da TreeView)



Esta nova interface centraliza toda a lógica de gerenciamento e manipulação dos dados exibidos na `TreeView`, incluindo carregamento, adição, remoção e persistência do estado.



*   A interface `ITreeViewDataService` expõe a coleção `TreeRoot` e métodos para manipular a estrutura de pastas e vídeos, como `AddFolderOrVideo`, `RemoveFolder`, `LoadInitialTree`, `GetNextUnwatchedVideo`, `ContarVideos`, `AtualizarNomeComProgresso`, etc.

*   A implementação `TreeViewDataService` lida com a leitura do sistema de arquivos, a criação dos `FolderItem` e `VideoItem`, e a interação com o `IPersistenceService` para salvar e carregar o estado da `TreeView` e dos vídeos assistidos.

*   `StubTreeViewDataService` é fornecido para facilitar o teste do `MainWindowViewModel` isoladamente.
