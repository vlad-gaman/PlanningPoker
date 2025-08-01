// Fireworks Logic Test Script
// Paste this into browser console on a planning poker room page to test fireworks

// Test Configuration
const FIREWORKS_TEST_CONFIG = {
    LOG_PREFIX: '🎆 FIREWORKS TEST:'
};

function logFireworks(message, type = 'info') {
    const emoji = {
        'info': 'ℹ️',
        'success': '✅',
        'error': '❌',
        'warning': '⚠️'
    };
    console.log(`${emoji[type]} ${FIREWORKS_TEST_CONFIG.LOG_PREFIX} ${message}`);
}

// Mock voteResultInfo data for testing (now includes hasEveryoneVoted)
const testFireworksScenarios = [
    {
        name: "Everyone votes same + everyone voted - should trigger",
        data: {
            votingFinished: true,
            hasEveryoneVoted: true,
            statistics: {
                marks: [{ mark: "5", percentage: 100 }],
                averageMark: 5
            }
        },
        expectedFireworks: true,
        mockEligiblePeople: 3
    },
    {
        name: "Everyone votes same + NOT everyone voted - should NOT trigger",
        data: {
            votingFinished: true,
            hasEveryoneVoted: false, // Key difference!
            statistics: {
                marks: [{ mark: "5", percentage: 100 }],
                averageMark: 5
            }
        },
        expectedFireworks: false,
        mockEligiblePeople: 3
    },
    {
        name: "Split votes + everyone voted - should not trigger",
        data: {
            votingFinished: true,
            hasEveryoneVoted: true,
            statistics: {
                marks: [
                    { mark: "3", percentage: 50 },
                    { mark: "5", percentage: 50 }
                ],
                averageMark: 4
            }
        },
        expectedFireworks: false,
        mockEligiblePeople: 3
    },
    {
        name: "Everyone votes same + single person - should not trigger",
        data: {
            votingFinished: true,
            hasEveryoneVoted: true,
            statistics: {
                marks: [{ mark: "5", percentage: 100 }],
                averageMark: 5
            }
        },
        expectedFireworks: false,
        mockEligiblePeople: 1
    },
    {
        name: "Everyone votes coffee + everyone voted - should trigger",
        data: {
            votingFinished: true,
            hasEveryoneVoted: true,
            statistics: {
                marks: [{ mark: "coffee", percentage: 100 }],
                averageMark: null
            }
        },
        expectedFireworks: true,
        mockEligiblePeople: 2
    },
    {
        name: "Voting not finished - should not trigger (regardless of votes)",
        data: {
            votingFinished: false, // Key difference!
            hasEveryoneVoted: true,
            statistics: {
                marks: [{ mark: "5", percentage: 100 }],
                averageMark: 5
            }
        },
        expectedFireworks: false,
        mockEligiblePeople: 3
    }
];

// Test the fireworks logic
function testFireworksLogic() {
    logFireworks('Starting fireworks logic tests');
    
    // Store original function
    const originalSetVoteResultInfo = window.setVoteResultInfo;
    if (!originalSetVoteResultInfo) {
        logFireworks('setVoteResultInfo function not found - are you on the room page?', 'error');
        return;
    }
    
    let testResults = [];
    
    testFireworksScenarios.forEach((scenario, index) => {
        logFireworks(`Test ${index + 1}: ${scenario.name}`);
        
        // Create mock DOM structure for eligible people count
        const testContainer = document.createElement('div');
        testContainer.style.display = 'none';
        testContainer.innerHTML = `
            <table><tbody id="people-dev">${'<tr></tr>'.repeat(Math.floor(scenario.mockEligiblePeople / 2))}</tbody></table>
            <table><tbody id="people-test">${'<tr></tr>'.repeat(Math.ceil(scenario.mockEligiblePeople / 2))}</tbody></table>
        `;
        document.body.appendChild(testContainer);
        
        // Track if fireworks were triggered
        let fireworksTriggered = false;
        const originalStopFireWorks = window.stopFireWorks;
        window.stopFireWorks = () => {
            fireworksTriggered = true;
            logFireworks(`Fireworks triggered for: ${scenario.name}`, 'success');
        };
        
        try {
            // Call setVoteResultInfo with mock data
            setVoteResultInfo(scenario.data);
            
            // Check result
            const passed = fireworksTriggered === scenario.expectedFireworks;
            testResults.push({
                name: scenario.name,
                expected: scenario.expectedFireworks,
                actual: fireworksTriggered,
                passed: passed
            });
            
            if (passed) {
                logFireworks(`✓ PASS: ${scenario.name}`, 'success');
            } else {
                logFireworks(`✗ FAIL: ${scenario.name} - Expected: ${scenario.expectedFireworks}, Got: ${fireworksTriggered}`, 'error');
            }
            
        } catch (error) {
            logFireworks(`ERROR in ${scenario.name}: ${error.message}`, 'error');
            testResults.push({
                name: scenario.name,
                expected: scenario.expectedFireworks,
                actual: false,
                passed: false,
                error: error.message
            });
        }
        
        // Cleanup
        document.body.removeChild(testContainer);
        window.stopFireWorks = originalStopFireWorks;
    });
    
    // Summary
    const passedTests = testResults.filter(t => t.passed).length;
    const totalTests = testResults.length;
    
    logFireworks('=== TEST SUMMARY ===');
    logFireworks(`${passedTests}/${totalTests} tests passed`, passedTests === totalTests ? 'success' : 'warning');
    
    testResults.forEach(result => {
        const status = result.passed ? '✓' : '✗';
        const message = result.error ? ` (Error: ${result.error})` : '';
        logFireworks(`${status} ${result.name}${message}`, result.passed ? 'success' : 'error');
    });
    
    return testResults;
}

// Test specifically for fist of five error and partial voting
function testFistOfFiveAndPartialVoting() {
    logFireworks('Testing fist of five + partial voting scenarios');
    
    const scenarios = [
        {
            name: "Fist of Five - Everyone voted same",
            data: {
                votingFinished: true,
                hasEveryoneVoted: true,
                statistics: {
                    marks: [{ mark: "0", percentage: 100 }],
                    averageMark: 0
                }
            },
            expectedFireworks: true
        },
        {
            name: "Fist of Five - Not everyone voted (should NOT trigger)",
            data: {
                votingFinished: true,
                hasEveryoneVoted: false,
                statistics: {
                    marks: [{ mark: "0", percentage: 100 }],
                    averageMark: 0
                }
            },
            expectedFireworks: false
        }
    ];
    
    let allPassed = true;
    
    scenarios.forEach(scenario => {
        try {
            // Create mock DOM
            const testContainer = document.createElement('div');
            testContainer.style.display = 'none';
            testContainer.innerHTML = `
                <table><tbody id="people-dev"><tr></tr></tbody></table>
                <table><tbody id="people-test"><tr></tr></tbody></table>
            `;
            document.body.appendChild(testContainer);
            
            // Track fireworks
            let fireworksTriggered = false;
            const originalStopFireWorks = window.stopFireWorks;
            window.stopFireWorks = () => {
                fireworksTriggered = true;
            };
            
            // Test the scenario
            setVoteResultInfo(scenario.data);
            
            const passed = fireworksTriggered === scenario.expectedFireworks;
            if (passed) {
                logFireworks(`✓ PASS: ${scenario.name}`, 'success');
            } else {
                logFireworks(`✗ FAIL: ${scenario.name} - Expected: ${scenario.expectedFireworks}, Got: ${fireworksTriggered}`, 'error');
                allPassed = false;
            }
            
            // Cleanup
            document.body.removeChild(testContainer);
            window.stopFireWorks = originalStopFireWorks;
            
        } catch (error) {
            logFireworks(`✗ ERROR in ${scenario.name}: ${error.message}`, 'error');
            allPassed = false;
        }
    });
    
    if (allPassed) {
        logFireworks('✓ All fist of five + partial voting tests PASSED', 'success');
    } else {
        logFireworks('✗ Some fist of five + partial voting tests FAILED', 'error');
    }
    
    return allPassed;
}

// Export to window for manual testing
window.fireworksTest = {
    testFireworksLogic,
    testFistOfFiveAndPartialVoting
};

// Instructions
console.log(`
🎆 FIREWORKS TEST SUITE LOADED

Test the fireworks logic:
• fireworksTest.testFireworksLogic() - Test all scenarios (including partial voting)
• fireworksTest.testFistOfFiveAndPartialVoting() - Test fist of five + partial voting

Start with: fireworksTest.testFireworksLogic()
`);