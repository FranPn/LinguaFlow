# LinguaFlow — Brief tecnico per continuare lo sviluppo

## Contesto
Clone concettuale semplificato dell'app "Fluently" (tutor linguistico AI conversazionale),
costruito come progetto portfolio personale. Obiettivo: pratica conversazionale in
inglese e svedese con correzioni strutturate e tracking progressi nel tempo.

## Stack deciso
- **Backend**: .NET 8, ASP.NET Core minimal API
- **Orchestration LLM**: Semantic Kernel + pacchetto ufficiale `Anthropic` (NuGet,
  NON `Anthropic.SDK` di tghamm che ha bug di compatibilità con Microsoft.Extensions.AI.Abstractions recenti)
- **LLM**: Claude Sonnet 5 via Anthropic API (chiave in user-secrets, key `Anthropic:ApiKey`)
- **STT/TTS**: Azure Speech (non ancora integrato) — scelto per coerenza con percorso
  di certificazione AI-103 dell'utente
- **Persistenza**: SQLite via EF Core — fatto (LinguaFlowDbContext, Sessions/Turns/
  Corrections come owned entities, migration applicata a startup)
- **Frontend**: PWA (Angular o React, non ancora iniziato) — installabile su home
  screen mobile, usabile "in giro" senza dipendere da un PC acceso, niente
  pubblicazione su store

## Struttura progetto esistente
```
LinguaFlow/
├── src/
│   ├── LinguaFlow.Api/              → Program.cs con endpoint minimal API
│   ├── LinguaFlow.Core/             → ConversationSession, ConversationTurn, Correction
│   └── LinguaFlow.Infrastructure/   → Agents/ConversationAgent.cs, Agents/CorrectionAgent.cs
├── tests/                           → non ancora creato
└── LinguaFlow.sln
```

## Decisione architetturale chiave: due agenti separati
Il ConversationAgent (system prompt in ConversationPrompts.BuildSystemPrompt) e il
CorrectionAgent (CorrectionPrompts.BuildSystemPrompt) sono deliberatamente separati
e chiamati in parallelo con Task.WhenAll sullo stesso turno utente:

- **ConversationAgent**: conversa naturalmente nella lingua target, NON corregge
  esplicitamente dentro la chat, fa domande di follow-up, risposte brevi (2-4 frasi)
- **CorrectionAgent**: riceve solo l'ultimo messaggio utente, risponde con un array
  JSON di correzioni strutturate (original/corrected/category/explanation), array
  vuoto se non ci sono errori

Motivo della separazione: un unico prompt che fa entrambe le cose tende o a
correggere troppo interrompendo il flusso, o a "dimenticare" di correggere quando
impegnato a rispondere bene — probabile causa delle review negative di Fluently
su correzioni inventate/mancanti.

## Endpoint implementati
- `GET /health` — healthcheck
- `POST /api/chat/test?message=...` — test grezzo diretto a IChatClient (da rimuovere
  o tenere solo per debug)
- `POST /api/session/start?targetLanguage=...&topic=...` — crea sessione, ritorna sessionId
- `POST /api/session/{id}/turn?message=...` — invia turno, ritorna { reply, corrections }
- `GET /api/progress?targetLanguage=...` — riepilogo aggregato su tutte le sessioni
  (opzionalmente filtrato per lingua): totale sessioni/turni/correzioni, conteggio
  per categoria d'errore, trend correzioni per sessione nel tempo

## Cosa manca (prossimi step)
1. ~~CorrectionAgent~~ — fatto: parsing JSON robusto con try/catch su chiamata LLM
   e su deserializzazione, estrazione tollerante a wrapping markdown
2. ~~Persistenza reale~~ — fatto: SQLite + EF Core, vedi sopra
3. **Integrazione Azure Speech**: STT per input vocale, TTS per risposta vocale del tutor
4. ~~Tracking progressi~~ — fatto: `IProgressRepository`/`EfProgressRepository` in
   Infrastructure, aggregazione in memoria su sessioni/turni/correzioni caricati da EF
   (owned collections annidate, più semplice che tradurre GroupBy in SQL). Esposto da
   `GET /api/progress`. Possibili estensioni future: filtro per intervallo di date,
   trend raggruppato per settimana invece che per sessione singola.
5. ~~Frontend PWA~~ — fatto: `src/LinguaFlow.Web` (React + Vite + TS, vite-plugin-pwa).
   Setup screen (lingua/argomento) → ChatScreen (bolle utente/assistente, correzioni
   inline sotto il turno utente, pulsante microfono disabilitato in attesa del punto 3)
   → ProgressScreen (chiama `/api/progress`). Client fetch in `src/api/client.ts`, tipi
   in `src/api/types.ts` che rispecchiano i record C#. CORS aggiunto in Program.cs per
   `http://localhost:5173` (dev server Vite). `.env.local` con `VITE_API_URL` (default
   `http://localhost:5204`, il profilo http di default di `dotnet run`).
   Bug fix collaterale in `EfSessionRepository.AddTurnsAsync`: i turni/correzioni hanno
   Id assegnato lato client (`Guid.NewGuid()` nel record), quindi EF Core li marcava
   Modified invece di Added quando raggiunti per fixup dalla navigation collection →
   `DbUpdateConcurrencyException` (UPDATE invece di INSERT, 0 righe interessate) al
   primo turno di ogni sessione. Fix: `db.Entry(entity).State = EntityState.Added`
   esplicito su turni e correzioni prima di `SaveChangesAsync`.
6. **Gestione latenza percepita**: la pipeline STT→LLM→TTS deve stare sotto 1-2
   secondi per sentirsi naturale in conversazione vocale — da tenere presente nel
   design degli endpoint audio

## Vincoli/preferenze dell'utente
- Sviluppo su WSL2 (Ubuntu), progetto in `~/LinguaFlow` (filesystem Linux nativo,
  NON `/mnt/c/...` per performance)
- .NET SDK 8.0.424 installato
- Preferenza per codice ben commentato e architettura pulita (separazione Core/
  Infrastructure/Api mantenuta rigorosamente — Core senza dipendenze esterne)
- Progetto personale/portfolio, non per pubblicazione commerciale immediata
