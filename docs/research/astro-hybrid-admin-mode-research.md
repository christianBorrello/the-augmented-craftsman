# Ricerca: Author Mode con Astro Hybrid Rendering

> **Scope**: Valutare la fattibilita e i trade-off di implementare un "author mode" (admin panel) direttamente nel progetto Astro esistente, usando il rendering ibrido (SSR per `/admin/*`, SSG per il blog pubblico), al posto della SPA React separata prevista nell'idea brief.
>
> **Data**: 2026-03-11
>
> **Contesto**: Il progetto usa Astro 5+ con `output: 'static'` e adapter Vercel. Il frontend gia utilizza Server Islands (`server:defer`), Astro Actions e Preact islands per le feature dinamiche.

---

## 1. Come Funziona il Rendering Ibrido in Astro 5+

### La Svolta di Astro 5: Hybrid e il Default

In Astro 5, l'opzione `output: 'hybrid'` **non esiste piu**. Il comportamento ibrido e ora il **default** sotto il nome `output: 'static'`. Qualsiasi pagina puo fare opt-out dal prerendering aggiungendo:

```astro
---
export const prerender = false;
---
```

Questo significa che **il progetto e gia configurato per il rendering ibrido** — basta aggiungere `export const prerender = false` nelle pagine admin e queste diventano SSR, mentre tutto il resto resta statico.

### Cosa Ottieni con `prerender = false`

Una pagina SSR in Astro ha accesso completo a:
- **`Astro.request`**: headers, cookies, method, URL
- **`Astro.cookies`**: lettura/scrittura cookie HTTP-only
- **`Astro.session`**: sessioni server-side (stabile da Astro 5.7)
- **`Astro.redirect()`**: redirect server-side
- **`Astro.response`**: impostare status code, headers, Cache-Control
- **Accesso a env vars private** (non `PUBLIC_*`)

### Come Vercel Gestisce le Pagine SSR

Con `@astrojs/vercel`, ogni pagina con `prerender = false` viene compilata in una **Vercel Serverless Function**. Le pagine statiche restano file HTML serviti dal CDN edge. Il risultato e un unico deployment dove:
- `/blog/*`, `/tags/*`, `/about` → HTML statico su CDN (velocissimo)
- `/admin/*` → Serverless Functions (SSR on-demand)

---

## 2. Astro Sessions (Stabile da v5.7)

### Overview

Le sessioni Astro sono **stabili e production-ready** dalla versione 5.7 (non piu sperimentali). Memorizzano dati server-side associati a un utente tramite un session ID inviato come cookie.

### Configurazione per Vercel

Il Vercel adapter **non ha un driver di default** per le sessioni. Serve configurarne uno manualmente. La scelta piu naturale e **Upstash Redis** (Vercel KV e stato deprecato e migrato a Upstash nel dicembre 2024):

```javascript
// astro.config.mjs
import { defineConfig } from 'astro/config';
import vercel from '@astrojs/vercel';

export default defineConfig({
  adapter: vercel(),
  session: {
    driver: 'redis',
    options: {
      url: process.env.REDIS_URL, // Upstash Redis URL
    },
  },
});
```

**Costo**: Upstash offre un free tier (10.000 comandi/giorno, 256MB) — piu che sufficiente per un singolo autore.

### Uso nelle Pagine, Actions e Middleware

```astro
---
// src/pages/admin/dashboard.astro
export const prerender = false;

const user = await Astro.session.get('user');
if (!user) return Astro.redirect('/admin/login');
---
<h1>Benvenuto, {user.name}</h1>
```

```typescript
// In un'Action
handler: async (input, { session }) => {
  const user = await session.get('user');
  // ...
}
```

```typescript
// In middleware
export const onRequest = defineMiddleware(async (context, next) => {
  const user = await context.session?.get('user');
  if (context.url.pathname.startsWith('/admin') && !user) {
    return context.redirect('/admin/login');
  }
  return next();
});
```

### Type Safety

```typescript
// src/env.d.ts
declare namespace App {
  interface SessionData {
    user: { id: string; name: string; email: string };
  }
}
```

---

## 3. Protezione Route Admin con Middleware

Astro supporta middleware che intercetta ogni richiesta. Per le pagine prerendered, il middleware gira a build time; per le pagine SSR (`prerender = false`), gira a **request time** — esattamente quello che serve per proteggere `/admin/*`.

Pattern consigliato:

```typescript
// src/middleware.ts
import { defineMiddleware } from 'astro:middleware';

const ADMIN_PREFIX = '/admin';
const LOGIN_PATH = '/admin/login';

export const onRequest = defineMiddleware(async (context, next) => {
  // Ignora le pagine statiche e la pagina di login
  if (!context.url.pathname.startsWith(ADMIN_PREFIX)) return next();
  if (context.url.pathname === LOGIN_PATH) return next();

  const user = await context.session?.get('user');
  if (!user) {
    return context.redirect(LOGIN_PATH);
  }

  // Inietta user nei locals per accesso in tutte le pagine admin
  context.locals.user = user;
  return next();
});
```

**Nota importante**: il middleware in Astro **non supporta edge middleware** per le sessioni. Su Vercel, gira come parte della serverless function, non come edge function separata.

---

## 4. Astro Actions per le Operazioni Admin

Le Actions sono gia usate nel progetto per i commenti. Per l'admin, si estenderebbero naturalmente:

```typescript
// src/actions/index.ts
export const server = {
  // Azione esistente
  postComment: defineAction({ /* ... */ }),

  // Nuove azioni admin
  createPost: defineAction({
    accept: 'form',
    input: z.object({
      title: z.string().min(1),
      content: z.string(),
      status: z.enum(['draft', 'published']),
      tags: z.string().optional(), // comma-separated
    }),
    handler: async (input, { session }) => {
      const user = await session.get('user');
      if (!user) throw new ActionError({ code: 'UNAUTHORIZED' });

      // Opzione A: Proxy al backend .NET
      const res = await fetch(`${API}/api/posts`, { /* ... */ });

      // Opzione B: Accesso diretto al DB (elimina il backend)
      // await db.insert(posts).values({ ... });
    },
  }),
};
```

### La Grande Domanda: Proxy vs Accesso Diretto

Qui emerge la decisione architetturale chiave:

| Approccio | Pro | Contro |
|---|---|---|
| **Proxy al backend .NET** | Domain logic resta nel backend; il backend serve anche come API pubblica; separazione netta | Latenza aggiuntiva (Vercel → Fly.io); il backend .NET deve restare attivo |
| **Accesso diretto al DB da Astro** | Elimina il backend; deploy unico; latenza minore | Domain logic migra in TypeScript; perde il valore di portfolio .NET; duplicazione logica |
| **Ibrido**: admin in Astro, blog pubblico da backend | Ogni adapter ha la sua responsabilita; graduale | Due fonti di verita per le write; complessita di sincronizzazione |

---

## 5. Vercel Adapter: Capacita e Limiti

### Capacita
- **Serverless Functions**: ogni pagina SSR diventa una function
- **Fluid Compute** (2025): cold start quasi eliminati — il 99.37% delle richieste non ha cold start
- **Scale to One**: su piano Pro, almeno un'istanza resta attiva
- **ISR**: supporto per Incremental Static Regeneration con `Cache-Control` headers
- **Timeout**: 10s su Hobby, 60s su Pro, 900s su Enterprise
- **Bundle size max**: 50MB per funzione (compresso)

### Limiti Rilevanti
- **Edge Functions**: max 1MB, no Node.js APIs native — le sessioni Astro NON funzionano in edge middleware
- **Sessioni**: serve un driver esterno (Upstash Redis) — costo aggiuntivo (anche se il free tier basta)
- **Bundle bloat**: il bundler Vercel a volte include dipendenze non necessarie nelle functions SSR
- **Cold start** (raro ma possibile): ~200-500ms per il primo hit dopo inattivita su piano Hobby
- **Nessun filesystem persistente**: non puoi usare `fs` driver per le sessioni su Vercel

### Costo su Piano Hobby (gratuito)
- 100GB bandwidth/mese
- 100GB-hrs function execution
- Serverless function timeout: 10 secondi
- **Sufficiente per un blog personale con un solo autore admin**

---

## 6. Analisi Real-World: Chi Fa Cosi?

### Pattern Emergente
Il pattern "blog statico + admin SSR nello stesso progetto Astro" e **emergente ma non consolidato**. La maggior parte degli esempi reali usa CMS esterni (Storyblok, Contentful, Sanity) come admin, non un admin custom in Astro.

### Esempi Trovati
1. **Blog con hybrid rendering su Render.com**: blog posts statici + dashboard utente SSR nello stesso progetto
2. **Optimizely + Astro SSR**: CMS headless con visual builder, SSR per preview in tempo reale
3. **Astro + Cloudflare Workers**: blog serverless a costo zero con pagine admin SSR

### Cosa NON Ho Trovato
- Nessun esempio maturo e open-source di "admin panel completo in Astro" per la gestione contenuti
- Nessun template ufficiale Astro per admin/dashboard
- Ghost, WordPress headless, e altri CMS mantengono tutti l'admin separato dal frontend

---

## 7. Trade-off: Astro Hybrid vs SPA React Separata

### Cosa Guadagni con Astro Hybrid Admin

| Vantaggio | Dettaglio |
|---|---|
| **Un solo progetto, un solo deploy** | Elimina la complessita di gestire `admin/` come progetto separato |
| **Zero JavaScript per default** | Le pagine admin possono funzionare come form HTML progressivamente migliorati |
| **Sessioni server-side native** | `Astro.session` — niente JWT, niente token management client-side |
| **Astro Actions** | Type-safe, progressive enhancement, validazione Zod integrata |
| **Middleware auth centralizzato** | Un unico middleware protegge tutte le route `/admin/*` |
| **Stessa infrastruttura** | Un solo deployment Vercel, un solo dominio |
| **Meno codice da mantenere** | Niente React Router, niente state management, niente build tooling separato |

### Cosa Perdi con Astro Hybrid Admin

| Svantaggio | Dettaglio |
|---|---|
| **UX meno "app-like"** | Navigazione tra pagine admin = full page reload (no SPA transitions), a meno di usare View Transitions |
| **Editor markdown limitato** | In Astro SSR non hai lo stesso ecosistema di editor React (TipTap, Milkdown, CodeMirror). Servirebbero client islands con Preact/React |
| **Nessun real-time preview** | Un editor con preview split-pane richiede un island client-side pesante |
| **Accoppiamento frontend-admin** | Un bug nel deploy admin potrebbe impattare il blog pubblico (mitigabile con buoni test) |
| **Pattern non consolidato** | Pochi esempi reali; territorio meno battuto |
| **Astro non e un framework "app"** | Astro e content-first — forzarlo a fare da framework app per form complessi puo creare frizione |

---

## 8. Architettura Proposta (se si procede)

```
the-augmented-craftsman/
  frontend/
    src/
      pages/
        index.astro          ← SSG (statico)
        blog/[slug].astro    ← SSG (statico)
        tags/...             ← SSG (statico)
        admin/
          login.astro        ← SSR (prerender = false)
          dashboard.astro    ← SSR (prerender = false)
          posts/
            index.astro      ← SSR (lista post)
            new.astro        ← SSR (crea post)
            [id]/edit.astro  ← SSR (modifica post)
      actions/
        index.ts             ← postComment (esistente)
        admin.ts             ← createPost, updatePost, deletePost, login
      middleware.ts           ← Auth guard per /admin/*
      components/
        admin/               ← Layout e componenti admin
        MarkdownEditor.tsx   ← Preact island per editor (client:load)
    astro.config.mjs         ← + session driver (redis/upstash)
  backend/                   ← .NET 10 API (resta per le API pubbliche)
```

### Flusso di Autenticazione

```
1. GET /admin/login → Pagina SSR con form
2. POST /admin/login (Action) → Verifica credenziali vs backend .NET
3. Session.set('user', { id, name, email })
4. Redirect → /admin/dashboard
5. Middleware verifica session su ogni richiesta /admin/*
```

### Scelta Critica: Da Dove Vengono i Dati?

**Raccomandazione**: le Actions admin fanno **proxy al backend .NET** (come gia fa `postComment`). Questo:
- Mantiene la domain logic nel backend (valore portfolio)
- Non duplica la business logic
- Il backend .NET resta la singola fonte di verita
- L'admin Astro e un driving adapter, esattamente come la SPA React sarebbe stata

---

## 9. Spunti Creativi — "What If..."

1. **What if l'editor markdown fosse un Preact island con preview live?** Un `<MarkdownEditor client:load />` dentro una pagina SSR admin — la pagina gestisce auth/sessione, il componente gestisce l'editing. Il meglio di entrambi i mondi.

2. **What if si usassero le View Transitions per dare all'admin un feeling SPA?** Il `<ClientRouter />` di Astro puo far sembrare la navigazione admin istantanea, senza il peso di un framework SPA.

3. **What if l'admin fosse solo una "fase 1" in Astro, con upgrade a SPA React se cresce?** Si parte con form HTML progressivamente migliorati in Astro. Se l'esperienza d'uso richiede piu interattivita, si estrae l'admin in SPA React. Le Actions definiscono gia l'interfaccia.

4. **What if si eliminasse il backend .NET per l'admin, accedendo direttamente a PostgreSQL da Astro?** Con Drizzle ORM o Prisma, le Actions Astro scrivono direttamente su Neon PostgreSQL. Il backend .NET resta solo per le API pubbliche del blog. Pero: domain logic in due linguaggi.

5. **What if la sessione fosse gestita interamente con cookie HTTP-only firmati, senza Redis?** Si evita Upstash. Il middleware firma un JWT e lo mette in un cookie HTTP-only. Astro lo verifica a ogni richiesta. Piu semplice, meno infrastruttura, ma non si ha `Astro.session`.

---

## 10. Gap e Rischi Aperti

| Rischio | Severita | Mitigazione |
|---|---|---|
| Astro Sessions + Vercel: il driver Redis richiede Upstash (costo potenziale) | Bassa | Free tier 10K comandi/giorno — un autore ne usa ~100 |
| Middleware non gira in edge su Vercel per le sessioni | Media | Accettabile — gira nella serverless function, latenza leggermente piu alta |
| Editor markdown complesso richiede comunque un framework client | Media | Preact island (`client:load`) — gia usato nel progetto |
| Nessun esempio maturo di "admin panel in Astro" nel wild | Media | Il pattern e semplice (form + actions + middleware); la complessita e bassa per un MVP singolo autore |
| Bug in admin deploy potrebbe impattare blog pubblico | Media | Pagine statiche sono gia buildate e su CDN — un errore SSR non tocca l'HTML statico |
| Cold start su Vercel Hobby per prime richieste admin | Bassa | Solo per l'autore, non per i lettori; Fluid Compute riduce al 0.63% |

---

## Fonti

### Documentazione Ufficiale Astro
- [On-demand Rendering](https://docs.astro.build/en/guides/on-demand-rendering/)
- [Sessions](https://docs.astro.build/en/guides/sessions/)
- [Actions](https://docs.astro.build/en/guides/actions/)
- [Middleware](https://docs.astro.build/en/guides/middleware/)
- [Authentication](https://docs.astro.build/en/guides/authentication/)
- [Astro 5.7 Release (Sessions Stable)](https://astro.build/blog/astro-570/)
- [Upgrade to Astro v5](https://docs.astro.build/en/guides/upgrade-to/v5/)
- [Vercel Adapter](https://docs.astro.build/en/guides/integrations-guide/vercel/)

### Vercel
- [Astro on Vercel](https://vercel.com/docs/frameworks/frontend/astro)
- [Fluid Compute](https://vercel.com/docs/fluid-compute)
- [Cold Start Performance](https://vercel.com/kb/guide/how-can-i-improve-serverless-function-lambda-cold-start-performance-on-vercel)
- [Redis on Vercel (Upstash)](https://vercel.com/docs/redis)

### Tutorial e Guide
- [Astro Middleware: Route Guarding with Auth Injection](https://medium.com/@whatsamattr/how-i-do-astro-middleware-c8463c47b3e3)
- [Authentication and Authorization in Astro (LogRocket)](https://blog.logrocket.com/astro-authentication-authorization/)
- [Hybrid Rendering in Astro (LogRocket)](https://blog.logrocket.com/hybrid-rendering-astro-guide/)
- [Complete Guide to Astro SSR](https://eastondev.com/blog/en/posts/dev/20251202-astro-ssr-guide/)
- [Better Auth + Astro Integration](https://better-auth.com/docs/integrations/astro)
- [Prisma + Better Auth + Astro](https://www.prisma.io/docs/guides/betterauth-astro)

### Contesto Progetto
- [Admin SPA Idea Brief](../brainstorm/admin-spa-idea-brief.md)
- [Dynamic Content Research](./dynamic-content-on-static-astro-research.md)
