# Termo de Abertura do Projeto (TAP) — ECO

## 1. Identificação do Projeto

| Campo | Valor |
|---|---|
| Nome do projeto | **ECO** — de "eco", a ação que se repete |
| Tipo de documento | Termo de Abertura do Projeto (TAP) |
| Autor | Davi Campaner Fernandes |

## 2. Autor / Situação da equipe

O projeto foi iniciado individualmente por Davi Campaner Fernandes. O grupo formal do projeto ainda será definido/ampliado — por isso este documento não define papéis de gerente de projeto, patrocinador ou partes interessadas externas; esses pontos serão revisitados quando o grupo estiver formado.

## 3. Justificativa

Automação de tarefas repetitivas em computador é um campo já estabelecido, conhecido como RPA (Robotic Process Automation). Ferramentas como UiPath, Automation Anywhere e Power Automate Desktop já resolvem esse problema em empresas grandes — o que valida que o problema é real. O que falta é uma opção **simples, gratuita e não voltada para empresas grandes**, que permita a uma pessoa automatizar sua própria rotina de escritório (dados, financeiro, operações) sem depender de ferramentas pagas, complexas ou pensadas para times de RPA corporativos.

## 4. Finalidade

Entregar uma versão própria, funcional e bem escopada de uma ferramenta de automação de tarefas repetitivas no computador, começando por um MVP (Fase 1) que resolve o problema por gravação e reprodução de ações de mouse/teclado por coordenadas de tela — sem exigir reconhecimento visual, IA ou infraestrutura complexa.

## 5. Objetivo Geral e Específicos

**Objetivo geral:** desenvolver uma ferramenta desktop para Windows que grave e reproduza sequências de ações do usuário (mouse e teclado), automatizando tarefas repetitivas de escritório, com suporte a pausa/retomada e agendamento.

**Objetivos específicos:**

- Implementar um **Gravador** que capture eventos globais de mouse/teclado do sistema operacional e os transforme em uma sequência estruturada de passos (`MacroStep`).
- Implementar um **Reprodutor** que execute essa sequência fielmente ao que foi gravado, respeitando o timing entre ações e permitindo pausar/retomar via atalho global.
- Implementar um **Armazenamento** local (SQLite) para nomear, listar, editar e excluir macros gravadas — incluindo a edição pontual do tempo de espera (`DelayBeforeMs`) de um passo específico após a gravação.
- Implementar um **Agendador** que dispare a execução de uma macro em horário definido, sem exigir que o usuário inicie manualmente.
- Suportar um passo de **texto com variáveis dinâmicas** (ex.: `"""dia_atual"""`), substituídas pelo valor correspondente no momento da execução.

## 6. Descrição

O sistema é dividido em módulos independentes, cada um com responsabilidade única: **Gravador (Recorder)**, **Armazenamento (Storage)**, **Reprodutor (Player)**, **Agendador (Scheduler)** e **Interface (UI)**. Um módulo não depende dos detalhes internos do outro — o Agendador, por exemplo, só precisa saber "chegou a hora, executa a macro X", sem saber como o Reprodutor faz cliques na tela.

Essa separação é formalizada como **Clean Architecture**, em 4 projetos na solution:

- **Domain** — entidades puras (`Macro`, `MacroStep`, `Schedule`), sem dependência externa.
- **Application** — casos de uso (gravar, reproduzir, pausar/retomar, agendar) e as interfaces implementadas pela camada externa (`IMacroRepository`, `IInputRecorder`, `IInputPlayer`).
- **Infrastructure** — implementações concretas: captura de eventos via API do Windows, banco SQLite, agendador.
- **Presentation.WPF** — a interface gráfica.

**Stack técnica:** C# (.NET), WPF, SQLite (via `Microsoft.Data.Sqlite` ou EF Core), captura/reprodução de input via APIs nativas do Windows (`SetWindowsHookEx`, `SendInput`), agendamento via Quartz.NET (a confirmar frente a `System.Threading.Timer` nativo).

## 7. Requisitos

**Contrato de dados (`MacroStep`):** todo passo tem `Order` (posição na sequência) e `DelayBeforeMs` (espera antes de executar, editável manualmente após a gravação). Cinco tipos de passo compõem a Fase 1:

| Tipo | Campos específicos | Origem |
|---|---|---|
| `ClickStep` | X, Y (coordenada absoluta) | Capturado pelo hook de mouse |
| `KeyPressStep` | Tecla pressionada | Capturado pelo hook de teclado |
| `TypeTextStep` | Texto literal | Capturado pelo hook de teclado |
| `TypeVariableTextStep` | Texto com variáveis (ex.: `"""dia_atual"""`) | Digitado numa caixa separada, aberta por atalho dedicado — a captura global de teclado fica pausada enquanto essa caixa está aberta |
| `WaitStep` | Duração em ms | Inserido manualmente ou herdado do intervalo gravado |

**Requisitos funcionais adicionais:**

- Tela para gravar e nomear uma macro.
- Tela para listar, editar (incluindo o delay de um passo específico) e excluir macros salvas.
- Tela de "Schedule" para escolher uma macro e definir horário/recorrência de execução.
- Atalho global `Ctrl+Alt+Pause` para pausar/retomar a execução em andamento.
- Aviso no aplicativo quando a reprodução encontrar um erro (elemento/tela fora do estado esperado) — sem retry automático ou skip de passo.

## 8. Entregas Principais (Fase 1 / MVP)

1. **Gravação e reprodução por coordenadas absolutas de tela** — sem reconhecimento visual, sem OCR, timing fiel ao que foi gravado.
2. **Pausar e retomar** — atalho `Ctrl+Alt+Pause`, estado mantido somente em memória durante a execução.
3. **Tela de Schedule** — execução automática em horário/recorrência definidos.
4. **Inserção de texto com variáveis dinâmicas** — passo `TypeVariableTextStep`, substituição no momento da execução.

## 9. Escopo

**Dentro do escopo (Fase 1):** os quatro itens da seção anterior, para ambiente Windows, uso desktop, single-user.

**Fora do escopo por enquanto:**

- Reconhecimento de elementos por leitura de tela (OCR, casamento de imagem, modelos de visão computacional).
- Ações semânticas (detectar "abriu o programa X" em vez de gravar cliques).
- Integração com UI Automation / árvore de acessibilidade.
- Coordenadas relativas à janela (só absolutas, por enquanto).
- Persistência de execução pausada entre reinícios do app.
- Modo de timing ajustável ou "modo rápido" automático.
- Distinção de botão do mouse (esquerdo/direito), clique duplo ou scroll no `ClickStep` — ponto ainda não fechado, tratado como fora de escopo até sinalização em contrário.

## 10. Premissas

- A tela permanece no mesmo estado entre a gravação e a reprodução — a automação da Fase 1 é **assistida**, o usuário está ciente de quando ela roda e evita usar o computador para outra coisa durante a execução.
- Ambiente Windows corporativo, uso local e single-user.
- O aplicativo-alvo da automação não roda com privilégio elevado diferente do automatizador (premissa a validar cedo na implementação).

## 11. Restrições

- Stack técnica já decidida (C#/.NET, WPF, SQLite) — sem reavaliação de tecnologia durante a Fase 1.
- Escopo deliberadamente pequeno, para ser concluído com folga dentro do tempo disponível — o maior risco deste tipo de projeto é de tamanho, não técnico.
- Desenvolvimento atualmente solo, sem divisão de frentes entre integrantes ainda.
- Sem orçamento: desenvolvimento e infraestrutura locais, sem custo de nuvem ou serviços pagos.

## 12. Riscos

| Risco | Mitigação |
|---|---|
| Captura global de teclado/mouse é arquiteturalmente parecida com um keylogger | Dados sempre locais, sob controle do usuário, sem envio externo; seção dedicada de segurança/privacidade na documentação |
| Coordenadas absolutas quebram com DPI scaling diferente, múltiplos monitores ou janela fora de posição | Documentar como limitação conhecida da Fase 1 |
| Timing fixo é frágil se o sistema real demorar mais para responder | Limitação inerente ao modelo "assistida", não tratada como bug |
| `SendInput` pode falhar se o app-alvo roda elevado e o automatizador não | Validar esse cenário cedo na implementação |
| Concorrência de acesso ao SQLite (Scheduler e UI ao mesmo tempo) | Atenção a locks mesmo em uso single-user |
| Escopo "vazar" para funcionalidades de fases futuras antes da hora | Manter disciplina de escopo da Fase 1 durante o desenvolvimento |

## 13. Critérios de Sucesso

- A Fase 1 do ECO funciona de ponta a ponta com um caso de uso real de escritório (gravar, reproduzir, pausar/retomar, agendar), sem depender de reconhecimento visual ou IA.
- O sistema roda de forma estável em ambiente Windows corporativo padrão, com todos os dados de gravação mantidos localmente.
- A separação em camadas (Clean Architecture) permite que fases futuras (ações semânticas, leitura de tela, reconhecimento visual) entrem como novos adaptadores em `Infrastructure`, sem reescrever o núcleo do sistema.

## 14. Critérios de Aceitação

| Entrega | Critério de aceitação |
|---|---|
| Gravação e reprodução | O Reprodutor executa uma macro gravada replicando fielmente as coordenadas de clique, teclas e timing registrados |
| Pausar e retomar | `Ctrl+Alt+Pause` pausa e retoma a execução em andamento sem perder o passo atual |
| Tela de Schedule | Uma macro agendada roda sozinha no horário definido, sem o usuário precisar iniciar manualmente |
| Texto com variáveis dinâmicas | `TypeVariableTextStep` substitui corretamente `"""dia_atual"""` (e demais variáveis suportadas) pelo valor correspondente no momento da execução |
| Armazenamento | O usuário consegue nomear, listar, editar (incluindo o delay de um passo específico) e excluir macros salvas |
