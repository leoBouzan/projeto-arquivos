# FileShare

## Sobre o projeto

O **FileShare** e um projeto academico desenvolvido no contexto de estudos de **Aplicacoes para Web**. A proposta do sistema e permitir o compartilhamento temporario de arquivos por meio de links com expiracao, limite de downloads e limpeza automatica dos arquivos expirados.

Este repositorio **nao representa um produto final** e **nao deve ser tratado como uma plataforma pronta para producao**. O objetivo principal e servir como base de aprendizado, modelagem arquitetural e evolucao tecnica de um sistema web moderno com frontend e backend desacoplados.

## Objetivo academico

Este projeto foi estruturado com foco educacional para exercitar conceitos como:

- desenvolvimento de aplicacoes web com frontend e backend separados;
- arquitetura em camadas com **Clean Architecture**;
- separacao de responsabilidades com **CQRS**;
- modelagem de dominio com abordagem **DDD Lite**;
- persistencia, storage de arquivos e processamento em background;
- preocupacoes de seguranca, expiracao e controle de acesso.

Em outras palavras, o FileShare foi pensado como um projeto de faculdade que vai alem de um CRUD simples e tenta aplicar boas praticas de engenharia de software em um contexto realista.

## Escopo funcional

Atualmente, o sistema contempla os seguintes fluxos principais:

- upload de arquivos;
- geracao de link temporario para acesso;
- expiracao por tempo;
- expiracao por limite maximo de downloads;
- consulta de disponibilidade e metadados do arquivo;
- download de arquivos validos;
- exclusao logica;
- limpeza automatica de arquivos expirados por worker em background;
- mecanismos basicos de seguranca, como validacao de token e rate limit por IP e dispositivo.

## Stack utilizada

### Backend

- **ASP.NET Core 10**
- **Entity Framework Core 10**
- **SQLite** para desenvolvimento local
- estrutura preparada para evolucao para **PostgreSQL**
- armazenamento local de arquivos, com desenho preparado para object storage

### Frontend

- **Angular 21**
- aplicacao standalone
- consumo da API via HTTP com proxy local

### Arquitetura

- **Clean Architecture**
- **CQRS**
- **DDD Lite**
- **Worker Service** para tarefas de expiracao e limpeza

## Estrutura do repositorio

```text
FileShare.slnx
src/
  backend/
    FileShare.API
    FileShare.Application
    FileShare.Contracts
    FileShare.Domain
    FileShare.Infrastructure
    FileShare.Worker
  frontend/
    fileshare-web
tests/
docs/
scripts/
```

## Como executar localmente

Na raiz do projeto, utilize os scripts preparados para desenvolvimento local:

```bash
./scripts/run-api.sh
./scripts/run-worker.sh
./scripts/run-web.sh
```

Cada comando deve ser executado em um terminal separado.

### Enderecos locais

- Frontend: `http://localhost:4200`
- API: `http://localhost:5216`

## Como validar

### Backend

```bash
dotnet build FileShare.slnx
dotnet test FileShare.slnx
```

### Frontend

```bash
cd src/frontend/fileshare-web
npm run build
```

## Observacoes importantes

- Este projeto foi construído com **finalidade academica**.
- A aplicacao ainda esta em processo de evolucao e refinamento.
- Algumas decisoes tecnicas foram tomadas para facilitar desenvolvimento local e demonstracao dos conceitos.
- Embora haja preocupacoes reais com arquitetura e seguranca, o objetivo aqui e **aprendizado**, e nao entrega de produto comercial final.

## Possiveis evolucoes futuras

- migracao definitiva para PostgreSQL em todos os ambientes;
- integracao com S3, MinIO ou Azure Blob Storage;
- autenticacao de usuarios e autoria dos arquivos;
- auditoria e observabilidade mais completas;
- testes automatizados mais abrangentes;
- pipeline de deploy e conteinerizacao.

## Documentacao complementar

A documentacao do projeto pode ser expandida futuramente com diagramas, fluxos e detalhamento arquitetural complementar conforme a evolucao do trabalho academico.
