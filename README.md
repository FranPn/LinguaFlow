# LinguaFlow

Tutor linguistico conversazionale con correzioni grammaticali in tempo reale, costruito come progetto portfolio personale — un esperimento di architettura agentica con Semantic Kernel e Claude API.

Nato dal tentativo di capire come funzionano app come [Fluently](https://play.google.com/store/apps/details?id=app.getfluently.app): stessa idea di base (conversazione AI + correzioni), costruita da zero per esercitare Semantic Kernel, .NET e progettazione di sistemi multi-agente.

## Cosa fa

- Conversazione libera in inglese o svedese su un argomento a scelta
- Correzioni grammaticali strutturate su ogni turno, senza interrompere il flusso della conversazione
- Tracking dei progressi nel tempo: errori ricorrenti per categoria, trend per sessione

## Perché due agenti, non uno

Un unico prompt che deve *sia* conversare naturalmente *sia* correggere ogni errore tende a fallire in uno dei due compiti: o interrompe il flusso per correggere, o "dimentica" di farlo mentre è concentrato sulla risposta. LinguaFlow separa le due responsabilità in due agenti chiamati in parallelo sullo stesso turno:

- **ConversationAgent** — conversa nella lingua target, fa domande di follow-up, non corregge esplicitamente
- **CorrectionAgent** — riceve solo l'ultimo messaggio utente, restituisce un array JSON di correzioni strutturate (originale, corretta, categoria, spiegazione)

## Stack

| Layer | Tecnologia |
|---|---|
| Backend | .NET 8, ASP.NET Core Minimal API |
| Orchestrazione LLM | Semantic Kernel + [Anthropic SDK ufficiale](https://www.nuget.org/packages/Anthropic) |
| Modello | Claude Sonnet 5 |
| Persistenza | SQLite + EF Core (owned entities per Session → Turns → Corrections) |
| Frontend | React + TypeScript + Vite, PWA installabile |

## Architettura

```
src/
├── LinguaFlow.Core/            → Dominio: ConversationSession, Correction, ISessionRepository, IProgressRepository (nessuna dipendenza esterna)
├── LinguaFlow.Infrastructure/  → Agenti (Semantic Kernel), EF Core, implementazioni repository
├── LinguaFlow.Api/             → Minimal API, DI, endpoint HTTP
└── LinguaFlow.Web/             → React PWA
```

Separazione Core/Infrastructure/Api rigorosa: il dominio non conosce Semantic Kernel, EF Core, o alcun dettaglio implementativo — dipende solo dalle proprie interfacce.

## Avvio in locale

**Backend**
```bash
cd src/LinguaFlow.Api
dotnet user-secrets set "Anthropic:ApiKey" "la-tua-chiave"
dotnet run
```

**Frontend**
```bash
cd src/LinguaFlow.Web
npm install
npm run dev
```

## Stato del progetto

- [x] Conversazione + correzioni grammaticali
- [x] Persistenza SQLite
- [x] Tracking progressi aggregato
- [x] Frontend PWA
- [ ] Input/output vocale (Azure Speech STT/TTS)
- [ ] Valutazione pronuncia (Azure Pronunciation Assessment) con esercizi mirati
- [ ] Hardening: autenticazione, rate limiting, validazione input

Progetto in sviluppo attivo, costruito senza fretta come esercizio di architettura più che come prodotto finito.
