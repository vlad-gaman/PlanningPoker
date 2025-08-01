# Voting Behavior Test Cases

## Expected Behavior
Voting cards should be **disabled** in these scenarios:
1. When everyone has voted AND results are being shown (auto-show with countdown)
2. When someone clicks "Show votes" (force show) - regardless of how many people voted
3. When everyone has voted AND countdown is 0 (immediate show)
4. After results are displayed, until "Clear votes" is clicked

Voting cards should be **enabled** in these scenarios:
1. When room starts (no votes cast)
2. After "Clear votes" is clicked
3. When not everyone has voted (partial voting state)

## Current Issues
1. **Single person voting**: After 1 person votes in a 1-person room, cards remain enabled
2. **Force show votes**: When "Show votes" is clicked before everyone votes, cards remain enabled
3. **Inconsistent behavior**: 1-person vs multi-person rooms behave differently

## Test Scenarios

### Scenario 1: Single Person Room
1. Person joins room ✓ (cards enabled)
2. Person votes ❌ (cards should be disabled but remain enabled)
3. Results shown ❌ (cards should be disabled)
4. Clear votes ✓ (cards enabled)

### Scenario 2: Multi-Person Room - Auto Show
1. Multiple people join room ✓ (cards enabled)
2. First person votes ✓ (cards remain enabled)
3. Last person votes ✓ (cards disabled, countdown starts)
4. Results shown ✓ (cards disabled)
5. Clear votes ✓ (cards enabled)

### Scenario 3: Multi-Person Room - Force Show
1. Multiple people join room ✓ (cards enabled)
2. Some people vote ✓ (cards remain enabled)
3. Someone clicks "Show votes" ❌ (cards should be disabled but remain enabled)
4. Results shown ❌ (cards should be disabled)
5. Clear votes ✓ (cards enabled)

### Scenario 4: Configuration Changes
1. AutoShowVotes = false
2. CountdownSeconds = 0
3. Different participant counts

## Root Cause Analysis ✅ SOLVED

### Primary Issue: Incorrect AutoShowVotes Logic
**Location:** `site.js:788`
**Bug:** `autoShowVotes: countdownSeconds === 0`

This logic was backwards:
- When countdown = 5 seconds (default) → `autoShowVotes = false` → No auto-show happens
- When countdown = 0 seconds → `autoShowVotes = true` → Auto-show works

### Impact on Both Issues:
1. **Single Person Voting**: If countdown > 0, `autoShowVotes` was `false`, so no auto-show triggered
2. **Force Show Votes**: This should work regardless of `autoShowVotes`, and it does (server calls `GenerateResultsAndStatistics()` with `VotingFinished = true`)

### Server-Side Logic (Correct):
- `Vote()` method checks: `room.DidEveryoneVote() && room.Configuration.AutoShowVotes`
- `ForceShowVotes()` method calls `ShowVotesAndStatistics()` directly
- `GenerateResultsAndStatistics()` sets `VotingFinished = true`
- `VotesShown` event sent with `votingFinished = true`
- Client receives event and calls `disableVoting(true)`

### Client-Side Logic (Was Broken):
- Room settings save function incorrectly set `autoShowVotes` based on countdown duration
- Should be: `autoShowVotes: true` (always auto-show when everyone votes)
- Countdown controls timing, not whether to show

## Fixes Applied ✅

### 1. Room Settings AutoShowVotes Logic
**File:** `site.js:788`
**Change:** `autoShowVotes: true, // Always auto-show when everyone votes (countdown controls timing)`

**Problem:** Room settings incorrectly set `autoShowVotes: countdownSeconds === 0`
**Solution:** Always set to `true` - countdown controls timing, not whether to show

### 2. Client-Side Voting Disable Logic
**File:** `site.js:598-609`
**Change:** REMOVED incorrect `disableVoting(true)` when `hasEveryoneVoted = true`

**Problem:** Initial fix incorrectly disabled voting during countdown when everyone voted
**Solution:** Only disable voting when `votingFinished = true` (results actually shown), allow vote changes during countdown

### 3. Server-Side Status Broadcasting
**File:** `RoomHub.cs:225-226`  
**Change:** Added `VoteStatusUpdate` event after each vote

**Problem:** Server only sent `VoteCast` events, clients never knew when everyone had voted
**Solution:** Send updated `VoteResultInfo` after each vote so clients can update voting status

### 4. Client-Side Status Update Handler
**File:** `site.js:145-150`
**Change:** Added `VoteStatusUpdate` event handler

**Problem:** No way for clients to receive updated voting status
**Solution:** Handle new `VoteStatusUpdate` event to update voting state in real-time

## Expected Behavior After Fixes ✅
1. **Single person voting**: Cards remain enabled during countdown, disabled when results shown
2. **Multi-person voting**: Cards remain enabled during countdown, disabled when results shown
3. **Force show votes**: Cards disabled immediately when results are forced to show
4. **Clear votes**: Cards re-enabled for new voting round
5. **Partial voting**: Cards remain enabled until results are shown
6. **During countdown**: Cards enabled for vote changes until results display
7. **Results displayed**: Cards disabled until votes are cleared

## Testing
Use the comprehensive test script: `comprehensive-voting-test.js`
1. Load script in browser console on room page  
2. Run: `votingTest.runAllTests()`
3. Verify all tests pass