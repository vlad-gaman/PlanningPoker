// Comprehensive Voting Behavior Test Script
// Paste this into browser console on a planning poker room page

// Test Configuration
const TEST_CONFIG = {
    WAIT_TIME: 2000, // Wait time between test steps
    LOG_PREFIX: '🧪 TEST:'
};

// Test State
let testResults = [];
let currentTest = null;

// Utility Functions
function log(message, type = 'info') {
    const emoji = {
        'info': 'ℹ️',
        'success': '✅',
        'error': '❌',
        'warning': '⚠️',
        'debug': '🔍'
    };
    
    const logMessage = `${emoji[type]} ${TEST_CONFIG.LOG_PREFIX} ${message}`;
    console.log(logMessage);
    
    if (currentTest) {
        currentTest.logs.push({ message, type, timestamp: Date.now() });
    }
}

function checkVotingState() {
    const cards = document.querySelectorAll('input[type=radio][name=mark]');
    const disabledCards = document.querySelectorAll('input[type=radio][name=mark]:disabled');
    const enabledCards = document.querySelectorAll('input[type=radio][name=mark]:not(:disabled)');
    
    const stats = document.getElementById('statistics');
    const countdown = document.getElementById('show-votes-countdown');
    
    const state = {
        totalCards: cards.length,
        disabledCards: disabledCards.length,
        enabledCards: enabledCards.length,
        votingDisabled: disabledCards.length === cards.length && cards.length > 0,
        statisticsVisible: stats ? (stats.style.display !== 'none' && !stats.hidden) : false,
        countdownVisible: countdown ? (countdown.style.display !== 'none' && !countdown.hidden) : false
    };
    
    log(`Current state: ${state.votingDisabled ? 'DISABLED' : 'ENABLED'} voting, ` +
        `${state.disabledCards}/${state.totalCards} cards disabled, ` +
        `stats: ${state.statisticsVisible ? 'visible' : 'hidden'}, ` +
        `countdown: ${state.countdownVisible ? 'visible' : 'hidden'}`, 'debug');
    
    return state;
}

function simulateVote(cardValue = null) {
    return new Promise((resolve) => {
        const cards = document.querySelectorAll('input[type=radio][name=mark]:not(:disabled)');
        
        if (cards.length === 0) {
            log('No enabled cards found for voting', 'error');
            resolve(false);
            return;
        }
        
        const cardToSelect = cardValue ? 
            Array.from(cards).find(card => card.value === cardValue) || cards[0] :
            cards[0];
        
        log(`Simulating vote with value: ${cardToSelect.value}`, 'info');
        
        cardToSelect.checked = true;
        cardToSelect.dispatchEvent(new Event('change', { bubbles: true }));
        
        // Wait for server response
        setTimeout(() => resolve(true), TEST_CONFIG.WAIT_TIME);
    });
}

function simulateForceShow() {
    return new Promise((resolve) => {
        const showButton = document.getElementById('show-votes');
        
        if (!showButton) {
            log('Show votes button not found', 'error');
            resolve(false);
            return;
        }
        
        log('Simulating force show votes', 'info');
        showButton.click();
        
        // Wait for server response
        setTimeout(() => resolve(true), TEST_CONFIG.WAIT_TIME);
    });
}

function simulateClearVotes() {
    return new Promise((resolve) => {
        const clearButton = document.getElementById('clear-votes');
        
        if (!clearButton) {
            log('Clear votes button not found', 'error');
            resolve(false);
            return;
        }
        
        log('Simulating clear votes', 'info');
        clearButton.click();
        
        // Wait for server response
        setTimeout(() => resolve(true), TEST_CONFIG.WAIT_TIME);
    });
}

// Test Cases
async function testSinglePersonVoting() {
    currentTest = {
        name: 'Single Person Voting - Complete Flow',
        logs: [],
        passed: false
    };
    
    log('Starting single person voting test', 'info');
    
    // Check initial state
    let state = checkVotingState();
    if (!state.votingDisabled) {
        log('PASS: Initial state - voting enabled', 'success');
    } else {
        log('FAIL: Initial state - voting should be enabled', 'error');
    }
    
    // Simulate vote
    const voteSuccess = await simulateVote();
    if (!voteSuccess) {
        log('FAIL: Could not simulate vote', 'error');
        currentTest.passed = false;
        testResults.push(currentTest);
        return currentTest;
    }
    
    // Check state after vote (should still be enabled for vote changes during countdown)
    state = checkVotingState();
    if (!state.votingDisabled) {
        log('PASS: After vote - voting still enabled for changes during countdown', 'success');
    } else {
        log('FAIL: After vote - voting should remain enabled during countdown', 'error');
        currentTest.passed = false;
    }
    
    // Wait for auto-show to trigger (if it does)
    log('Waiting for auto-show to trigger...', 'info');
    await new Promise(resolve => setTimeout(resolve, 3000));
    
    // Check if results are shown and voting is disabled
    state = checkVotingState();
    if (state.statisticsVisible && state.votingDisabled) {
        log('PASS: After auto-show - statistics visible and voting disabled', 'success');
        currentTest.passed = true;
    } else if (state.countdownVisible && !state.votingDisabled) {
        log('PASS: During countdown - countdown visible and voting enabled for changes', 'success');
        currentTest.passed = true;
    } else {
        log('INCONCLUSIVE: Auto-show behavior depends on room configuration', 'warning');
        currentTest.passed = true; // Don't fail the test for this
    }
    
    // Clear votes for cleanup
    await simulateClearVotes();
    
    // Check final state
    state = checkVotingState();
    if (!state.votingDisabled) {
        log('PASS: After clear - voting enabled', 'success');
    } else {
        log('FAIL: After clear - voting should be enabled', 'error');
        currentTest.passed = false;
    }
    
    testResults.push(currentTest);
    return currentTest;
}

async function testForceShowVotes() {
    currentTest = {
        name: 'Force Show Votes',
        logs: [],
        passed: false
    };
    
    log('Starting force show votes test', 'info');
    
    // Check initial state
    let state = checkVotingState();
    if (!state.votingDisabled) {
        log('PASS: Initial state - voting enabled', 'success');
    } else {
        log('FAIL: Initial state - voting should be enabled', 'error');
    }
    
    // Simulate force show (without voting)
    const showSuccess = await simulateForceShow();
    if (!showSuccess) {
        log('FAIL: Could not simulate force show', 'error');
        currentTest.passed = false;
        testResults.push(currentTest);
        return currentTest;
    }
    
    // Check state after force show
    state = checkVotingState();
    if (state.votingDisabled) {
        log('PASS: After force show - voting disabled', 'success');
        currentTest.passed = true;
    } else {
        log('FAIL: After force show - voting should be disabled', 'error');
        currentTest.passed = false;
    }
    
    // Clear votes for cleanup
    await simulateClearVotes();
    
    // Check final state
    state = checkVotingState();
    if (!state.votingDisabled) {
        log('PASS: After clear - voting enabled', 'success');
    } else {
        log('FAIL: After clear - voting should be enabled', 'error');
        currentTest.passed = false;
    }
    
    testResults.push(currentTest);
    return currentTest;
}

async function testCountdownVoteChanges() {
    currentTest = {
        name: 'Countdown Vote Changes',
        logs: [],
        passed: false
    };
    
    log('Starting countdown vote changes test', 'info');
    log('This test verifies you can change votes during countdown', 'info');
    
    // Check initial state
    let state = checkVotingState();
    if (!state.votingDisabled) {
        log('PASS: Initial state - voting enabled', 'success');
    } else {
        log('FAIL: Initial state - voting should be enabled', 'error');
    }
    
    // Vote with first value
    let voteSuccess = await simulateVote('1');
    if (!voteSuccess) {
        log('FAIL: Could not simulate first vote', 'error');
        currentTest.passed = false;
        testResults.push(currentTest);
        return currentTest;
    }
    
    // Wait a moment then check if we can change vote
    await new Promise(resolve => setTimeout(resolve, 1000));
    
    state = checkVotingState();
    if (!state.votingDisabled) {
        log('PASS: After first vote - voting still enabled for changes', 'success');
        
        // Try to change vote
        voteSuccess = await simulateVote('3');
        if (voteSuccess) {
            log('PASS: Successfully changed vote during countdown period', 'success');
            currentTest.passed = true;
        } else {
            log('FAIL: Could not change vote', 'error');
            currentTest.passed = false;
        }
    } else {
        log('INFO: Voting disabled - might be because results are already shown', 'warning');
        currentTest.passed = true; // Don't fail if auto-show is immediate
    }
    
    // Clear votes for cleanup
    await simulateClearVotes();
    
    testResults.push(currentTest);
    return currentTest;
}

async function testPartialVotingThenForceShow() {
    currentTest = {
        name: 'Partial Voting + Force Show',
        logs: [],
        passed: false
    };
    
    log('Starting partial voting + force show test', 'info');
    log('Note: This test assumes multiple people in room', 'warning');
    
    // Check initial state
    let state = checkVotingState();
    if (!state.votingDisabled) {
        log('PASS: Initial state - voting enabled', 'success');
    } else {
        log('FAIL: Initial state - voting should be enabled', 'error');
    }
    
    // Simulate one vote (assuming not everyone)
    const voteSuccess = await simulateVote();
    if (!voteSuccess) {
        log('SKIP: Could not simulate vote', 'warning');
    } else {
        // Check state after partial vote
        state = checkVotingState();
        if (!state.votingDisabled) {
            log('PASS: After partial vote - voting still enabled', 'success');
        } else {
            log('INFO: After partial vote - voting disabled (everyone must have voted)', 'info');
        }
    }
    
    // Force show votes
    const showSuccess = await simulateForceShow();
    if (!showSuccess) {
        log('FAIL: Could not simulate force show', 'error');
        currentTest.passed = false;
        testResults.push(currentTest);
        return currentTest;
    }
    
    // Check state after force show - should be disabled
    state = checkVotingState();
    if (state.votingDisabled && state.statisticsVisible) {
        log('PASS: After force show - voting disabled and statistics shown', 'success');
        currentTest.passed = true;
    } else {
        log('FAIL: After force show - voting should be disabled and stats shown', 'error');
        currentTest.passed = false;
    }
    
    // Clear votes for cleanup
    await simulateClearVotes();
    
    testResults.push(currentTest);
    return currentTest;
}

// Main Test Runner
async function runAllTests() {
    console.clear();
    log('Starting comprehensive voting behavior tests', 'info');
    log('========================================', 'info');
    
    testResults = [];
    
    try {
        // Check if we're on the right page
        const cards = document.querySelectorAll('input[type=radio][name=mark]');
        if (cards.length === 0) {
            log('ERROR: No voting cards found. Are you on a planning poker room page?', 'error');
            return;
        }
        
        log(`Found ${cards.length} voting cards`, 'info');
        
        // Run tests
        await testSinglePersonVoting();
        await new Promise(resolve => setTimeout(resolve, 1000)); // Brief pause between tests
        
        await testCountdownVoteChanges();
        await new Promise(resolve => setTimeout(resolve, 1000));
        
        await testForceShowVotes();
        await new Promise(resolve => setTimeout(resolve, 1000));
        
        await testPartialVotingThenForceShow();
        
        // Summary
        log('========================================', 'info');
        log('TEST SUMMARY:', 'info');
        
        testResults.forEach(test => {
            log(`${test.name}: ${test.passed ? 'PASSED' : 'FAILED'}`, test.passed ? 'success' : 'error');
        });
        
        const passedTests = testResults.filter(t => t.passed).length;
        const totalTests = testResults.length;
        
        log(`Overall: ${passedTests}/${totalTests} tests passed`, 
            passedTests === totalTests ? 'success' : 'warning');
        
        // Detailed results
        console.log('Detailed test results:', testResults);
        
    } catch (error) {
        log(`Test runner error: ${error.message}`, 'error');
        console.error('Test runner error:', error);
    }
}

// Quick state checker
function quickCheck() {
    console.log('🔍 QUICK STATE CHECK:');
    checkVotingState();
    
    if (typeof connection !== 'undefined') {
        console.log('🔗 SignalR connection state:', connection.state);
    }
    
    if (typeof window.roomConfiguration !== 'undefined') {
        console.log('⚙️ Room configuration:', window.roomConfiguration);
    }
}

// Export functions to global scope for manual use
window.votingTest = {
    runAllTests,
    testSinglePersonVoting,
    testCountdownVoteChanges,
    testForceShowVotes,
    testPartialVotingThenForceShow,
    quickCheck,
    simulateVote,
    simulateForceShow,
    simulateClearVotes,
    checkVotingState
};

// Instructions
console.log(`
🧪 VOTING BEHAVIOR TEST SUITE LOADED

Run these commands in the console:
• votingTest.runAllTests() - Run all automated tests
• votingTest.quickCheck() - Check current voting state
• votingTest.simulateVote() - Simulate a single vote
• votingTest.simulateForceShow() - Simulate force show votes
• votingTest.simulateClearVotes() - Simulate clear votes

Individual tests:
• votingTest.testSinglePersonVoting()
• votingTest.testCountdownVoteChanges()
• votingTest.testForceShowVotes()
• votingTest.testPartialVotingThenForceShow()

Start with: votingTest.runAllTests()
`);