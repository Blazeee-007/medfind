# MedFind — Design System (deliberately NOT AI-generic)

## The problem: what "AI-generated" looks like

AI tools default to the same template every time. Avoid ALL of these:

- ❌ Indigo/violet (`#6366f1`) or blue-ish Tailwind `bg-indigo-500 / bg-blue-600` main buttons
- ❌ `from-purple-500 to-pink-500` gradient hero, or any "purple → pink" gradient
- ❌ Rounded-3xl everything, huge pill buttons, heavy drop shadows floating in space
- ❌ Centered hero: big tagline + subtitle + one big CTA + 3 feature cards with 24px line icons
- ❌ Default Inter font everywhere at one size
- ❌ `bg-gray-50` page background + white cards + shadow-md
- ❌ Generic icon-set feature grid ("Fast, Secure, Reliable")
- ❌ Emoji in UI copy 🚀✨ as decoration
- ❌ Lottie/hero illustrations of people pointing at charts

If your screen could be any startup's landing page, restart the design.

---

## The concept: **"pharmacy ledger"** — trustworthy, clinical, warm, a little analog

MedFind is about *trust and freshness* (verified stock). The design should feel like a **modern medical ledger / apothecary label**: precise structure, a warm off-white paper, an ink-dark text, one confident accent green (the cross/pharmacy color done tastefully), monospace for data and timestamps.

Keywords: **editorial, grid-driven, utilitarian, calm, text-first, tabular data as hero.**

---

## Palette (use these exact values)

```css
:root{
  /* ground */
  --paper:      #F5F1E8;   /* warm off-white, not gray, not pure white */
  --paper-2:    #EDE7D9;   /* slightly deeper for panels */
  --ink:        #1C2B25;   /* near-black green ink, NOT #000 */
  --ink-soft:   #4A5A52;

  /* accent — the ONE color. A deep pharmacy green, not mint, not teal-cyan */
  --green:      #0F6B4F;
  --green-deep: #0A4A37;
  --green-tint: #DCEBE4;

  /* signal colors used ONLY for status, never decoration */
  --verified:   #1F8A5B;   /* fresh stock */
  --stale:      #B7791F;   /* needs update */
  --out:        #A23B2E;   /* empty */

  --line:       #D8D1C2;   /* hairline borders */
}
```

**Rules:**
- Accent green is the ONLY saturated color. Everything else is paper + ink + hairlines.
- No gradients except one subtle `--paper` → `--paper-2` on large panels. No colored gradients ever.
- Verified/stale/out colors appear only as small dots, thin bars, or text — never as big blocks.

---

## Typography (this is 60% of "looks designed")

- **Display / headings:** a characterful serif → `"Fraunces"` (Google Fonts) or `"Newsreader"`. Gives editorial weight, instantly not-AI.
- **Body / UI:** `"IBM Plex Sans"` — clinical, humanist, not the overused Inter.
- **Data / timestamps / quantities / codes:** `"IBM Plex Mono"` — makes numbers feel like a real ledger. Use for `verified 12 min ago`, distances, stock counts, IDs.

Scale (use a real type scale, not all 16px):
```
display:  clamp(2.5rem, 5vw, 4rem)  Fraunces 600
h1:       2rem   Fraunces 600
h2:       1.375rem Fraunces 600
body:     1rem   IBM Plex Sans 400   line-height 1.6
small:    0.8125rem IBM Plex Sans 500
mono:     0.8125rem IBM Plex Mono 400  letter-spacing .02em
```
Track headings slightly tight (`-0.01em`). Mono gets slightly loose (`.02em`).

---

## Layout — grid, not centered-hero

- **Search page:** a **two-column app layout**, not a marketing page.
  - Left rail (240px): brand mark, search inputs, filters (radius slider, "verified within" select), saved — like a tool sidebar.
  - Main: a **result list rendered as rows in a table/ledger**, NOT cards floating in a grid.
    Each row: pharmacy name · address · distance (mono) · a compact stock strip · `✓ verified 12m ago` (mono).
  - Hairline dividers between rows. No card shadows.
- **Detail page:** two-column — availability ledger (left) + a static map panel (right).
- Max content width ~1120px, but the app fills the viewport (it's a tool, not a landing page).

### The "ledger row" component (signature element)
```
┌────────────────────────────────────────────────────────────┐
│ ● Green Cross Pharmacy          Paracetamol 500mg   ◼◼◼◼◻ 6 │
│   12 MG Road · 1.2 km           verified 12 min ago   [→]  │
└────────────────────────────────────────────────────────────┘
```
- `●` status dot (verified green / stale amber / out red)
- stock shown as small squares (◼) + mono count — a distinctive, memorable element
- distance + verified time in mono
- 1px bottom hairline; hover = `--paper-2` background, no lift/shadow

---

## Component rules

- **Buttons:** rectangular with 2px radius (NOT pills). Primary = solid `--green` on `--paper` text; secondary = 1px `--line` border, ink text. No shadows. Hover = `--green-deep`.
- **Inputs:** underline or 1px boxed, `--paper-2` fill, focus ring = 2px `--green-tint`. Labels above in small caps mono.
- **Badges:** tiny, uppercase mono, 1px border. `VERIFIED` / `STALE` / `OUT`.
- **Borders:** always 1px `--line` hairlines — borders are the main structural device, not shadows.
- **Empty states:** a short mono message + one line of copy, left-aligned. No sad-face illustrations.
- **Iconography:** thin 1.5px line icons only where they aid scanning (search, map pin). No icon feature-grids.

---

## Motion (subtle, purposeful)

- 120–160ms ease on hover/focus only.
- Result rows stagger-fade in on first load (40ms each) — feels alive without being flashy.
- NO bounce, NO parallax, NO confetti.

---

## Copy voice

- Plain, clinical, honest. "Verified 12 minutes ago." Not "Instantly find medicines near you! 🚀"
- No exclamation marks in product UI. No marketing superlatives.
- Sentence case for labels. Uppercase only for tiny mono badges.

---

## Anti-AI checklist before you ship a screen

- [ ] No indigo/blue/violet/purple anywhere
- [ ] No colored gradient
- [ ] Not a centered hero + 3 cards
- [ ] Custom serif display + IBM Plex Sans + IBM Plex Mono (not Inter/Roboto)
- [ ] Warm paper background (not gray-50, not pure white)
- [ ] Hairline borders as primary structure, shadows almost absent
- [ ] Data (numbers/times) in mono, and they're prominent
- [ ] One accent color only
- [ ] Copy is calm and specific, no emoji
- [ ] If you squint, it looks like a **precise medical tool**, not a SaaS landing page

> Fonts: load Fraunces + IBM Plex Sans + IBM Plex Mono via Google Fonts in `index.html` with `display=swap`.
> Fallbacks: `Fraunces, Georgia, serif` · `'IBM Plex Sans', system-ui, sans-serif` · `'IBM Plex Mono', ui-monospace, monospace`.
