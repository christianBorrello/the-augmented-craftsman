# Wave Decisions: Author Mode DISCUSS

**Feature**: author-mode
**Wave**: DISCUSS
**Data**: 2026-03-14
**Product Owner**: Luna

---

## Decisioni Prese Durante la DISCUSS Wave

Questo file documenta le decisioni architetturali e di prodotto prese durante la DISCUSS wave, con il loro razionale. Sono vincolanti per la DESIGN wave.

---

### D-01: Approccio Admin — Astro Hybrid (NON SPA React separata)

**Decisione**: L'author-mode e' integrato nel progetto frontend Astro esistente, usando hybrid rendering (SSR per /admin/*, SSG per il blog pubblico).

**Razionale**: Un solo progetto, un solo deploy, un solo dominio. Nessuna complessita' infrastrutturale aggiuntiva. Il blog e' content-first — l'admin non ha bisogno di SPA-level interactivity.

**Alternativa scartata**: SPA React separata — avrebbe richiesto un terzo progetto, un terzo deploy, CORS, subdomain, state management.

**Riferimento**: `docs/brainstorm/author-mode-inplace-idea-brief.md`

---

### D-02: Autenticazione Admin — OAuth Google/GitHub + Whitelist ADMIN_EMAIL

**Decisione**: L'autore si autentica tramite OAuth Google o GitHub. Il backend .NET verifica che l'email corrisponda a `ADMIN_EMAIL` (env var). Nessun sistema di password aggiuntivo.

**Razionale**: Zero nuova infrastruttura auth. Il flow OAuth e' gia' presente nel progetto (per i commenti dei lettori). La sicurezza e' delegata a Google/GitHub che hanno 2FA, anomaly detection, ecc. Un solo autore = whitelist di 1 email.

**Alternativa scartata A**: Magic link via email — richiederebbe un servizio SMTP aggiuntivo.
**Alternativa scartata B**: Email + password — richiederebbe gestione password, recovery, rate limiting.
**Alternativa scartata C**: Credenziali in env var Astro — bypassa il backend (fonte di verita'), meno sicuro.
**Alternativa scartata D**: WebAuthn/Passkey — overkill per MVP, alta complessita'.

---

### D-03: In-Place Editing MVP — Toolbar + Link a Pagina Dedicata (NON Editor Inline)

**Decisione**: La Server Island `EditControls` mostra una toolbar sulla pagina pubblica. Il bottone [Modifica] e' un link a `/admin/posts/{id}/edit` (pagina SSR con editor Tiptap completo). Non c'e' editing inline sul DOM della pagina pubblica nell'MVP.

**Razionale**: Il contenuto della pagina e' HTML renderizzato da markdown. Attivare Tiptap su quell'HTML richiede conversione HTML → ProseMirror model → markdown con rischio di perdita di formattazione. Il layout della pagina non e' ottimizzato per `contentEditable`. Il beneficio dell'in-place (vedere il post nel contesto) e' preservato: l'autore legge, clicca [Modifica] dall'interno della pagina, viene portato nell'editor.

**Iterazione 2**: Tiptap inline sul corpo del testo — solo per il body, non per titolo/tag/immagine. Questo limita il rischio di conversione.

---

### D-04: Dopo Salvataggio Post Pubblicato — Spinner Rebuild + Redirect /blog/{slug}

**Decisione**: Dopo la pubblicazione (status: published), viene mostrato uno spinner "Pubblicazione in corso..." mentre attende il rebuild Vercel. Al completamento, redirect automatico a `/blog/{slug}`. Timeout dopo 60s con link manuale.

**Razionale**: Il feedback diretto sull'esito e' il momento di tensione massima del flusso. L'autore vuole vedere il post live. Il redirect al post pubblico chiude il ciclo emotivo (soddisfazione).

---

### D-05: Cancellazione Post — Soft Delete (Archived)

**Decisione**: Nessun hard delete nel MVP. I post vengono archiviati (`status: archived`), non cancellati. Sono recuperabili dalla tab "Archiviati".

**Razionale**: Contenuto che sembrava non piu' utile spesso lo torna ad essere. L'hard delete e' irreversibile e non ha nessun beneficio reale per un blog personale.

---

### D-06: Sessioni — Astro Sessions + Upstash Redis

**Decisione**: Le sessioni admin usano Astro Sessions (stable da v5.7) con driver Upstash Redis.

**Razionale**: L'unica opzione supportata con `@astrojs/vercel`. Il free tier Upstash (10K comandi/giorno) e' ampiamente sufficiente per un singolo autore (~100 comandi/giorno stimati).

**Dipendenza tecnica critica**: Upgrade Astro da 5.5 a 5.7+ necessario prima dell'implementazione.

---

### D-07: Scope MVP — 12 User Stories in 3 Release

**Decisione**: Il MVP include 12 user stories organizzate in:
- Walking Skeleton (S1-S5): login → crea bozza → vedi in lista
- Release 1: pubblicazione + upload + tags + editcontrols
- Release 2: archiviazione + feedback rebuild + ripristino

**Fuori scope MVP confermato**: auto-save, editor inline nativo, scheduling, versioning, multi-autore.

---

### D-08: Slug Immutabile dopo Prima Pubblicazione

**Decisione**: Lo slug di un post e' generato dal titolo alla creazione ed e' editabile fino alla prima pubblicazione. Dopo la prima pubblicazione, lo slug e' immutabile.

**Razionale**: Cambiare lo slug di un post pubblicato genera broken links (link esterni, RSS, bookmark). La stabilita' degli URL e' una best practice fondamentale per i blog.

---

## Decisioni Aperte (Red Cards)

| # | Questione | Priorita' | Chi risolve |
|---|---|---|---|
| RC-01 | Come funziona tecnicamente il callback OAuth dal backend .NET verso Astro Sessions? Il backend deve restituire i dati utente in un formato che Astro possa usare per `session.set()`. Serve un endpoint dedicato o si usa il flow OAuth standard con redirect a Astro? | Alta | DESIGN wave (solution-architect) |
| RC-02 | Il backend deve esporre `GET /api/posts?admin=true` che includa le bozze — questo endpoint e' pubblicamente accessibile? Serve protezione auth sul backend oltre all'auth Astro? | Alta | DESIGN wave |
| RC-03 | Quale e' il comportamento esatto della pagina /blog/{slug} quando il post e' archiviato? 404 nativo Astro o redirect a una pagina "Contenuto rimosso"? | Media | DESIGN wave |
| RC-04 | Il rebuild Vercel rigenera l'intero sito o solo la pagina del post modificato? ISR (Incremental Static Regeneration) e' configurato? | Media | DESIGN wave |

---

## Artefatti Prodotti

| File | Descrizione |
|---|---|
| `journey-author-mode-visual.md` | ASCII flow + mockup TUI per tutte e 4 le journey |
| `journey-author-mode.yaml` | Schema strutturato del journey con artefatti condivisi e Gherkin |
| `journey-author-mode.feature` | Scenari Gherkin completi per tutte le journey |
| `shared-artifacts-registry.md` | Registro artefatti condivisi con fonti e consumatori |
| `story-map.md` | Story map con backbone, walking skeleton e release slices |
| `prioritization.md` | Prioritizzazione con score V*U/E e MoSCoW |
| `requirements.md` | Documento requisiti completo (funzionali, NFR, business rules, rischi) |
| `user-stories.md` | 12 user stories complete con template LeanUX |
| `acceptance-criteria.md` | AC consolidate per tutte le stories |
| `dor-checklist.md` | DoR validation — tutte 12 PASSED |
| `outcome-kpis.md` | KPI di outcome con formula Who/Does What/By How Much |
| `wave-decisions.md` | Questo file — decisioni e red cards |

---

## Handoff a DESIGN Wave

**Stato**: Pronto per handoff a `nw:design` (solution-architect)

**Prossimi passi per la DESIGN wave**:
1. Risolvere RC-01: flow OAuth backend .NET → Astro Sessions
2. Risolvere RC-02: protezione API admin nel backend
3. Progettare la struttura del database per `previous_status` (US-12)
4. Definire la strategia ISR/rebuild Vercel (RC-04)
5. Progettare il sistema di routing per post archiviati (RC-03)

**Upgrade tecnico prerequisito**: Astro 5.5 → 5.7+ (Sessioni stabili) deve avvenire prima dell'inizio della DESIGN wave.
