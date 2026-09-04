# ADR 0001 — Arquitetura e estrutura do projeto

## Status

Aceito.

## Contexto

O sistema precisa separar claramente as regras de negócio (o que é uma `Macro`, um `MacroStep`, como ela deve se comportar) dos detalhes técnicos de implementação (SQLite, hooks do Windows, WPF), para que:

- O núcleo do sistema possa ser testado sem depender de banco de dados real ou de hooks do sistema operacional.
- A evolução futura (ações semânticas, leitura de tela, reconhecimento visual) possa entrar como novos adaptadores, sem exigir reescrever o núcleo.
- Um módulo não precise conhecer os detalhes internos do outro (ex.: o Agendador só precisa saber "chegou a hora, executa a macro X").

A decisão foi adotar **Clean Architecture**, com a regra de dependência sempre apontando para dentro (em direção ao `Domain`).

## Decisão

### Estrutura de projetos

| Projeto | Papel | Depende de |
|---|---|---|
| `src/ECO.Domain` | Entidades puras (`Macro`, `MacroStep` e seus 5 tipos, `Schedule`) | Nenhum |
| `src/ECO.Application` | Casos de uso e interfaces (`IMacroRepository`, `IInputRecorder`, `IInputPlayer`) | `ECO.Domain` |
| `src/ECO.Infrastructure` | Implementações concretas (SQLite, hooks do Windows, Scheduler) | `ECO.Domain`, `ECO.Application` |
| `src/ECO.Presentation.WPF` | Interface gráfica (telas de gravar, listar, editar, Schedule) | `ECO.Application`, `ECO.Infrastructure` |
| `tests/ECO.Domain.Tests` | Testes do domínio | `ECO.Domain` |
| `tests/ECO.Application.Tests` | Testes dos casos de uso | `ECO.Application` |

`ECO.Domain` não referencia nenhum outro projeto — é o núcleo, isolado de qualquer detalhe técnico. A seta de dependência sobe numa via só: `Presentation.WPF` → `Application`/`Infrastructure` → `Domain`.

### Formato da solution: `.slnx`

Optou-se pelo formato `.slnx` (XML, mais enxuto e legível que o `.sln` clássico) em vez do formato tradicional, já suportado nativamente pelo Rider e pelo SDK do .NET usado no projeto (net10.0).

### Convenção de pastas: `src/` e `tests/`

Projetos de produto ficam em `src/`, projetos de teste ficam em `tests/`, cada um espelhando o nome do projeto testado (`ECO.Domain` → `ECO.Domain.Tests`).

### Colisão de nome: `ECO.Application` vs. `System.Windows.Application`

O projeto `ECO.Application` tem esse nome por convenção de Clean Architecture (camada de casos de uso). O WPF, por sua vez, exige que a classe principal do app (`App.xaml.cs`) herde de `System.Windows.Application`. Como `ECO.Presentation.WPF` referencia `ECO.Application`, o identificador `Application` ficava ambíguo entre os dois.

**Correção aplicada:** em `src/ECO.Presentation.WPF/GlobalUsings.cs`, foi adicionado um alias global:

```csharp
global using Application = System.Windows.Application;
```

Isso resolve a ambiguidade em todo o projeto `ECO.Presentation.WPF`, sem precisar qualificar `System.Windows.Application` em cada arquivo.

## Consequências

- Adicionar uma nova regra de negócio deve começar em `ECO.Domain`, nunca em `ECO.Infrastructure` ou `ECO.Presentation.WPF`.
- Trocar SQLite por outro banco, ou adicionar reconhecimento visual no futuro, afeta só `ECO.Infrastructure` — `Domain` e `Application` não precisam mudar.
- Qualquer novo projeto WPF ou que referencie `ECO.Application` deve repetir o alias de `GlobalUsings.cs` caso enfrente a mesma colisão de nome.
