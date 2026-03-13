# Idea Brief: Author Mode — In-Place Editing con Astro Hybrid

**Data**: 2026-03-11
**Stato**: Brainstorm completato, pronto per Discuss
**Sostituisce**: `admin-spa-idea-brief.md` (SPA React separata — scartata in favore della semplicità)

---

## Problema

Il blog "The Augmented Craftsman" non ha un'interfaccia per scrivere e gestire i post. L'autore non può pubblicare contenuti senza interagire direttamente con l'API o il database.

## Soluzione

Un **author mode integrato nel frontend Astro esistente**, usando il rendering ibrido (SSR per le poche pagine admin, SSG per il blog pubblico) e **Server Islands** per iniettare i controlli di editing direttamente sulle pagine pubbliche.

L'autore autenticato vede il blog come i lettori, ma con una toolbar floating che gli permette di modificare i post in-place. Per creare nuovi post e gestire la lista, 2-3 pagine admin SSR minimali.

## Perché non la SPA React separata

| Aspetto | SPA React | Astro Hybrid (scelto) |
|---|---|---|
| Progetti da mantenere | 3 (frontend + admin + backend) | 2 (frontend + backend) |
| Deploy | 3 separati (Vercel + Vercel + Fly.io) | 2 (Vercel + Fly.io) |
| Esperienza di editing | Context-switch tra admin e sito | Editing nel contesto reale della pagina |
| JS per i lettori | 0 | ~0 (Server Island vuota) |
| Complessità infrastruttura | Moderata (subdomain, CORS, CSP dedicata) | Bassa (un solo dominio) |
| Stack aggiuntivo | React Router, state management, build separato | Niente — stesso stack Astro |
| Priorità del progetto | Qualità esperienza editing | **Semplicità** |

**Principio guida**: la semplicità è la priorità. Un solo progetto frontend, un solo deploy, un solo dominio.

## Decisioni architetturali

| Decisione | Scelta | Rationale |
|---|---|---|
| Rendering ibrido | Astro `output: 'static'` + `prerender = false` per admin | Il progetto è già configurato: in Astro 5+ il comportamento ibrido è il default |
| In-place editing | Server Islands (`server:defer`) su pagine SSG | La pagina resta statica per i lettori; la Server Island inietta toolbar solo per l'autore — pattern già usato nel progetto |
| Pagine admin SSR | Solo `/admin/login`, `/admin/posts`, `/admin/posts/new` | Il minimo necessario per creare post e gestire la lista; il resto è editing in-place |
| Editor | Preact island (`client:load`) con Tiptap o Milkdown | Headless, framework-agnostic, supporto markdown import/export |
| Sessioni | `Astro.session` (stabile da v5.7) + Upstash Redis | Sessioni server-side, niente JWT client-side; Upstash free tier (10K comandi/giorno) sufficiente |
| Auth middleware | Middleware Astro su `/admin/*` | Verifica sessione server-side, redirect a login se non autenticato |
| Proxy al backend | Le Actions admin chiamano l'API .NET | Domain logic resta nel backend; Astro è solo un driving adapter |
| Upload immagini | Action che proxya a backend .NET → ImageKit | Stesso pattern delle Actions esistenti |

## Come funziona

### Per il lettore (zero impatto)

1. Visita `/blog/my-post` → pagina statica, CDN, velocissima
2. Server Island `EditControls` fa GET → server verifica cookie → nessuna sessione
3. Ritorna fragment vuoto → il lettore non vede nulla, non paga costi JS

### Per l'autore (editing in-place)

1. Visita `/blog/my-post` → pagina statica, CDN
2. Server Island `EditControls` fa GET → server verifica cookie → sessione autore valida
3. Ritorna toolbar floating con bottone "Modifica"
4. Click "Modifica" → editor Tiptap/Milkdown si attiva inline sul contenuto
5. Modifica testo, upload immagini
6. Click "Salva" → Action chiama backend .NET → salva → trigger rebuild pagina statica

### Per creare un nuovo post

1. Autore clicca "+ Nuovo Post" (bottone visibile via Server Island sulla pagina blog)
2. Redirect a `/admin/posts/new` (pagina SSR)
3. Compila form con stesso editor usato per l'in-place editing
4. Salva come draft o pubblica direttamente

## Struttura

```
frontend/
  src/
    pages/
      blog/[slug].astro          ← SSG (invariato) + Server Island EditControls
      admin/
        login.astro              ← SSR (prerender = false)
        posts/
          index.astro            ← SSR — lista post con stato, azioni
          new.astro              ← SSR — form creazione post
    actions/
      index.ts                   ← postComment (esistente)
      admin.ts                   ← login, createPost, updatePost, deletePost, uploadImage
    components/
      EditControls.astro         ← Server Island — auth check + toolbar
      InlineEditor.tsx           ← Preact island — editor Tiptap/Milkdown
      admin/
        PostForm.astro           ← Form condiviso tra new e edit
        PostList.astro           ← Tabella post con azioni
    middleware.ts                ← Auth guard per /admin/*
  astro.config.mjs               ← + session driver (redis/upstash)
```

## Scope MVP (primo rilascio)

### In scope

- **Login**: Form login → sessione server-side → cookie HTTP-only
- **Editing in-place**: Toolbar floating su pagine blog per l'autore, attiva editor inline
- **Crea post**: Form su `/admin/posts/new` con titolo, editor markdown, selezione tag, upload immagine copertina, stato draft/published
- **Lista post**: Tabella su `/admin/posts` con stato, azioni (modifica in-place, cancella)
- **Upload immagine copertina**: Via Action che proxya a backend → ImageKit
- **Gestione tag**: Selezionare tag esistenti o crearne di nuovi
- **Stato post**: Toggle bozza ↔ pubblicato
- **Middleware auth**: Protezione `/admin/*`

### Fuori scope (iterazioni successive)

- Auto-save (localStorage debounced)
- Drag & drop immagini nell'editor inline
- Split-pane editor con preview
- Keyboard shortcuts (Ctrl+S, etc.)
- Scheduling pubblicazione
- Versioning / cronologia modifiche
- Gestione commenti e moderazione
- Analytics e statistiche
- Multi-autore
- OAuth / social login

## Dipendenze tecniche

| Componente | Tecnologia | Stato |
|---|---|---|
| Astro hybrid rendering | `output: 'static'` + `prerender = false` | Già configurato |
| Vercel adapter SSR | `@astrojs/vercel` | Già installato |
| Server Islands | `server:defer` | Già usate nel progetto |
| Preact islands | `@astrojs/preact` | Già installato |
| Astro Actions | `defineAction` + Zod | Già usate (postComment) |
| Astro Sessions | `Astro.session` (stabile v5.7) | Da configurare (driver redis) |
| Upstash Redis | Driver sessioni per Vercel | Da aggiungere (free tier) |
| Editor rich text | Tiptap o Milkdown | Da scegliere e installare |
| Middleware | `defineMiddleware` | Da creare |

## Vincoli

- Zero impatto sulle performance del blog pubblico per i lettori
- L'API .NET backend resta la singola fonte di verità per la domain logic
- Il blog è personale: un solo autore, nessun workflow multi-utente
- Le pagine blog restano SSG (statiche, CDN-cached)
- Seguire le best practices del progetto (BEST_PRACTICES.md, CLAUDE.md)

## Rischi identificati

| Rischio | Severità | Mitigazione |
|---|---|---|
| Editor inline complesso da integrare in Astro | Media | Preact island isolato; se troppo complesso, fallback a form tradizionale su `/admin/posts/[id]/edit` |
| Server Island aggiunge una richiesta GET per ogni visitatore | Bassa | Ritorna fragment vuoto per i lettori; costo trascurabile |
| Astro Sessions + Vercel richiede Upstash Redis | Bassa | Free tier sufficiente (10K comandi/giorno, un solo autore ~100) |
| Pattern non consolidato (pochi esempi nel wild) | Media | L'architettura è semplice (form + actions + middleware); complessità bassa per un MVP singolo autore |
| Cold start serverless su Vercel Hobby | Bassa | Solo per l'autore, non per i lettori; Fluid Compute riduce al 0.63% |

## Fonti

- Ricerca Astro hybrid: `docs/research/astro-hybrid-admin-mode-research.md`
- Ricerca in-place editing: `docs/research/inline-editing-patterns-research.md`
- Idea brief precedente (SPA): `docs/brainstorm/admin-spa-idea-brief.md`
- Pattern di riferimento: Editable Website/Svedit, Medium (modello UX), Storyblok/Sanity (visual editing tecnico)
