# Mappers and validations stay one class per output object, even though that is about 100 mapper files

An architecture review (ticket 155) found about 100 mapper files (Controller 58, Service 34, Repository 8), each with an interface nearly as large as its body, and `RoomEventPublisher` injecting 10-13 mappers. It proposed grouping mapping and validation per concept (Room, Person, Vote).

Decision: keep the `CLAUDE.md` rules as they are: one class per file, one mapper per output object, an interface per mapper, validations in their own `...Validation` classes. No rule change, no regrouping.

Why:
- The count is the price of an explicit, uniform convention, not a defect. Nothing is broken by it; the cost is file count only.
- Regrouping touches about 100 files and their tests, so it is hard to reverse, for no behaviour gain.
- The two smaller claims did not hold up: `RoomConfigurationValidation` is controller input validation used by `RoomHub`, which is where `CLAUDE.md` puts it, so a service-level caller skipping it is by design. `PersonService.Upsert` ignoring the `TryAdd` result is a separate small bug (ticket filed).

Considered options:
- **Group mapping and validation per concept**: fewer files, but breaks the one-mapper-per-output-object rule and the file-per-class rule. Rejected.
- **Trim the `RoomEventPublisher` fan-out instead**: the real smell is the publisher's 10-13 injected mappers. Giving it one collaborator (for example a payload factory) fixes that without changing the mapper rules. This is the sanctioned alternative if the fan-out ever hurts; no ticket now.

Consequences:
- Adding a new output object still means a new mapper, an interface and a registration.
- Anyone proposing a rule change should reopen this with a concrete defect, not file count.
