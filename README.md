# Moser.RagAi

Intranet **policy assistant**: ask a question → retrieve relevant policy chunks → generate an answer from those chunks → run a **C# policy guardrail** (Allow / Deny / NeedsHuman).

This is a personal case-study / learning repo, not a client product. It shows a working RAG loop in .NET with an explicit guardrail that can still deny after the model speaks.

| Name on disk / GitHub | Value |
| --- | --- |
| Folder | `Moser.Enterprise.Blueprint` (historical) |
| Solution | `Moser.RagAi.sln` |
| Root namespace | `Moser.RagAi.*` |
| Remote | `github.com/moserkristian/Moser.RagAi` (repo renamed from Moser.Enterprise.Blueprint) |

## What it does

1. **Ingest** seed markdown under `data/seed/policy/` → chunk → embed → upsert into an in-memory vector index.
2. **Ask** → embed the question → vector search + light lexical filter → stream chat completion grounded on retrieved text.
3. **Policy** (deterministic C#) → Deny hard cases (secrets, bypass, insider trading…), Require human on conflicting numbers (refund windows, expense caps), Allow when grounded.

UI (Blazor): Home (how ingest works), Ask, Status (index + Ollama/stub), Legend (glossary).

## Stack

- .NET 10, Blazor Server (`src/Web`)
- .NET Aspire AppHost (`src/Orchestration/AppHost`) — wires Web + external native Ollama
- Microsoft.Extensions.AI + Ollama (`llama3.2` chat, `nomic-embed-text` embeddings)
- In-memory vector store (`CommunityToolkit.VectorData.InMemory`) — prototype, not Azure AI Search
- Stub chat/embed when Ollama is offline so the app still runs
- xUnit policy unit tests + golden-question evals

## Layout

```
data/seed/policy/     sample company policies (.md)
src/
  Modules/
    Ingestion/        read → chunk → embed → index
    Assistant/        Ask handler, Policy, Ollama/stub, evals
  Orchestration/      Aspire AppHost + ServiceDefaults
  Web/                Blazor UI hosting both modules
tests/
  Assistant.UnitTests/
  Tests/              Aspire-orchestrated tests
```

## Run

1. Install [Ollama](https://ollama.com/) on Windows and pull models (or let the helper script do it):

```powershell
.\Ensure-NativeOllama.ps1
```

Default endpoint: `http://127.0.0.1:11434`  
Models: `llama3.2`, `nomic-embed-text`

2. Start the app:

```powershell
dotnet run --project src\Orchestration\AppHost
```

3. Open the Aspire dashboard / Web external URL.  
   If Ollama is down at startup, Web uses **stub** mode (Status page explains how to reconnect).

## Tests

```powershell
dotnet test tests\Assistant.UnitTests\Assistant.UnitTests.csproj
dotnet test src\Modules\Assistant\Assistant.Evals\Assistant.Evals.csproj
```

Policy unit tests cover Deny / NeedsHuman / Allow without a live LLM.  
Evals run golden questions through the full Ask path (ingest + retrieve + generate + policy).

## Honest scope (what this is / is not)

**Is**

- A runnable RAG demo with citations and a post-generation C# guardrail
- Hybrid retrieval (vector + lexical overlap)
- Clear modular split: Ingestion vs Assistant vs Web
- Seed corpus large enough to show conflicts (e.g. refund 14 vs 30 days)

**Is not**

- Production IAM / multi-tenant SaaS
- Persistent vector DB or Azure AI Search
- Full agent framework / tool calling
- A finished “enterprise blueprint” platform — older Catalog leftovers were removed; name on disk is historical

## Case study (one paragraph for CV / LinkedIn)

Built a .NET policy Q&A demo: markdown policies → chunk/embed → hybrid retrieval → streamed answers with citations → C# Allow/Deny/NeedsHuman guardrail. Aspire + Blazor + local Ollama (stub fallback). Unit tests and golden evals for guardrail behaviour.

## License

All rights reserved by the author unless a LICENSE file says otherwise.
