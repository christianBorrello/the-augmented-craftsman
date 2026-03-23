# Journey Visual: Author Mode

**Feature**: author-mode
**Persona**: Christian Borrello — unico autore del blog "The Augmented Craftsman"
**Data**: 2026-03-14

---

## Flusso Complessivo (4 journey correlate)

```
TRIGGER: "Voglio scrivere/modificare qualcosa"
          |
          v
    [JOURNEY A: LOGIN]
    /admin/login
    OAuth Google/GitHub + whitelist ADMIN_EMAIL
          |
          v
    +-----------+----------+
    |                      |
    v                      v
[JOURNEY B]          [JOURNEY D]
Crea nuovo post      Gestione lista post
/admin/posts/new     /admin/posts
    |                      |
    |              [modifica esistente]
    |                      |
    v                      v
[JOURNEY C: IN-PLACE EDITING]
/blog/{slug} --> EditControls toolbar
--> link a /admin/posts/{id}/edit
--> editor Tiptap completo
          |
          v
    [SALVA + REBUILD]
    Spinner attesa Vercel rebuild
    --> Redirect /blog/{slug}
```

---

## Arco Emotivo Globale

```
LOGIN        EDITOR APERTO    SCRITTURA      SALVATAGGIO    RISULTATO
  |               |               |               |              |
Neutro/       Concentrato     In flusso      Incertezza    Soddisfazione
Pratico       (focus task)    creativo       (funziona?    + conferma
"devo         "ci sono"       "sto           rebuild ok?)  "e' online"
entrare"                      creando"
```

Il picco di tensione e' il **salvataggio**: l'autore ha appena finito di scrivere e non sa se il rebuild andra' a buon fine. Il feedback visivo (spinner + conferma) risolve questa tensione o la trasforma in un'azione di recovery chiara.

---

## Journey A — Login Admin

### Trigger
Christian vuole pubblicare un post o modificare uno esistente. Non ha una sessione attiva.

### Flusso

```
[Visita /admin/login]
         |
         v
+------------------------------------------+
|  The Augmented Craftsman — Admin         |
|                                          |
|  Accedi come autore                      |
|                                          |
|  [  Accedi con Google  ]                 |
|  [  Accedi con GitHub  ]                 |
|                                          |
|  Solo l'autore autorizzato puo'          |
|  accedere a questa sezione.              |
+------------------------------------------+
         |
         v
[OAuth flow Google/GitHub]
         |
         v
[Backend .NET verifica: email == ADMIN_EMAIL]
         |
    +----+----+
    |         |
    v         v
[MATCH]   [NO MATCH]
    |         |
    v         v
[Sessione  [Redirect /admin/login
admin      con messaggio "Accesso
creata]    non autorizzato"]
    |
    v
[Redirect /admin/posts]
```

### Mockup — Pagina Login

```
+--------------------------------------------------+
|  The Augmented Craftsman                         |
+--------------------------------------------------+
|                                                  |
|                                                  |
|         Accesso Autore                           |
|                                                  |
|    +------------------------------------------+ |
|    |                                          | |
|    |   [G]  Accedi con Google                 | |
|    |                                          | |
|    |   [GH] Accedi con GitHub                 | |
|    |                                          | |
|    +------------------------------------------+ |
|                                                  |
|    Solo l'autore autorizzato puo' accedere       |
|    a questa sezione del blog.                    |
|                                                  |
+--------------------------------------------------+
```

### Stato emotivo
- Entrata: neutro/pratico — e' una routine, non un'avventura
- Uscita (successo): sollievo + orientamento — "sono dentro, cosa faccio?"
- Uscita (errore): frustrazione controllata — il messaggio deve essere chiaro e non generico

### Error paths
- Email non in whitelist: pagina login con banner "Account non autorizzato. Solo l'autore del blog puo' accedere."
- OAuth fallisce (provider down): pagina login con banner "Autenticazione temporaneamente non disponibile. Riprova tra qualche minuto."
- Sessione scaduta durante la navigazione admin: redirect silenzioso a /admin/login con banner "Sessione scaduta. Accedi di nuovo."

---

## Journey B — Crea Nuovo Post

### Trigger
Christian ha un'idea, vuole pubblicarla. Arriva da /admin/posts cliccando "+ Nuovo post".

### Flusso

```
[/admin/posts]
      |
      v
[Clicca "+ Nuovo post"]
      |
      v
[/admin/posts/new]
      |
      v
[Compila: Titolo]
      |
      v
[Slug: generato automaticamente dal titolo]
[Editabile manualmente se necessario]
      |
      v
[Editor Tiptap — corpo del post]
      |
      v
[Seleziona/crea Tag]
      |
      v
[Upload immagine copertina (opzionale)]
      |
      v
[Stato: Draft | Pubblica ora]
      |
      v
[Clicca "Salva" o "Pubblica"]
      |
      v
[Action admin.createPost → backend .NET]
      |
    +--+--+
    |     |
    v     v
[OK]  [ERRORE]
    |     |
    v     v
[Se Draft:  [Form con messaggio
redirect    di errore specifico,
/admin/     dati preservati]
posts]
    |
[Se Published:]
    |
    v
[Spinner "Rebuild in corso..."]
    |
    v
[Vercel rebuild completo]
    |
    v
[Redirect /blog/{slug}]
```

### Mockup — Form Nuovo Post

```
+--------------------------------------------------+
|  The Augmented Craftsman — Admin                 |
|  [< Lista post]                                  |
+--------------------------------------------------+
|                                                  |
|  Nuovo post                                      |
|                                                  |
|  Titolo *                                        |
|  +--------------------------------------------+ |
|  | TDD con .NET 10: la doppia copertura        | |
|  +--------------------------------------------+ |
|                                                  |
|  Slug (generato automaticamente)                 |
|  +--------------------------------------------+ |
|  | tdd-con-dotnet-10-la-doppia-copertura       | |
|  +--------------------------------------------+ |
|                                                  |
|  Contenuto *                                     |
|  +--------------------------------------------+ |
|  | [B] [I] [H2] [H3] [Link] [Codice] [Img]   | |
|  |--------------------------------------------|  |
|  |                                            | |
|  | Il test outside-in parte sempre da...      | |
|  |                                            | |
|  +--------------------------------------------+ |
|                                                  |
|  Tag                                             |
|  +--------------------------------------------+ |
|  | [TDD x] [.NET x]  [+ Aggiungi tag]         | |
|  +--------------------------------------------+ |
|                                                  |
|  Immagine copertina                              |
|  +--------------------------------------------+ |
|  |  [Scegli file]  Nessun file selezionato    | |
|  +--------------------------------------------+ |
|                                                  |
|  Stato                                           |
|  ( ) Bozza                                       |
|  (*) Pubblica ora                                |
|                                                  |
|  [Salva bozza]              [Pubblica]           |
|                                                  |
+--------------------------------------------------+
```

### Stato emotivo
- Entrata: motivato/energico — ha un'idea da condividere
- Durante scrittura: in flusso — l'editor deve sparire, solo il contenuto
- Salvataggio: picco di incertezza — "funziona? il rebuild parte?"
- Uscita (successo pubblicazione): soddisfazione — vede il post live sul suo blog

---

## Journey C — Modifica Post Esistente (In-Place)

### Trigger
Christian legge un suo post pubblicato e nota un errore o vuole aggiornarlo.

### Flusso (MVP)

```
[Christian visita /blog/{slug}]
         |
         v
[Pagina SSG carica istantaneamente dalla CDN]
         |
         v
[Server Island EditControls fa GET]
[Server verifica cookie sessione]
         |
    +----+----+
    |         |
    v         v
[AUTORE]  [LETTORE]
    |         |
    v         v
[Toolbar   [Fragment
floating]   vuoto —
    |       zero impatto]
    v
+------------------------------------------+
|  The Augmented Craftsman                 |
|                            [BOZZA][Modifica] |
+------------------------------------------+
|                                          |
|  TDD con .NET 10: la doppia copertura    |
|  14 marzo 2026 · 8 min lettura           |
|  [TDD] [.NET]                            |
|                                          |
|  Il test outside-in parte sempre da...   |
|                                          |
+------------------------------------------+
         |
[Clicca "Modifica"]
         |
         v
[Redirect a /admin/posts/{id}/edit]
         |
         v
[Form identico al "Nuovo post"
 pre-compilato con i dati esistenti]
         |
         v
[Modifica, poi Salva]
         |
         v
[Spinner rebuild]
         |
         v
[Redirect /blog/{slug} — versione aggiornata]
```

### Mockup — Toolbar EditControls (visibile solo all'autore)

```
+--------------------------------------------------+
|  The Augmented Craftsman          [Bozza][Modifica]|
+--------------------------------------------------+
|                                                  |
|  TDD con .NET 10: la doppia copertura            |
|  14 marzo 2026 · 8 min lettura                   |
|  [TDD] [.NET] [Clean Architecture]               |
|                                                  |
|  Il test outside-in parte sempre da un           |
|  confine reale del sistema...                    |
|                                                  |
+--------------------------------------------------+

Badge di stato:
  [Pubblicato] = verde
  [Bozza]      = grigio/arancio
```

### Mockup — Form Edit Post (SSR)

```
+--------------------------------------------------+
|  The Augmented Craftsman — Admin                 |
|  [< Lista post]  [Vedi post pubblico ->]         |
+--------------------------------------------------+
|                                                  |
|  Modifica post                                   |
|                                                  |
|  Titolo *                                        |
|  +--------------------------------------------+ |
|  | TDD con .NET 10: la doppia copertura        | |
|  +--------------------------------------------+ |
|                                                  |
|  [... stessi campi del form Nuovo Post ...]      |
|                                                  |
|  Stato                   [Archivia post]         |
|  (*) Pubblicato                                  |
|  ( ) Bozza                                       |
|                                                  |
|  [Salva modifiche]          [Pubblica]           |
|                                                  |
+--------------------------------------------------+
```

### Stato emotivo
- Entrata: leggera irritazione (ha visto un errore) o curiosita' (vuole aggiornare)
- Dopo click "Modifica": orientamento rapido — il form e' familiare
- Dopo salvataggio: sollievo + soddisfazione

---

## Journey D — Gestione Lista Post

### Trigger
Christian vuole avere una visione d'insieme del blog: cosa e' pubblicato, cosa e' in bozza, quali post archiviare.

### Flusso

```
[/admin/posts]
         |
         v
+--------------------------------------------------+
|  The Augmented Craftsman — Admin                 |
|                              [+ Nuovo post]      |
+--------------------------------------------------+
|  Post                                            |
|                                                  |
|  TITOLO              STATO      DATA     AZIONI  |
|  TDD con .NET 10...  Pubblicato 14/03    [M][A]  |
|  Clean Arch intro    Bozza      12/03    [M][A]  |
|  DDD per blog CRUD   Pubblicato 10/03    [M][A]  |
|  ...                                             |
|                                                  |
|  [M] = Modifica  [A] = Archivia                  |
+--------------------------------------------------+
```

### Azioni disponibili
- **Modifica**: link a /admin/posts/{id}/edit
- **Archivia**: soft delete — il post diventa "archiviato", non visibile ai lettori, recuperabile
- **Cambio stato inline**: toggle Bozza / Pubblicato direttamente dalla lista (via Action)

### Mockup — Lista Post

```
+--------------------------------------------------+
|  The Augmented Craftsman — Admin   [+ Nuovo post]|
+--------------------------------------------------+
|                                                  |
|  I tuoi post (12 totali)                         |
|  [Tutti] [Pubblicati] [Bozze] [Archiviati]       |
|                                                  |
|  +-----------+------------+--------+----------+  |
|  | Titolo    | Stato      | Data   | Azioni   |  |
|  +-----------+------------+--------+----------+  |
|  | TDD con   | Pubblicato | 14/03  | [M] [A]  |  |
|  | .NET 10   |            |        |          |  |
|  +-----------+------------+--------+----------+  |
|  | Clean Ar  | Bozza      | 12/03  | [M] [A]  |  |
|  | intro     |            |        |          |  |
|  +-----------+------------+--------+----------+  |
|  | DDD per   | Pubblicato | 10/03  | [M] [A]  |  |
|  | blog CRUD |            |        |          |  |
|  +-----------+------------+--------+----------+  |
|                                                  |
+--------------------------------------------------+
```

---

## Journey Trasversale — Feedback Salvataggio e Rebuild

Questa micro-journey si innesta in B e C al momento del salvataggio.

```
[Utente clicca "Salva" / "Pubblica"]
         |
         v
[Action admin.createPost / admin.updatePost]
[Chiamata al backend .NET]
         |
    +----+----+
    |         |
    v         v
[Backend OK] [Backend ERRORE]
    |             |
    v             v
[Se Draft:    [Form con errore
redirect      specifico, dati
/admin/posts] preservati]
    |
[Se Published:]
    |
    v
+------------------------------------------+
|  Rebuild in corso...                     |
|                                          |
|  [=====>         ] Pubblicazione...      |
|                                          |
|  Il post sara' visibile ai lettori       |
|  tra qualche secondo.                    |
+------------------------------------------+
         |
         v
[Vercel rebuild hook completato]
         |
         v
[Redirect automatico a /blog/{slug}]
[Il post e' live]
```

### Mockup — Spinner Rebuild

```
+--------------------------------------------------+
|  The Augmented Craftsman — Admin                 |
+--------------------------------------------------+
|                                                  |
|                                                  |
|    Pubblicazione in corso...                     |
|                                                  |
|    [##########                    ] 30%          |
|                                                  |
|    Rebuild del sito statico in corso.            |
|    Il post sara' visibile ai lettori             |
|    tra qualche secondo.                          |
|                                                  |
|    Non chiudere questa finestra.                 |
|                                                  |
+--------------------------------------------------+
```

---

## Integrazione tra Journey

```
Journey A (Login)
     |
     +--> Journey D (Lista) <--> Journey B (Crea)
                |
                +--> Journey C (Modifica in-place)
                          |
                          +--> /admin/posts/{id}/edit
                                    |
                                    +--> Feedback Rebuild
                                              |
                                              +--> /blog/{slug}
```

**Artefatti condivisi che attraversano le journey**:
- `session.user.isAdmin` — verificato a ogni richiesta admin
- `post.id` — chiave di identita' del post tra lista, edit, Server Island
- `post.slug` — usato per URL pubblico e redirect post-save
- `post.status` — visualizzato nella lista, nel badge EditControls, nel form
- `tags[]` — sincronizzati tra form e pagina pubblica
- `coverImage.url` — storage ImageKit, mostrato in form e pagina pubblica
