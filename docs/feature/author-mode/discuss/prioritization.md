# Prioritization: Author Mode

**Data**: 2026-03-14

---

## Release Priority

| Priority | Release | Target Outcome | Stories | Rationale |
|---|---|---|---|---|
| 1 | Walking Skeleton | End-to-end: login → crea bozza → vedi in lista | S1, S2, S3, S4, S6 | Valida l'assunzione core: auth + scrittura + persistenza funzionano insieme |
| 2 | Release 1 — "Pubblica" | Christian pubblica il primo post senza toccare DB/API | S5, S7, S8, S9 | Outcome principale della feature — il blog diventa scrivibile |
| 3 | Release 2 — "Gestisci" | Christian mantiene il blog ordinato nel tempo | S10, S11, S12 | Valore incrementale — il blog gia' funziona, questo migliora la manutenzione |

---

## Backlog Suggerito

| Story ID | Titolo | Release | Priorita' | Value | Urgency | Effort | Score | Outcome Link | Dipendenze |
|---|---|---|---|---|---|---|---|---|---|
| US-01 | Login admin OAuth | WS | P1 | 5 | 5 | 2 | 12.5 | KPI-1 | Nessuna |
| US-02 | Middleware guard /admin/* | WS | P1 | 5 | 5 | 1 | 25 | KPI-1 | US-01 |
| US-03 | Form crea post (titolo + contenuto + bozza) | WS | P1 | 5 | 5 | 3 | 8.3 | KPI-2 | US-01, US-02 |
| US-04 | Lista post con stato e filtri | WS | P1 | 4 | 4 | 2 | 8 | KPI-2 | US-01, US-02 |
| US-05 | Salva post come bozza | WS | P1 | 5 | 5 | 1 | 25 | KPI-2 | US-03 |
| US-06 | Upload immagine copertina | R1 | P2 | 3 | 3 | 3 | 3 | KPI-2 | US-03 |
| US-07 | Toolbar EditControls in-place | R1 | P2 | 4 | 4 | 2 | 8 | KPI-3 | US-01, US-04 |
| US-08 | Pubblica post con rebuild Vercel | R1 | P2 | 5 | 5 | 3 | 8.3 | KPI-2 | US-05 |
| US-09 | Gestione tag (selezione + creazione) | R1 | P2 | 3 | 3 | 2 | 4.5 | KPI-2 | US-03 |
| US-10 | Archiviazione post (soft delete) | R2 | P3 | 3 | 2 | 1 | 6 | KPI-3 | US-04 |
| US-11 | Feedback rebuild e gestione errori | R2 | P3 | 4 | 3 | 2 | 6 | KPI-3 | US-08 |
| US-12 | Ripristino post archiviato | R2 | P3 | 2 | 2 | 1 | 4 | KPI-3 | US-10 |

> **Nota**: Score = (Value x Urgency) / Effort. Tie-breaking: Walking Skeleton > Assunzione piu' rischiosa > Valore piu' alto.
> Story ID provvisori — ID definitivi assegnati in Phase 4 (Requirements). Revisita questa tabella dopo la produzione di user-stories.md.

---

## Assunzione piu' Rischiosa

**L'assunzione che potrebbe uccidere la feature**: Astro Sessions + Upstash Redis su Vercel funziona correttamente con il flow OAuth del backend .NET.

Questa combinazione (Astro 5.7 Sessions + Upstash driver + OAuth callback dal backend .NET → sessione Astro) non e' documentata in esempi pubblici. Se fallisce, l'intero author mode non e' costruibile senza cambiare approccio architetturale.

**Mitigazione**: US-01 (Login) deve essere il primo task implementato — non perche' sia il piu' semplice, ma perche' valida questa assunzione prima di costruire il resto.

**Fallback se l'assunzione fallisce**: cookie HTTP-only firmato invece di Astro Sessions (autenticazione stateless senza Redis) — cambia l'implementazione ma non i requisiti utente.

---

## MoSCoW

| Categoria | Stories |
|---|---|
| **Must Have** | US-01, US-02, US-03, US-04, US-05, US-08 |
| **Should Have** | US-06, US-07, US-09 |
| **Could Have** | US-10, US-11, US-12 |
| **Won't Have (MVP)** | Auto-save, editor inline nativo, scheduling, versioning |
