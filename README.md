# Pratiyogita

School exams/olympiads (**Academic**) and sports tournaments (**Sports**) — registration, scheduling,
results, school/location-wise toppers, news, and community contributions (donations/sponsorship) —
for the `*.keshavsingh.in` family.

- **Backend**: `Pratiyogita.Api`, ASP.NET Core, **.NET 10**, MongoDB.
- **Frontend**: Angular **22** (standalone components, latest stable at time of writing), public-facing site.
- **Admin**: this repo has **no admin UI of its own** — every write action that needs `Admin`/`Editor`
  is a normal REST endpoint on `Pratiyogita.Api`; the screens to drive them belong in the `admin`
  repo's Angular app (see "Admin management" below), same as every other app in the family.

## Why this name

"Pratiyogita" (प्रतियोगिता) is Hindi for "competition/contest" — broad enough to cover both an
exam/olympiad and a sports tournament without forcing an awkward umbrella term. Rename freely before
you push the real GitHub repo if you'd prefer something else; nothing below depends on the name
except the `Pratiyogita.Api` project name, the Mongo database name, and the `pratiyogita.keshavsingh.in`
CNAME.

## Architecture: modular monolith today, microservice-ready boundaries

A full microservice deployment (separate processes, a gateway, per-service databases) is real
operational overhead — extra infra, network hops, distributed-transaction problems — for a platform
that doesn't yet have traffic to justify it, and it's not how any sibling app in this family
(`admin`, `ghar-ledger`, `content-blog`) is deployed either. Instead, `Pratiyogita.Api` is **one**
ASP.NET Core project, but organized so each bounded context is already isolated enough to be lifted
out into its own service later with minimal rework:

- Each module owns **one Mongo collection** and never reaches into another module's collection
  directly — cross-module references are always by `Id` string (e.g. `Registration.SchoolId`), never
  a Mongo `$lookup`/join at the storage layer.
- Each module is a self-contained `Service` + `Controller` + `Models`/`Dtos` slice
  (`Services/SchoolService.cs`, `Controllers/SchoolsController.cs`, …) with no shared mutable state.
- The one place that *does* read across modules is `LeaderboardService` (toppers need
  Result → Registration → StudentProfile → School → Location). If this repo is ever split into real
  microservices, `LeaderboardService` is exactly the piece that becomes its own read-model/reporting
  service (or a Mongo aggregation pipeline / search index) that subscribes to change events from the
  others, instead of doing in-process joins.

If/when scale genuinely demands it, the natural split is: **Identity & Schools**, **Competitions &
Results**, **Contributions**, **News** — each behind an API gateway, each still validating the same
shared SSO JWT (see below) so auth never has to be re-architected.

## Domain model (one Mongo collection per module)

| Module | Collection | Model | Notes |
|---|---|---|---|
| Locations | `locations` | `Location` | Standardized city/village/state/district so results can be grouped by place reliably (not free-text matching). |
| Schools | `schools` | `School` | Self-registered by any signed-in user, `Pending` until an `Admin` approves it (anti-impersonation gate) and assigns a unique `Code`. |
| Students | `student_profiles` | `StudentProfile` | **Not** a login account — keyed by the SSO `sub` (see Auth below). Holds school, class/grade, academic year, DOB, email, phone, guardian. |
| Competitions | `competitions` | `Competition` | One shape for both exams and tournaments; `Type` (`Academic`/`Sports`) + free-text `Category` (e.g. "Mathematics Olympiad", "Cricket (U-14)") + `Level` (School/Inter-School/City/District/State/National). |
| Registrations | `registrations` | `Registration` | A student (or a school's team) entering a competition. |
| Fixtures | `fixtures` | `Fixture` | Scheduling — round name, participants, venue, date/time, status. |
| Results | `results` | `Result` | Generic `Score`/`Rank` per registration — fits marks, runs, goals, time, whatever the category needs. |
| Contributions | `contributions` | `Contribution` | Donations/sponsorship — see payment flow below. |
| News | `news_posts` | `NewsPost` | The "what's next" announcements page. |
| Categories | `competition_categories` | `CompetitionCategory` | Admin-managed master data (e.g. "Mathematics Olympiad", "Cricket (U-14)") — the leaderboard filter and competition forms read from this instead of free text. |
| Platform settings | `platform_settings` | `PlatformSettings` | Single document, currently just the UPI payout id/payee name — DB-backed and Admin-editable at runtime (see payment flow below), not just an appsettings value. |

## Auth: SSO resource server (same pattern as `ghar-ledger`)

This app **never logs anyone in**. Every `*.keshavsingh.in` app shares one identity provider — but
note the domain split: `id.keshavsingh.in` is the actual API/backend (this is what `idpUrl` in
`environment.ts` must point at), while `admin.keshavsingh.in` is only the admin frontend SPA
(GitHub Pages/Fastly — it has no API behind it and returns a blanket 405 for any non-GET verb).
`Pratiyogita.Api` only **validates** the JWT that flow issues (same `Jwt:Issuer`/`Audience`/`SigningKey`
as every sibling app) and keeps its own
domain-specific data (a `StudentProfile`) keyed by the token's `sub` claim. Role checks
(`Roles.Admin` / `Roles.Editor` from `KeshavSingh.Core`) gate every write that should be admin-managed:
approving schools, creating/publishing competitions & results, scheduling fixtures, verifying
contributions, and publishing news.

## Admin management (build this in the `admin` repo, not here)

Everything an operator needs is already exposed as `[Authorize(Roles = ...)]` endpoints on
`Pratiyogita.Api` (school approval queue, competition/fixture/result CRUD, contribution
verification queue, news CRUD, category/location master-data management, `PUT /api/settings/payments`).
As a stopgap until that's built, this repo's own frontend now ships a minimal `/admin/data` screen
(`Admin`-role-guarded, linked from the header as "Manage data") for the three things needed before
the public pages are useful at all: **Locations**, **Categories**, and the **Payments** UPI id — plus
an inline "Configure now" shortcut on the Contribute page itself when an Admin views it unconfigured.
**Next step, not yet done**: move the rest (schools/competitions/fixtures/results/contributions/news)
into a proper feature area in `admin`'s Angular app (mirroring how it already has
Notes/ShortLinks/Finance/etc. as self-contained feature folders) with a `pratiyogita.service.ts`
calling this API's base URL (cross-origin, same bearer token — `Pratiyogita.Api`'s CORS already
allows the SSO family via `AddKeshavSsoCors`).

## Contribution / payment flow (Google Pay, PhonePe, Paytm, …)

No card or bank credential is ever handled by this API, and no merchant/payment-gateway account is
required to ship this:

1. The public Contribute page asks `GET /api/contributions/upi-link` for a `upi://pay?pa=...` deep
   link built from the non-secret `Payments:UpiVpa`/`Payments:PayeeName` config. **Every** UPI app
   (Google Pay, PhonePe, Paytm, BHIM, …) registers itself as a handler for this one intent — there is
   no per-app integration to write.
2. The contributor pays in their own app, then pastes the UPI reference/UTR number back into the
   form, which hits `POST /api/contributions` (anonymous-friendly, rate-limited) and is stored as
   `Pending`.
3. An `Admin` reconciles that reference number against the real bank/UPI statement and calls
   `PUT /api/contributions/{id}/verify` (or `/reject`).
4. `GET /api/leaderboard/contributors` — the public "who has contributed how much" board — only ever
   sums **Verified**, non-anonymous contributions, so nothing can inflate it by claiming a payment
   that never landed.

If real-time automatic verification is wanted later (no manual reconciliation), add a
`IPaymentGatewayClient` (Razorpay/Cashfree, both support UPI + webhooks) behind
`ContributionService` — the model already has `TransactionRef`/`Status` ready for that.

The QR code shown on the Contribute page is generated **entirely client-side** (the `qrcode` npm
package, no third-party QR API) from the exact same link/VPA already shown as plain text — nothing
is sent to an external service just to render a code.

### Configuring the UPI payout id

`Payments:UpiVpa`/`Payments:PayeeName` in `appsettings.json`/environment variables are only the
**first-run seed** — `PlatformSettingsService` copies them into the `platform_settings` Mongo
document once, then every request reads from there. To set or change the payout id after that, an
`Admin` calls `PUT /api/settings/payments` (or the admin app, once built) — don't just edit the seed
and redeploy expecting it to take effect again. `GET /api/contributions/upi-link` returns
`{ configured: false }` (not an error) until it's set, which the Contribute page shows as a plain
"not set up yet" message instead of a broken/empty panel.

### Trust & integrity — stopping a spoofed QR/payment link

The donation flow's biggest real risk isn't this app's code, it's someone tricking a contributor
into paying an attacker's UPI id instead of the real one. Mitigations already in place:

- The payout VPA is **never client-supplied** — `GET /api/contributions/upi-link` builds the link
  purely from the server-side `platform_settings` document; there is no request parameter that can
  override it, so a malicious client can't make the *real* API return someone else's VPA.
- Changing the VPA is **Admin-role-gated** (`PUT /api/settings/payments`, same JWT/role check as
  every other admin action) and **audited** (`LastUpdatedByUserId`/`LastUpdatedAt` on the document).
- The Contribute page always shows the **VPA in plain text right next to the QR code**, specifically
  so a human can visually verify it before scanning/paying — never trust a QR code (from an email,
  poster, or chat message) that can't be cross-checked against the VPA shown live on
  `pratiyogita.keshavsingh.in` itself.
- Because the frontend is a static site, the VPA is fetched fresh from the API on every page load —
  it is never baked into the deployed JS bundle, so a stale/cached frontend can't silently keep
  showing an old (or tampered) id after an Admin rotates it.

## Frontend settings (theme, language, my details)

Signed-in users get a gear icon in the header → `/settings`, with three sections: **My details**
(read-only SSO account info — name/email/roles come from the shared IdP and can't be edited here —
plus an editable form for the domain-specific `StudentProfile`: school, class, DOB, phone),
**Appearance** (light/dark, `ThemeService`, persisted to `localStorage`, applied via a
`data-theme` attribute the CSS variables in `styles.css` key off), and **Language** (`LocaleService`,
persisted preference only for now — it doesn't yet translate page content; wiring real i18n against
that stored preference is a follow-up).

## Deployment

- **Backend**: `render.yaml` — a single Render Docker web service built from `backend/Dockerfile`
  (same layered restore/publish pattern as `ghar-ledger`/`admin`). Needs `Jwt__SigningKey` set to the
  **exact same value** as `admin`'s, plus `Mongo__ConnectionString` and (optionally)
  `Payments__UpiVpa`, all as Render dashboard secrets — never committed.
- **Frontend**: `.github/workflows/deploy-frontend.yml` builds and publishes to GitHub Pages under
  the `CNAME` in this repo (`pratiyogita.keshavsingh.in`), same as the other family sites.
- **CI**: `.github/workflows/backend-ci.yml` builds/publishes the API on every push touching `backend/**`.

## Local development

```powershell
# Backend (needs a local MongoDB on localhost:27017, or set Mongo__ConnectionString)
cd backend/Pratiyogita.Api
dotnet run

# Frontend
cd frontend
npm install
npm start   # http://localhost:4200, expects the API at http://localhost:5100/api (see environment.ts)
```

The backend already builds successfully against the sibling package repos in this workspace via the
`UseLocalProjectReferences`/`SkipPrivatePackages` fallback (same mechanism every sibling app uses) —
no `PACKAGES_READ_TOKEN` needed for local builds.

## Next steps

- [ ] Move schools/competitions/fixtures/results/contributions/news admin UI (this repo currently
      only has `/admin/data` for locations/categories/payments) to the `admin` repo.
- [ ] Decide on and wire real school `Code` allocation rules (currently set by an Admin at approval time).
- [ ] Consider a real payment gateway (Razorpay/Cashfree) for automatic contribution verification.
- [ ] Wire `LocaleService`'s stored language preference to real page-content translation.
- [ ] Add pagination to list endpoints once data volume grows.
- [ ] Add unit tests (none included in this initial scaffold — mirrors the shape of existing service
      tests in `admin`/`ghar-ledger`, e.g. `KeshavSingh.*.Tests` conventions).
