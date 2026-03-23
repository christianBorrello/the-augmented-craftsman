# Acceptance Test Review: Author Mode

**Feature**: author-mode
**Wave**: DISTILL
**Reviewer**: acceptance-designer (self-review, critique-dimensions skill)
**Date**: 2026-03-14

---

```yaml
review_id: "accept_rev_author_mode_2026_03_14"
reviewer: "acceptance-designer (peer review)"

strengths:
  - "Walking skeleton scenario 1 passes the litmus test: title describes user goal,
     Given/When describe user actions, Then steps describe observable outcomes
     (post in list with status badge, not accessible on public blog). Demo-able to stakeholder."
  - "Error path ratio is 43% across the full suite, exceeding the 40% target.
     Auth feature alone is 62% error paths — correctly reflects that auth failure modes
     are the highest-risk scenarios."
  - "Business rules BR-04 (slug immutability) and BR-06 (restore to prior status) each
     have dedicated focused scenarios, not just covered implicitly by happy paths."
  - "Single-use token replay attack scenario is explicit — this is a security-critical
     behaviour that would otherwise be assumed rather than verified."
  - "No Gherkin scenario contains technical terms. Verified by grep across all 5 feature files."
  - "All 12 user stories are covered. AC items that were implementation-coupled in
     DISCUSS have been rewritten as observable behaviours."

issues_identified:
  happy_path_bias:
    - issue: "US-04 (post list) has 5 scenarios, only 1 error/edge. The empty-list scenario
               is an edge case but the filter scenarios are all happy paths."
      severity: "medium"
      recommendation: "Acceptable for this story — list filtering is low-risk. The real
                       error paths (backend down, auth failure) are covered by US-02 and US-05."
    - issue: "US-11 (rebuild feedback) has 4 scenarios, only 1 error (rebuild timeout).
               The rebuild hook failure scenario is in the feature file but not yet expanded."
      severity: "medium"
      recommendation: "Rebuild hook failure is covered at the unit test level by the software-crafter.
                       The acceptance test covers the observable user outcome (timeout + manual link).
                       Acceptable for MVP."

  gwt_format:
    - issue: "None found. All scenarios follow Given-When-Then with single When actions."
      severity: "none"

  business_language:
    - issue: "None found. No HTTP verbs, status codes, JSON references, or infrastructure
               terms (Redis, JWT, etc.) appear in any Gherkin scenario."
      severity: "none"

  coverage_gaps:
    - issue: "AC-07-8 (blog page remains SSG, not affected by Server Island for readers) is
               an architectural constraint, not a user-observable behaviour at the API boundary.
               This cannot be tested via the .NET API driving port."
      severity: "low"
      recommendation: "This constraint is enforced by DD-04 (output: hybrid) and verified by the
                       CI prerender guard. Not an acceptance test responsibility."
    - issue: "AC-11-3 (60-second timeout with manual link) involves timing that is not
               feasible in an acceptance test without a test clock."
      severity: "low"
      recommendation: "The timeout behaviour is tested at the unit level in the RebuildService.
                       The acceptance test covers the observable outcome: 'rebuild triggered' or
                       'rebuild not triggered'. The 60-second detail is implementation."

  walking_skeleton_centricity:
    - issue: "None found. All three @walking_skeleton scenarios are framed as user goals,
               not technical layer connectivity."
      severity: "none"

  priority_validation:
    - issue: "None found. Walking skeleton addresses the highest-risk path (OAuth flow is
               identified as the highest-risk item in the DISCUSS risk register). The skeleton
               starts there."
      severity: "none"

approval_status: "approved"

notes:
  - "AC items from DISCUSS that were implementation-coupled have been rewritten as per the
     DESIGN reviewer note. Examples: 'Astro crea sessione' → 'admin session is active';
     'context.locals.user viene popolato' → 'post list is returned normally';
     'export const prerender = false' → enforced by CI guard, not acceptance test."
  - "OQ-03 (EditControls [Archive] button) resolved as MVP: [Modifica] only, as recommended
     by DESIGN/PLATFORM wave decisions."
  - "The acceptance tests exercise the .NET API driving port only. Frontend-layer behaviours
     (Astro session creation, Astro middleware redirect) are beyond the boundary of the
     backend acceptance test project. These are validated by the three-layer auth design."
```
