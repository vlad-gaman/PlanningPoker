# Research: password hashing and TOTP libraries for the management page

Issue: vlad-gaman/PlanningPoker#77 (child of map #71).
Access date for every source below: 2026-11-12.
Target project: `PlanningPokerUi/PlanningPokerUi.csproj` on `origin/main` declares `<TargetFramework>net6.0</TargetFramework>`. The `angular-migration` branch has `PlanningPoker.*` projects that were not inspected for TFM (out of scope for this note).

No `docs/`, ADR or research convention exists on `origin/main`, so this file is placed at `docs/research/management-page-auth-libs.md`.

## Summary

- **Password hashing:** Keep the hashing behind a service. The lowest-risk option is ASP.NET Core's built-in `PasswordHasher<TUser>` (Microsoft-owned, MIT, no extra dependency, PBKDF2-HMAC-SHA512 with 100,000 iterations by default, reports rehash-needed). If Argon2 is required, `Isopoh.Cryptography.Argon2` is the better fit than Konscious, but both have stale NuGet releases. BCrypt.Net-Next is actively maintained, but bcrypt is the weaker choice of the three for new code.
- **TOTP:** `Otp.NET` 1.4.1 (MIT, released 2025-12-01, targets net6.0) is the maintained option with a small, direct API. TwoFactorAuth.Net and GoogleAuthenticator are stale.
- Put both behind a service (`IPasswordHashingService`, `ITotpService`). Controllers and repositories should not call the libraries directly.

## Comparison table

| Library | License | Latest NuGet (version, date) | Repo activity (verified) | Target frameworks (latest) | API shape |
|---|---|---|---|---|---|
| ASP.NET Core `PasswordHasher<TUser>` (Microsoft.Extensions.Identity.Core) | MIT | 10.0.15 (2026-11-10); 11.0.0 (2026-11-10, net11.0 TFM) | Maintained in dotnet/aspnetcore | `.NETFramework4.6.2`, `net10.0`, `.NETStandard2.0` for 10.0.15 | `HashPassword(user, pw)`, `VerifyHashedPassword(user, hash, pw)` returns `PasswordVerificationResult` |
| BCrypt.Net-Next | MIT | 4.2.0 (2026-05-11); 4.2.1 is listed with `published` 1900-01-01 (unverified meaning) | Last commit 2026-07-08; release v4.2.0 2026-05-11; latest tag v4.2.2; 6 open issues (includes PRs) | `.NETFramework4.6.2`, `4.7.2`, `4.8`, `.NETStandard2.0`, `.NETStandard2.1`, `net10.0` | `BCrypt.HashPassword(pw, workFactor)`, `BCrypt.Verify(pw, hash)`; `BCryptExtendedV3` for HMAC pre-hash |
| Konscious.Security.Cryptography.Argon2 | MIT | 1.3.1 (2024-06-19) | Last commit 2024-06-18; no tags or releases; 22 open issues (includes PRs); open PRs from 2025-12 to 2026-11 unmerged | `.NETFramework4.6`, `.NETStandard1.3`, `net6.0`, `net8.0` | `new Argon2id(passwordBytes)`, `GetBytes(n)` returns raw bytes |
| Isopoh.Cryptography.Argon2 | NOASSERTION on GitHub; NuGet license field empty (unverified) | 2.0.0 (2023-08-17) | Repo `pushed_at` 2026-09-12 (signing-related commit); no GitHub release after v1.0 (2017); issue #60 (2026-09-08) asks for a release because the reusable `Argon2Memory` API is on master but absent from 2.0.0 | `.NETCoreApp3.1`, `net6.0`, `net7.0`, `.NETStandard2.0` | `Argon2.Hash(pw)` returns an encoded string; `Argon2.Verify(hash, pw)`; `Argon2Config` for tuning |
| Otp.NET | MIT | 1.4.1 (2025-12-01) | Last commit 2025-12-01 (bump version); release v1.4.1 2025-12-01; 20 open issues (includes PRs) | `.NETFramework4.6.1`, `.NETStandard2.0`, `net5.0`, `net6.0`, `net7.0`, `net8.0`, `net9.0`, `net10.0` | `new Totp(key, mode, step, totpSize)`, `ComputeTotp()`, `VerifyTotp(code, out timeWindowUsed, window)` |
| TwoFactorAuth.Net | MIT | 1.4.0 (2020-07-10) | Last commit 2022-09-22; no releases; 7 open issues | `.NETFramework4.6`, `.NETStandard2.0` | Not sketched (stale; see below) |
| GoogleAuthenticator (BrandonPotter) | Apache-2.0 | 3.2.0 (2024-01-12) | Last commit 2026-11-02 (dependabot merge); release v3.2 2024-01-12; 6 open issues | `.NETFramework4.6.2`, `.NETStandard2.0` | Not sketched (see below) |
| ASP.NET Core Identity built-in TOTP | MIT (part of ASP.NET Core) | n/a (Identity framework) | Maintained in dotnet/aspnetcore | n/a | `UserManager` authenticator key and token APIs; not sketched in full (see below) |

Sources for the table: the NuGet rows come from `https://api.nuget.org/v3/registration5-gz-semver2/<id>/index.json` for each package id (bcrypt.net-next, konscious.security.cryptography.argon2, isopoh.cryptography.argon2, otp.net, twofactorauth.net, googleauthenticator, microsoft.extensions.identity.core), accessed 2026-11-12. The repo rows come from `gh api repos/<owner>/<repo>` and `gh api repos/<owner>/<repo>/commits?per_page=1`, `.../releases/latest`, `.../tags`, `.../issues?state=open&sort=updated`, accessed 2026-11-12. Open-issue counts from GitHub include pull requests.

## Password hashing

### ASP.NET Core `PasswordHasher<TUser>` (built-in)

- **Owner and license:** Microsoft, MIT. Source: `https://github.com/dotnet/aspnetcore/blob/main/src/Identity/Extensions.Core/src/PasswordHasher.cs`, `https://github.com/dotnet/aspnetcore/blob/main/src/Identity/Extensions.Core/src/PasswordHasherOptions.cs` (raw file contents read 2026-11-12). Package license via `https://api.nuget.org/v3/registration5-gz-semver2/microsoft.extensions.identity.core/index.json` (`licenseExpression` MIT for 10.0.x).
- **Package and namespace:** `Microsoft.AspNetCore.Identity.PasswordHasher<TUser>` ships in `Microsoft.Extensions.Identity.Core` (and the `Microsoft.AspNetCore.Identity` assembly). Source: `https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.passwordhasher-1` (accessed 2026-11-12).
- **Default algorithm (source):** Version 3 format is PBKDF2 with HMAC-SHA512, 128-bit salt, 256-bit subkey, 100,000 iterations (the header comment in `PasswordHasher.cs`). `PasswordHasherOptions.IterationCount` defaults to 100_000 and `CompatibilityMode` defaults to `IdentityV3`. Source: `PasswordHasherOptions.cs` in the link above.
- **Verification result:** `VerifyHashedPassword` returns `PasswordVerificationResult`, including `SuccessRehashNeeded` when the stored hash was produced by an older format, a lower iteration count, or a weaker PRF (`PasswordHasher.cs`, `VerifyHashedPassword`).
- **API shape:**

  ```csharp
  using Microsoft.AspNetCore.Identity;

  var hasher = new PasswordHasher<AppUser>(); // or inject IOptions<PasswordHasherOptions>
  string hash = hasher.HashPassword(user, password);
  PasswordVerificationResult result = hasher.VerifyHashedPassword(user, hash, password);
  if (result == PasswordVerificationResult.SuccessRehashNeeded) { /* rehash and persist */ }
  ```

- **Fit with Controller-Service-Repository:** The service calls the hasher. The repository stores only the opaque string. Rehash-on-login belongs in the service.
- **Caveat:** `PasswordHasher<TUser>` is PBKDF2, not Argon2. It is the standard for ASP.NET Core Identity and needs no extra package. It is a reasonable choice if the team does not require Argon2 specifically.

### BCrypt.Net-Next

- **License and owner:** MIT, owner BcryptNet. Source: GitHub `https://api.github.com/repos/BcryptNet/bcrypt.net` (license `MIT`, accessed 2026-11-12), NuGet license `https://www.nuget.org/packages/BCrypt.Net-Next/4.2.0/license`.
- **Maintenance:** Latest NuGet 4.2.0 (2026-05-11). Last commit 2026-07-08 ("chore: packages"). Release v4.2.0 published 2026-05-11 (`https://github.com/BcryptNet/bcrypt.net/releases`). Latest tag v4.2.2. 6 open issues including PRs; the top open issue (#305, "Version 5 release", updated 2026-10-19) indicates a v5 release is planned. Repo README notes "The main code, documentation and website refer to V5 which is being prepared for release" (`https://github.com/BcryptNet/bcrypt.net/blob/main/readme.md`).
- **Targets:** Latest NuGet entry includes `.NETStandard2.0`, `.NETStandard2.1`, `net10.0` and `.NETFramework4.6.2+`. Usable from net6.0 via `.NETStandard2.0`.
- **API shape** (`https://github.com/BcryptNet/bcrypt.net/blob/main/readme.md`, quick start; `src/BCrypt.Net/BCrypt.cs` lines 153 and 170):

  ```csharp
  using BCryptNet;

  string hash = BCrypt.HashPassword("my password");   // default work factor 11
  bool ok = BCrypt.Verify("my password", hash);
  ```

  `BCryptExtendedV3.HashPassword(hmacKey, inputKey, workFactor, hashType)` pre-hashes with HMAC (default SHA384) before bcrypt (`src/BCrypt.Net/BCryptExtendedV3.cs`).
- **Adapter:** `src/BCrypt.Net.IdentityExtensions/BCryptPasswordHasher.cs` wraps `PasswordHasher<TUser>` and implements `IPasswordHasher<TUser>`, so a bcrypt hash can be used through Identity's interface (`https://github.com/BcryptNet/bcrypt.net/blob/main/src/BCrypt.Net.IdentityExtensions/BCryptPasswordHasher.cs`).
- **Fit with layering:** The hasher is called from the service. Repository stores the string.
- **Unverified:** The bcrypt 72-byte input limit is well known, but no primary source for it was fetched in this run, so it is not stated as fact here. Verify before relying on it.

### Konscious.Security.Cryptography.Argon2

- **License and owner:** MIT, owner kmaragon. NuGet license: `https://www.nuget.org/packages/Konscious.Security.Cryptography.Argon2/1.3.1/license`. GitHub: `https://github.com/kmaragon/Konscious.Security.Cryptography` (license MIT, accessed 2026-11-12).
- **Maintenance (stale):** Latest NuGet 1.3.1 (2024-06-19). Last commit 2024-06-18. No tags or GitHub releases. 22 open issues including PRs. PRs #66 (.NET 10 target, 2026-06-17), #68 (opt-in `IArgon2MemoryAllocator`, 2026-09-21) and #69 (allow zero-length passwords, 2026-11-03) are open. `https://github.com/kmaragon/Konscious.Security.Cryptography/pulls`.
- **Targets:** `.NETFramework4.6`, `.NETStandard1.3`, `net6.0`, `net8.0`.
- **API shape** (`https://github.com/kmaragon/Konscious.Security.Cryptography/blob/master/README.md`, Argon2 section):

  ```csharp
  using Konscious.Security.Cryptography;

  var argon2 = new Argon2id(passwordBytes)
  {
      DegreeOfParallelism = 4,
      MemorySize = 65536,   // KiB
      Iterations = 3,
      Salt = salt,          // caller generates and stores
  };
  byte[] hash = argon2.GetBytes(32);   // raw bytes only
  ```

  The README states the library returns raw bytes. Salt storage, parameter storage, encoding, and constant-time comparison are the caller's responsibility.
- **Fit with layering:** Hashing logic lives in the service. The service must also store salt and parameters, so it needs a wrapper that encodes them. More code than Isopoh.

### Isopoh.Cryptography.Argon2

- **License:** GitHub reports `NOASSERTION` (`https://api.github.com/repos/mheyman/Isopoh.Cryptography.Argon2`). NuGet license field is empty for 2.0.0. The repo's `LICENSE` file was not fetched, so the exact license is unverified.
- **Maintenance (mixed):** Latest NuGet 2.0.0 (2023-08-17). Repo `pushed_at` 2026-09-12 (last commit "Allow one hour for SignPath approval"). GitHub releases list only `v1.0` (2017-03-14). Latest tag `v2.0.0`. Issue #60 (2026-09-08), "Please publish a release that includes the reusable Argon2Memory API (present on master, absent from 2.0.0)" (`https://github.com/mheyman/Isopoh.Cryptography.Argon2/issues/60`), indicates the README describes features not present in the published 2.0.0 package. Pin against the published NuGet 2.0.0 and verify the API against it, not the README.
- **Targets:** `.NETCoreApp3.1`, `net6.0`, `net7.0`, `.NETStandard2.0`.
- **API shape** (`https://github.com/mheyman/Isopoh.Cryptography.Argon2/blob/main/README.md`, usage):

  ```csharp
  using Isopoh.Cryptography.Argon2;

  string hash = Argon2.Hash("password1");              // encoded string including parameters and salt
  bool ok = Argon2.Verify(hash, "password1");
  ```

  The encoded string carries parameters and salt, so no separate salt storage is needed. This matches the Argon2 PHC string convention.
- **Fit with layering:** Simplest Argon2 API of the three. Call from the service only.

## TOTP

### Otp.NET

- **License and owner:** MIT, owner kspearrin. GitHub `https://github.com/kspearrin/Otp.NET` (license MIT, accessed 2026-11-12). NuGet `https://www.nuget.org/packages/Otp.NET`.
- **Maintenance:** Latest NuGet 1.4.1 (2025-12-01). Last commit 2025-12-01. Release v1.4.1 (2025-12-01). 20 open issues including PRs, most recent issue #68 "Add custom extra url query values for OtpUri class" (2026-06-04), #15 "TOTP One Time Use" (2026-06-04). Issue #62 "Does not work with Microsoft ... security-info" (2026-04-20).
- **Targets:** `.NETFramework4.6.1`, `.NETStandard2.0`, `net5.0`, `net6.0`, `net7.0`, `net8.0`, `net9.0`, `net10.0`. Fits the repo's net6.0 directly.
- **API shape** (`https://github.com/kspearrin/Otp.NET/blob/master/README.md`, TOTP sections):

  ```csharp
  using OtpNet;

  var totp = new Totp(secretKey);                       // secretKey: byte[]
  string code = totp.ComputeTotp(DateTime.UtcNow);
  bool ok = totp.VerifyTotp(userCode, out long timeWindowUsed,
                            new VerificationWindow(previous: 1, future: 1));
  ```

  - `Totp(byte[] key, OtpHashMode mode = Sha1, int step = 30, int totpSize = 6)` (`README`, "Creation of a TOTP object").
  - `timeWindowUsed` is the caller's replay-protection input. README: "It is up to the consumer of this library to ensure that only one match for a given time step window is actually accepted."
  - `VerificationWindow.RfcSpecifiedNetworkDelay` is provided for one-step previous and future tolerance.
  - Base32 encoding and `OtpUri` are also in the README.
- **Fit with layering:** TOTP verification belongs in a service (`ITotpService`). The service loads the secret through the repository, calls `VerifyTotp`, compares `timeWindowUsed` to the last accepted step stored for the user, and persists it through the repository. Replay protection cannot be done by the library, per the README.

### Alternatives

- **TwoFactorAuth.Net** (`https://github.com/RobThree/TwoFactorAuth.Net`, MIT, NuGet 1.4.0 2020-07-10, last commit 2022-09-22). Stale. Not recommended for new code. Not sketched.
- **GoogleAuthenticator** (`https://github.com/BrandonPotter/GoogleAuthenticator`, Apache-2.0, NuGet 3.2.0 2024-01-12, last commit 2026-11-02 as dependabot). Lightly maintained; NuGet release is over two years old. Not sketched.
- **ASP.NET Core Identity TOTP** (`https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-enable-qrcodes`, accessed 2026-11-12). The page confirms ASP.NET Core Identity uses TOTP for authenticator apps and that the template does not generate QR codes. It also warns: "The user can enter the code multiple times and authenticate successfully before it expires." That confirms replay protection is needed. Using Identity's `UserManager` TOTP would couple the app to Identity. Its API surface was not fetched in full here, so it is listed as unverified beyond that page.

## Layering summary

- **Controllers** call an application service, for example `IPasswordHashingService` or `IAuthService`. They do not call hashing or TOTP libraries directly.
- **Services** own hashing and TOTP. They call the chosen library and handle rehash-on-login and TOTP replay checks (`timeWindowUsed`).
- **Repositories** store only the hash string, the TOTP secret (encrypted at rest, see below), and the last accepted TOTP time step. They do not call hashing or TOTP code.
- **Encryption at rest:** TOTP secrets are recoverable by design and cannot be hashed. Encrypt them at rest. The ASP.NET Core Data Protection API is the platform option. Source for the Data Protection claim was not fetched in this run (unverified).

## Recommendation

**Password hashing:** Use ASP.NET Core `PasswordHasher<TUser>` behind `IPasswordHashingService`. It is Microsoft-maintained, MIT, needs no extra dependency, and returns `SuccessRehashNeeded` for rehash-on-login. If the team requires Argon2, use `Isopoh.Cryptography.Argon2` (encoded-string API), but pin the published 2.0.0 and verify the API against it, because the repo README describes unpublished features (issue #60). Konscious is stale (last release 2024) and returns raw bytes that need custom encoding. BCrypt.Net-Next is maintained, but for new code bcrypt is weaker than the other two.

**TOTP:** Use `Otp.NET` 1.4.1 (MIT, net6.0-compatible, released 2025-12-01) behind `ITotpService`. Store the last accepted time step per user and reject any code whose `timeWindowUsed` is not greater, per the README's replay note. TwoFactorAuth.Net and GoogleAuthenticator are stale.

## Unverified claims

- bcrypt 72-byte input limit: not checked against a primary source in this run.
- Isopoh.Cryptography.Argon2 license: GitHub reports NOASSERTION; the LICENSE file was not fetched.
- BCrypt.Net-Next 4.2.1 entry with `published` 1900-01-01 in the NuGet index: meaning not verified.
- Data Protection API as the at-rest encryption choice: no Microsoft Learn page was fetched in this run.
- ASP.NET Core Identity's TOTP API surface beyond the QR-code page: not fetched in full.
- TwoFactorAuth.Net and GoogleAuthenticator API shapes: not sketched.
- Whether `Microsoft.Extensions.Identity.Core` is part of the `Microsoft.AspNetCore.App` shared framework for net6.0: not verified from a primary source in this run. The package is available on NuGet for net6-compatible `.NETStandard2.0`.
- Support status of net6.0 (end of life): not checked against a Microsoft lifecycle page in this run.
