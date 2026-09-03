# Visão geral do projeto

## A ideia

Uma ferramenta de automação de tarefas no computador, inspirada na função de "gravar macro" do Excel, mas aplicada ao sistema operacional inteiro, não só dentro de uma planilha, mas em qualquer programa (abrir o navegador, preencher um sistema, exportar um relatório, mover um arquivo, o que for).

Na prática, o programa funciona assim:

- **Gravar** — o usuário aperta "gravar" e faz normalmente uma tarefa repetitiva no computador. O programa registra cada clique e cada tecla digitada.
- **Reproduzir** — depois, o programa repete exatamente essa sequência sozinho, sem o usuário precisar refazer manualmente.
- **Pausar e retomar** — se no meio da execução for preciso entrar e fazer algo manual (corrigir algo, digitar uma informação não prevista), dá para pausar, fazer a parte manual, e retomar de onde parou.
- **Agendar** — dá para programar uma automação gravada para rodar sozinha em um horário específico, sem precisar estar na frente do computador (tela "Schedule").

## Motivação

Automação de tarefas repetitivas em computador é um campo já estabelecido, conhecido como RPA (Robotic Process Automation). Ferramentas como UiPath, Automation Anywhere e Power Automate Desktop já fazem esse tipo de trabalho em empresas grandes — o que valida que o problema é real. O objetivo aqui não é provar que a ideia funciona, é entregar uma versão própria, funcional e bem escopada, para automatizar rotinas de trabalho de escritório (dados, financeiro, operações) sem depender de ferramentas de mercado que são pagas, complexas ou pensadas para empresas grandes.

Existe também uma linha de pesquisa mais recente (2024–2026) chamada "GUI agents" ou "computer use agents", que usa modelos de IA com visão para operar interfaces gráficas sem depender de coordenadas fixas (ex.: projeto UFO2 da Microsoft). Essa linha se conecta com a evolução de longo prazo deste projeto (Fases 3/4 abaixo), mas não é pré-requisito para o MVP.

Público-alvo: ambiente corporativo Windows. Multiplataforma não é requisito.

## Arquitetura geral

O sistema é dividido em módulos independentes, cada um com responsabilidade única:

- **Gravador (Recorder)** — escuta os eventos de mouse e teclado do sistema operacional e transforma cada ação em um registro estruturado (tipo de ação, coordenada, tecla, tempo entre ações).
- **Armazenamento (Storage)** — salva e organiza as automações gravadas ("macros"): nomear, listar, editar (incluindo o tempo de espera de um passo específico, ver seção "Contrato de dados"), excluir.
- **Reprodutor (Player)** — executa a sequência de ações gravadas, na ordem e no tempo certos, com suporte a pausar e retomar no meio da execução.
- **Agendador (Scheduler)** — dispara a execução de uma automação em um horário definido, sem exigir início manual.
- **Interface (UI)** — telas de gravar/nomear uma macro, listar macros salvas, e a tela "Schedule".

Um módulo não depende dos detalhes internos do outro: o Agendador, por exemplo, só precisa saber "chegou a hora, executa a macro X" — não sabe nem precisa saber como o Reprodutor faz cliques na tela.

Essa separação é formalizada como **Clean Architecture**, em 4 projetos na solution:

- **Domain** — entidades puras (`Macro`, `MacroStep`, `Schedule`), sem nenhuma dependência externa.
- **Application** — os casos de uso (gravar, reproduzir, pausar/retomar, agendar) e as interfaces que a camada externa implementa (`IMacroRepository`, `IInputRecorder`, `IInputPlayer`).
- **Infrastructure** — implementações concretas: captura de eventos via API do Windows, banco de dados SQLite, o agendador.
- **Presentation.WPF** — a interface gráfica.

O código de domínio nunca depende de detalhes como "é SQLite" ou "é WPF" — só das interfaces que ele mesmo define. Isso facilita testar `Domain`/`Application` sem depender de banco real ou hooks do Windows, e permite que a evolução futura (Fases 2-4) entre como novos adaptadores em `Infrastructure`, sem alterar o núcleo do sistema.

## Contrato de dados (MacroStep)

Toda automação gravada é uma lista ordenada de passos (`MacroStep`). Todo passo tem dois campos em comum:

- `Order` — posição do passo na sequência.
- `DelayBeforeMs` — tempo de espera antes de executar o passo. É **editável manualmente pelo usuário depois da gravação** (ex.: se a gravação levou 10s porque o usuário demorou pra encontrar onde clicar, esse tempo pode ser encurtado depois, na tela de editar macro) — não é um "modo rápido" automático, é ajuste pontual por passo.

Cinco tipos de passo compõem a Fase 1:

| Tipo | Campos específicos | Origem |
|---|---|---|
| `ClickStep` | X, Y (coordenada absoluta) | Capturado pelo hook de mouse |
| `KeyPressStep` | Tecla pressionada | Capturado pelo hook de teclado |
| `TypeTextStep` | Texto literal (ex.: `google.com`) | Capturado pelo hook de teclado durante a gravação normal |
| `TypeVariableTextStep` | Texto com variáveis (ex.: `"""dia_atual"""`) | **Não** vem do hook de teclado — o usuário aciona um atalho dedicado que abre uma caixa de texto separada para compor o texto; a captura global de teclado fica pausada enquanto essa caixa está aberta |
| `WaitStep` | Duração em ms | Inserido manualmente ou herdado do intervalo gravado |

`TypeTextStep` e `TypeVariableTextStep` são tipos separados (em vez de um único tipo com detecção de `"""..."""` dentro do texto) para evitar ambiguidade — texto literal que por acaso contivesse `"""..."""` seria substituído por engano — e para deixar explícito, por tipo, se aquele passo passa por substituição de variável na reprodução ou não. Essa substituição acontece na camada `Application`, antes de repassar o valor final ao `IInputPlayer`.

## Stack técnica

- **Linguagem**: C# (.NET).
- **UI**: WPF.
- **Captura de eventos globais e reprodução de input**: APIs nativas do Windows (`SetWindowsHookEx`, `SendInput` via P/Invoke).
- **Armazenamento**: SQLite (via `Microsoft.Data.Sqlite` ou EF Core).
- **Agendamento**: Quartz.NET (ou `System.Threading.Timer` nativo, a confirmar durante a implementação).

**Por que C# em vez de Python ou TS/Electron/Tauri:** o app é Windows-only e desktop (não web), então as APIs nativas do Win32 para hook global de input e simulação de eventos (`SetWindowsHookEx`, `SendInput`) trazem mais robustez de timing e menos fricção (ex.: falso-positivo de antivírus) do que uma stack em Python. TS via Electron/Tauri exigiria uma ponte entre dois runtimes (front em TS, automação nativa em C#/Rust) sem benefício real para o escopo atual.

## Fase 1 — MVP (o que será construído primeiro)

A Fase 1 inclui quatro capacidades:

1. **Gravação e reprodução por coordenadas absolutas de tela** — sem reconhecimento visual, sem OCR, sem casamento de imagem, sem IA de visão computacional. O timing entre ações é reproduzido **fielmente** ao que foi gravado (sem "modo rápido" configurável nesta fase).
2. **Pausar e retomar** — atalho global **`Ctrl+Alt+Pause`**. O estado de pausa vive **somente em memória** durante a execução; se o app fechar no meio de uma pausa, o progresso da execução é perdido (a macro gravada em si continua salva). Persistência de execução pausada entre reinícios fica para fase futura.
3. **Tela de Schedule** — escolher uma macro salva e definir horário/recorrência de execução automática.
4. **Inserção de texto com variáveis dinâmicas** — via passo `TypeVariableTextStep` (ver "Contrato de dados" abaixo), o usuário compõe, numa caixa de texto separada acionada por atalho dedicado, um texto usando variáveis do sistema delimitadas por aspas triplas (ex.: `"""dia_atual"""`), substituídas pelo valor correspondente no momento da execução — evitando ter que regravar a automação só porque um dado como a data muda a cada execução.

Se a reprodução encontrar um erro (ex.: elemento não está no estado esperado), o comportamento é **exibir um aviso no app** — sem retry automático ou skip de passo.

**Limitação assumida:** como a automação depende de coordenadas de tela, ela pressupõe que a tela está no mesmo estado de quando foi gravada. Por isso a Fase 1 é uma automação **"assistida"** — o usuário está ciente de quando ela roda e evita usar o computador para outra coisa durante a execução. Esse é o problema que a evolução futura (Fase 2 em diante) começa a resolver.

## Fora de escopo por enquanto

- Reconhecimento de elementos por leitura de tela (OCR, casamento de imagem, modelos de visão computacional).
- Ações semânticas (detectar "abriu o programa X" e gravar como instrução direta, em vez de cliques).
- Integração com UI Automation / árvore de acessibilidade.
- Coordenadas relativas à janela (só absolutas, por enquanto).
- Persistência de execução pausada entre reinícios do app.
- Modo de timing ajustável (só fiel, por enquanto).

## Evolução futura (visão de longo prazo)

Depois da Fase 1 validada, a ideia é evoluir o motor de reprodução em etapas, tornando as automações mais robustas a mudanças na tela — sem precisar refazer o que já foi construído:

- **Fase 2 — Ações semânticas**: em vez de gravar a sequência de cliques usada para abrir um programa (que quebra se o ícone mudar de lugar), o sistema passa a reconhecer "o usuário abriu o programa X" e grava isso como uma instrução direta.
- **Fase 3 — Leitura de tela em apps acessíveis**: para cliques dentro de um programa, tentar localizar o elemento pela estrutura interna da interface (funciona melhor em apps web e apps Windows nativos bem construídos), em vez de só pela coordenada.
- **Fase 4 — Reconhecimento visual**: uso de OCR e, potencialmente, modelos de visão computacional para localizar elementos mesmo em apps sem boa estrutura de acessibilidade. Direção de pesquisa/trabalho futuro, não requisito da entrega principal.

A Fase 1 sozinha já é um projeto completo e funcional — as fases seguintes são evolução, não pré-requisito.

## Pontos de atenção

- **Segurança e privacidade**: gravar cliques e teclas do sistema inteiro é, na arquitetura, parecido com o que um keylogger faz. Os dados devem ficar sempre locais, sob controle do usuário, sem envio para fora. Texto digitado (incluindo eventual senha gravada por engano) fica em SQLite; criptografia em repouso é uma melhoria possível, não bloqueante para o MVP.
- **Escopo controlado**: o maior risco deste tipo de projeto não é técnico, é de tamanho — fácil querer construir "tudo" (reconhecimento visual, qualquer aplicativo, qualquer cenário) e não terminar nada. A Fase 1 foi definida para ser pequena o suficiente para ser concluída com folga.
- **Coordenadas absolutas quebram** com DPI scaling diferente, múltiplos monitores, ou janela em posição diferente da gravação. Limitação conhecida da Fase 1.
- **Timing fixo é frágil** se o sistema real demorar mais para responder (app abrindo, página carregando) — limitação inerente do modelo "assistida".
- **Isolamento de UI privilege**: se o aplicativo-alvo da automação roda elevado (admin) e este programa não, `SendInput` pode não conseguir enviar eventos para ele. Validar cedo na implementação.
- **Concorrência no SQLite**: Scheduler e UI podem acessar o banco ao mesmo tempo (ex.: agendamento disparando enquanto a UI edita uma macro) — requer atenção a locks, mesmo em uso single-user.

## Decisões já fechadas

| Decisão | Escolha |
|---|---|
| Linguagem | C# (.NET) |
| UI | WPF |
| Arquitetura | Clean Architecture (Domain / Application / Infrastructure / Presentation.WPF) |
| Storage | SQLite |
| Scheduler | Quartz.NET (a confirmar vs. Timer nativo) |
| Coordenadas de clique | Absolutas (Fase 1) |
| Timing entre ações | Fiel ao gravado (Fase 1) |
| Persistência de pause/resume | Somente em memória (Fase 1) |
| Atalho de pause/resume | `Ctrl+Alt+Pause` |
| Erro durante reprodução | Aviso no app |
| Contrato de `MacroStep` | 5 tipos (`ClickStep`, `KeyPressStep`, `TypeTextStep`, `TypeVariableTextStep`, `WaitStep`), com `Order`/`DelayBeforeMs` comuns — ver seção "Contrato de dados" |
| Edição pós-gravação | `DelayBeforeMs` de um passo é editável manualmente pelo usuário, sem precisar regravar a macro |
| Texto literal vs. texto com variável | Tipos de passo separados (`TypeTextStep` / `TypeVariableTextStep`), não um único tipo com detecção de sintaxe |

## Decisões em aberto

- Lista de variáveis dinâmicas suportadas na Fase 1 (exemplo usado até agora: `"""dia_atual"""`).
- Sintaxe exata do atalho de teclado para abrir a caixa de texto do `TypeVariableTextStep`.
- Se `ClickStep` precisa distinguir botão do mouse (esquerdo/direito) e clique duplo/scroll, ou fica só clique simples com botão esquerdo por enquanto.
- Como o `MacroStep` é persistido no SQLite (uma tabela com coluna de tipo + colunas específicas nulas conforme o tipo, ou tabelas separadas por tipo).
- Confirmar Quartz.NET vs. `System.Threading.Timer` para o Scheduler.
