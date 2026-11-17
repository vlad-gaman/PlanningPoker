# Phase 5 spec: shell and credits (slice 5)

Hand-off spec for slice 5 of the Angular migration of PlanningPoker: the theme control, the credits page content, the footer and the rest of the page shell (navbar, favicons, titles). Source of truth for the *why* is the wayfinder map, [issue #3](https://github.com/vlad-gaman/PlanningPoker/issues/3); this file repeats the facts a builder needs. Repo rules in `CLAUDE.md` and the layering of `docs/phase-1-spec.md` apply.

Scope is **parity only**: no new features. Frontend only: no backend, hub, REST or Swagger change. Branch: `slice-5-shell-and-credits`, one pull request into `angular-migration`, reviewed by the owner, CI green before merge. The old `PlanningPokerUi` project is not touched.

## 0. Where this slice sits

[Issue #15](https://github.com/vlad-gaman/PlanningPoker/issues/15) orders the slices; this one is the last before the cutover. The boundary, from the slice 2 to 4 tables:

| In slice 5 (this spec) | Later |
|---|---|
| Theme control that matches the old one (select with the palette icon) | Cutover: parity checklist, merge to `main`, retire `PlanningPokerUi`: slice 6 |
| Credits page: the Icons attributions and the Back to Home button, next to the libraries from slice 3 | |
| Footer: copyright, Credits link, Buy Me a Coffee link | |
| Navbar: logo and name, container width | |
| Favicons, touch icons and the page title per route | |

Already in place (slice 0 and 3): `ThemeService` (auto / light / dark, persisted, `data-theme` on `<html>`, the pre-paint script in `index.html`), the colour variables in `styles.scss` (both themes), a cycle button in the header, a footer with a `/credits` link, the `features/credits` page with the library list (`credits.data.ts`), the `Room/:id` and `Credits` redirects, `favicon.ico` and the six `*-finger` PNGs in `public/`.

## 1. Old behaviour to match

From `_Layout.cshtml`, `Credits.cshtml`, `site.css` and `site.js`:

**Navbar**

- White (themed) bar with a bottom border and a small shadow, content in a centred container. Left: the 28 px logo (`favicon-32x32.png`) and the product name linking home. Right: the theme control.
- The theme control is a bordered box with a palette emoji and a `<select>` with Auto, Light, Dark. It shows the stored preference on load and applies the choice at once. Hover: card hover background and primary-colour border.
- The old collapse button (`navbar-toggler`) has nothing to collapse and does nothing useful; it is not carried over.

**Footer**

- Bar with a top border, always at the bottom of the page. Left: "© 2025 - PlanningPokerUi", a divider, a "Credits" link. Right: a round yellow Buy Me a Coffee icon linking to `https://buymeacoffee.com/vladg` in a new tab (`noopener noreferrer`); hover scales and fades it slightly.

**Credits page**

- Title "Credits & Attributions"; a card "Icons" with the sentence "We would like to thank the following creators for their excellent work:" and seven links, all "<Name> icons created by Freepik - Flaticon": poker, fist, finger, two, three, four, fingers (URLs `https://www.flaticon.com/free-icons/<slug>`, slugs as listed, link `title` = "<slug> icons"), opened in a new tab. A "Back to Home" button at the bottom.

**Head**

- Tab title "<page> - PlanningPokerUi"; favicons at 16, 32, 48 (`.ico`), 192 and 512 px, and an Apple touch icon.

**Theme storage**

- Old key `planningpoker_theme_preference` in `localStorage`; the Angular client uses `pp-theme` since slice 0. Preferences are not migrated (decision 4).

## 2. Contract

No REST, hub or Swagger change. No new routes (`/credits` and the `Credits` redirect exist). `localStorage` key stays `pp-theme`.

## 3. Angular pieces (`PlanningPoker.Client`)

- **`app` shell** (`app.html`, `app.scss`):
  - Navbar: brand (logo `favicon-32x32.png` at 28x28 with alt "Planning Poker logo", then "Planning Poker") linking home, and `<app-theme-select>` on the right. Contents sit in a centred container (max-width as the old Bootstrap container, 1140 px at desktop, full width with side padding on a phone). Bottom border and shadow use `--border-color` / `--shadow-color`.
  - Footer: `© {{ year }} - Planning Poker`, a divider, the Credits link, and on the right the Buy Me a Coffee link with the logo `https://cdn.buymeacoffee.com/buttons/bmc-new-btn-logo.svg` (12x12, yellow round background, hover scale 1.05 / opacity 0.9, dark-mode brightness filter as the old CSS). Stays at the bottom on a short page (the flex column in `app.scss` already does this) and fits at 360 px without overflow (the right link may wrap under the left group).
  - The cycle button and `NEXT_THEME` are removed with the cycle code in `App`.
- **`shared/theme-select`** (new, standalone, OnPush): a native `<select>` (Auto, Light, Dark) with the palette emoji, bound to `ThemeService.preference` and calling `ThemeService.set`; accessible name "Theme"; styled with the site variables so both themes work, the dark option colours included. A native select on purpose: it is what the old UI had, works with the phone's picker, and needs no Material select overlay.
- **`ThemeService`**: unchanged except as required by the control; no new API.
- **`features/credits`**: page title "Credits & Attributions"; an "Icons" section (card) with the sentence and the seven links, from a new `ICON_CREDITS` list in `credits.data.ts` (`{ title, url, label }`); the existing "Libraries" section from slice 3 keeps its place after it in its own card; a "Back to Home" button (`routerLink="/"`, Material button) at the bottom. All external links `target="_blank" rel="noopener noreferrer"`.
- **Head** (`index.html`, `public/`): copy `favicon-16x16.png`, `favicon-32x32.png`, `android-chrome-192x192.png`, `android-chrome-512x512.png` and `apple-touch-icon.png` from `PlanningPokerUi/wwwroot` into `public/` (the `favicon.ico` is already there) and add the `<link rel="icon">` / `apple-touch-icon` tags with sizes. The old non-standard `type="image/x-icon"` on the PNGs and the `-precomposed` touch-icon rel are not carried over (decision 6).
- **Titles**: each route in `app.routes.ts` gets a `title` ("Home", "Room", "Credits") and a `TitleStrategy` that produces "<page> - Planning Poker", with plain "Planning Poker" when a route has none.
- **Styles**: everything above uses the site colour variables; no hard-coded light colours. Check the footer and the select in dark mode. The dark-mode image filters of the old CSS are already kept for card and mark images (slices 2 and 3); no global `img` filter is added (decision 7).

## 4. Tests (TDD: write each failing test first)

### 4.1 Vitest

- `ThemeSelect`: shows the stored preference; picking an option calls `ThemeService.set` and persists; follows a preference change made elsewhere.
- `App`: navbar has the home link and the theme select (the cycle button is gone); footer has the Credits link to `/credits` and the coffee link with `target="_blank"` and `rel="noopener noreferrer"`; the year comes from the clock (inject a fixed date).
- `Credits`: seven icon links with the right URLs, titles and labels, then the two library links; every external link has `rel="noopener noreferrer"`; the Back to Home button links to `/`.
- Title strategy: `Credits - Planning Poker`, default title when a route has none.
- Update `app.spec.ts` (the cycle test is replaced by the select test).

### 4.2 Playwright

1. Switch the select to Dark: `<html data-theme="dark">`, the choice survives a reload; Auto follows the emulated colour scheme.
2. Footer Credits link opens the credits page (Icons and Libraries visible); Back to Home returns to `/`; `/Credits` redirects.
3. Phone viewport (375x812), light and dark: navbar and footer fit, no horizontal overflow.
4. `document.title` is "Credits - Planning Poker" on the credits page.

Existing e2e flows that click the old theme button (check `walking-skeleton.spec.ts`) are updated to use the select.

## 5. Acceptance criteria

1. `ng test`, `ng build` and the Playwright flows above pass in CI; `dotnet test` still passes (no backend change).
2. No unused code: the cycle button, `NEXT_THEME` and the tests for them are gone.
3. Light and dark mode and the 360 px layout of navbar, footer and credits page were checked in a browser.
4. Every attribution of the old credits page is on the new page, with working links.
5. Nothing in the slice touches the server, the hub contract or Swagger.

## 6. Open questions for review

Each has a recommendation; the spec above already follows it. Change it if you disagree.

1. **Native select, not a cycle button. Recommended.** The slice 0 button cycles auto -> light -> dark and only shows the current word. Parity (and a clearer control) is the old select. Alternative: keep the button.
2. **Product name "Planning Poker", not "PlanningPokerUi". Recommended.** The old name is the project's folder name leaking into the UI; the slice 0 shell already says "Planning Poker". Tab title and footer follow.
3. **Copyright year from the clock. Recommended.** Old: hard-coded "2025". Alternative: keep "2025" or show "2025 - <current>"; say if the owner wants a fixed year.
4. **Theme preference is not migrated from the old key. Recommended.** The old and new app share an origin only at cutover; a one-time read of `planningpoker_theme_preference` costs code for a once-per-user nicety. Alternative: read the old key as a fallback in `readStored`.
5. **Buy Me a Coffee icon stays a remote image. Recommended (parity).** Old: loaded from `cdn.buymeacoffee.com`. Alternative: copy the SVG into `public/` so the page has no third-party request, at the price of owning a vendor asset.
6. **Standards-conforming head tags. Recommended.** The old PNG favicons were tagged `image/x-icon` and the touch icon used the obsolete `-precomposed` rel. Same files, correct tags; no visible change.
7. **No global dark-mode `img` filter. Recommended.** The old CSS dimmed every image in dark mode and, by a leftover rule, put a dark background behind images in light mode. Only the card and mark images are treated, since slice 2; the logo and coffee icon need nothing. Alternative: port the global rule.
8. **No web app manifest.** The old app had none (only icon tags). Not added; a PWA manifest would be a new feature.
9. **Per-route titles via a `TitleStrategy`.** Small, but it is code the old Razor layout got for free from `ViewData["Title"]`; the alternative is a fixed title, which is a visible regression for people with many tabs.
