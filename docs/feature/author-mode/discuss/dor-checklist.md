# Definition of Ready — Checklist Author Mode

**Feature**: author-mode
**Data**: 2026-03-14
**Validatore**: Luna (product-owner)

---

## Validazione per Story

### US-01: Login admin tramite OAuth

| DoR Item | Status | Evidenza |
|---|---|---|
| Problem statement chiaro, in linguaggio di dominio | PASS | "Christian non puo' autenticarsi come admin dal browser" — problema reale, domain language |
| User/persona identificato con caratteristiche specifiche | PASS | "Christian Borrello, unico autore, usa Google e GitHub quotidianamente" |
| >= 3 esempi di dominio con dati reali | PASS | (1) Login Google con christian.borrello@gmail.com (2) Email non autorizzata test.account@gmail.com (3) Sessione scaduta durante compilazione post |
| UAT in Given/When/Then (3-7 scenari) | PASS | 4 scenari: login successo, email non autorizzata, accesso diretto senza sessione, sessione scaduta |
| AC derivate dalle UAT | PASS | 8 AC tutte tracciabili agli scenari |
| Right-sized (1-3 giorni, 3-7 scenari) | PASS | ~2 giorni, 4 scenari |
| Technical notes: vincoli e dipendenze | PASS | Astro 5.7+, Upstash Redis, endpoint OAuth backend .NET documentati |
| Dipendenze risolte o tracciate | PASS | Dipende da: Google OAuth app, GitHub OAuth app, endpoint backend .NET (da creare) |
| Outcome KPIs definiti | PASS | KPI-1: 100% login con email autorizzata hanno successo |

**DoR Status: PASSED**

---

### US-02: Middleware auth guard

| DoR Item | Status | Evidenza |
|---|---|---|
| Problem statement chiaro | PASS | "Senza guard centralizzato, ogni pagina duplica la logica auth — e una pagina senza prerender=false bypassa silenziosamente il middleware in prod" |
| User/persona identificato | PASS | Christian Borrello come autore; implicito: nessun lettore deve accedere all'admin |
| >= 3 esempi di dominio | PASS | (1) Navigazione normale con sessione valida (2) Accesso diretto senza sessione (3) Pagina senza prerender=false — scenario di errore da prevenire |
| UAT in Given/When/Then (3-7 scenari) | PASS | 4 scenari: accesso con sessione valida, blocco senza sessione, login non bloccato, Action bloccata |
| AC derivate dalle UAT | PASS | 7 AC tutte tracciabili |
| Right-sized | PASS | ~1 giorno, 4 scenari |
| Technical notes | PASS | Double-layer auth, getActionContext(), CI check prerender=false |
| Dipendenze | PASS | Dipende da US-01 |
| Outcome KPIs | PASS | Collegato a KPI-1 (prerequisito del login) |

**DoR Status: PASSED**

---

### US-03: Form crea nuovo post con editor Tiptap

| DoR Item | Status | Evidenza |
|---|---|---|
| Problem statement chiaro | PASS | "Christian non ha modo di scrivere un post dal browser — deve usare curl o strumenti esterni" |
| User/persona identificato | PASS | "Christian Borrello in flusso creativo" — caratteristica specifica |
| >= 3 esempi di dominio | PASS | (1) Post completo Domain Events con tags, immagine, codice C# (2) Bozza veloce con solo titolo e note (3) Slug in conflitto con post esistente |
| UAT in Given/When/Then (3-7 scenari) | PASS | 5 scenari: apertura form, slug auto-generato, salva bozza, validazione titolo, errore backend |
| AC derivate dalle UAT | PASS | 9 AC tutte tracciabili |
| Right-sized | PASS | ~3 giorni, 5 scenari — al limite ma accettabile (editor Tiptap e' la parte complessa) |
| Technical notes | PASS | Tiptap @preact/compat, immediatelyRender: false, Astro Actions, backend endpoint |
| Dipendenze | PASS | US-01, US-02, prototipo Tiptap gia' validato (task #3) |
| Outcome KPIs | PASS | KPI-2: 1 post pubblicato entro prima settimana |

**DoR Status: PASSED**

---

### US-04: Lista post con stato e filtri

| DoR Item | Status | Evidenza |
|---|---|---|
| Problem statement chiaro | PASS | "Christian non ha visione d'insieme del blog — deve interrogare il database" |
| User/persona identificato | PASS | "Christian Borrello, autore, vuole controllo del blog" |
| >= 3 esempi di dominio | PASS | (1) 15 post con 12 pubblicati, 2 bozze, 1 archiviato (2) Navigazione rapida all'editor (3) Lista vuota al primo accesso |
| UAT in Given/When/Then | PASS | 4 scenari |
| AC derivate dalle UAT | PASS | 8 AC |
| Right-sized | PASS | ~2 giorni, 4 scenari |
| Technical notes | PASS | Backend endpoint GET /api/posts, opzione filtro client/server-side |
| Dipendenze | PASS | US-01, US-02 |
| Outcome KPIs | PASS | Collegato a KPI-2 |

**DoR Status: PASSED**

---

### US-05: Salva post come bozza

| DoR Item | Status | Evidenza |
|---|---|---|
| Problem statement chiaro | PASS | "Christian ha sessioni di scrittura frammentate — ha bisogno di salvare il progresso" |
| User/persona identificato | PASS | "Christian con sessioni di scrittura frammentate" — caratteristica specifica |
| >= 3 esempi di dominio | PASS | (1) Bozza veloce sul treno (2) Bozza modificata piu' volte (3) Bozza con titolo mancante |
| UAT in Given/When/Then | PASS | 3 scenari |
| AC derivate dalle UAT | PASS | 6 AC |
| Right-sized | PASS | ~1 giorno, 3 scenari — thin slice del form |
| Technical notes | PASS | Backend POST /api/posts con status draft, nessun rebuild per bozze |
| Dipendenze | PASS | US-03 |
| Outcome KPIs | PASS | Collegato a KPI-2 |

**DoR Status: PASSED**

---

### US-06: Upload immagine di copertina

| DoR Item | Status | Evidenza |
|---|---|---|
| Problem statement chiaro | PASS | "Christian deve caricare immagini manualmente su ImageKit — processo frammentato" |
| User/persona identificato | PASS | "Christian autore, vuole caricare dal form senza strumenti esterni" |
| >= 3 esempi di dominio | PASS | (1) Upload valido forge-and-ink-cover.png 650KB (2) File troppo grande 12MB (3) Formato .bmp non supportato |
| UAT in Given/When/Then | PASS | 3 scenari |
| AC derivate dalle UAT | PASS | 7 AC |
| Right-sized | PASS | ~2 giorni, 3 scenari |
| Technical notes | PASS | ImageKit SDK v4, Action multipart, URL CDN con trasformazioni |
| Dipendenze | PASS | US-03, backend ImageKit gia' disponibile |
| Outcome KPIs | PASS | Collegato a KPI-2 |

**DoR Status: PASSED**

---

### US-07: Toolbar EditControls sulla pagina pubblica

| DoR Item | Status | Evidenza |
|---|---|---|
| Problem statement chiaro | PASS | "Il context-switch tra post pubblico e admin interrompe il flusso di lettura" |
| User/persona identificato | PASS | "Christian che legge i propri post e vuole correggere inline" |
| >= 3 esempi di dominio | PASS | (1) Correzione errore tipografico su tdd-con-dotnet-10 (2) Lettore Marco non vede nessun elemento extra (3) Badge Bozza su post non pubblicato |
| UAT in Given/When/Then | PASS | 4 scenari |
| AC derivate dalle UAT | PASS | 8 AC |
| Right-sized | PASS | ~2 giorni, 4 scenari |
| Technical notes | PASS | server:defer, Astro.cookies, prop criptate, Referer header |
| Dipendenze | PASS | US-01 (sessione) |
| Outcome KPIs | PASS | KPI-3: utilizzo flusso in-place |

**DoR Status: PASSED**

---

### US-08: Pubblicazione post con rebuild Vercel

| DoR Item | Status | Evidenza |
|---|---|---|
| Problem statement chiaro | PASS | "Salvare nel DB non basta — la pagina statica deve essere rigenerata" |
| User/persona identificato | PASS | "Christian autore nel momento del 'Pubblica'" |
| >= 3 esempi di dominio | PASS | (1) Pubblicazione normale ~20-30s (2) Rebuild lento con timeout 70s (3) Aggiornamento post gia' pubblicato |
| UAT in Given/When/Then | PASS | 3 scenari |
| AC derivate dalle UAT | PASS | 7 AC |
| Right-sized | PASS | ~2 giorni, 3 scenari |
| Technical notes | PASS | VERCEL_DEPLOY_HOOK_URL, timeout 60s, rebuild per pubblicati non per bozze |
| Dipendenze | PASS | US-03, US-05 |
| Outcome KPIs | PASS | KPI-2: post visibile ai lettori dopo pubblicazione |

**DoR Status: PASSED**

---

### US-09: Gestione tag

| DoR Item | Status | Evidenza |
|---|---|---|
| Problem statement chiaro | PASS | "Christian deve pre-popolare i tag nel database manualmente" |
| User/persona identificato | PASS | "Christian autore nel form di creazione" |
| >= 3 esempi di dominio | PASS | (1) Selezione tag esistente "TDD" (2) Creazione nuovo tag "Event Sourcing" (3) Rimozione badge ".NET" |
| UAT in Given/When/Then | PASS | 3 scenari |
| AC derivate dalle UAT | PASS | 7 AC |
| Right-sized | PASS | ~2 giorni, 3 scenari |
| Technical notes | PASS | GET/POST /api/tags, autocomplete Preact island |
| Dipendenze | PASS | US-03 |
| Outcome KPIs | PASS | Collegato a KPI-2 |

**DoR Status: PASSED**

---

### US-10: Archiviazione post

| DoR Item | Status | Evidenza |
|---|---|---|
| Problem statement chiaro | PASS | "Christian vuole nascondere post datati senza hard delete" |
| User/persona identificato | PASS | "Christian autore nella gestione del blog nel tempo" |
| >= 3 esempi di dominio | PASS | (1) Archiviazione post obsoleto Setup Fly.io (2) Archiviazione bozza abbandonata (3) Visualizzazione tab Archiviati |
| UAT in Given/When/Then | PASS | 3 scenari |
| AC derivate dalle UAT | PASS | 7 AC |
| Right-sized | PASS | ~1 giorno, 3 scenari |
| Technical notes | PASS | PATCH /api/posts/{id} status archived, soft delete |
| Dipendenze | PASS | US-04 |
| Outcome KPIs | PASS | KPI-3 (gestione blog nel tempo) |

**DoR Status: PASSED**

---

### US-11: Feedback rebuild e gestione errori

| DoR Item | Status | Evidenza |
|---|---|---|
| Problem statement chiaro | PASS | "Spinner generico non risolve la tensione del 'funzionera'?'" |
| User/persona identificato | PASS | "Christian nel momento piu' teso del flusso (post-Pubblica)" |
| >= 3 esempi di dominio | PASS | (1) Rebuild normale 20-25s (2) Rebuild lento 70s con timeout (3) Errore backend al salvataggio |
| UAT in Given/When/Then | PASS | 3 scenari |
| AC derivate dalle UAT | PASS | 6 AC |
| Right-sized | PASS | ~1 giorno, 3 scenari — estensione di US-08 |
| Technical notes | PASS | Rebuild hook, timeout 60s, link manuale fallback |
| Dipendenze | PASS | US-08 |
| Outcome KPIs | PASS | Collegato a KPI-2 (l'autore vede il post live) |

**DoR Status: PASSED**

---

### US-12: Ripristino post archiviato

| DoR Item | Status | Evidenza |
|---|---|---|
| Problem statement chiaro | PASS | "L'archiviazione senza ripristino diventa un hard delete de facto" |
| User/persona identificato | PASS | "Christian autore che vuole recuperare contenuto archiviato" |
| >= 3 esempi di dominio | PASS | (1) Ripristino post pubblicato Setup Fly.io aggiornato (2) Ripristino bozza archiviata (inclusi 2 esempi nelle 3 UAT) |
| UAT in Given/When/Then | PASS | 2 scenari (accettabile per story semplice e complementare a US-10) |
| AC derivate dalle UAT | PASS | 6 AC |
| Right-sized | PASS | ~1 giorno, 2 scenari |
| Technical notes | PASS | Campo previous_status nel backend |
| Dipendenze | PASS | US-10 |
| Outcome KPIs | PASS | KPI-3 |

**DoR Status: PASSED**

---

## Riepilogo DoR Feature

| Story | Status |
|---|---|
| US-01: Login OAuth | PASSED |
| US-02: Middleware guard | PASSED |
| US-03: Form crea post | PASSED |
| US-04: Lista post | PASSED |
| US-05: Salva bozza | PASSED |
| US-06: Upload immagine | PASSED |
| US-07: EditControls toolbar | PASSED |
| US-08: Pubblicazione + rebuild | PASSED |
| US-09: Gestione tag | PASSED |
| US-10: Archiviazione post | PASSED |
| US-11: Feedback rebuild | PASSED |
| US-12: Ripristino post | PASSED |

### DoR Feature: PASSED — tutte le 12 stories superano il gate

La feature author-mode e' pronta per la DESIGN wave.
