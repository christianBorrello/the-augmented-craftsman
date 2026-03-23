# Story Map: Author Mode

**User**: Christian Borrello — unico autore del blog "The Augmented Craftsman"
**Goal**: Pubblicare e gestire i post del blog senza interagire con API o database direttamente
**Data**: 2026-03-14

---

## Backbone (Attivita' in sequenza cronologica)

| Accedi come autore | Crea post | Gestisci post esistenti | Pubblica / salva |
|---|---|---|---|
| Naviga a /admin/login | Apri form nuovo post | Visualizza lista post | Salva come bozza |
| Clicca OAuth (Google/GitHub) | Scrivi titolo e slug | Filtra per stato | Pubblica con rebuild |
| Verifica email == ADMIN_EMAIL | Scrivi contenuto con editor | Naviga all'editor da pagina pubblica | Archivia post |
| Sessione admin creata | Seleziona / crea tag | Modifica post esistente | Attendi feedback rebuild |
| Middleware protegge /admin/* | Carica immagine copertina | Ripristina post archiviato | Vedi post live |
| | Scegli stato (bozza / pubblicato) | | |

---

## Walking Skeleton

Il minimo end-to-end che dimostra che l'author mode funziona:

```
[Login OAuth] → [Crea post con titolo + contenuto] → [Salva bozza] → [Vedi in lista]
```

Questo slice copre tutte le attivita' principali con il minimo indispensabile:
- Auth funziona (sessione creata, middleware attivo)
- Scrittura funziona (form, editor, backend)
- Persistenza funziona (post salvato, visibile in lista)
- Non include publish/rebuild (troppo complesso per lo skeleton)

**Story dello skeleton**:
- S1: Login admin tramite OAuth
- S2: Middleware guard /admin/*
- S3: Form creazione post (titolo + contenuto + bozza)
- S4: Lista post con stato

---

## Mappa Completa

```
Accedi        Crea post     Gestisci       Pubblica/Salva
----------    ----------    ----------     ----------
S1: Login     S3: Form      S4: Lista      S6: Salva bozza
OAuth         nuovo post    post
..........    ..........    ..........     ........... <- walking skeleton line
S2: Middle-   S5: Upload    S7: Edit       S8: Pubblica
ware guard    immagine +    in-place       + rebuild
              tag           (toolbar)
              S9: Crea      S10: Arch-     S11: Feed-
              nuovo tag     ivia post      back rebuild
                            S12: Ripri-
                            stina post
```

### Walking Skeleton
- **S1**: Login admin tramite OAuth Google/GitHub + whitelist email
- **S2**: Middleware guard su /admin/* con redirect a login se non autenticato
- **S3**: Form creazione post (titolo, contenuto Tiptap, stato draft/published)
- **S4**: Lista post con stato, azioni base
- **S6**: Salva post come bozza → redirect lista

### Release 1 — "L'autore puo' pubblicare"
Target outcome: Christian pubblica il primo post senza toccare database o API

- **S8**: Pubblicazione post con rebuild Vercel + spinner + redirect a /blog/{slug}
- **S5**: Upload immagine copertina via ImageKit
- **S9**: Gestione tag (selezione esistenti + creazione nuovi)
- **S7**: Toolbar EditControls sulla pagina pubblica + link a /admin/posts/{id}/edit

### Release 2 — "L'autore gestisce il blog nel tempo"
Target outcome: Christian mantiene il blog ordinato senza frizione

- **S10**: Archiviazione post (soft delete con conferma)
- **S11**: Feedback rebuild dettagliato (gestione errori, timeout, retry)
- **S12**: Ripristino post archiviato

---

## Scope Assessment

**Scope Assessment: PASS — 12 stories, 3 bounded contexts (Auth/Content/Media), stimato 8-10 giorni totali**

Contesti coinvolti:
- **Auth**: Login OAuth, sessioni, middleware (S1, S2)
- **Content**: CRUD post, lista, edit, stato (S3, S4, S6, S7, S8, S10, S11, S12)
- **Media**: Upload immagine copertina via ImageKit (S5)
- **Tags**: Creazione e selezione tag (S9) — lightweight, parte del bounded context Content

Il Walking Skeleton (S1-S4, S6) e' consegnabile in 3-4 giorni e dimostra end-to-end.
Release 1 aggiunge il valore core (pubblicazione) in altri 3-4 giorni.
Release 2 completa il loop di gestione in 2 giorni.

Non viene proposto lo split in deliverable separati perche':
- Le 12 stories hanno dipendenze verticali forti (non puo' pubblicare senza auth)
- Il Walking Skeleton da' valore dimostrabile in meno di una settimana
- Il progetto ha un solo sviluppatore (Christian stesso)
