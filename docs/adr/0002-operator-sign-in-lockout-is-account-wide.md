# The Operator sign-in lockout is account-wide, so an outsider can lock the Operator out

`LoginLockoutService` keeps two failure counters per failed sign-in: one for the sender's IP address and one for the single Operator account (`AccountKey`). Both lock at `MaxFailures` (5) failures and stay locked for `LockoutDuration` (15 minutes). These are the `Operator` appsettings defaults, and the counters are in memory only, so a restart clears them. `OperatorAuthService` checks `IsLocked` before looking at the credentials, in both the password step and the TOTP step, and wrong passwords and wrong TOTP codes both count as failures. A locked account refuses every sign-in, including one with the correct password and code, until the lock expires. A finished sign-in clears the account counter and the counter of the IP address it came from.

Decision: keep the account-wide lockout and accept the consequence. Anyone who can reach the sign-in endpoint can send 5 wrong passwords and keep the Operator out of the management page. Attempts made while locked are refused without counting, so the lock is not extended; to hold it indefinitely the outsider repeats the 5 failures each time it expires. In exchange, password guessing spread over many IP addresses still hits one shared limit: with only per-IP counters an attacker with many addresses gets 5 guesses per address per 15 minutes, with the account counter 5 guesses per 15 minutes in total. The management page only lets the Operator close rooms and remove people, so a lockout costs less than a successful guess.

Considered options:
- **A much higher account limit than the per-IP one**: one source could no longer lock the Operator out alone, but an attacker with enough addresses still could, and the guesses allowed across addresses grow by the same factor. It weakens the protection the account counter exists for without removing the lockout, so it was rejected.
- **A correct password plus a valid TOTP code signs in even while the account is locked (the per-IP lock still applies)**: it would let the Operator back in, but it adds a second path around the lockout to get wrong, and it only helps when TOTP is enabled (`TotpSecret` is optional). It was rejected as more code and risk than a single-Operator management page warrants.

Consequences:
- The Operator can be locked out by a stranger. Failed and refused sign-ins are recorded in the issues log (`RecordFailedLogin`, with `locked` set when refused), which is where to see it happening.
- Revisit this if the lockout is seen being abused in practice.
