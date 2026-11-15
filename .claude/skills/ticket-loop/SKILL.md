---
name: ticket-loop
description: "Orchestrate a queue of GitHub tickets on a timed loop: one ticket per tick, sub-agents triage and implement, merge when green. Use when the user says 'loop through the tickets', 'work through tickets N to M', or 'orchestrate the open tickets'."
disable-model-invocation: true
---

Run a queue of tickets on a loop. You are the orchestrator: you decide and delegate. You never implement, test or research yourself.

## Inputs (ask only if missing)

- **Queue:** a ticket range ("80 to 92") or a filter ("open Angular-migration tickets"). Tickets live in `vlad-gaman/PlanningPoker` (see `docs/agents/issue-tracker.md`).
- **Interval:** default 50 minutes (`delaySeconds: 3000`). Use what the user says.
- **Skip list:** tickets to ignore.
- **Triage:** off by default. Only triage tickets when the user asks for it.

## Each tick

Take at most **one** ticket per tick. A ticket finishing never starts the next one in the same tick; the next ticket starts on the next tick.

1. **Pick.** List the queue with `gh issue list`. Order by dependency, then number: read each ticket's "blocked by" lines and the `docs/phase-*-spec.md` slice order, and never start a ticket before the ones it depends on are merged. Skip the skip list.
2. **Triage (only if the user asked for it).** For a `needs-triage` or `needs-info` ticket (see `docs/agents/triage-labels.md`), delegate a `grilling` session to an **Opus** sub-agent, record the decisions on the ticket and relabel it `ready-for-agent`. Without a triage request, skip this step and take the ticket as written; if it can't be implemented as written, treat it as a blocker (step 8).
3. **Implement.** Delegate to a **Sonnet** sub-agent in its own worktree, told to use `/implement <ticket>`. The brief names the ticket, the base branch (`main`) and "follow CLAUDE.md, add tests, add an e2e test for any user-visible flow". Ask it to open the PR and report.
4. **Review the PR.**
   - A PR with open-ended questions in its body: have an Opus sub-agent `grilling` them, then send the decision back to the implementer. You do not guess.
   - Assumptions anywhere in the work get the same grilling treatment.
   - A PR for a user-visible change with no e2e test goes back to the implementer.
   - Optionally run the `pr-reviewer` agent and send findings back.
5. **CI.** After the PR is raised, stay in this tick and wait for CI to go green (`gh pr checks <n> --watch`, in the background). If a check fails, have the implementer fix it and push, then wait again.
6. **Deploy and check.** Once CI is green, deploy the branch to the staging slot (`ship-pr` step 4) and do a quick check on staging that the ticket is actually implemented: open the page in the built-in browser and exercise the behaviour the ticket describes. If it isn't, send the gap back to the implementer and repeat from step 5.
7. **Merge.** When CI is green, the staging check passes and the review is clean, use the `ship-pr` skill from its merge step. Close the ticket. Work that isn't development (docs, labels, housekeeping) can be merged without a loop tick of its own.
8. **Problems.** A question or failure that blocks the ticket: skip it and leave a comment saying why. One that doesn't block it: raise a new ticket and carry on. Never stall the loop on one ticket.
9. **Report and reschedule.** Give a few lines: ticket, state, PR link, anything waiting on the user. Then call `ScheduleWakeup` with the same prompt and the interval. Stop the loop (`stop: true`) when the queue is empty or the user says stop.

## Rules

- Keep a running table (ticket, state, PR, next action) in your replies so progress survives a context reset.
- Decisions come from you or from a grilling sub-agent, never from a Sonnet implementer.
- Ask the user only for what a sub-agent can't settle: product direction, credentials, anything outside the repo.
