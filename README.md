# Moser.Enterprise.Blueprint

Proof-of-concept for an intranet policy assistant: RAG (retrieval-augmented generation) plus a small set of real services.

## Layout

*   **`src/Modules`** — libraries hosted inside another process (no own URL).
    *   **Ingestion** — read `.md` → chunk → embed → upsert into the index.
    *   **Assistant** — ask, retrieve, generate, Policy guardrail.
*   **`src/Microservices/People`** — employee directory API. Stateless, so Aspire runs **two replicas**.
*   **`src/Web`** — Blazor UI (BFF). Hosts the modules. Calls `people-api`.
*   **`src/BuildingBlocks`** — shared DDD/CQRS primitives.

Catalog (products) and Sources (pointer CRUD) are gone: they were not intranet bounded contexts.

## Run

Native Ollama on `http://localhost:11434`, then:

```powershell
dotnet run --project src\Orchestration\AppHost
```
