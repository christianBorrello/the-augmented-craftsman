# Requirements: Author Mode

**Feature**: author-mode
**Data**: 2026-03-14
**Stato**: Ready for DESIGN wave

---

## Business Context

Il blog "The Augmented Craftsman" e' operativo con frontend Astro (Vercel) e backend .NET 10 (Koyeb), ma l'autore non ha nessun meccanismo web-based per pubblicare contenuti. Christian Borrello deve attualmente interagire con API o database direttamente. L'author-mode risolve questo gap abilitando la gestione completa del blog dal browser.

**Principio guida**: un solo progetto frontend, un solo deploy, un solo dominio. Zero impatto sulle performance del blog pubblico per i lettori.

---

## Scope MVP

### In Scope

| # | Capacita' | Story |
|---|---|---|
| 1 | Login admin tramite OAuth (Google/GitHub) + whitelist ADMIN_EMAIL | US-01 |
| 2 | Middleware auth guard su /admin/* | US-02 |
| 3 | Form creazione post con editor Tiptap | US-03 |
| 4 | Lista post con stato e filtri | US-04 |
| 5 | Salva post come bozza | US-05 |
| 6 | Upload immagine di copertina via ImageKit | US-06 |
| 7 | Toolbar EditControls sulla pagina pubblica (Server Island) | US-07 |
| 8 | Pubblicazione post con rebuild Vercel | US-08 |
| 9 | Gestione tag (selezione + creazione) | US-09 |
| 10 | Archiviazione post (soft delete) | US-10 |
| 11 | Feedback rebuild e gestione errori | US-11 |
| 12 | Ripristino post archiviato | US-12 |

### Fuori Scope (Iterazioni Successive)

- Auto-save con localStorage debounced
- Editor Tiptap inline sulla pagina pubblica (Pattern C nativo — Iterazione 2)
- Scheduling pubblicazione
- Versioning e cronologia modifiche
- Gestione commenti e moderazione
- Analytics e statistiche
- Multi-autore

---

## Architettura Decisionale (Vincoli per DESIGN wave)

Queste decisioni sono fisse — non appartengono alla DESIGN wave:

| Decisione | Scelta | Rationale |
|---|---|---|
| Approccio admin | Astro Hybrid (NON SPA React separata) | Un solo progetto, un solo deploy, un solo dominio |
| Auth | OAuth Google/GitHub + whitelist ADMIN_EMAIL | Zero nuova infrastruttura, sicurezza delegata al provider |
| Sessioni | Astro Sessions + Upstash Redis | Unica opzione supportata con @astrojs/vercel |
| In-place editing MVP | Server Island + link a /admin/posts/{id}/edit | Evita complessita' editor inline, rimanda a iterazione 2 |
| Dopo salvataggio | Spinner rebuild + redirect /blog/{slug} | Feedback diretto sull'esito della pubblicazione |
| Cancellazione post | Soft delete (archived) | Contenuto recuperabile, nessun hard delete |
| Editor contenuto | Tiptap via @preact/compat | Gia' validato come prototipo nel progetto |

---

## Requisiti Funzionali

### RF-01: Autenticazione e Sessioni

- L'autore si autentica tramite OAuth Google o GitHub
- Il backend .NET verifica che l'email OAuth corrisponda a `ADMIN_EMAIL` (env var)
- La sessione admin e' creata in Upstash Redis e accessibile via `Astro.session`
- Il cookie di sessione e' HTTP-only e Secure
- Il middleware blocca tutte le richieste `/admin/*` senza sessione valida
- La sessione scaduta reindirizza a `/admin/login` con messaggio esplicativo

### RF-02: Gestione Post — Creazione

- Il form di creazione post ha: Titolo (obbligatorio), Slug (auto-generato, editabile), Contenuto (editor Tiptap), Tag (multi-select + creazione), Immagine copertina (upload), Stato (bozza/pubblicato)
- Lo slug e' generato dal backend tramite il Value Object `Slug` nel dominio
- Il form usa Astro Actions per il submit (progressively enhanced)
- Errori backend preservano i dati del form

### RF-03: Gestione Post — Lista

- Lista SSR su `/admin/posts` in ordine cronologico inverso
- Filtro per stato: Tutti / Pubblicati / Bozze / Archiviati
- Azioni su ogni post: Modifica (link a /admin/posts/{id}/edit), Archivia
- Stato vuoto: messaggio con link a /admin/posts/new

### RF-04: Editing In-Place

- Server Island `EditControls` su ogni pagina blog: ritorna fragment vuoto per i lettori, toolbar per l'autore
- La toolbar mostra badge stato e link [Modifica] a /admin/posts/{id}/edit
- La pagina blog resta SSG per tutti i lettori

### RF-05: Pubblicazione e Rebuild

- I post con `status: published` triggerano il rebuild Vercel tramite hook
- Feedback visivo durante il rebuild (spinner + messaggio)
- Redirect a /blog/{slug} al completamento
- Timeout dopo 60s con link manuale
- Le bozze non triggerano rebuild

### RF-06: Archiviazione e Ripristino

- Soft delete con `status: archived`
- Modale di conferma prima dell'archiviazione
- Tab "Archiviati" nella lista post
- Ripristino al precedente stato (Published o Draft)

### RF-07: Gestione Media

- Upload immagine copertina: JPG, PNG, WebP, max 5MB
- Preview dell'immagine nel form dopo upload
- ImageKit CDN come storage permanente

### RF-08: Gestione Tag

- Autocomplete tag esistenti nel form
- Creazione nuovo tag inline dal form
- Rimozione tag selezionato (client-side)

---

## Requisiti Non-Funzionali

### Performance (NFR-01)

- Le pagine blog pubbliche (/blog/*) mantengono LCP < 1.5s dopo il deploy dell'author-mode
- La Server Island EditControls non aggiunge latenza percettibile per i lettori (fragment vuoto)
- Le pagine admin (/admin/*) possono avere cold start fino a 500ms (accettabile per un solo utente)

### Sicurezza (NFR-02)

- Nessun dato admin e' esposto in pagine SSG
- Il `post.id` nelle Server Island e' trasmesso come prop criptata (meccanismo Astro)
- `security.checkOrigin: true` abilitato in astro.config.mjs (protezione CSRF)
- Double-layer authorization: middleware + ogni action handler
- Sessioni invalidate dopo inattivita' prolungata (configurazione Redis TTL)

### Affidabilita' (NFR-03)

- In caso di errore backend: i dati del form sono preservati
- In caso di rebuild fallito: il post e' comunque salvato nel backend
- In caso di sessione scaduta: redirect chiaro a /admin/login

### Compatibilita' (NFR-04)

- Il sistema funziona su Chrome, Firefox, Safari (ultimi 2 major versions)
- Il form e' utilizzabile senza JavaScript avanzato (progressive enhancement via Astro Actions)
- L'editor Tiptap e' disabilitato su mobile (uso desktop only per l'MVP)

---

## Regole di Business

| ID | Regola |
|---|---|
| BR-01 | Un solo autore autorizzato (ADMIN_EMAIL). Nessun sistema di registrazione. |
| BR-02 | I post in bozza non sono mai accessibili ai lettori, anche tramite URL diretto |
| BR-03 | I post archiviati non sono accessibili ai lettori. L'URL restituisce 404 o redirect. |
| BR-04 | Lo slug di un post pubblicato e' immutabile dopo la prima pubblicazione (evita broken links) |
| BR-05 | L'immagine di copertina e' opzionale — i post possono essere pubblicati senza |
| BR-06 | I tag sono condivisi tra i post — la creazione di un tag e' globale, non per-post |
| BR-07 | Il rebuild Vercel deve essere triggerato per ogni modifica a post con status "published" |

---

## Dipendenze Tecniche

| Dipendenza | Stato | Note |
|---|---|---|
| Astro 5.7+ | Da fare — upgrade da 5.5 | Richiesto per Astro Sessions stabili |
| Upstash Redis | Da fare | Driver sessioni su Vercel — free tier sufficiente |
| Backend .NET — endpoint OAuth callback | Da fare | Verifica email + restituzione dati utente |
| Backend .NET — endpoint CRUD post | Da fare | POST/GET/PATCH /api/posts con auth admin |
| Backend .NET — endpoint tags | Da fare | GET /api/tags, POST /api/tags |
| Backend .NET — proxy upload ImageKit | Gia' disponibile (ImageKit SDK v4) | Adattare per upload da admin |
| Vercel Deploy Hook | Da fare | URL per rebuild trigger post-pubblicazione |
| `export const prerender = false` | Da fare | Su ogni pagina /admin/* |
| Tiptap prototipo | Gia' completato (task #3) | Configurazione compat preact validata |

---

## Rischi

| Rischio | Probabilita' | Impatto | Mitigazione |
|---|---|---|---|
| Astro Sessions + Upstash + OAuth .NET: combinazione non documentata | Media | Alto | US-01 come primo task implementato — valida l'assunzione prima del resto |
| `prerender = false` dimenticato su pagina admin | Alta | Critico | CI check grep + review checklist in DESIGN wave |
| Rebuild Vercel lento o instabile | Bassa | Medio | Timeout + link manuale (US-11) |
| Tiptap @preact/compat: possibili problemi di build | Media | Medio | Prototipo gia' validato; fallback: @astrojs/react con scoping |
| Slug immutabilita' non rispettata | Bassa | Alto | Business Rule BR-04 documentata esplicitamente per il backend |
