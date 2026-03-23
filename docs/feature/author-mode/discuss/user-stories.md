<!-- markdownlint-disable MD024 -->
# User Stories: Author Mode

**Feature**: author-mode
**Data**: 2026-03-14
**Autore**: Christian Borrello

---

## US-01: Login admin tramite OAuth

### Problem

Christian Borrello e' l'unico autore di "The Augmented Craftsman". Attualmente non esiste nessun meccanismo per autenticarsi come admin nel frontend — l'unico modo per pubblicare un post e' interagire direttamente con l'API o il database. Questo rende la pubblicazione impossibile dal browser e impraticabile in mobilita'.

### Who

- Christian Borrello | unico autore, usa Google e GitHub quotidianamente | vuole accedere all'admin senza gestire password separate

### Solution

Una pagina `/admin/login` SSR con due bottoni OAuth (Google e GitHub). Il flow OAuth reindirizza al backend .NET che verifica che l'email corrisponda a `ADMIN_EMAIL` in env var. Se corrisponde, il backend risponde con i dati dell'utente e Astro crea una sessione admin server-side. Il cookie di sessione e' HTTP-only.

### Domain Examples

#### 1: Happy Path — Login con Google
Christian apre il browser, naviga su `the-augmented-craftsman.com/admin/login`. Clicca "Accedi con Google". Viene reindirizzato al flow OAuth di Google, approva l'accesso. Il backend verifica `christian.borrello@gmail.com == ADMIN_EMAIL`. La sessione viene creata. Christian vede `/admin/posts` con la lista dei suoi post.

#### 2: Email OAuth non autorizzata
Christian e' loggato su Chrome con un account Google secondario (`test.account@gmail.com`). Clicca "Accedi con Google", il flow completa, ma il backend rileva che `test.account@gmail.com != ADMIN_EMAIL`. Christian vede la pagina `/admin/login` con il banner "Account non autorizzato. Solo l'autore del blog puo' accedere." e puo' riprovare con l'account corretto.

#### 3: Sessione scaduta durante lavoro
Christian e' nel mezzo della compilazione di un post su `/admin/posts/new`. La sessione Redis scade dopo 24 ore di inattivita'. Al prossimo submit del form, il middleware blocca la richiesta e reindirizza a `/admin/login` con il banner "Sessione scaduta. Accedi di nuovo." I dati nel form sono persi (motivazione per auto-save nella iterazione 2).

### UAT Scenarios (BDD)

#### Scenario: Login con successo tramite Google
Given Christian visita /admin/login senza sessione attiva
When Christian clicca "Accedi con Google"
And il flow OAuth completa con christian.borrello@gmail.com
And il backend conferma l'email come admin autorizzata
Then Christian viene reindirizzato a /admin/posts
And la sessione admin e' attiva nel browser (cookie HTTP-only)
And il middleware permette l'accesso a tutte le pagine /admin/*

#### Scenario: Email OAuth non autorizzata
Given Christian visita /admin/login
When il flow OAuth completa con un email diverso da ADMIN_EMAIL
Then Christian rimane su /admin/login
And vede il messaggio "Account non autorizzato. Solo l'autore del blog puo' accedere."
And nessuna sessione admin viene creata

#### Scenario: Accesso diretto a pagina admin senza sessione
Given un visitatore senza sessione admin tenta di accedere direttamente a /admin/posts
When il middleware verifica la sessione
Then il visitatore viene reindirizzato a /admin/login
And non vede nessun contenuto admin

#### Scenario: Sessione scaduta
Given Christian e' autenticato con una sessione che scade in Redis
When Christian naviga su una qualsiasi pagina /admin/*
Then viene reindirizzato a /admin/login
And vede il banner "Sessione scaduta. Accedi di nuovo."

### Acceptance Criteria

- [ ] Pagina /admin/login mostra due bottoni: "Accedi con Google" e "Accedi con GitHub"
- [ ] Il flow OAuth delega al provider scelto e riporta il callback al backend .NET
- [ ] Il backend confronta l'email OAuth con ADMIN_EMAIL (env var) e risponde con i dati utente o errore
- [ ] Astro crea una sessione admin in Upstash Redis con `isAdmin: true`
- [ ] Il cookie di sessione e' HTTP-only e Secure
- [ ] Il middleware blocca tutte le richieste /admin/* senza sessione valida e reindirizza a /admin/login
- [ ] Email non autorizzata mostra messaggio specifico, nessuna sessione creata
- [ ] Sessione scaduta reindirizza a /admin/login con messaggio chiarificatore

### Outcome KPIs

- **Who**: Christian Borrello (autore)
- **Does what**: Completa il login admin e accede a /admin/posts
- **By how much**: 100% dei tentativi con email autorizzata hanno successo
- **Measured by**: Log sessioni Upstash Redis
- **Baseline**: 0% (funzionalita' non esiste)

### Technical Notes

- Richiede Astro 5.7+ per Sessioni stabili (upgrade da 5.5 necessario)
- Richiede Upstash Redis configurato (`UPSTASH_REDIS_REST_URL`, `UPSTASH_REDIS_REST_TOKEN` in Vercel env)
- La pagina /admin/login deve avere `export const prerender = false`
- Backend .NET necessita di un endpoint OAuth callback che accetti il token e verifichi l'email
- `security.checkOrigin: true` deve essere abilitato in astro.config.mjs
- Dipendenza esterna: Google OAuth app e GitHub OAuth app configurate nei provider

---

## US-02: Middleware auth guard per tutte le route /admin/*

### Problem

Senza un meccanismo di protezione centralizzato, ogni pagina admin dovrebbe duplicare la logica di verifica della sessione. In Astro, il middleware e' l'unico punto che intercetta tutte le richieste prima che raggiungano le pagine — ma funziona solo su pagine SSR. Se una pagina admin dimentica `export const prerender = false`, il middleware viene bypassato silenziosamente in produzione.

### Who

- Christian Borrello | autore | vuole che le pagine admin siano protette senza dover scrivere codice auth ripetuto
- Implicito: nessun lettore non autorizzato deve accedere ai dati admin

### Solution

Un middleware Astro (`src/middleware.ts`) che per ogni richiesta a `/admin/*` verifica la sessione. Se valida, inietta `context.locals.user` per tutte le pagine admin. Se invalida, reindirizza a `/admin/login`. La pagina `/admin/login` e' esclusa dal guard. Ogni pagina admin avra' esplicitamente `export const prerender = false`.

### Domain Examples

#### 1: Navigazione normale con sessione valida
Christian e' loggato. Naviga da `/admin/posts` a `/admin/posts/new` a `/admin/posts/42/edit`. Il middleware verifica la sessione a ogni cambio pagina, trova `isAdmin: true`, inietta `user` nei locals. Christian non nota nulla — semplicemente funziona.

#### 2: Accesso diretto senza sessione
Un lettore curioso digita `the-augmented-craftsman.com/admin/posts` nella barra degli indirizzi. Il middleware non trova sessione admin. Il lettore viene reindirizzato a `/admin/login`. Non vede nessun dato dei post.

#### 3: Pagina admin senza prerender=false (scenario di errore da prevenire)
Se una pagina admin fosse accidentalmente prerendered (mancanza di `export const prerender = false`), il middleware non verrebbe eseguito su Vercel e la pagina sarebbe accessibile senza autenticazione. La soluzione e' avere un test CI che verifica la presenza del flag su tutte le pagine `/admin/*`.

### UAT Scenarios (BDD)

#### Scenario: Middleware permette accesso con sessione valida
Given Christian ha una sessione admin valida
When Christian naviga su /admin/posts
Then la pagina viene servita normalmente
And `Astro.locals.user` contiene i dati di Christian

#### Scenario: Middleware blocca accesso senza sessione
Given un visitatore senza sessione
When visita /admin/posts/new
Then viene reindirizzato a /admin/login
And non vede nessun contenuto della pagina

#### Scenario: Pagina di login non e' protetta dal guard
Given un visitatore senza sessione
When visita /admin/login
Then vede la pagina di login normalmente (il middleware non la blocca)

#### Scenario: Action admin bloccata senza sessione
Given un utente senza sessione admin
When tenta di chiamare l'Action admin.createPost direttamente
Then l'Action risponde con ActionError code "UNAUTHORIZED"
And nessun dato viene scritto sul backend

### Acceptance Criteria

- [ ] Il middleware verifica la sessione per ogni richiesta a `/admin/*` (eccetto `/admin/login`)
- [ ] Con sessione valida: `context.locals.user` viene popolato con i dati dell'autore
- [ ] Senza sessione valida: redirect a `/admin/login`
- [ ] La pagina `/admin/login` non e' soggetta al guard
- [ ] Ogni Action admin verifica `context.locals.user` e lancia `UNAUTHORIZED` se assente
- [ ] Il middleware usa `getActionContext()` per bloccare anche le Action non autenticate
- [ ] `security.checkOrigin: true` protegge da CSRF su tutte le Action

### Technical Notes

- Implementare double-layer authorization: (1) middleware con `getActionContext()`, (2) ogni action handler
- Il middleware gira come serverless function su Vercel (non edge) — latenza accettabile per l'autore
- Dipende da US-01 (sessione admin creata)
- CI check consigliato: grep per `prerender = false` in tutti i file sotto `src/pages/admin/`

---

## US-03: Form crea nuovo post con editor Tiptap

### Problem

Christian non ha nessun modo di scrivere un post direttamente dal browser. Deve usare strumenti esterni (editor di testo, chiamate curl all'API) e gestire il markdown manualmente. Questo rende la pubblicazione lenta e tecnica, non creativa.

### Who

- Christian Borrello | autore | si trova in flusso creativo e vuole scrivere senza frizione tecnica

### Solution

Una pagina SSR `/admin/posts/new` con un form per la creazione di un post. Il form include: campo titolo (testo), campo slug (auto-generato dal titolo, editabile), editor Tiptap per il contenuto (Preact island con `client:only="preact"`), selezione tag, upload immagine copertina, scelta stato (bozza/pubblicato). L'editor e' headless con toolbar floating.

### Domain Examples

#### 1: Creazione post completo
Christian ha appena finito di riflettere su un pattern DDD. Apre `/admin/posts/new`, scrive il titolo "Domain Events in un blog CRUD: overkill o necessita'?". Lo slug si aggiorna automaticamente a `domain-events-in-un-blog-crud-overkill-o-necessita`. Scrive il corpo nell'editor Tiptap, aggiunge codice C# con il blocco codice, seleziona i tag "DDD" e "Clean Architecture", carica un'immagine PNG da 800KB. Sceglie "Pubblica ora" e clicca "Pubblica".

#### 2: Bozza veloce da un'idea
Christian ha un'idea a meta' giornata ma non ha tempo di svilupparla. Apre il form, scrive solo il titolo "Refactoring verso il Pattern Strangler Fig" e qualche riga di note nel corpo. Seleziona "Bozza" e clicca "Salva bozza". Il post e' salvato, torna al lavoro e completa il post la sera.

#### 3: Slug in conflitto
Christian prova a pubblicare un post con titolo identico a uno esistente. Il backend restituisce un errore "Slug gia' esistente". Il form rimane aperto con tutti i dati preservati. Christian modifica manualmente il campo slug aggiungendo un suffisso (`-v2`) e salva con successo.

### UAT Scenarios (BDD)

#### Scenario: Autore apre il form per un nuovo post
Given Christian e' autenticato come admin
When naviga su /admin/posts/new
Then vede il form con i campi: Titolo, Slug, Contenuto (editor Tiptap), Tag, Immagine copertina, Stato
And il campo Slug e' vuoto e editabile
And l'editor Tiptap e' renderizzato lato client senza errori SSR

#### Scenario: Slug generato automaticamente dal titolo
Given Christian e' su /admin/posts/new
When digita "Domain Events in un blog CRUD" nel campo Titolo
Then il campo Slug viene pre-compilato con "domain-events-in-un-blog-crud"
And lo slug rimane editabile manualmente

#### Scenario: Salva post come bozza
Given Christian ha compilato il form con titolo "Domain Events" e corpo "Il test outside-in..."
And ha selezionato stato "Bozza"
When clicca "Salva bozza"
Then il post viene creato sul backend con status "draft"
And Christian viene reindirizzato a /admin/posts
And "Domain Events" appare in lista con badge "Bozza"
And il post non e' accessibile su /blog/domain-events

#### Scenario: Validazione titolo obbligatorio
Given Christian e' su /admin/posts/new
And ha lasciato il campo Titolo vuoto
When clicca "Salva bozza"
Then vede il messaggio "Il titolo e' obbligatorio"
And il form non viene inviato
And i dati del contenuto sono preservati

#### Scenario: Backend non raggiungibile al salvataggio
Given Christian ha compilato il form completo
And il backend .NET non e' raggiungibile
When clicca "Salva bozza"
Then vede il banner "Impossibile salvare. Controlla la connessione e riprova."
And rimane su /admin/posts/new con i dati preservati

### Acceptance Criteria

- [ ] Pagina /admin/posts/new ha `export const prerender = false`
- [ ] Il campo Titolo e' obbligatorio (validazione client e server)
- [ ] Lo Slug viene generato automaticamente dal Titolo (logica di slugificazione nel backend)
- [ ] Lo Slug e' editabile manualmente dall'autore
- [ ] L'editor Tiptap e' un'isola Preact con `client:only="preact"` e `immediatelyRender: false`
- [ ] L'editor supporta: paragrafo, heading H2/H3, grassetto, corsivo, codice inline, blocco codice, link
- [ ] Il form non viene inviato se il Titolo e' vuoto
- [ ] In caso di errore backend, i dati del form sono preservati e viene mostrato un messaggio specifico
- [ ] Slug duplicato mostra errore specifico con campo slug evidenziato

### Outcome KPIs

- **Who**: Christian Borrello (autore)
- **Does what**: Pubblica un post dal form /admin/posts/new senza accedere a DB o API
- **By how much**: 1 post pubblicato entro la prima settimana di utilizzo
- **Measured by**: Numero di post status=published creati via admin UI
- **Baseline**: 0 (canale assente)

### Technical Notes

- Tiptap richiede `@tiptap/react` + `@preact/compat` (gia' configurato nel progetto)
- `immediatelyRender: false` e' obbligatorio in `useEditor()` per evitare errori SSR
- Il form usa Astro Actions (`admin.createPost`) — submit progressivamente migliorato
- Il backend .NET deve esporre `POST /api/posts` con autenticazione admin
- La slugificazione avviene nel backend (Value Object `Slug` nel dominio)
- Dipende da US-01 e US-02

---

## US-04: Lista post con stato e filtri

### Problem

Christian non ha nessuna visione d'insieme dei post del blog. Non sa quanti post sono pubblicati, quanti in bozza, quali potrebbe voler modificare. Deve interrogare il database o chiamare l'API per avere queste informazioni.

### Who

- Christian Borrello | autore | vuole il controllo del proprio blog con una singola pagina

### Solution

Una pagina SSR `/admin/posts` che mostra tutti i post in ordine cronologico inverso. Ogni riga mostra: titolo (troncato), badge stato (Pubblicato/Bozza/Archiviato), data creazione, azioni [Modifica][Archivia]. I tab "Tutti / Pubblicati / Bozze / Archiviati" filtrano la lista. Il bottone "+ Nuovo post" e' visibile in alto a destra.

### Domain Examples

#### 1: Visione d'insieme del blog attivo
Christian apre `/admin/posts` e vede 15 post. 12 pubblicati, 2 bozze, 1 archiviato. I badge colorati gli permettono di identificare a colpo d'occhio lo stato di ogni post. Clicca il tab "Bozze" e vede solo i 2 post non finiti.

#### 2: Navigazione rapida all'editor
Christian legge la lista e vuole modificare "Clean Architecture per un blog CRUD" (pubblicato il 10/03). Clicca [M] (Modifica) sulla riga corrispondente. Viene portato a `/admin/posts/38/edit` con il form pre-compilato.

#### 3: Lista vuota al primo accesso
Christian ha appena configurato il sistema ma non ha ancora scritto nessun post. La lista mostra "Nessun post ancora. Scrivi il tuo primo post!" con un link a `/admin/posts/new`.

### UAT Scenarios (BDD)

#### Scenario: Autore vede la lista completa dei post
Given Christian e' autenticato come admin
And esistono 12 post pubblicati e 2 bozze nel backend
When Christian naviga su /admin/posts
Then vede 14 post in ordine cronologico inverso (piu' recente prima)
And ogni riga mostra: titolo, badge stato, data, azioni [M][A]
And i tab Tutti/Pubblicati/Bozze/Archiviati sono visibili

#### Scenario: Filtro per bozze
Given Christian e' su /admin/posts con 12 pubblicati e 2 bozze
When clicca il tab "Bozze"
Then vede solo i 2 post in bozza
And il conteggio mostra "2 bozze"

#### Scenario: Lista vuota al primo accesso
Given Christian e' autenticato e non ha ancora creato post
When naviga su /admin/posts
Then vede il messaggio "Nessun post ancora. Scrivi il tuo primo post!"
And vede il link "+ Scrivi il primo post" che porta a /admin/posts/new

#### Scenario: Navigazione rapida all'editor dalla lista
Given Christian e' su /admin/posts
When clicca [M] sul post "Clean Architecture per un blog CRUD"
Then viene reindirizzato a /admin/posts/38/edit
And il form e' pre-compilato con i dati del post

### Acceptance Criteria

- [ ] Pagina /admin/posts ha `export const prerender = false`
- [ ] I post sono mostrati in ordine cronologico inverso (piu' recente prima)
- [ ] Ogni riga mostra: titolo (max 60 caratteri, troncato), badge stato, data (DD/MM/YYYY), [M][A]
- [ ] Tab Tutti/Pubblicati/Bozze/Archiviati filtrano la lista
- [ ] Se la lista e' vuota: messaggio con link a /admin/posts/new
- [ ] Il bottone "+ Nuovo post" e' sempre visibile in header
- [ ] [M] porta a /admin/posts/{id}/edit
- [ ] [A] avvia il flusso di archiviazione (US-10)

### Technical Notes

- Il backend deve esporre `GET /api/posts?status=all&admin=true` (lista completa per l'admin)
- Il filtro per stato puo' essere client-side (JavaScript) o server-side (query param) — soluzione-neutral
- Dipende da US-01 e US-02

---

## US-05: Salva post come bozza

### Problem

Christian a volte inizia un post senza avere il tempo di finirlo. Ha bisogno di poter salvare il lavoro parziale senza pubblicarlo, e ritrovarlo in seguito per completarlo.

### Who

- Christian Borrello | autore con sessioni di scrittura frammentate | vuole salvare il progresso senza pubblicare

### Solution

Il form di creazione e di edit hanno un pulsante "Salva bozza" separato da "Pubblica". Cliccando "Salva bozza", il post viene salvato con `status: draft`. Nessun rebuild Vercel viene triggerato. L'autore viene reindirizzato a `/admin/posts` e vede il post nella lista con badge "Bozza".

### Domain Examples

#### 1: Salvataggio bozza veloce
Christian e' sul treno, ha 20 minuti. Apre `/admin/posts/new`, scrive il titolo e le prime 3 sezioni di un post su TDD. Clicca "Salva bozza". Viene riportato alla lista, vede "TDD esteso: outer loop" con badge "Bozza". La sera riapre il post dalla lista e lo completa.

#### 2: Modifica di una bozza esistente
Christian riapre la bozza dalla lista, clicca [M], arriva a `/admin/posts/43/edit`. Completa il post, clicca di nuovo "Salva bozza" per un ulteriore controllo. Poi clicca "Pubblica" per renderlo visibile.

#### 3: Bozza con titolo obbligatorio
Christian prova a salvare una bozza con il campo titolo vuoto. Il form segnala l'errore "Il titolo e' obbligatorio" senza inviare la richiesta.

### UAT Scenarios (BDD)

#### Scenario: Salva post parziale come bozza
Given Christian e' su /admin/posts/new con titolo "TDD esteso" e contenuto parziale
When clicca "Salva bozza"
Then il post viene salvato con status "draft"
And Christian viene reindirizzato a /admin/posts
And il post appare in lista con badge "Bozza"
And il post non e' accessibile su /blog/tdd-esteso

#### Scenario: Nessun rebuild per le bozze
Given Christian salva un post come bozza
When l'Action admin.createPost viene eseguita
Then nessuna chiamata al rebuild hook Vercel viene effettuata
And il redirect avviene immediatamente a /admin/posts (nessuno spinner)

#### Scenario: Bozza aggiornata da edit
Given Christian e' su /admin/posts/43/edit per una bozza esistente
When aggiunge contenuto e clicca "Salva bozza"
Then la bozza viene aggiornata nel backend
And Christian viene reindirizzato a /admin/posts
And il post rimane in stato "Bozza"

### Acceptance Criteria

- [ ] Il pulsante "Salva bozza" salva il post con `status: draft`
- [ ] Le bozze non triggerano il rebuild Vercel
- [ ] Dopo il salvataggio come bozza: redirect a /admin/posts
- [ ] Le bozze non sono accessibili su /blog/{slug} (il backend non le serve)
- [ ] Il titolo e' obbligatorio anche per le bozze
- [ ] I dati del form sono preservati in caso di errore backend

### Technical Notes

- Il backend deve supportare `POST /api/posts` con `status: "draft"`
- Il backend non deve includere le bozze nelle risposte pubbliche del blog
- Dipende da US-03 (form creazione) e US-01/02 (auth)

---

## US-06: Upload immagine di copertina

### Problem

Ogni post del blog ha (o dovrebbe avere) un'immagine di copertina. Attualmente Christian deve caricare le immagini manualmente su ImageKit e copiare l'URL nel database — un processo manuale e frammentato.

### Who

- Christian Borrello | autore | vuole caricare un'immagine di copertina dal form senza strumenti esterni

### Solution

Il form di creazione e di edit include un campo upload per l'immagine di copertina. Quando Christian seleziona un file, una Action Astro lo invia al backend .NET che lo carica su ImageKit e restituisce l'URL CDN. Una preview dell'immagine appare nel form. L'URL viene salvato insieme al post.

### Domain Examples

#### 1: Upload immagine valida
Christian seleziona `forge-and-ink-cover.png` (650KB, 1200x630px). La preview appare nel form in pochi secondi. Al salvataggio del post, l'immagine e' visibile nella pagina pubblica tramite URL ImageKit con trasformazioni automatiche.

#### 2: File troppo grande
Christian prova a caricare uno screenshot a 12MB. Il sistema mostra "File troppo grande. Dimensione massima: 5MB." Il form rimane aperto, il post puo' essere salvato senza immagine.

#### 3: Formato non supportato
Christian prova a caricare un file `.bmp`. Il sistema mostra "Formato non supportato. Usa JPG, PNG o WebP." Il form rimane aperto.

### UAT Scenarios (BDD)

#### Scenario: Upload immagine valida
Given Christian e' su /admin/posts/new
When seleziona un file JPG da 800KB
Then il file viene caricato su ImageKit via backend .NET
And una preview dell'immagine appare nel form
And l'URL ImageKit viene associato al post al salvataggio

#### Scenario: File troppo grande
Given Christian e' su /admin/posts/new
When seleziona un file da 8MB
Then vede il messaggio "File troppo grande. Dimensione massima: 5MB."
And nessun upload viene avviato
And il form rimane modificabile

#### Scenario: Post salvato senza immagine
Given Christian e' su /admin/posts/new senza aver caricato un'immagine
When clicca "Salva bozza"
Then il post viene salvato correttamente senza immagine di copertina
And la pagina pubblica usa l'immagine di fallback del blog

### Acceptance Criteria

- [ ] Il campo upload accetta JPG, PNG, WebP con dimensione massima 5MB
- [ ] La validazione del tipo e della dimensione avviene prima dell'upload (client-side)
- [ ] Dopo upload riuscito: preview dell'immagine nel form
- [ ] L'URL ImageKit viene salvato nel post al submit del form
- [ ] In caso di upload fallito: messaggio specifico, il post e' salvabile senza immagine
- [ ] Formato non supportato: messaggio specifico

### Technical Notes

- Il backend .NET usa ImageKit SDK v4 per il caricamento
- L'Action `admin.uploadCoverImage` riceve il file come multipart form data
- Il backend restituisce l'URL CDN con trasformazioni (es. `?tr=w-1200,h-630,c-at_max`)
- Dipende da US-03 (form), US-01/02 (auth)

---

## US-07: Toolbar EditControls sulla pagina pubblica

### Problem

Quando Christian legge un suo post pubblicato e vuole correggerlo, deve aprire un altro tab, navigare all'admin, trovare il post nella lista e poi aprire l'editor. Il context-switch e' fastidioso e interrompe il flusso di lettura.

### Who

- Christian Borrello | autore che legge i propri post | vuole modificare un post dalla pagina pubblica senza perdere il contesto

### Solution

Una Server Island `EditControls` viene inclusa in ogni pagina blog. Per i lettori ritorna HTML vuoto (zero impatto). Per l'autore autenticato ritorna una toolbar floating con: badge stato del post ([Pubblicato]/[Bozza]) e bottone [Modifica] che porta a `/admin/posts/{id}/edit`. La pagina blog resta SSG per tutti i lettori.

### Domain Examples

#### 1: Autore corregge un errore tipografico
Christian legge "TDD con .NET 10" su `/blog/tdd-con-dotnet-10` e nota un errore tipografico. Vede la toolbar in alto a destra con [Pubblicato][Modifica]. Clicca [Modifica], arriva all'editor, corregge, salva. Il rebuild aggiorna la pagina statica. Viene reindirizzato al post corretto.

#### 2: Lettore non vede nessun elemento extra
Marco, un lettore, visita lo stesso post. La Server Island fa una richiesta al server, trova nessuna sessione admin, restituisce un fragment vuoto. Marco vede la pagina esattamente come tutti gli altri lettori — zero differenze.

#### 3: Badge "Bozza" su post non pubblicato
Christian visita la pagina di una bozza tramite link diretto (es. per un'anteprima). La toolbar mostra [Bozza] con colore distinto (grigio/arancio), segnalando che il post non e' ancora pubblico.

### UAT Scenarios (BDD)

#### Scenario: Autore autenticato vede la toolbar
Given Christian e' autenticato come admin
And visita /blog/tdd-con-dotnet-10
When la Server Island EditControls viene renderizzata dal server
Then la toolbar floating appare con badge [Pubblicato] e bottone [Modifica]
And la toolbar non altera il layout della pagina per i lettori

#### Scenario: Lettore non vede la toolbar
Given un lettore visita /blog/tdd-con-dotnet-10 senza sessione admin
When la Server Island EditControls fa la richiesta al server
Then viene restituito un fragment HTML vuoto
And nessun elemento extra e' visibile nel DOM della pagina
And il LCP della pagina non e' impattato dalla Server Island

#### Scenario: Autore clicca Modifica
Given Christian vede la toolbar su /blog/tdd-con-dotnet-10
When clicca [Modifica]
Then viene reindirizzato a /admin/posts/42/edit
And il form e' pre-compilato con i dati attuali del post

#### Scenario: Badge Bozza su post non pubblicato
Given Christian visita la pagina di una bozza
When la toolbar viene renderizzata
Then il badge mostra [Bozza] con stile visivo distinto da [Pubblicato]

### Acceptance Criteria

- [ ] La Server Island `EditControls` usa `server:defer` nella pagina blog
- [ ] Per lettori non autenticati: restituisce fragment vuoto, zero HTML visibile
- [ ] Per l'autore autenticato: mostra badge stato e bottone [Modifica]
- [ ] Il badge stato corrisponde allo stato attuale del post nel backend
- [ ] Il bottone [Modifica] porta a /admin/posts/{id}/edit pre-compilato
- [ ] Il fallback della Server Island e' un fragment vuoto (nessun placeholder visibile)
- [ ] Il `post.id` viene passato alla Server Island come prop criptata (non esposta in chiaro)
- [ ] La pagina blog resta SSG — nessuna modifica al rendering per i lettori

### Technical Notes

- Le props della Server Island sono criptate nativamente da Astro — il post.id non e' leggibile dall'HTML
- La Server Island usa `Astro.cookies` per verificare la sessione (non `Astro.session` — per compatibilita' con le pagine SSG)
- Il `Referer` header puo' essere usato per ottenere l'URL della pagina corrente (l'`Astro.url` nella Server Island non e' l'URL del blog)
- Dipende da US-01 (sessione)

---

## US-08: Pubblicazione post con rebuild Vercel

### Problem

Salvare un post come "pubblicato" nel database non basta — la pagina statica del blog deve essere rigenerata affinche' i lettori vedano il contenuto. Il rebuild Vercel avviene automaticamente su push git, ma non su aggiornamenti via API. Serve un meccanismo che triggeri il rebuild dopo la pubblicazione.

### Who

- Christian Borrello | autore | vuole vedere il suo post live sul sito pochi secondi dopo aver cliccato "Pubblica"

### Solution

Dopo aver salvato un post con `status: published`, l'Action `admin.publishPost` chiama il rebuild hook Vercel (`VERCEL_DEPLOY_HOOK_URL`). Il frontend mostra uno spinner con messaggio "Pubblicazione in corso..." mentre attende il completamento. Al completamento, reindirizza automaticamente a `/blog/{slug}`. In caso di timeout o errore del rebuild, mostra un messaggio con link manuale.

### Domain Examples

#### 1: Pubblicazione normale
Christian clicca "Pubblica" su un post completo. Vede lo spinner "Pubblicazione in corso... Il post sara' visibile ai lettori tra qualche secondo." Dopo 20-30 secondi, viene automaticamente reindirizzato a `/blog/domain-events-in-un-blog-crud` e vede il post live. Apre una finestra in incognito per verificare — il post e' accessibile.

#### 2: Rebuild lento (connessione lenta o Vercel congestionata)
Il rebuild impiega piu' del previsto (timeout dopo 60s). Lo spinner si trasforma in "Il post e' stato salvato. Il sito si aggiornera' a breve." con un link "Vedi il post" che porta a `/blog/{slug}`. Christian puo' aspettare o navigare manualmente.

#### 3: Aggiornamento di un post gia' pubblicato
Christian modifica un post esistente gia' pubblicato. Dopo il salvataggio, il rebuild rigenera solo la pagina del post modificato (o l'intero sito, a seconda della configurazione Vercel). Il flusso e' identico alla prima pubblicazione.

### UAT Scenarios (BDD)

#### Scenario: Pubblicazione post con rebuild completato
Given Christian ha compilato il form con un post completo
And ha selezionato "Pubblica ora"
When clicca "Pubblica"
Then il post viene salvato nel backend con status "published"
And viene mostrato lo spinner "Pubblicazione in corso..."
And il rebuild hook Vercel viene chiamato
And al completamento Christian viene reindirizzato a /blog/{slug}
And il post e' accessibile ai lettori

#### Scenario: Rebuild timeout
Given Christian ha pubblicato un post
And il rebuild Vercel non completa entro 60 secondi
Then lo spinner si trasforma in "Il post e' stato salvato. Il sito si aggiornera' a breve."
And viene mostrato un link manuale "Vedi il post" a /blog/{slug}
And il post risulta comunque salvato nel backend

#### Scenario: Modifica di post gia' pubblicato
Given Christian e' su /admin/posts/42/edit per un post pubblicato
When modifica il titolo e clicca "Salva modifiche"
Then il post viene aggiornato nel backend
And il rebuild hook Vercel viene chiamato
And al completamento Christian viene reindirizzato a /blog/{slug} con il titolo aggiornato

### Acceptance Criteria

- [ ] Cliccare "Pubblica" salva il post con `status: published` nel backend
- [ ] Dopo il salvataggio: chiama il Vercel rebuild hook (`VERCEL_DEPLOY_HOOK_URL` in env)
- [ ] Durante il rebuild: mostra spinner "Pubblicazione in corso..."
- [ ] Dopo rebuild completato: redirect a /blog/{slug}
- [ ] In caso di timeout (> 60s): mostra messaggio con link manuale a /blog/{slug}
- [ ] In caso di errore del rebuild: il post e' comunque salvato nel backend (non si perde il contenuto)
- [ ] Le bozze aggiornate non triggerano il rebuild

### Technical Notes

- Il Vercel rebuild hook e' una URL configurata in `VERCEL_DEPLOY_HOOK_URL` (env var)
- Il polling del rebuild puo' essere fatto tramite Vercel REST API o con timeout fisso
- Dipende da US-03 (form creazione), US-05 (salva bozza) — condivide la stessa Action con status diverso

---

## US-09: Gestione tag (selezione e creazione)

### Problem

I post del blog devono essere categorizzati con tag per permettere la navigazione per tema. Christian deve poter selezionare tag esistenti e crearne di nuovi direttamente dal form del post, senza dover pre-popolare il database manualmente.

### Who

- Christian Borrello | autore | vuole taggare i post senza uscire dal form di creazione

### Solution

Il form di creazione e di edit include un campo tag multi-select. Quando Christian digita, viene mostrato un autocomplete con i tag esistenti. Se il tag cercato non esiste, puo' crearlo con un click. I tag selezionati appaiono come badge rimovibili. Al salvataggio, i tag vengono associati al post tramite il backend.

### Domain Examples

#### 1: Selezione tag esistente
Christian scrive "TDD" nel campo tag. L'autocomplete mostra "TDD" nella lista dei tag esistenti. Christian clicca su "TDD" — il badge appare nel campo, Christian e' pronto per aggiungerne altri.

#### 2: Creazione nuovo tag
Christian sta scrivendo un post su "Event Sourcing" — un tag che non esiste ancora. Digita "Event Sourcing" nel campo, l'autocomplete non trova corrispondenze e mostra "+ Crea tag 'Event Sourcing'". Christian clicca. Il tag viene creato nel backend e aggiunto al post.

#### 3: Rimozione tag selezionato
Christian ha aggiunto per errore il tag ".NET" a un post che riguarda solo Astro. Clicca la [x] sul badge ".NET". Il tag viene rimosso dalla selezione (senza chiamare il backend — e' una modifica locale al form).

### UAT Scenarios (BDD)

#### Scenario: Selezione tag esistente dall'autocomplete
Given Christian e' su /admin/posts/new
And nel backend esistono i tag "TDD", ".NET", "Clean Architecture"
When Christian digita "TDD" nel campo tag
Then l'autocomplete mostra "TDD" nella lista
When Christian clicca su "TDD"
Then "TDD" appare come badge selezionato nel form
And viene incluso nel post al salvataggio

#### Scenario: Creazione nuovo tag
Given Christian e' su /admin/posts/new
And il tag "Event Sourcing" non esiste nel backend
When Christian digita "Event Sourcing" nel campo tag
Then l'autocomplete non trova corrispondenze
And mostra l'opzione "+ Crea tag 'Event Sourcing'"
When Christian clicca sull'opzione
Then il tag "Event Sourcing" viene creato nel backend
And appare come badge selezionato nel form

#### Scenario: Rimozione tag selezionato
Given Christian ha selezionato i tag "TDD" e ".NET" nel form
When clicca [x] sul badge ".NET"
Then ".NET" viene rimosso dalla selezione del form
And non sara' incluso nel post al salvataggio

### Acceptance Criteria

- [ ] Il campo tag mostra i tag esistenti in autocomplete durante la digitazione
- [ ] E' possibile selezionare piu' tag contemporaneamente
- [ ] Se un tag non esiste: mostra opzione "+ Crea tag '{nome}'"
- [ ] La creazione di un nuovo tag chiama il backend per la persistenza prima di associarlo
- [ ] I tag selezionati appaiono come badge rimovibili
- [ ] La rimozione di un badge e' client-side (non chiama il backend)
- [ ] I tag vengono associati al post al momento del salvataggio definitivo

### Technical Notes

- Il backend deve esporre `GET /api/tags` (lista completa) e `POST /api/tags` (creazione)
- L'autocomplete puo' essere implementato come componente Preact island (client:load) per interattivita'
- Dipende da US-03 (form creazione)

---

## US-10: Archiviazione post (soft delete)

### Problem

Christian ha post datati o non piu' rilevanti che vuole rimuovere dalla visibilita' pubblica senza cancellarli definitivamente. Il hard delete e' troppo aggressivo per un contenuto che potrebbe essere utile in futuro o ripristinato.

### Who

- Christian Borrello | autore | vuole nascondere post senza perdere il contenuto

### Solution

Nella lista post, ogni riga ha il bottone [A] (Archivia). Cliccando, appare una modale di conferma "Archivia questo post? Non sara' piu' visibile ai lettori. Potrai ripristinarlo dalla tab 'Archiviati'." Confermando, il post passa a `status: archived`, scompare dalle tab "Tutti" e "Pubblicati", appare nella tab "Archiviati". I post archiviati non sono accessibili su /blog/{slug}.

### Domain Examples

#### 1: Archiviazione di un post obsoleto
Christian ha pubblicato "Setup di Fly.io nel 2024" — le istruzioni sono obsolete. Dalla lista clicca [A], legge la modale di conferma, clicca "Archivia". Il post scompare dalla lista principale. I lettori che visitano l'URL ricevono 404 (o un redirect a una pagina "Contenuto rimosso").

#### 2: Archiviazione di una bozza abbandonata
Christian ha una bozza "Idee su DDD" mai completata. Dall'editor clicca "Archivia post". Il post sparisce dalle bozze. Non viene mostrato nessuno spinner di rebuild (era una bozza, non impatta il sito statico).

#### 3: Visione dei post archiviati
Christian clicca la tab "Archiviati" nella lista. Vede i post archiviati con data di archiviazione. Ogni riga ha il bottone [Ripristina] invece di [Archivia].

### UAT Scenarios (BDD)

#### Scenario: Archiviazione post con conferma
Given Christian e' su /admin/posts
When clicca [A] su "Setup di Fly.io nel 2024 (Pubblicato)"
Then appare la modale "Archivia questo post? Non sara' piu' visibile ai lettori."
When Christian clicca "Archivia"
Then il post passa a status "archived" nel backend
And scompare dalla tab "Tutti" e "Pubblicati"
And appare nella tab "Archiviati" con data di archiviazione
And non e' piu' accessibile su /blog/setup-di-flyio-nel-2024

#### Scenario: Annullamento archiviazione
Given Christian e' su /admin/posts
And ha cliccato [A] su un post
When nella modale clicca "Annulla"
Then la modale si chiude
And il post rimane nella lista con stato invariato

#### Scenario: Visualizzazione post archiviati
Given Christian ha archiviato 2 post
When clicca il tab "Archiviati"
Then vede i 2 post archiviati con data di archiviazione
And ogni riga ha il bottone [Ripristina] al posto di [Archivia]

### Acceptance Criteria

- [ ] Il bottone [A] (Archivia) e' presente su ogni riga della lista post
- [ ] Cliccando [A] appare una modale di conferma prima dell'archiviazione
- [ ] Post archiviato: `status: archived` nel backend, non accessibile ai lettori
- [ ] Post archiviato scompare dalla tab "Tutti" e "Pubblicati"
- [ ] Tab "Archiviati" mostra solo i post archiviati con data
- [ ] Post archiviati che erano pubblicati: il rebuild rimuove la pagina statica (o serve 404)
- [ ] Post archiviati che erano bozze: nessun rebuild necessario

### Technical Notes

- Il backend deve supportare `PATCH /api/posts/{id}` con `status: "archived"`
- I post archiviati non devono essere restituiti dalle API pubbliche del blog
- Dipende da US-04 (lista post)

---

## US-11: Feedback rebuild e gestione errori

### Problem

Durante il rebuild Vercel, Christian non sa quanto aspettare, se il processo sta procedendo, e cosa fare se fallisce. Uno spinner generico senza informazioni non e' sufficiente — la tensione del "funzionera'?" deve essere risolta dal feedback visivo.

### Who

- Christian Borrello | autore nel momento piu' teso del flusso (dopo aver cliccato Pubblica) | ha bisogno di sapere cosa sta succedendo

### Solution

La schermata di rebuild mostra: messaggio "Pubblicazione in corso...", barra di progresso animata, messaggio secondario "Il post sara' visibile ai lettori tra qualche secondo. Non chiudere questa finestra." In caso di timeout (60s): messaggio "Il post e' stato salvato. Il sito si aggiornera' a breve." con link a /blog/{slug}. In caso di errore backend: messaggio specifico con suggerimento di azione.

### Domain Examples

#### 1: Rebuild che completa normalmente
Christian pubblica un post. Vede lo spinner per 20-25 secondi. Il rebuild completa. Viene portato su `/blog/nuovo-post` e vede il post live.

#### 2: Rebuild lento (Vercel sovraccarico)
Il rebuild impiega 70 secondi. A 60 secondi, lo spinner mostra: "Ci sta' impiegando piu' del previsto. Il post e' comunque salvato." con il link "Apri il post" disabilitato per i primi 30 secondi (rebuild probabilmente non ancora completato), poi attivo dopo 60s.

#### 3: Errore durante il rebuild
Il Vercel hook non risponde (rete down). L'Action registra l'errore. Il frontend mostra: "Il post e' stato salvato ma il sito non e' ancora aggiornato. Riprova il rebuild manualmente dal pannello Vercel, o attendi — il sito si aggiorna automaticamente ad ogni nuovo deploy."

### UAT Scenarios (BDD)

#### Scenario: Feedback corretto durante rebuild normale
Given Christian ha appena pubblicato un post
When il rebuild e' in corso
Then vede lo spinner con messaggio "Pubblicazione in corso..."
And il messaggio "Non chiudere questa finestra." e' visibile
When il rebuild completa
Then viene reindirizzato automaticamente a /blog/{slug}

#### Scenario: Messaggio di timeout dopo 60 secondi
Given il rebuild Vercel non completa entro 60 secondi
Then lo spinner viene sostituito dal messaggio "Il post e' stato salvato."
And un link "Vedi il post" e' disponibile per navigare a /blog/{slug}
And il contenuto e' comunque accessibile nel backend

#### Scenario: Messaggio specifico per errore backend
Given il backend .NET restituisce un errore durante il salvataggio del post
Then il spinner non appare
And viene mostrato un messaggio di errore specifico (es. "Errore di connessione al backend")
And il form rimane aperto con i dati preservati per un nuovo tentativo

### Acceptance Criteria

- [ ] Durante il rebuild: messaggio "Pubblicazione in corso..." con barra animata
- [ ] Al completamento: redirect automatico a /blog/{slug}
- [ ] Dopo 60s senza completamento: messaggio con link manuale a /blog/{slug}
- [ ] Errore backend al salvataggio: messaggio specifico, form preservato, nessun spinner
- [ ] Errore rebuild hook: messaggio informativo (il post e' salvato, rebuild ritarda)
- [ ] Il link "Vedi il post" a /blog/{slug} e' sempre disponibile come fallback dopo il timeout

### Technical Notes

- Il rebuild hook Vercel e' una chiamata POST a URL del tipo `https://api.vercel.com/v1/integrations/deploy/...`
- Il polling del completamento e' opzionale — il timeout con link manuale e' sufficiente per il MVP
- Dipende da US-08 (pubblicazione)

---

## US-12: Ripristino post archiviato

### Problem

Christian ha archiviato un post pensando di non averne piu' bisogno, ma poi vuole pubblicarlo di nuovo (magari dopo averlo aggiornato). Senza un meccanismo di ripristino, l'archiviazione sarebbe permanente de facto.

### Who

- Christian Borrello | autore | vuole recuperare contenuto archiviato

### Solution

Nella tab "Archiviati" della lista post, ogni riga ha il bottone [Ripristina] invece di [Archivia]. Cliccando [Ripristina], il post torna allo stato precedente all'archiviazione (se era Published: torna Published, se era Draft: torna Draft). Appare un toast di conferma "Post ripristinato."

### Domain Examples

#### 1: Ripristino di un post pubblicato
Christian aveva archiviato "Setup di Fly.io nel 2024". Aggiorna il post con informazioni 2026 nella pagina edit, poi dalla tab "Archiviati" clicca [Ripristina]. Il post torna Published. Il rebuild aggiorna la pagina statica.

#### 2: Ripristino di una bozza
Christian aveva archiviato una bozza "Idee su DDD". Clicca [Ripristina]. Il post torna Draft. Nessun rebuild necessario (era una bozza).

### UAT Scenarios (BDD)

#### Scenario: Ripristino post da archiviato a pubblicato
Given Christian e' su /admin/posts tab "Archiviati"
And "Setup di Fly.io nel 2024" era Published prima dell'archiviazione
When Christian clicca [Ripristina]
Then il post torna a status "published"
And appare nella tab "Pubblicati"
And scompare dalla tab "Archiviati"
And il rebuild Vercel viene triggerato
And il post e' nuovamente accessibile su /blog/setup-di-flyio-nel-2024

#### Scenario: Ripristino bozza archiviata
Given "Idee su DDD" era Draft prima dell'archiviazione
When Christian clicca [Ripristina]
Then il post torna a status "draft"
And appare nella tab "Bozze"
And nessun rebuild viene triggerato

### Acceptance Criteria

- [ ] Il bottone [Ripristina] e' visibile nella tab "Archiviati" per ogni post
- [ ] Il ripristino riporta il post allo stato precedente (Published o Draft)
- [ ] Post ripristinato come Published: triggera il rebuild Vercel
- [ ] Post ripristinato come Draft: nessun rebuild
- [ ] Toast di conferma "Post ripristinato." dopo il ripristino
- [ ] Il post scompare dalla tab "Archiviati" e appare nella tab corretta

### Technical Notes

- Il backend deve conservare lo stato precedente all'archiviazione per poterlo ripristinare
- Alternativa: usare un campo `previous_status` nel database
- Dipende da US-10 (archiviazione)
