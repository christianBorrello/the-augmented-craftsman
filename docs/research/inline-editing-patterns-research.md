# In-Place Editing su Siti Statici: Ricerca & Pattern

> **Ambito ricerca**: Come implementare l'editing in-place (inline) su un blog statico — dove l'utente autenticato vede i controlli di editing direttamente sulla pagina pubblicata, senza pannello admin separato.
>
> **Contesto progetto**: "The Augmented Craftsman" — frontend Astro SSG (Vercel) + backend .NET 10 (Fly.io), Hexagonal Architecture.
>
> **Data**: 2026-03-11

---

## 1. Executive Summary

L'editing in-place e' un pattern in cui l'autore autenticato vede la pagina pubblicata arricchita di controlli di editing — stesso URL, stessa pagina, trasformata in editor. Questo elimina il context-switch tra "sito pubblico" e "pannello admin".

La ricerca ha identificato **tre famiglie di approcci** nel panorama reale:

1. **CMS con Visual Editor** (Storyblok, Sanity, Contentful, TinaCMS) — editing overlaid via iframe + bridge JavaScript
2. **Editor In-Place nativi** (Editable Website/Svedit, Medium) — la pagina stessa diventa l'editor
3. **Pannelli Admin separati** (Ghost, WordPress, Decap CMS, Payload CMS) — editing in interfaccia dedicata

**Scoperta chiave per Astro**: Le Server Islands (`server:defer`) sono il meccanismo ideale per iniettare controlli di editing in pagine statiche senza sacrificare le performance SSG. La pagina resta statica per i lettori; solo per l'autore autenticato, una Server Island carica i controlli di editing.

---

## 2. Implementazioni Reali

### 2.1 Ghost

**Modello**: Pannello admin completamente separato dal sito pubblicato.

- L'editor (basato su Lexical, ex Mobiledoc/Koenig) vive nell'interfaccia admin `/ghost/`
- L'API Admin richiede token server-side — non e' progettata per editing front-end
- La documentazione e' esplicita: "The admin API key must be kept private" — impossibile esporre editing sul sito pubblicato
- Nessun supporto per inline editing sulla pagina pubblica

**Lezione**: Ghost ha scelto deliberatamente la separazione totale. L'editing front-end non e' nel loro modello.

---

### 2.2 WordPress (Full Site Editing / Gutenberg)

**Modello**: Editor separato, ma con "Full Site Editing" che simula il sito reale.

- Usa un sistema a doppio storage: template come file nel tema + versioni modificate salvate come CPT (`wp_template`)
- Il Site Editor lavora su copie CPT, non modifica direttamente il sito live
- "Template synchronization" duplica i template in draft; quando l'utente li modifica diventano "publish"
- Non e' vero inline editing sulla pagina pubblica — e' un editor separato che replica l'aspetto del sito

**Lezione**: Anche WordPress, con le sue risorse, non ha implementato vero editing in-place. Il "Full Site Editing" e' un editor che assomiglia al sito, non il sito che diventa editor.

---

### 2.3 Medium — Il "Modello Medium"

**Modello**: Stesso URL, la pagina si trasforma in editor.

- Medium usa `contentEditable` come base, ma con un layer custom di gestione sopra
- L'articolo "Why ContentEditable is Terrible" (del team Medium) documenta le sfide: inconsistenza tra browser, imprevedibilita' del DOM, necessita' di un modello dati custom sopra il DOM
- L'approccio: il DOM contentEditable e' la "view", ma il "model" e' un documento strutturato separato
- La transizione reader → editor avviene sullo stesso URL: i controlli di editing appaiono quando l'utente autenticato clicca "Edit"
- Non open source — nessun codice disponibile per analisi

**Lezione**: Il modello Medium e' l'ideale UX ma richiede un editor custom sofisticato. Non e' banale da replicare.

---

### 2.4 Storyblok — Visual Editor con Bridge

**Modello**: Il sito viene caricato in un iframe dentro l'editor Storyblok. Un bridge JavaScript abilita la comunicazione bidirezionale.

**Architettura tecnica**:
- Il sito e' caricato in un `<iframe>` all'interno dell'interfaccia Storyblok
- Il bridge JS (`StoryblokBridge`) viene inizializzato con `useStoryblokBridge(storyId, callback)`
- Ogni elemento editabile ha attributi data: `data-blok-c` (tipo componente) e `data-blok-uid` (ID univoco)
- La funzione helper `storyblokEditable(blok)` ritorna questi attributi da applicare agli elementi DOM
- Quando l'utente clicca su un elemento nel preview, Storyblok identifica il campo sorgente tramite gli attributi
- Le modifiche nel pannello laterale vengono riflesse nel preview in tempo reale via callback

**Comunicazione**: `window.postMessage` tra iframe (sito) e parent (editor Storyblok).

**Lezione**: Il pattern iframe + bridge e' potente ma richiede che il sito sia consapevole del CMS (attributi data su ogni elemento editabile).

---

### 2.5 Sanity — Visual Editing con Stega Encoding

**Modello**: Il piu' sofisticato — encoding invisibile nei contenuti per mapping automatico content → source.

**Architettura tecnica**:
- **Content Source Maps**: Metadati che mappano ogni frammento di contenuto al documento e campo sorgente nel CMS
- **Stega Encoding**: Caratteri UTF-8 invisibili (zero-width spaces, zero-width joiners) vengono appenditi alle stringhe di testo
- Questi caratteri codificano i dati della Content Source Map — documento ID, campo, dataset
- Il testo appare identico visivamente, ma contiene metadati nascosti
- Il pacchetto `@sanity/visual-editing` decodifica questi metadati e genera overlay cliccabili
- **Presentation Tool**: Strumento in Sanity Studio che carica il sito in un iframe e abilita click-to-edit
- Il sito viene caricato in iframe; i click sugli elementi vengono intercettati e mappati al campo sorgente

**Content Source Maps — struttura**:
```json
{
  "documents": [{"_id": "doc-123"}],
  "paths": ["$['title']", "$['body']"],
  "mappings": {
    "$[0]['authorName']": {
      "source": { "document": 0, "path": 1, "type": "documentValue" }
    }
  }
}
```

**Trade-off**: Lo stega encoding puo' causare problemi se le stringhe vengono usate in logica business (i caratteri invisibili alterano confronti e lunghezze). Sanity fornisce helper per pulire i dati quando necessario.

**Lezione**: L'approccio piu' elegante per mapping automatico, ma richiede un CMS che supporti Content Source Maps nativamente.

---

### 2.6 Contentful — Live Preview con SDK

**Modello**: Preview side-by-side in iframe + Inspector Mode per click-to-edit.

**Architettura tecnica**:
- Il contenuto viene caricato in un iframe all'interno dell'interfaccia Contentful
- Richiede configurazione security: `X-Frame-Options` rimosso o `Content-Security-Policy: frame-ancestors https://app.contentful.com`
- Cookie di auth richiedono `SameSite=None; Secure` per funzionare cross-iframe
- **Live Preview SDK** (JavaScript/React) abilita:
  - Aggiornamenti live: il preview si aggiorna in tempo reale mentre l'editor modifica
  - **Inspector Mode**: cliccando "Edit" su un elemento, si salta direttamente al campo sorgente
- Senza SDK: solo preview side-by-side base (nessun aggiornamento live)

**Lezione**: Il pattern iframe richiede configurazione security non banale. L'Inspector Mode (click → jump to field) e' piu' realistico dell'editing inline vero e proprio.

---

### 2.7 TinaCMS — Contextual Editing con React Hook

**Modello**: Sidebar di editing sincronizzata con preview live del sito.

**Architettura tecnica**:
- Hook centrale: `useTina({ query, variables, data })` — connette il componente React al sistema di editing
- Il hook registra i campi nella sidebar e abilita la re-idratazione live
- In edit mode: le modifiche nella sidebar si riflettono in tempo reale sulla pagina
- In produzione: l'hook passa semplicemente i dati iniziali (nessun overhead)
- Richiede React e rendering client-side per gli aggiornamenti live
- Non usa overlay cliccabili sulla pagina — l'editing avviene nella sidebar

**Limitazioni**: Solo React/Next.js. Nessun supporto Astro nativo. Richiede che le pagine supportino rendering client-side.

**Lezione**: Buon modello per sidebar editing, ma il requisito React + client-side rendering lo rende inadatto per Astro SSG.

---

### 2.8 Editable Website (Svedit) — Il Piu' Vicino al Nostro Modello

**Modello**: La pagina stessa e' l'editor. `Ctrl+E` per entrare in edit mode. **Questo e' il progetto piu' rilevante per la nostra ricerca.**

**Architettura tecnica**:
- Costruito su **SvelteKit + SQLite**
- Usa **Svedit**, un editor custom che modella il contenuto come grafo JSON immutabile
- **Schema → Document → Session**: tre layer

```
Schema: definisce i tipi di nodo (document, block, text, annotation)
Document: mappa piatta di nodi per ID, con root document_id
Session: gestisce stato, selezione, history
```

- **Nodi di tipo text**: supportano split/join (Enter/Backspace)
- **Nodi di tipo block**: strutturati con proprieta' multiple
- **Annotation**: formattazione inline (bold, link) applicata a range di testo
- **Stato immutabile con copy-on-write**: solo le parti modificate vengono copiate
- **Transazioni atomiche** con undo/redo
- **Nessun toggle esplicito read/edit**: i componenti Svelte renderizzano in modo editabile o read-only in base al contesto Svedit
- **Configurazione via `SessionConfig`**: mappa schema → componenti UI

```javascript
const config = {
  node_components: { Page, Paragraph, List },
  system_components: { NodeCursorTrap, Overlays },
  inserters: { text: (tr) => {...} },
  create_commands_and_keymap: (context) => ({...})
}
```

- **Selezione bidirezionale**: il sistema sincronizza la selezione DOM (contentEditable) con il modello interno tramite `NodeCursorTrap`
- **"Chromeless canvas"**: toolbar e menu in overlay separati, il contenuto resta pulito

**Persistenza**: Le modifiche vengono salvate nel database SQLite. L'utente autenticato vede le modifiche persistite; i visitatori vedono la versione pubblica.

**Lezione**: L'approccio piu' radicale — nessun CMS, nessun pannello admin, la pagina E' l'editor. Funziona bene per SvelteKit (server-side rendering), ma richiede un framework con rendering server + hydration.

---

### 2.9 Payload CMS — Admin React con Live Preview

**Modello**: Admin panel React (integrato in Next.js) + live preview opzionale.

- Admin costruita come React app dentro `/app` di Next.js
- Editor Lexical per rich text
- Query dirette al database nei React Server Components (no REST/GraphQL intermediario)
- Live preview disponibile ma non come editing inline sulla pagina pubblica

---

### 2.10 Decap CMS (ex Netlify CMS)

**Modello**: Pannello admin separato, tipicamente su `/admin/`, con preview laterale.

- Workflow Git-based: le modifiche committano direttamente nel repository
- Preview pane configurabile per collezione
- Deploy preview links per vedere le modifiche su staging
- Non e' inline editing — e' un'interfaccia admin con preview

---

## 3. Classificazione dei Pattern Tecnici

### 3.1 Pattern A — Pannello Admin Separato
**Chi lo usa**: Ghost, WordPress, Decap CMS, Payload CMS
**Come funziona**: Interfaccia dedicata su URL/subdomain separato. Nessuna modifica al sito pubblico.
**Pro**: Separazione netta, sicurezza, nessun JS aggiuntivo sul sito pubblico
**Contro**: Context-switch tra admin e sito, l'autore non vede il risultato nel contesto reale

### 3.2 Pattern B — Iframe + Bridge (Visual Editing)
**Chi lo usa**: Storyblok, Sanity, Contentful, TinaCMS
**Come funziona**: Il sito viene caricato in un iframe dentro l'interfaccia del CMS. Un bridge JavaScript abilita comunicazione bidirezionale.
**Pro**: L'editor vede il sito reale, click-to-edit sui contenuti
**Contro**: Richiede CMS esterno, configurazione security (iframe, CORS, cookies), il sito deve essere "CMS-aware" (attributi data, SDK)

### 3.3 Pattern C — La Pagina E' l'Editor (In-Place Nativo)
**Chi lo usa**: Editable Website/Svedit, Medium
**Come funziona**: La pagina pubblicata si trasforma in editor per l'utente autenticato. Stesso URL, stessa pagina, UI di editing sovrapposta.
**Pro**: Zero context-switch, editing nel contesto reale, UX superiore
**Contro**: Complessita' di implementazione, richiede editor custom, JS significativo per l'editing

---

## 4. Editor Rich Text per In-Place Editing

Se si sceglie il Pattern C, serve un editor embeddabile nella pagina. Opzioni principali:

### 4.1 Tiptap (ProseMirror-based)
- **Headless**: nessuna UI predefinita, controllo totale
- **Bubble/floating menu**: toolbar contestuali vicino al testo selezionato
- **Framework-agnostic**: React, Vue, Svelte, vanilla JS
- **Collaborazione**: supporto Y.js per editing multi-utente
- **Markdown**: import/export markdown supportato
- **Ideale per**: costruire un editor Medium-like custom

### 4.2 Lexical (Meta)
- **Framework extensible**: plugin-based, core minimale
- **Ogni istanza si attacca a un singolo `contentEditable`**
- **Stato**: modello editor state (corrente + pending)
- **Usato da**: Payload CMS, probabilmente prodotti Meta
- **React bindings** inclusi, ma framework-agnostic nel core

### 4.3 Slate
- **React-based**: specificamente progettato per React
- **DOM-parallel**: struttura documento che rispecchia il DOM
- **Plugin-first**: completa personalizzazione del comportamento
- **31.6k GitHub stars**, usato da molti progetti
- **Ideale per**: editor custom complessi in React

### 4.4 Editor.js
- **Block-based**: ogni blocco e' un elemento indipendente (paragrafo, heading, immagine, lista)
- **Output JSON**: non HTML — contenuto portabile
- **Plugin-driven**: ogni tipo di blocco e' un plugin
- **Ideale per**: editing strutturato a blocchi (stile Notion)

### 4.5 Milkdown (ProseMirror + Remark)
- **WYSIWYG Markdown**: renderizza markdown come rich text editabile
- **Plugin architecture**: tutto e' un plugin
- **Headless**: nessun CSS incluso
- **Y.js collaboration**: supporto nativo
- **Ideale per**: blog dove il formato nativo e' markdown

### 4.6 Svedit
- **Svelte-native**: progettato specificamente per Svelte/SvelteKit
- **Grafo JSON immutabile**: modello dati sofisticato
- **Transazioni atomiche**: undo/redo robusto
- **"Content is the editor"**: la pagina stessa diventa editabile
- **Limitazione**: solo Svelte

---

## 5. Fattibilita' Astro — Server Islands per Edit Controls

### 5.1 Il Problema Fondamentale

Le pagine del blog sono **SSG** (statiche, CDN-cached). Come aggiungere editing in-place a pagine statiche?

### 5.2 Le Quattro Opzioni

#### Opzione A — Rendere TUTTE le pagine SSR
```astro
---
export const prerender = false; // Su ogni pagina blog
---
```
**Pro**: Accesso completo a session/cookie su ogni pagina
**Contro**: Perde tutti i benefici SSG (CDN caching, performance, costo zero). Per un singolo autore che edita raramente, sacrificare le performance di TUTTI i lettori e' inaccettabile.

**Verdetto**: Scartata.

#### Opzione B — Server Islands per Controlli di Editing (RACCOMANDATA)
```astro
---
// src/components/EditControls.astro
const session = Astro.cookies.get('session');
const isAuthor = await verifyAuthorSession(session);
---
{isAuthor && (
  <div class="edit-toolbar">
    <button onclick="enableEditing()">Modifica</button>
    <button onclick="openEditor()">Editor Completo</button>
  </div>
)}
{!isAuthor && <Fragment />} <!-- Nulla per i lettori -->
```

```astro
---
// src/pages/blog/[slug].astro (resta SSG!)
import EditControls from '../components/EditControls.astro';
---
<article>
  <h1>{post.title}</h1>
  <div set:html={post.content} />
</article>

<EditControls server:defer slug={post.slug}>
  <Fragment slot="fallback" /> <!-- Nulla durante il caricamento -->
</EditControls>
```

**Come funziona**:
1. La pagina viene servita come HTML statico dalla CDN (velocissima per TUTTI i lettori)
2. Un piccolo script inline richiede la Server Island `EditControls`
3. La Server Island controlla il cookie di sessione sul server
4. Se l'utente e' l'autore: ritorna HTML con toolbar di editing
5. Se l'utente e' un lettore: ritorna un fragment vuoto
6. Il lettore non vede nulla e non paga nessun costo JS/latenza

**Pro**:
- Pagine restano SSG (CDN-cached, velocissime)
- Auth check avviene server-side (sicuro, nessun token nel client)
- I lettori pagano solo il costo di una richiesta GET extra (che ritorna vuota)
- L'autore vede i controlli di editing nel contesto reale della pagina
- Props criptate — nessun dato sensibile nell'URL

**Contro**:
- Richiede adapter server-side (Vercel adapter gia' in uso)
- Ogni visitatore fa una richiesta alla Server Island (anche se ritorna vuota per i lettori)
- Server Island ha contesto isolato (`Astro.url` non e' l'URL della pagina — usare `Referer` header)

**Verdetto**: L'approccio ideale per il nostro caso d'uso.

#### Opzione C — Client-Side JS per Auth Check + Overlay
```javascript
// Inline script nella pagina statica
const session = await fetch('/api/auth/session');
if (session.isAuthor) {
  document.querySelector('.edit-overlay').style.display = 'block';
}
```

**Pro**: Nessun server-side rendering necessario
**Contro**: Espone endpoint auth al client, flash di contenuto (FOUC), JS extra per tutti i visitatori, meno sicuro

**Verdetto**: Funziona ma inferiore a Server Islands sotto ogni aspetto.

#### Opzione D — Service Worker / Edge Middleware
```typescript
// middleware.ts
export const onRequest = defineMiddleware(async (context, next) => {
  const session = await getSession(context.request.headers);
  if (session?.isAuthor) {
    // Iniettare script/UI di editing nella risposta
  }
  return next();
});
```

**Pro**: Intercetta la risposta prima che arrivi al client
**Contro**: Il middleware Astro non puo' modificare pagine pre-renderizzate (SSG), funziona solo con SSR. Per le pagine statiche, il middleware non viene eseguito.

**Verdetto**: Non applicabile a pagine SSG.

### 5.3 Architettura Raccomandata — Server Islands + Client Island Editor

```
┌─────────────────────────────────────────────────────────┐
│  Blog Post Page (STATICA, CDN-cached)                   │
│                                                         │
│  ┌───────────────────────────────────────────────────┐  │
│  │  Article Content (HTML statico pre-renderizzato)  │  │
│  │  <h1>{title}</h1>                                 │  │
│  │  <div>{content}</div>                             │  │
│  └───────────────────────────────────────────────────┘  │
│                                                         │
│  ┌───────────────────────────────────────────────────┐  │
│  │  EditControls (Server Island, server:defer)       │  │
│  │                                                   │  │
│  │  IF autore autenticato:                           │  │
│  │    → Mostra toolbar con bottoni Edit/Preview      │  │
│  │    → Carica InlineEditor (Preact island,          │  │
│  │      client:idle) con Tiptap/Milkdown             │  │
│  │    → Editor sovrappone il contenuto statico       │  │
│  │    → Save chiama API backend                      │  │
│  │                                                   │  │
│  │  IF lettore:                                      │  │
│  │    → Ritorna fragment vuoto                       │  │
│  │    → Zero impatto su performance                  │  │
│  └───────────────────────────────────────────────────┘  │
│                                                         │
│  ┌───────────────────────────────────────────────────┐  │
│  │  LikeButton (Client Island, client:visible)       │  │
│  │  CommentList (Server Island, server:defer)        │  │
│  └───────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
```

### 5.4 Flusso Dettagliato per l'Autore

1. L'autore visita `/blog/my-post` (pagina statica, CDN)
2. La pagina carica istantaneamente (HTML statico)
3. Lo script della Server Island fa GET a `/_server-islands/EditControls`
4. Il server verifica il cookie di sessione → utente e' l'autore
5. Il server ritorna HTML con:
   - Toolbar floating (CSS) con bottoni "Modifica" / "Preview" / "Salva"
   - Script che carica l'editor Preact island
6. L'autore clicca "Modifica"
7. L'editor (Tiptap/Milkdown) si sovrappone al contenuto statico:
   - Il `<div>` del contenuto diventa `contentEditable`
   - Oppure: l'editor sostituisce il contenuto con una versione editabile
8. L'autore modifica il testo
9. L'autore clicca "Salva"
10. Il client invia PUT a `/api/posts/{id}` con il contenuto aggiornato
11. Il backend salva e triggera un rebuild/revalidazione della pagina statica
12. La pagina si ricarica con il contenuto aggiornato

### 5.5 Flusso per il Lettore

1. Il lettore visita `/blog/my-post` (pagina statica, CDN)
2. La pagina carica istantaneamente (HTML statico)
3. Lo script della Server Island fa GET a `/_server-islands/EditControls`
4. Il server verifica il cookie di sessione → nessuna sessione autore
5. Il server ritorna un fragment vuoto
6. Nessun impatto visibile o di performance

---

## 6. Gestione Immagini In-Place

### 6.1 Pattern comune (tutti i CMS)
- **Drag & drop zone**: l'utente trascina un'immagine sul contenuto
- **Upload immediato**: l'immagine viene caricata su un CDN (ImageKit nel nostro caso)
- **Placeholder temporaneo**: durante l'upload, un placeholder con spinner
- **URL sostituzione**: completato l'upload, il placeholder diventa `<img src="imagekit-url">`

### 6.2 Implementazione per il nostro caso
- L'editor (Tiptap/Milkdown) gestisce il drag & drop
- L'upload va direttamente a ImageKit (SDK gia' integrato nel backend)
- Il backend genera URL ottimizzati con trasformazioni ImageKit
- L'URL viene inserito nel markdown/rich text

---

## 7. Workflow Save/Publish

### 7.1 Pattern osservati

| Piattaforma | Salvataggio | Pubblicazione |
|---|---|---|
| Medium | Auto-save continuo (debounced) | Bottone "Publish" esplicito |
| Ghost | Auto-save ogni modifica | Toggle Draft ↔ Published |
| WordPress | Save Draft / Update | Bottone "Publish" / "Update" |
| Editable Website | Save esplicito (Ctrl+S) | Immediato (nessun draft) |
| Storyblok | Save in draft | Publish esplicito |

### 7.2 Raccomandazione per il nostro caso

```
Modifica in-place → Auto-save locale (localStorage, debounced)
                  → Bottone "Salva Bozza" (PUT /api/posts/{id} con status: draft)
                  → Bottone "Pubblica" (PUT /api/posts/{id} con status: published)
                  → Trigger rebuild pagina statica
```

- **Auto-save locale**: le modifiche non perse se il browser crasha
- **Salva bozza**: persiste sul server ma non visibile ai lettori
- **Pubblica**: aggiorna la pagina statica (richiede rebuild/revalidazione)

---

## 8. Decisione Architetturale: Admin SPA vs In-Place Editing

### 8.1 Contesto

Esiste gia' un idea brief per un **Admin SPA separato** (`docs/brainstorm/admin-spa-idea-brief.md`). Questa ricerca esplora l'alternativa in-place editing.

### 8.2 Confronto

| Aspetto | Admin SPA Separata | In-Place Editing (Server Islands) |
|---|---|---|
| **UX autore** | Context-switch tra admin e sito | Editing nel contesto reale |
| **Complessita'** | Moderata (React SPA standard) | Alta (editor inline, Server Islands, sync) |
| **Impatto su frontend** | Zero (app separata) | Significativo (editor JS, Server Islands) |
| **JS per i lettori** | Zero (app separata) | Quasi zero (Server Island vuota) |
| **Portfolio value** | Dimostra separazione degli adapter | Dimostra pattern avanzati (Server Islands, editor custom) |
| **MVP velocity** | Veloce (textarea + form standard) | Lenta (editor inline richiede piu' lavoro) |
| **Upgrade path** | Aggiungere editor rich-text nella SPA | Gia' integrato se si sceglie Tiptap/Milkdown |
| **Mobile editing** | Funziona (form standard) | Problematico (contentEditable su mobile e' scarso) |
| **Gestione lista post** | Naturale (tabella/lista nella SPA) | Innaturale (serve comunque una pagina di gestione) |
| **Creazione nuovo post** | Form standard nella SPA | Serve una route dedicata (non puo' essere "in-place" su un post che non esiste) |

### 8.3 Approccio Ibrido (Raccomandato)

**Non sono mutuamente esclusivi.** L'approccio ottimale e':

1. **MVP**: Admin SPA separata (come nel brief esistente) — textarea markdown, form standard, veloce da costruire
2. **Iterazione futura**: Aggiungere "Quick Edit" in-place via Server Islands
   - Bottone "Edit" sulla pagina pubblica (visibile solo all'autore)
   - Click → apre un editor inline per modifiche rapide (typo, correzioni)
   - Per editing completo → link "Apri nell'editor" che porta alla SPA admin
3. **Evoluzione**: Se l'editing in-place si dimostra sufficiente, la SPA admin diventa opzionale

Questo approccio incrementale segue il principio "the simplest thing that could possibly work" e permette di validare entrambi i pattern.

---

## 9. Implicazioni Tecniche per Astro

### 9.1 Server Islands — Conferme dalla Documentazione

- **Funzionano su pagine SSG**: Si'. La pagina resta statica; la Server Island viene richiesta separatamente
- **Accesso a cookie/sessione**: Si'. La Server Island ha accesso completo a `Astro.cookies`
- **Rendering condizionale**: Si'. Puo' ritornare HTML diverso (o vuoto) in base all'auth
- **Props criptate**: Le props vengono criptate nell'URL (sicuro)
- **Contesto pagina**: `Astro.url` ritorna `/_server-islands/ComponentName` — usare `Referer` header per l'URL della pagina reale
- **Caching**: Le richieste GET possono essere cachate con `Cache-Control`; per l'auth, evitare il caching
- **Output mode**: Richiede `'server'` o `'hybrid'` — `'hybrid'` e' gia' ideale (pagine statiche di default, SSR dove serve)

### 9.2 Requisiti Tecnici

```
astro.config.mjs:
  output: 'hybrid'          // gia' necessario per Server Islands esistenti
  adapter: vercel()         // gia' in uso

Nuovi componenti:
  src/components/EditControls.astro    — Server Island per auth check
  src/components/InlineEditor.tsx      — Preact island per editing (se si sceglie in-place)

Dipendenze aggiuntive (solo per in-place editing):
  @tiptap/core + @tiptap/starter-kit   — editor rich text (~30KB)
  OPPURE
  milkdown                              — WYSIWYG markdown (~25KB)
```

### 9.3 Astro Authentication — Stato dell'Arte

La documentazione Astro conferma:
- Auth via cookie HTTP-only (gia' implementato nel progetto)
- Middleware per protezione route (funziona solo su pagine SSR, non su SSG)
- Server Islands hanno accesso a `Astro.cookies` e `Astro.request.headers`
- Soluzioni supportate: Better Auth, Clerk, Lucia
- Il progetto usa gia' ASP.NET Identity nel backend — la Server Island puo' verificare il token/cookie chiamando il backend

---

## 10. Raccomandazione Finale

### Per il MVP: Admin SPA Separata
Come gia' pianificato nel brief esistente. E' l'approccio piu' veloce, testato, e funzionale.

### Per l'iterazione successiva: "Quick Edit" via Server Islands
Aggiungere un bottone "Modifica" sulla pagina pubblica (visibile solo all'autore via Server Island) che:
- Per modifiche minori: attiva editing inline (Tiptap con bubble menu)
- Per editing completo: reindirizza alla SPA admin

### Tecnologie per l'editor inline:
- **Primo scelta: Tiptap** — headless, framework-agnostic, bubble menu nativo, markdown import/export, comunita' attiva
- **Alternativa: Milkdown** — se il formato nativo resta markdown, Milkdown offre WYSIWYG markdown nativo

### Pattern da seguire:
- **Storyblok/Sanity come ispirazione** per il mapping content → editable region
- **Svedit/Editable Website come ispirazione** per il modello "la pagina e' l'editor"
- **Server Islands come meccanismo** per iniettare i controlli senza impatto sui lettori

---

## 11. Fonti & Riferimenti

### Documentazione Ufficiale
- [Astro Server Islands](https://docs.astro.build/en/guides/server-islands/)
- [Astro 5 — Server Islands](https://astro.build/blog/astro-5/)
- [Astro 4.12 — Server Islands Intro](https://astro.build/blog/astro-4120/)
- [Astro Authentication Guide](https://docs.astro.build/en/guides/authentication/)
- [Astro On-Demand Rendering](https://docs.astro.build/en/guides/on-demand-rendering/)

### CMS Visual Editing
- [Sanity Visual Editing](https://www.sanity.io/docs/visual-editing)
- [Sanity Content Source Maps](https://www.sanity.io/docs/visual-editing/content-source-maps)
- [Sanity Stega Encoding](https://www.sanity.io/docs/stega)
- [Sanity Visual Editing Repository](https://github.com/sanity-io/visual-editing)
- [Contentful Live Preview](https://www.contentful.com/developers/docs/tutorials/general/live-preview/)
- [Storyblok JS SDK](https://www.storyblok.com/docs/packages/storyblok-js)
- [TinaCMS Contextual Editing](https://tina.io/docs/contextual-editing/overview/)
- [TinaCMS React Visual Editing](https://tina.io/docs/contextual-editing/react/)

### Editor Rich Text
- [Tiptap](https://tiptap.dev/) — headless rich text editor (ProseMirror-based)
- [Lexical](https://lexical.dev/) — Meta's extensible text editor framework
- [Slate](https://github.com/ianstormtaylor/slate) — customizable rich text framework
- [Editor.js](https://editorjs.io/) — block-based editor
- [Milkdown](https://milkdown.dev/) — plugin-driven WYSIWYG markdown editor

### Progetti In-Place Editing
- [Editable Website](https://editable.website/) — SvelteKit + Svedit in-place editing
- [Svedit](https://github.com/michael/svedit) — Svelte content editing library
- [Payload CMS](https://payloadcms.com/) — Next.js native CMS con Lexical
- [Decap CMS](https://decapcms.org/) — Git-based CMS per siti statici

### Admin Panel Pattern
- Ghost Admin API — pannello separato, API server-side only
- WordPress Full Site Editing — editor simula il sito ma non e' inline
- Payload CMS — admin React integrata in Next.js

### Referenze Interne al Progetto
- `docs/brainstorm/admin-spa-idea-brief.md` — idea brief per Admin SPA
- `docs/research/dynamic-content-on-static-astro-research.md` — ricerca su contenuto dinamico su pagine statiche
- `docs/research/author-mode-best-practices.md` — best practices per author mode
