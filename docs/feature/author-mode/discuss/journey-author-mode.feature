# Feature: Author Mode
# Gherkin scenarios per ogni step del journey author-mode
# Persona: Christian Borrello — unico autore del blog

Feature: Author Mode — Gestione contenuti del blog

  Background:
    Given il blog "The Augmented Craftsman" è attivo
    And il backend .NET è raggiungibile su Koyeb
    And ADMIN_EMAIL è configurato come "christian.borrello@gmail.com"

  # ─── JOURNEY A: LOGIN ──────────────────────────────────────────────────────

  Rule: Solo l'autore con email configurata in ADMIN_EMAIL può autenticarsi come admin

    Scenario: Autore accede con successo tramite Google OAuth
      Given Christian visita /admin/login senza una sessione attiva
      When Christian clicca "Accedi con Google"
      And il flow OAuth completa con l'email "christian.borrello@gmail.com"
      Then Christian viene reindirizzato a /admin/posts
      And la sessione admin è attiva con ruolo "author"
      And il cookie di sessione è HTTP-only e sicuro

    Scenario: Autore accede con successo tramite GitHub OAuth
      Given Christian visita /admin/login senza una sessione attiva
      When Christian clicca "Accedi con GitHub"
      And il flow OAuth completa con l'email "christian.borrello@gmail.com"
      Then Christian viene reindirizzato a /admin/posts
      And la sessione admin è attiva con ruolo "author"

    Scenario: Email OAuth non autorizzata — accesso negato
      Given Christian visita /admin/login
      When il flow OAuth completa con l'email "altro@gmail.com"
      Then Christian rimane su /admin/login
      And vede il messaggio "Account non autorizzato. Solo l'autore del blog può accedere."
      And nessuna sessione admin viene creata

    Scenario: Sessione scaduta durante navigazione admin
      Given Christian è autenticato con una sessione admin
      And la sessione scade o viene rimossa da Redis
      When Christian naviga su /admin/posts
      Then il middleware reindirizza a /admin/login
      And vede il banner "Sessione scaduta. Accedi di nuovo."

    Scenario: Accesso diretto a pagina admin senza sessione
      Given un visitatore non autenticato
      When visita /admin/posts
      Then viene reindirizzato a /admin/login
      And non vede nessun contenuto admin

  # ─── JOURNEY B: CREA NUOVO POST ────────────────────────────────────────────

  Rule: L'autore può creare un post come bozza o pubblicarlo direttamente

    Scenario: Autore apre il form per un nuovo post
      Given Christian è autenticato come admin
      When Christian naviga su /admin/posts/new
      Then vede il form con i campi: Titolo, Slug, Contenuto, Tag, Immagine copertina, Stato
      And il campo Slug è vuoto
      And l'editor Tiptap è disponibile per il contenuto
      And i tag esistenti sono disponibili per la selezione

    Scenario: Slug viene generato automaticamente dal titolo
      Given Christian è su /admin/posts/new
      When Christian inserisce il titolo "TDD con .NET 10: la doppia copertura"
      Then il campo Slug viene pre-compilato con "tdd-con-dotnet-10-la-doppia-copertura"
      And lo slug rimane editabile manualmente

    Scenario: Autore salva un post come bozza
      Given Christian ha compilato il form con:
        | Campo    | Valore                              |
        | Titolo   | TDD con .NET 10: la doppia copertura |
        | Contenuto| Il test outside-in parte sempre...  |
        | Stato    | Bozza                               |
      When Christian clicca "Salva bozza"
      Then il post viene salvato sul backend con status "draft"
      And Christian viene reindirizzato a /admin/posts
      And il post appare in lista con badge "Bozza"
      And il post NON è accessibile su /blog/tdd-con-dotnet-10

    Scenario: Autore pubblica un nuovo post con rebuild
      Given Christian ha compilato il form con titolo "TDD con .NET 10: la doppia copertura"
      And ha selezionato lo stato "Pubblica ora"
      When Christian clicca "Pubblica"
      Then il post viene salvato sul backend con status "published"
      And viene mostrato lo spinner "Pubblicazione in corso..."
      And al completamento del rebuild Christian viene reindirizzato a /blog/tdd-con-dotnet-10
      And il post è accessibile ai lettori su /blog/tdd-con-dotnet-10

    Scenario: Form con titolo mancante — validazione
      Given Christian è su /admin/posts/new
      And ha lasciato il campo Titolo vuoto
      When Christian clicca "Salva bozza"
      Then vede il messaggio "Il titolo è obbligatorio"
      And il form non viene inviato
      And i dati già inseriti nel contenuto sono preservati

    Scenario: Salvataggio fallisce per backend non raggiungibile
      Given Christian ha compilato il form completo
      And il backend .NET non è raggiungibile
      When Christian clicca "Salva bozza"
      Then vede il banner "Impossibile salvare. Controlla la connessione e riprova."
      And rimane sulla pagina /admin/posts/new
      And i dati del form sono preservati

    Scenario: Autore aggiunge tag esistenti al post
      Given Christian è su /admin/posts/new
      And il sistema ha tag disponibili: "TDD", ".NET", "Clean Architecture"
      When Christian seleziona i tag "TDD" e ".NET"
      Then i tag selezionati appaiono come badge nel form
      And verranno associati al post al momento del salvataggio

    Scenario: Autore crea un nuovo tag non esistente
      Given Christian è su /admin/posts/new
      And vuole usare il tag "DDD"
      And il tag "DDD" non esiste ancora
      When Christian digita "DDD" nel campo tag e clicca "Aggiungi tag"
      Then il tag "DDD" viene creato sul backend
      And appare come badge selezionato nel form

    Scenario: Autore carica un'immagine di copertina
      Given Christian è su /admin/posts/new
      When Christian seleziona un file immagine locale "cover.jpg" (< 5MB)
      Then il file viene caricato su ImageKit tramite il backend
      And una preview dell'immagine appare nel form
      And l'URL ImageKit viene memorizzato per il post

    Scenario: Upload immagine copertina fallisce
      Given Christian è su /admin/posts/new
      When Christian tenta di caricare un file da 15MB
      Then vede il messaggio "File troppo grande. Dimensione massima: 5MB"
      And il form rimane modificabile senza immagine

  # ─── JOURNEY C: MODIFICA IN-PLACE ──────────────────────────────────────────

  Rule: L'autore autenticato vede una toolbar sulla pagina pubblica che permette la navigazione all'editor

    Scenario: Autore autenticato vede la toolbar EditControls
      Given Christian è autenticato come admin
      And Christian visita /blog/tdd-con-dotnet-10
      When la Server Island EditControls viene renderizzata
      Then la toolbar floating appare con il badge "[Pubblicato]" e il bottone "[Modifica]"
      And la toolbar non altera il layout della pagina per i lettori

    Scenario: Lettore non autenticato non vede la toolbar
      Given un lettore visita /blog/tdd-con-dotnet-10 senza sessione admin
      When la Server Island EditControls viene renderizzata
      Then viene restituito un fragment HTML vuoto
      And nessun elemento extra è visibile al lettore
      And la performance della pagina non è impattata

    Scenario: Toolbar mostra stato "Bozza" per post non pubblicato
      Given Christian è autenticato come admin
      And Christian visita la pagina di un post in bozza tramite link diretto
      When la Server Island EditControls viene renderizzata
      Then la toolbar mostra il badge "[Bozza]" con stile visivo distinto
      And il bottone "[Modifica]" è disponibile

    Scenario: Autore naviga all'editor dal post pubblico
      Given Christian vede la toolbar su /blog/tdd-con-dotnet-10
      When Christian clicca "Modifica"
      Then viene reindirizzato a /admin/posts/42/edit
      And il form è pre-compilato con il titolo, contenuto, tag e immagine attuali
      And lo slug è mostrato in sola lettura

    Scenario: Autore salva modifiche a post pubblicato
      Given Christian è su /admin/posts/42/edit con il post pre-compilato
      When Christian modifica il titolo in "TDD con .NET 10: outer e inner loop"
      And clicca "Salva modifiche"
      Then il post viene aggiornato sul backend
      And viene mostrato lo spinner di rebuild
      And al completamento Christian viene reindirizzato a /blog/tdd-con-dotnet-10
      And il post mostra il titolo aggiornato

    Scenario: Autore modifica post in bozza — nessun rebuild necessario
      Given Christian è su /admin/posts/43/edit per un post in bozza
      When Christian modifica il contenuto e clicca "Salva modifiche"
      Then il post viene aggiornato sul backend come bozza
      And Christian viene reindirizzato a /admin/posts
      And non viene mostrato nessun spinner di rebuild
      And il post rimane non accessibile ai lettori

  # ─── JOURNEY D: LISTA POST ─────────────────────────────────────────────────

  Rule: L'autore può visualizzare e gestire tutti i post del blog

    Scenario: Autore vede la lista completa dei post
      Given Christian è autenticato come admin
      When Christian naviga su /admin/posts
      Then vede tutti i post in ordine cronologico inverso
      And ogni riga mostra: titolo, stato (badge), data, azioni [M][A]
      And i tab permettono di filtrare per: Tutti, Pubblicati, Bozze, Archiviati

    Scenario: Autore filtra per post in bozza
      Given Christian è su /admin/posts con 5 pubblicati e 2 bozze
      When Christian clicca il tab "Bozze"
      Then vede solo i 2 post in bozza
      And i post pubblicati non appaiono nella lista filtrata

    Scenario: Autore archivia un post pubblicato
      Given Christian è su /admin/posts
      When Christian clicca [A] sul post "Clean Arch intro (Pubblicato)"
      And conferma nella modale "Archivia questo post?"
      Then il post viene archiviato sul backend (soft delete)
      And scompare dalla tab "Tutti" e "Pubblicati"
      And appare nella tab "Archiviati" con data di archiviazione
      And il post NON è più accessibile ai lettori su /blog/clean-arch-intro

    Scenario: Autore ripristina un post archiviato
      Given Christian è su /admin/posts tab "Archiviati"
      And "Clean Arch intro" è archiviato
      When Christian clicca [Ripristina] su "Clean Arch intro"
      Then il post torna nello stato precedente all'archiviazione (Pubblicato o Bozza)
      And riappare nella tab corrispondente

    Scenario: Archiviazione fallisce per errore backend
      Given Christian è su /admin/posts
      When Christian tenta di archiviare "Clean Arch intro"
      And il backend restituisce un errore
      Then vede il banner "Impossibile archiviare il post. Riprova."
      And il post rimane nella lista con lo stato invariato

  # ─── SICUREZZA ─────────────────────────────────────────────────────────────

  Rule: Le Actions admin sono protette da autenticazione a doppio livello

    Scenario: Action admin chiamata senza sessione valida viene bloccata
      Given un utente senza sessione admin
      When tenta di chiamare l'Action admin.createPost direttamente
      Then l'Action risponde con ActionError code "UNAUTHORIZED"
      And nessun dato viene scritto sul backend

    Scenario: CSRF protection attiva su tutte le Action
      Given security.checkOrigin è true in astro.config.mjs
      When una richiesta POST arriva con Origin diverso dal dominio del blog
      Then la richiesta viene bloccata prima di raggiungere il handler
      And viene restituito HTTP 403
