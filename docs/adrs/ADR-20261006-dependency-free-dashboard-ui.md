---
id: ADR-20261006-dependency-free-dashboard-ui
status: accepted
date: 2026-10-06
superseded-by:
scope: Stella.FeatureManagement.Dashboard.UI
summary: keep the embedded UI on react/react-dom only; style it with token-based CSS (light + dark) and in-house primitives
---
## Context
- The dashboard ships embedded inside the NuGet package, so every runtime dependency grows the package and every consumer's attack surface.
- The UI had grown to one 1,279-line component plus a 1,680-line global stylesheet: six button styles, hard-coded colors, no dark mode, and inaccessible modals.
- Rejected: Radix + Lucide + Sonner. They would give accessibility for free, but they add runtime dependencies to the package.
- Rejected: Tailwind + shadcn/ui. That means the biggest rewrite and the most dependencies, for a small single-screen app.

## Decision
- Keep `react` and `react-dom` as the only runtime dependencies.
- Define all colors, spacing, type and radii as CSS custom properties in `src/styles/tokens.css`.
  - Dark values apply under `prefers-color-scheme` unless `data-theme="light"` is set, and always under `data-theme="dark"`.
  - `useTheme` persists the choice, and an inline script in `index.html` applies it before first paint.
- Build UI pieces from the in-house primitives in `src/components`: `Button`/`IconButton`, `Switch` (`role="switch"`), `Dialog`/`ConfirmDialog` (focus trap, Escape, focus restore), `Toast` and `Icon`.
- Report mutation failures inline (in a dialog or editor) or as an error toast. Never block the list behind an error modal.

Binds: new UI must use the tokens (no raw colors) and the shared primitives. Adding a runtime npm dependency needs a new ADR.
