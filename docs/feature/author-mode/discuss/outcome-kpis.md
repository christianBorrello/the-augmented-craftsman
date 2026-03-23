# Outcome KPIs: Author Mode

**Feature**: author-mode
**Data**: 2026-03-14

---

## Feature: Author Mode

### Objective

Entro la fine del Q2 2026, Christian puo' pubblicare e gestire i post del blog "The Augmented Craftsman" interamente dal browser, senza mai toccare database, API o terminale per operazioni di contenuto.

---

### Outcome KPIs

| # | Who | Does What | By How Much | Baseline | Measured By | Type |
|---|---|---|---|---|---|---|
| KPI-1 | Christian (autore) | Completa il login admin e accede a /admin/posts | 100% dei tentativi di login con email autorizzata hanno successo | 0% (funzionalita' non esiste) | Log di sessione Upstash Redis | Leading |
| KPI-2 | Christian (autore) | Pubblica un post dal form /admin/posts/new senza accedere a database o API direttamente | 1 post pubblicato entro la prima settimana di utilizzo | 0 post (canale assente) | Numero di post con status=published creati via admin UI | Leading |
| KPI-3 | Christian (autore) | Modifica un post esistente dall'interno della pagina pubblica (via EditControls toolbar) | Almeno 1 utilizzo del flusso in-place al mese | 0 (funzionalita' non esiste) | Log Action admin.updatePost con referer=/blog/* | Leading |
| KPI-4 | Lettori | Esperienza di lettura non degradata dalla presence di author-mode | Nessun aumento misurabile di LCP o FID su pagine /blog/* | LCP attuale < 1.5s (da Vercel Analytics) | Vercel Web Vitals (LCP, FID, CLS) su pagine /blog/* | Guardrail |

---

### Metric Hierarchy

- **North Star**: KPI-2 — Christian pubblica autonomamente dal browser (comportamento abilitatore di tutto il blog)
- **Leading Indicators**:
  - KPI-1 — Il login funziona (prerequisito di tutto)
  - KPI-3 — Il flusso in-place e' usato (editing fluido senza context switch)
- **Guardrail Metrics**:
  - KPI-4 — Le performance del blog pubblico non degradano (zero impatto sui lettori)
  - Tasso di errore Action admin < 1% (zero errori 500 in operazioni di scrittura)

---

### Measurement Plan

| KPI | Data Source | Collection Method | Frequency | Owner |
|---|---|---|---|---|
| KPI-1 | Upstash Redis / Astro Sessions log | Conteggio sessioni admin create con successo vs tentativi | Ogni login | Christian (revisione manuale iniziale) |
| KPI-2 | Backend .NET database | Query COUNT su posts WHERE status='published' AND created_via='admin_ui' | Settimanale | Christian |
| KPI-3 | Vercel Function logs | Log delle chiamate Action admin.updatePost con header Referer=/blog/* | Mensile | Christian |
| KPI-4 | Vercel Web Analytics | Dashboard Vercel — LCP, FID, CLS su /blog/* | Continuo (alerting su degradazione > 10%) | Vercel dashboard |

---

### Hypothesis

**KPI-2**: Crediamo che fornire un'interfaccia di editing web-based integrata nel frontend Astro per Christian Borrello consentira' di pubblicare post sul blog con frequenza regolare.
Sapremo che e' vero quando Christian pubblichera' almeno 1 post a settimana per 4 settimane consecutive tramite l'admin UI.

**KPI-4**: Crediamo che l'utilizzo di Server Islands (fragment vuoto per i lettori) e pagine SSG invariate non degradera' le performance del blog pubblico.
Sapremo che e' vero quando il LCP su /blog/* rimarra' sotto 1.5s dopo il deploy della feature.

---

### Note sul Contesto

Questo e' un blog personale con un singolo autore. I KPI non hanno obiettivi statisticamente significativi — il significato e' qualitativo:
- KPI-1 e KPI-2 misurano se la funzionalita' e' usabile al primo utilizzo reale
- KPI-3 misura se il flusso in-place e' abbastanza comodo da essere usato nella pratica
- KPI-4 e' l'unico KPI con un vero target quantitativo (tutela i lettori)
