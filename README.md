# Moser.RagAi

Enterprise **RAG assistant** demo: pick a sector corpus → ask a question → retrieve relevant chunks → generate an answer from those chunks → run a **C# policy guardrail** (Allow / Deny / NeedsHuman).

Default corpus is **legal / law firm**. Other high-value demos: real estate, healthcare, finance, insurance, consulting, corporate HR.

This is a personal case-study / learning repo, not a client product. It shows a working RAG loop in .NET with an explicit guardrail that can still deny after the model speaks.

| | |
| --- | --- |
| Solution | `Moser.RagAi.sln` |
| Root namespace | `Moser.RagAi.*` |
| Remote | `github.com/moserkristian/Moser.RagAi` |

## What it does

1. **Ingest** office files under `data/seed/{scenario}/` (PDF, Word `.docx`/`.doc`, Outlook/Gmail `.eml`, stamped PNG scans + OCR sidecar) → convert → chunk → embed → upsert into **PostgreSQL + pgvector**.
2. **Ask** (Instagram Direct-style inbox) → embed the question → hybrid search (cosine + FTS) → stream a chat completion grounded on retrieved text. Switch scenario on Ask to clear and reload another corpus.
3. **Policy** (deterministic C#) → Deny hard cases (secrets, bypass, insider trading…), Require human on conflicting numbers (refund windows, expense caps), Allow when grounded.

UI (Blazor): Home, Ask (inbox + thread + sources), System status (runtime, mailbox, converters), How it works (pipeline + live parser).

## Stack

- .NET 10, Blazor Server (`src/Web`)
- .NET Aspire AppHost (`src/Orchestration/AppHost`) — Web + `pgvector/pgvector:pg17` + external native Ollama
- Microsoft.Extensions.AI + Ollama (`llama3.2` chat, `nomic-embed-text` embeddings)
- PostgreSQL **pgvector** (HNSW, hybrid vector + `simple` FTS) — memory cosine index only when no `ConnectionStrings:rag`
- Stub chat/embed when Ollama is offline so the app still runs
- xUnit policy unit tests + golden-question evals

## Layout

```
data/seed/
  legal/          Hoferová & Bartoš (SAK) — default
  real-estate/    Dunaj Reality
  healthcare/     Poliklinika Karlovka
  finance/        Karpaty Wealth o.c.p. (NBS)
  insurance/      Tatry Poisťovňa
  consulting/     Karpaty Advisory
  corporate/      Moser Slovakia s. r. o.
src/
  Modules/
    Ingestion/        convert → chunk → embed → pgvector
    Assistant/        Ask handler, Policy, Ollama/stub, evals
  Orchestration/      Aspire AppHost + ServiceDefaults
  Tools/SeedPack      materialize markdown drafts into office files
  Web/                Blazor UI
tests/
  Assistant.UnitTests/
  Tests/              Aspire-orchestrated tests
```

## Run

1. Docker Desktop (for Postgres/pgvector) and [Ollama](https://ollama.com/) on Windows:

```powershell
.\Ensure-NativeOllama.ps1
```

Default endpoint: `http://127.0.0.1:11434`  
Models: `llama3.2`, `nomic-embed-text`

2. Start the app (AppHost starts `pgvector/pgvector:pg17` and injects `ConnectionStrings:rag`):

```powershell
dotnet run --project src\Orchestration\AppHost
```

3. Open the Aspire dashboard / Web external URL.  
   If Ollama is down at startup, Web uses **stub** mode (System status explains how to reconnect).  
   If you run Web without AppHost, the assistant falls back to an in-process cosine index.

Regenerate office files from leftover `.md` drafts:

```powershell
dotnet run --project src\Tools\SeedPack -- data\seed
```

Configure default scenario in `appsettings.json` (`Assistant:Scenario`, e.g. `Legal`).

## Tests

```powershell
dotnet test tests\Assistant.UnitTests\Assistant.UnitTests.csproj
dotnet test src\Modules\Assistant\Assistant.Evals\Assistant.Evals.csproj
```

Policy unit tests cover Deny / NeedsHuman / Allow without a live LLM.  
Evals run golden questions through the full Ask path against the **corporate** seed (ingest + retrieve + generate + policy). Evals use the memory index (no Docker).

## Honest scope (what this is / is not)

**Is**

- A runnable RAG demo with citations and a post-generation C# guardrail
- Hybrid retrieval (pgvector cosine + FTS; lexical gate in the handler)
- Real office-file converters (PDF, Word, MIME mail, scan OCR sidecar)
- Multiple sector seeds (legal default; switch on Ask)

**Is not**

- Production IAM / multi-tenant SaaS
- A live Gmail OAuth connector (mailbox panel reads ingested `.eml` exports)
- Full agent framework / tool calling

## Case study (one paragraph for CV / LinkedIn)

Built a .NET enterprise RAG demo: Slovak office corpora (PDF/Word/mail/scans) → convert/OCR → chunk/embed → PostgreSQL pgvector hybrid retrieval → streamed answers with citations → C# Allow/Deny/NeedsHuman guardrail. Aspire + Blazor + local Ollama (stub fallback). Switchable demos for legal, real estate, and other high-value sectors. Unit tests and golden evals for guardrail behaviour.

## License

All rights reserved by the author unless a LICENSE file says otherwise.
