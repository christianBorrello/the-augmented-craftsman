# Shared Artifacts Registry — Author Mode

**Feature**: author-mode
**Data**: 2026-03-14

Registro di tutti gli artefatti condivisi tra le journey del feature author-mode.
Ogni artefatto ha un'unica fonte di verita' e consumatori documentati.

---

## Registro Artefatti

### 1. `session.user` / `session.user.isAdmin`

| Campo | Valore |
|---|---|
| **Fonte di verita'** | Upstash Redis via Astro Sessions (`Astro.session`) |
| **Owner** | Middleware Astro (`src/middleware.ts`) |
| **Integration risk** | HIGH — se la sessione non e' verificata, qualsiasi utente accede all'admin |

**Consumatori**:
- `src/middleware.ts` — verifica isAdmin su ogni richiesta /admin/*
- `src/components/EditControls.astro` (Server Island) — decide se mostrare toolbar
- `src/actions/admin.ts` — ogni action handler verifica `context.locals.user`
- ogni pagina SSR /admin/* — legge `Astro.locals.user` per personalizzazione

**Flusso**:
```
OAuth callback
  → backend .NET verifica email == ADMIN_EMAIL
  → Astro.session.set('user', { id, email, isAdmin: true })
  → middleware legge session e scrive context.locals.user
  → action handlers leggono context.locals.user
```

**Validazione**: se `session.user` e' presente ma `isAdmin` e' false, il middleware blocca l'accesso a /admin/* e ritorna 403.

---

### 2. `post.id`

| Campo | Valore |
|---|---|
| **Fonte di verita'** | Backend .NET — generato al momento della creazione in PostgreSQL |
| **Owner** | Backend .NET (`POST /api/posts` → risposta con `id`) |
| **Integration risk** | HIGH — se l'ID e' incoerente, la modifica viene applicata al post sbagliato |

**Consumatori**:
- URL `/admin/posts/{id}/edit` — pagina edit SSR
- Action `admin.updatePost` — identificatore per PATCH /api/posts/{id}
- Action `admin.archivePost` — identificatore per DELETE/PATCH /api/posts/{id}
- Server Island `EditControls` — prop criptata per costruire link Modifica
- Backend .NET — chiave primaria in database

**Nota**: il `post.id` viene passato alla Server Island come prop criptata (meccanismo nativo di Astro per le Server Islands). Non e' mai esposto nell'HTML client-side in chiaro.

---

### 3. `post.slug`

| Campo | Valore |
|---|---|
| **Fonte di verita'** | Backend .NET — derivato dal titolo tramite logica di slugificazione nel domain |
| **Owner** | Backend .NET (`Slug` value object nel dominio) |
| **Integration risk** | HIGH — slug incoerente causa redirect a URL inesistente dopo il salvataggio |

**Consumatori**:
- URL pubblica `/blog/{slug}` — pagina SSG del post
- Redirect post-save (dopo rebuild Vercel) — `redirect('/blog/' + slug)`
- Campo Slug nel form creazione/edit — mostrare all'autore per verifica
- Server Island `EditControls` — link "Vedi post pubblico"

**Regola**: lo slug NON e' modificabile dalla pagina edit per evitare broken links. E' modificabile solo durante la creazione (prima pubblicazione). Dopo la prima pubblicazione, lo slug e' immutabile.

---

### 4. `post.status`

| Campo | Valore |
|---|---|
| **Fonte di verita'** | Backend .NET — campo `status` enum: `draft | published | archived` |
| **Owner** | Backend .NET |
| **Integration risk** | MEDIUM — stato incoerente mostra informazioni contraddittorie all'autore |

**Consumatori**:
- Badge nella lista admin `/admin/posts` — [Pubblicato] [Bozza] [Archiviato]
- Badge nella toolbar `EditControls` sulla pagina pubblica — [Pubblicato] [Bozza]
- Campo Stato nel form creazione/edit — radio button o dropdown
- Logica di rebuild: solo i post con status `published` triggerano il rebuild Vercel
- Visibilita' per i lettori: solo `published` e' accessibile su /blog/

---

### 5. `tags[]`

| Campo | Valore |
|---|---|
| **Fonte di verita'** | Backend .NET — tabella Tag in PostgreSQL, endpoint `GET /api/tags` |
| **Owner** | Backend .NET |
| **Integration risk** | MEDIUM — tag incoerenti tra admin e pagina pubblica causano confusione |

**Consumatori**:
- Autocomplete/multi-select nel form creazione post
- Autocomplete/multi-select nel form edit post
- Badge tag nella pagina pubblica del post
- Filtro tag nella lista admin (fuori scope MVP — iterazione futura)
- Pagine tag `/tags/{tagName}` (gia' esistenti — non impattate dall'author mode)

**Nota sulla creazione di nuovi tag**: quando l'autore crea un nuovo tag dal form, l'Action chiama il backend che crea il tag e lo associa al post in un'unica operazione atomica.

---

### 6. `coverImage.url`

| Campo | Valore |
|---|---|
| **Fonte di verita'** | ImageKit — URL CDN dell'immagine caricata |
| **Owner** | Backend .NET (proxy upload + generazione URL trasformazioni) |
| **Integration risk** | MEDIUM — URL incoerente causa immagine rotta nella pagina pubblica |

**Consumatori**:
- Preview nel form creazione/edit (thumbnail + URL raw)
- Pagina pubblica del post — `<img src="{coverImage.url}">` con trasformazioni ImageKit
- Lista admin — thumbnail ridotta (fuori scope MVP, ma URL necessario)
- Open Graph meta tag della pagina pubblica

**Flusso upload**:
```
Autore seleziona file
  → Action admin.uploadCoverImage
  → Backend .NET riceve multipart
  → Backend carica su ImageKit (SDK v4)
  → Backend restituisce URL CDN con trasformazioni
  → Frontend aggiorna preview nel form
  → URL salvato come campo del post al salvataggio finale
```

---

### 7. `ADMIN_EMAIL` (configurazione)

| Campo | Valore |
|---|---|
| **Fonte di verita'** | Variabile d'ambiente Vercel (`ADMIN_EMAIL`) |
| **Owner** | Configurazione infrastruttura |
| **Integration risk** | HIGH — se mancante o errata, l'autore non puo' mai autenticarsi |

**Consumatori**:
- Backend .NET — callback OAuth: confronto email utente con ADMIN_EMAIL
- Non esposta al client in nessuna forma

---

## Integration Checkpoints

| Checkpoint | Journey | Rischio se fallisce |
|---|---|---|
| `ADMIN_EMAIL` configurata in env | A (Login) | Autore bloccato fuori dall'admin |
| Upstash Redis raggiungibile | A (Login) | Sessione non creabile |
| Backend .NET raggiungibile per auth | A (Login) | Login fallisce |
| Ogni pagina /admin/* ha `prerender = false` | A, B, C, D | Middleware bypass silenzioso in prod |
| `security.checkOrigin: true` in astro.config | A, B, C, D | CSRF vulnerability |
| Backend .NET raggiungibile per CRUD | B, C, D | Operazioni su post falliscono |
| ImageKit raggiungibile | B, C | Upload immagine fallisce |
| Vercel rebuild hook configurato | B, C | Post pubblicati non aggiornano il sito statico |
| Server Islands abilitato (Astro 5.5+) | C | EditControls non renderizza |

---

## Artefatti Fuori Scope (Iterazione 2)

| Artefatto | Journey futura | Note |
|---|---|---|
| `post.content` (inline editor) | Editing in-place nativo | Tiptap inline direttamente sulla pagina pubblica |
| `autoSave.draft` | Auto-save | localStorage debounced |
| `post.schedule` | Scheduling | Data/ora di pubblicazione futura |
