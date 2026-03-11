# Idea Brief: Admin SPA — Author Mode

**Data**: 2026-03-08
**Stato**: Brainstorm completato, pronto per Discuss

---

## Problema

Il blog "The Augmented Craftsman" non ha un'interfaccia per scrivere e gestire i post. L'autore non può pubblicare contenuti senza interagire direttamente con l'API o il database.

## Soluzione

Una SPA separata (React + Vite) che funge da secondo driving adapter nell'Hexagonal Architecture esistente, deployata su un subdomain dedicato (`admin.theaugmentedcraftsman.com`).

## Decisioni architetturali

| Decisione | Scelta | Rationale |
|---|---|---|
| Separazione da Astro | SPA indipendente | Pattern universale (Ghost, WordPress headless); Astro è content-first, non adatto per admin UI |
| Framework | React + Vite | L'autore lo conosce; ecosistema editor ricchissimo per upgrade futuro a rich-text |
| Editor MVP | Textarea markdown + preview | Minimale, validabile velocemente; upgrade a TipTap/Milkdown come iterazione successiva |
| Auth | Login singolo autore | Blog personale, un solo utente admin |
| Deploy | Subdomain separato | Isolamento sicurezza (CSP dedicata, superficie d'attacco ridotta) |

## Contesto architetturale

```
the-augmented-craftsman/
  frontend/     ← Astro (sito pubblico, zero JS, SSG) — non cambia
  admin/        ← NUOVA: React + Vite (author mode)
  backend/      ← .NET 10 API (serve entrambi i frontend)
```

L'admin SPA è un driving adapter che consuma la stessa API port del frontend Astro. Nessuna modifica al core applicativo è necessaria per l'architettura — solo eventuali endpoint mancanti.

## Struttura dati esistente (da api.ts del frontend)

```typescript
interface ApiPost {
  id?: string;
  title: string;
  slug: string;
  content?: string;          // markdown
  status?: string;           // draft | published
  createdAt?: string;
  updatedAt?: string;
  publishedAt: string | null;
  featuredImageUrl: string | null;
  tags: string[] | null;
}
```

## Scope MVP (primo rilascio)

### In scope

- **Lista post**: Visualizzare tutti i post (bozze e pubblicati), con possibilità di modificare e cancellare
- **Crea post**: Form con titolo, textarea markdown con preview laterale, selezione tag, upload immagine copertina, stato (bozza/pubblicato)
- **Modifica post**: Stesso form precompilato con i dati esistenti
- **Cancella post**: Con conferma
- **Gestione tag**: Selezionare tag esistenti o crearne di nuovi inline
- **Upload immagine copertina**: Upload su ImageKit
- **Stato post**: Toggle bozza ↔ pubblicato
- **Autenticazione**: Login (singolo autore, ASP.NET Identity nel backend)

### Fuori scope (iterazioni successive)

- Editor rich-text (upgrade da markdown textarea)
- Gestione commenti e moderazione
- Analytics e statistiche
- Workflow editoriale (scheduling, revisioni, preview pubblica)
- Media library (gestione immagini oltre la copertina)
- Multi-autore

## Stack tecnico previsto

| Componente | Tecnologia |
|---|---|
| Framework | React 19 |
| Build tool | Vite |
| Styling | Da definire |
| Markdown preview | Da definire (react-markdown / marked) |
| HTTP client | fetch nativo o libreria leggera |
| Auth | JWT da ASP.NET Identity |
| Image upload | ImageKit SDK o upload diretto |
| Routing | React Router |

## Vincoli

- Zero impatto sul frontend Astro pubblico
- L'API .NET backend è la singola fonte di verità
- Il blog è personale: un solo autore, nessun workflow multi-utente
- Seguire le best practices del progetto (BEST_PRACTICES.md, CLAUDE.md)

## Fonti

- Ricerca: `docs/research/author-mode-best-practices.md` (18 fonti, confidenza alta)
- Pattern di riferimento: Ghost Admin (SPA Ember separata), WordPress headless
