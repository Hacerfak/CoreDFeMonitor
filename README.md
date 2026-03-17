# CoreDFeMonitor

Aplicativo Desktop multiplataforma (Windows e Linux) para monitoramento, manifestação e gestão de Documentos Fiscais Eletrônicos (DF-e) emitidos contra CNPJs cadastrados.

## 🚀 Tecnologias e Bibliotecas

- **.NET 10 SDK**
- **Avalonia UI** (com Material Design)
- **Zeus Fiscal** (Comunicação com a SEFAZ)
- **SQLite + EF Core** (Banco de dados local)
- **MediatR** (Mensageria in-process para CQRS e Eventos)

## 🏛️ Arquitetura

O projeto foi desenhado focando em manutenibilidade e alta coesão, respeitando:
- **SOLID & Clean Code**
- **CQRS** (Command Query Responsibility Segregation)
- **Vertical Slice Architecture (VSA)** dentro da camada de Aplicação e UI.
- **Arquitetura Baseada em Eventos** (Event-Driven)

### Estrutura de Projetos

- `CoreDFeMonitor.Core`: O coração do sistema. Contém as Entidades de Domínio, Interfaces de repositório e os Eventos do sistema (ex: `DFeRecebidoEvent`). Não tem dependência com nada além do próprio .NET.
- `CoreDFeMonitor.Application`: Contém os Casos de Uso divididos por *Features* usando CQRS (Commands e Queries). Orquestra as validações e as regras de negócio.
- `CoreDFeMonitor.Infrastructure`: Implementação das interfaces do Core. Contém o DbContext do EF Core (SQLite), a integração com o Zeus Fiscal e serviços de rede/arquivos.
- `CoreDFeMonitor.UI`: O aplicativo Avalonia. Consome a camada Application. A UI também é dividida em *Features* (Vertical Slices) para facilitar a manutenção.
- `CoreDFeMonitor.Worker`: Serviço que roda em segundo plano consultando periodicamente a SEFAZ em busca de novos documentos e disparando eventos para a UI se atualizar reativamente.

## 🎨 UX / UI

A interface segue os padrões do **Material Design** e foi construída respeitando as **10 Heurísticas de Nielsen**, garantindo visibilidade do status do sistema, prevenção de erros (principalmente em manifestações fiscais definitivas) e design minimalista focado nos dados primários.

## ⚙️ Como Executar

1. Certifique-se de ter o [.NET 10 SDK](https://dotnet.microsoft.com/) instalado.
2. Clone o repositório.
3. Restaure as dependências: `dotnet restore`
4. Execute o projeto UI: `dotnet run --project src/CoreDFeMonitor.UI/CoreDFeMonitor.UI.csproj`