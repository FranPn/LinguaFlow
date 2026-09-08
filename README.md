# LinguaFlow

Conversational language tutor with real-time grammar correction, built as a personal portfolio project — an experiment in agentic architecture with Semantic Kernel and the Claude API.

Born out of trying to understand how apps like [Fluently](https://play.google.com/store/apps/details?id=app.getfluently.app) work: same core idea (AI conversation + corrections), built from scratch to practice Semantic Kernel, .NET, and multi-agent system design.

## What it does

- Free-form conversation in English or Swedish on a topic of choice
- Structured grammar corrections on every turn, without interrupting the flow of conversation
- Progress tracking over time: recurring errors by category, per-session trends

## Why two agents, not one

A single prompt that both converses naturally *and* corrects every mistake tends to fail at one of the two: it either breaks the flow to correct, or "forgets" to correct while focused on responding well. LinguaFlow splits these responsibilities into two agents called in parallel on the same turn:

- **ConversationAgent** — converses in the target language, asks follow-up questions, doesn't correct explicitly
- **CorrectionAgent** — receives only the latest user message, returns a structured JSON array of corrections (original, corrected, category, explanation)

## Stack

| Layer | Technology |
|---|---|
| Backend | .NET 8, ASP.NET Core Minimal API |
| LLM orchestration | Semantic Kernel + [official Anthropic SDK](https://www.nuget.org/packages/Anthropic) |
| Model | Claude Sonnet 5 |
| Persistence | SQLite + EF Core (owned entities for Session → Turns → Corrections) |
| Frontend | React + TypeScript + Vite, installable PWA |

## Architecture

```
src/
├── LinguaFlow.Core/            → Domain: ConversationSession, Correction, ISessionRepository, IProgressRepository (no external dependencies)
├── LinguaFlow.Infrastructure/  → Agents (Semantic Kernel), EF Core, repository implementations
├── LinguaFlow.Api/             → Minimal API, DI, HTTP endpoints
└── LinguaFlow.Web/             → React PWA
```

Strict Core/Infrastructure/Api separation: the domain layer knows nothing about Semantic Kernel, EF Core, or any implementation detail — it only depends on its own interfaces.

## Running locally

**Backend**
```bash
cd src/LinguaFlow.Api
dotnet user-secrets set "Anthropic:ApiKey" "your-key-here"
dotnet run
```

**Frontend**
```bash
cd src/LinguaFlow.Web
npm install
npm run dev
```

## Project status

- [x] Conversation + grammar corrections
- [x] SQLite persistence
- [x] Aggregated progress tracking
- [x] PWA frontend
- [ ] Voice input/output (Azure Speech STT/TTS)
- [ ] Pronunciation assessment (Azure Pronunciation Assessment) with targeted exercises
- [ ] Hardening: authentication, rate limiting, input validation

Actively in development, built without rushing as an architecture exercise more than a finished product.
