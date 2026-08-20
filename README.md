# Moser.RagAi

Intranet policy assistant: RAG (retrieval-augmented generation) plus a C# policy guardrail.

## Layout

*   **`src/Modules/Ingestion`** — read `.md` → chunk → embed → upsert into the index.
*   **`src/Modules/Assistant`** — ask, retrieve, generate, Policy guardrail.
*   **`src/Web`** — Blazor UI. Hosts both modules.
*   **`src/BuildingBlocks/BuildingBlocks.API`** — reserved API primitives (empty for now).

## Run

Native Ollama on `http://localhost:11434`, then:

```powershell
dotnet run --project src\Orchestration\AppHost
```
