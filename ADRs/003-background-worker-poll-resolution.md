# ADR 003: Polling Background Worker for Poll Resolution and Daily Challenges

**Date:** November 15, 2026  
**Status:** Accepted  

## Context
Polls close at a user-defined time, and when they do the system must resolve them: determine the winning option(s), pay out staked points, and notify voters. The platform also has to create one system-generated "Daily Challenge" poll per day. Both are time-driven jobs that must run without any user request triggering them.

Options considered:
1. **A hosted `BackgroundService` that wakes up on a fixed interval** and processes whatever is due.
2. **One timer per poll**, scheduled when the poll is created.
3. **A job scheduler library** (Hangfire or Quartz).
4. **Lazy resolution**, resolving a poll the first time someone reads it after it closed.

## Decision
We use a `BackgroundService` (`PollBackgroundWorker`) that loops every **10 seconds**. Each cycle creates a DI scope and calls:

1. `EnsureDailyQuestionAsync()`: creates today's daily challenge if none exists.
2. `ResolveClosedPollsAsync()`: loads closed, unresolved polls, calls `Poll.Resolve()` for payouts, adds notifications, and saves everything in a single `SaveChangesAsync` (Unit of Work, see ADR 001).

All progress is stored in the database (`IsResolved`), not in memory. A restart or crash therefore loses nothing: unresolved polls are simply picked up on the next cycle, and an already-resolved poll is skipped.

Per-poll timers were rejected because they are lost on restart and need rebuilding from the database anyway. A scheduler library was rejected as too much infrastructure for two small jobs. Lazy resolution was rejected because notifications and point payouts would not happen until someone happens to open the poll.

## Consequences

### Positive
* **Simple:** about 50 lines, no extra dependencies, easy to read and debug.
* **Restart-safe:** the database is the source of truth, so no scheduled state can be lost.
* **No overlap within one instance:** the loop is sequential (work, then a 10 second delay), so a slow cycle cannot run concurrently with the next one.
* **Failures are isolated:** exceptions are caught and logged, and the next cycle retries.

### Negative
* **Resolution delay:** results and payouts can be up to ~10 seconds late, and the frontend compensates by reloading shortly after a poll closes.
* **Constant database load:** queries run every 10 seconds even when nothing is due. `EnsureDailyQuestionAsync` currently loads every poll with all related data just to check whether today's challenge exists, so the cost grows with the amount of data. A targeted `AnyAsync` query would fix this.
* **Single instance only:** if the app were scaled to several instances, each would run its own worker and could resolve the same poll and pay out twice. That would need a database-level guard (for example an atomic update on `IsResolved`) or a distributed lock.
* **Server-local daily boundary:** the daily challenge uses the server's local date, so its day boundary depends on the machine's time zone.
