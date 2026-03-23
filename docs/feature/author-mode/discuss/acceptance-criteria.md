# Acceptance Criteria: Author Mode

**Feature**: author-mode
**Data**: 2026-03-14

Riepilogo consolidato delle acceptance criteria per tutte le user stories.
Ogni item e' tracciabile alla story di origine e al relativo scenario BDD.

---

## US-01: Login admin tramite OAuth

| # | Criterio | Scenario |
|---|---|---|
| AC-01-1 | Pagina /admin/login mostra due bottoni: "Accedi con Google" e "Accedi con GitHub" | Happy path |
| AC-01-2 | Il flow OAuth delega al provider scelto e riporta il callback al backend .NET | Happy path |
| AC-01-3 | Il backend confronta l'email OAuth con ADMIN_EMAIL e risponde con i dati utente o errore 403 | Happy path + errore |
| AC-01-4 | Astro crea una sessione admin in Upstash Redis con `isAdmin: true` | Happy path |
| AC-01-5 | Il cookie di sessione e' HTTP-only e Secure | Sicurezza |
| AC-01-6 | Il middleware blocca tutte le richieste /admin/* senza sessione valida e reindirizza a /admin/login | Guard |
| AC-01-7 | Email non autorizzata: messaggio "Account non autorizzato", nessuna sessione creata | Errore |
| AC-01-8 | Sessione scaduta: redirect a /admin/login con messaggio "Sessione scaduta. Accedi di nuovo." | Edge case |

---

## US-02: Middleware auth guard

| # | Criterio | Scenario |
|---|---|---|
| AC-02-1 | Il middleware verifica la sessione per ogni richiesta a /admin/* (eccetto /admin/login) | Guard |
| AC-02-2 | Con sessione valida: `context.locals.user` viene popolato con i dati dell'autore | Happy path |
| AC-02-3 | Senza sessione valida: redirect a /admin/login | Guard |
| AC-02-4 | La pagina /admin/login non e' soggetta al guard | Edge case |
| AC-02-5 | Ogni Action admin verifica `context.locals.user` e lancia UNAUTHORIZED se assente | Sicurezza |
| AC-02-6 | Il middleware usa `getActionContext()` per bloccare anche le Action non autenticate | Sicurezza |
| AC-02-7 | `security.checkOrigin: true` protegge da CSRF su tutte le Action | Sicurezza |

---

## US-03: Form crea nuovo post con editor Tiptap

| # | Criterio | Scenario |
|---|---|---|
| AC-03-1 | Pagina /admin/posts/new ha `export const prerender = false` | Tecnico |
| AC-03-2 | Il campo Titolo e' obbligatorio (validazione client e server) | Validazione |
| AC-03-3 | Lo Slug viene generato automaticamente dal Titolo (backend) | Auto-slug |
| AC-03-4 | Lo Slug e' editabile manualmente dall'autore | Edit manuale |
| AC-03-5 | L'editor Tiptap e' un'isola Preact con `client:only="preact"` e `immediatelyRender: false` | Tecnico |
| AC-03-6 | L'editor supporta: paragrafo, H2, H3, grassetto, corsivo, codice inline, blocco codice, link | Funzionale |
| AC-03-7 | Il form non viene inviato se il Titolo e' vuoto, con messaggio "Il titolo e' obbligatorio" | Validazione |
| AC-03-8 | In caso di errore backend: dati del form preservati e messaggio specifico | Error path |
| AC-03-9 | Slug duplicato: messaggio di errore specifico con campo slug evidenziato | Error path |

---

## US-04: Lista post con stato e filtri

| # | Criterio | Scenario |
|---|---|---|
| AC-04-1 | Pagina /admin/posts ha `export const prerender = false` | Tecnico |
| AC-04-2 | I post sono mostrati in ordine cronologico inverso (piu' recente prima) | Ordinamento |
| AC-04-3 | Ogni riga mostra: titolo (max 60 char, troncato), badge stato, data (DD/MM/YYYY), [M][A] | Visualizzazione |
| AC-04-4 | Tab Tutti/Pubblicati/Bozze/Archiviati filtrano la lista | Filtri |
| AC-04-5 | Lista vuota: messaggio con link a /admin/posts/new | Stato vuoto |
| AC-04-6 | Il bottone "+ Nuovo post" e' sempre visibile in header | Navigazione |
| AC-04-7 | [M] porta a /admin/posts/{id}/edit | Navigazione |
| AC-04-8 | [A] avvia il flusso di archiviazione (modale + conferma) | Archiviazione |

---

## US-05: Salva post come bozza

| # | Criterio | Scenario |
|---|---|---|
| AC-05-1 | Il pulsante "Salva bozza" salva il post con `status: draft` | Happy path |
| AC-05-2 | Le bozze non triggerano il rebuild Vercel | Bozza |
| AC-05-3 | Dopo il salvataggio come bozza: redirect a /admin/posts | Post-save |
| AC-05-4 | Le bozze non sono accessibili su /blog/{slug} | Visibilita' |
| AC-05-5 | Il titolo e' obbligatorio anche per le bozze | Validazione |
| AC-05-6 | I dati del form sono preservati in caso di errore backend | Error path |

---

## US-06: Upload immagine di copertina

| # | Criterio | Scenario |
|---|---|---|
| AC-06-1 | Il campo upload accetta JPG, PNG, WebP con dimensione massima 5MB | Validazione |
| AC-06-2 | La validazione del tipo e della dimensione avviene client-side prima dell'upload | Validazione |
| AC-06-3 | Dopo upload riuscito: preview dell'immagine nel form | Feedback |
| AC-06-4 | L'URL ImageKit viene salvato nel post al submit del form | Persistenza |
| AC-06-5 | In caso di upload fallito: messaggio specifico, il post e' salvabile senza immagine | Error path |
| AC-06-6 | Formato non supportato: messaggio "Formato non supportato. Usa JPG, PNG o WebP." | Error path |
| AC-06-7 | File troppo grande: messaggio "File troppo grande. Dimensione massima: 5MB." | Error path |

---

## US-07: Toolbar EditControls sulla pagina pubblica

| # | Criterio | Scenario |
|---|---|---|
| AC-07-1 | La Server Island EditControls usa `server:defer` nella pagina blog | Tecnico |
| AC-07-2 | Per lettori non autenticati: restituisce fragment vuoto, zero HTML visibile | Lettori |
| AC-07-3 | Per l'autore autenticato: mostra badge stato e bottone [Modifica] | Autore |
| AC-07-4 | Il badge stato corrisponde allo stato attuale del post nel backend | Consistenza |
| AC-07-5 | Il bottone [Modifica] porta a /admin/posts/{id}/edit pre-compilato | Navigazione |
| AC-07-6 | Il fallback della Server Island e' un fragment vuoto (nessun placeholder visibile) | Lettori |
| AC-07-7 | Il `post.id` viene passato come prop criptata (non esposto in chiaro nel DOM) | Sicurezza |
| AC-07-8 | La pagina blog resta SSG — nessuna modifica al rendering per i lettori | Performance |

---

## US-08: Pubblicazione post con rebuild Vercel

| # | Criterio | Scenario |
|---|---|---|
| AC-08-1 | Cliccare "Pubblica" salva il post con `status: published` nel backend | Happy path |
| AC-08-2 | Dopo il salvataggio: chiama il Vercel rebuild hook (VERCEL_DEPLOY_HOOK_URL in env) | Rebuild |
| AC-08-3 | Durante il rebuild: spinner "Pubblicazione in corso..." | Feedback |
| AC-08-4 | Dopo rebuild completato: redirect a /blog/{slug} | Post-publish |
| AC-08-5 | In caso di timeout (> 60s): messaggio con link manuale a /blog/{slug} | Timeout |
| AC-08-6 | In caso di errore rebuild: il post e' comunque salvato nel backend | Error path |
| AC-08-7 | Le bozze aggiornate non triggerano il rebuild | Bozza |

---

## US-09: Gestione tag

| # | Criterio | Scenario |
|---|---|---|
| AC-09-1 | Il campo tag mostra i tag esistenti in autocomplete durante la digitazione | Autocomplete |
| AC-09-2 | E' possibile selezionare piu' tag contemporaneamente | Multi-select |
| AC-09-3 | Se un tag non esiste: mostra opzione "+ Crea tag '{nome}'" | Nuovo tag |
| AC-09-4 | La creazione di un nuovo tag chiama il backend per la persistenza | Nuovo tag |
| AC-09-5 | I tag selezionati appaiono come badge rimovibili | UX |
| AC-09-6 | La rimozione di un badge e' client-side (non chiama il backend) | UX |
| AC-09-7 | I tag vengono associati al post al salvataggio definitivo | Persistenza |

---

## US-10: Archiviazione post

| # | Criterio | Scenario |
|---|---|---|
| AC-10-1 | Il bottone [A] (Archivia) e' presente su ogni riga della lista post | UI |
| AC-10-2 | Cliccando [A] appare una modale di conferma prima dell'archiviazione | Conferma |
| AC-10-3 | Post archiviato: `status: archived`, non accessibile ai lettori | Archiviazione |
| AC-10-4 | Post archiviato scompare dalla tab "Tutti" e "Pubblicati" | Visualizzazione |
| AC-10-5 | Tab "Archiviati" mostra i post archiviati con data di archiviazione | Tab |
| AC-10-6 | Post archiviati che erano pubblicati: il rebuild rimuove la pagina statica | Rebuild |
| AC-10-7 | Post archiviati che erano bozze: nessun rebuild necessario | Bozza |

---

## US-11: Feedback rebuild e gestione errori

| # | Criterio | Scenario |
|---|---|---|
| AC-11-1 | Durante il rebuild: messaggio "Pubblicazione in corso..." con barra animata | Feedback |
| AC-11-2 | Al completamento: redirect automatico a /blog/{slug} | Completamento |
| AC-11-3 | Dopo 60s senza completamento: messaggio con link manuale a /blog/{slug} | Timeout |
| AC-11-4 | Errore backend al salvataggio: messaggio specifico, form preservato, nessun spinner | Error path |
| AC-11-5 | Errore rebuild hook: messaggio informativo (il post e' salvato, rebuild ritarda) | Error path |
| AC-11-6 | Il link "Vedi il post" a /blog/{slug} e' sempre disponibile come fallback dopo timeout | Fallback |

---

## US-12: Ripristino post archiviato

| # | Criterio | Scenario |
|---|---|---|
| AC-12-1 | Il bottone [Ripristina] e' visibile nella tab "Archiviati" per ogni post | UI |
| AC-12-2 | Il ripristino riporta il post allo stato precedente (Published o Draft) | Ripristino |
| AC-12-3 | Post ripristinato come Published: triggera il rebuild Vercel | Rebuild |
| AC-12-4 | Post ripristinato come Draft: nessun rebuild | Draft |
| AC-12-5 | Toast di conferma "Post ripristinato." dopo il ripristino | Feedback |
| AC-12-6 | Il post scompare dalla tab "Archiviati" e appare nella tab corretta | Visualizzazione |
