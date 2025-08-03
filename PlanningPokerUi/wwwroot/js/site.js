// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your Javascript code.
let personId = ""
let connection
let allChart
let devChart
let testChart
let fireWorks
let fireWorksIntervals = []
let enableLog = false

// Client-side caching configuration
const CACHE_KEYS = {
    ROOM_CREATION_PREFERENCES: 'planningpoker_creation_preferences',
    USER_NAME: 'planningpoker_user_name',
    CARD_SETS: 'planningpoker_card_sets',
    CARD_SETS_TIMESTAMP: 'planningpoker_card_sets_timestamp',
    THEME_PREFERENCE: 'planningpoker_theme_preference'
}
const CARD_SETS_CACHE_DURATION = 24 * 60 * 60 * 1000 // 24 hours in milliseconds

// Function to get display value for a vote mark
let getDisplayValue = function(mark) {
    // Use server-provided mapping if available, otherwise fall back to the mark itself
    if (window.cardSetMapping && window.cardSetMapping[mark]) {
        const displayValue = window.cardSetMapping[mark];
        // For PNG files, return just the mark value for text display contexts
        if (displayValue.endsWith('.png')) {
            return mark;
        }
        return displayValue;
    }
    return mark
}

// Function to get display value as HTML (for contexts that support images)
let getDisplayValueAsHTML = function(mark) {
    if (window.cardSetMapping && window.cardSetMapping[mark]) {
        const displayValue = window.cardSetMapping[mark];
        // For PNG files, return an img tag
        if (displayValue.endsWith('.png')) {
            return `<img src="/${displayValue}" alt="${mark}" style="width: 20px; height: 20px;" />`;
        }
        return displayValue;
    }
    return mark;
}

// Client-side caching utilities
let saveToLocalStorage = function(key, data) {
    try {
        localStorage.setItem(key, JSON.stringify(data));
    } catch (error) {
        console.warn('Failed to save to localStorage:', error);
    }
}

let loadFromLocalStorage = function(key) {
    try {
        const data = localStorage.getItem(key);
        return data ? JSON.parse(data) : null;
    } catch (error) {
        console.warn('Failed to load from localStorage:', error);
        return null;
    }
}

let isCardSetsCacheValid = function() {
    const timestamp = loadFromLocalStorage(CACHE_KEYS.CARD_SETS_TIMESTAMP);
    if (!timestamp) return false;
    
    const now = Date.now();
    return (now - timestamp) < CARD_SETS_CACHE_DURATION;
}

let getCachedCardSets = function() {
    if (isCardSetsCacheValid()) {
        return loadFromLocalStorage(CACHE_KEYS.CARD_SETS);
    }
    return null;
}

let cacheCardSets = function(cardSets) {
    saveToLocalStorage(CACHE_KEYS.CARD_SETS, cardSets);
    saveToLocalStorage(CACHE_KEYS.CARD_SETS_TIMESTAMP, Date.now());
}

let getCachedCreationPreferences = function() {
    return loadFromLocalStorage(CACHE_KEYS.ROOM_CREATION_PREFERENCES);
}

let cacheCreationPreferences = function(preferences) {
    saveToLocalStorage(CACHE_KEYS.ROOM_CREATION_PREFERENCES, preferences);
    console.log('Cached room creation preferences:', preferences);
}

let clearCachedCardSets = function() {
    localStorage.removeItem(CACHE_KEYS.CARD_SETS);
    localStorage.removeItem(CACHE_KEYS.CARD_SETS_TIMESTAMP);
}

let clearCachedCreationPreferences = function() {
    localStorage.removeItem(CACHE_KEYS.ROOM_CREATION_PREFERENCES);
}

let getCachedUserName = function() {
    return loadFromLocalStorage(CACHE_KEYS.USER_NAME);
}

let cacheUserName = function(userName) {
    if (userName && userName.trim()) {
        saveToLocalStorage(CACHE_KEYS.USER_NAME, userName.trim());
        console.log('Cached user name:', userName.trim());
    }
}

let clearCachedUserName = function() {
    localStorage.removeItem(CACHE_KEYS.USER_NAME);
}

let cacheRoomConfig = function(config) {
    // Cache room configuration for potential future use
    try {
        localStorage.setItem('planningpoker_room_config', JSON.stringify(config));
        console.log('Cached room configuration:', config);
    } catch (error) {
        console.warn('Failed to cache room configuration:', error);
    }
}

// Theme management functions
let getThemePreference = function() {
    return loadFromLocalStorage(CACHE_KEYS.THEME_PREFERENCE) || 'auto';
}

let saveThemePreference = function(theme) {
    saveToLocalStorage(CACHE_KEYS.THEME_PREFERENCE, theme);
    console.log('Cached theme preference:', theme);
}

let applyTheme = function(theme) {
    const html = document.documentElement;
    
    // Remove existing theme attributes
    html.removeAttribute('data-theme');
    
    // Apply the selected theme
    if (theme === 'dark') {
        html.setAttribute('data-theme', 'dark');
    } else if (theme === 'light') {
        html.setAttribute('data-theme', 'light');
    }
    // For 'auto', we don't set any attribute and let CSS media queries handle it
    
    // Update charts if they exist
    updateChartsForTheme();
}

let initializeTheme = function() {
    const savedTheme = getThemePreference();
    applyTheme(savedTheme);
    
    // Update the theme toggle to reflect current theme
    const themeSelect = document.getElementById('theme-select');
    if (themeSelect) {
        themeSelect.value = savedTheme;
    }
}

let getCurrentThemeColors = function() {
    const style = getComputedStyle(document.documentElement);
    return {
        primary: style.getPropertyValue('--chart-color-primary').trim(),
        success: style.getPropertyValue('--chart-color-success').trim(),
        textColor: style.getPropertyValue('--text-color').trim(),
        bgColor: style.getPropertyValue('--bg-color').trim()
    };
}

let updateChartsForTheme = function() {
    // Update chart colors when theme changes
    if (typeof allChart !== 'undefined' && allChart) {
        updateChartTheme(allChart);
    }
    if (typeof devChart !== 'undefined' && devChart) {
        updateChartTheme(devChart);
    }
    if (typeof testChart !== 'undefined' && testChart) {
        updateChartTheme(testChart);
    }
}

let updateChartTheme = function(chart) {
    if (!chart || !chart.data || !chart.data.datasets) return;
    
    const colors = getCurrentThemeColors();
    
    // Update chart background colors to use theme-appropriate colors
    const dataset = chart.data.datasets[0];
    if (dataset && dataset.backgroundColor && Array.isArray(dataset.backgroundColor)) {
        dataset.backgroundColor = dataset.backgroundColor.map(color => {
            if (color === 'green') {
                return colors.success;
            } else if (color === 'blue') {
                return colors.primary;
            }
            return color;
        });
    }
    
    // Update chart text color
    if (chart.options && chart.options.scales) {
        if (chart.options.scales.xAxes && chart.options.scales.xAxes[0]) {
            chart.options.scales.xAxes[0].ticks.fontColor = colors.textColor;
        }
        if (chart.options.scales.yAxes && chart.options.scales.yAxes[0]) {
            chart.options.scales.yAxes[0].ticks.fontColor = colors.textColor;
        }
    }
    
    chart.update();
}

let createThemeToggle = function() {
    // This function is now deprecated - theme toggle is in navbar
    // Keeping for backward compatibility but does nothing
    console.log('Theme toggle is now in navbar, createThemeToggle() is deprecated');
}

let getDefaultCreationPreferences = function() {
    return {
        cardSet: 'modified-fibonacci',
        countdownSeconds: 5,
        showFireworks: true,
        maxParticipants: 50,
        useFunRoomName: false
    };
}

let loadCreationPreferences = function() {
    const cached = getCachedCreationPreferences();
    return cached || getDefaultCreationPreferences();
}

let saveCurrentCreationPreferences = function(preferences) {
    // This function can be called with preferences object from the room creation modal
    if (!preferences) {
        // Fallback: try to read from creation modal elements if they exist
        const cardSetElement = document.getElementById('creation-card-set');
        const countdownElement = document.getElementById('creation-countdown-seconds');
        const fireworksElement = document.getElementById('creation-show-fireworks');
        const maxParticipantsElement = document.getElementById('creation-max-participants');
        const funRoomNameElement = document.getElementById('creation-use-fun-room-name');
        
        preferences = {
            cardSet: cardSetElement ? cardSetElement.value : 'modified-fibonacci',
            countdownSeconds: countdownElement ? parseInt(countdownElement.value) || 5 : 5,
            showFireworks: fireworksElement ? fireworksElement.checked : true,
            maxParticipants: maxParticipantsElement ? parseInt(maxParticipantsElement.value) || 50 : 50,
            useFunRoomName: funRoomNameElement ? funRoomNameElement.checked : false
        };
    }
    
    cacheCreationPreferences(preferences);
    return preferences;
}

// Debug function to inspect cached creation preferences
let debugCachedPreferences = function() {
    console.log('=== Cached Room Creation Preferences ===');
    const prefs = getCachedCreationPreferences();
    const userName = getCachedUserName();
    console.log('Creation Preferences:', prefs);
    console.log('Cached User Name:', userName);
    console.log('=========================================');
}

let connectToRoom = function (guid, personGuid) {
    personId = personGuid;
    
    connection = new signalR.HubConnectionBuilder()
        .withUrl("/roomhub")
        .withAutomaticReconnect([0, 2000, 10000, 30000]) // Retry after 0ms, 2s, 10s, 30s
        .configureLogging(signalR.LogLevel.Information)
        .build();

    // Set up event handlers
    setupSignalRHandlers(guid);
    setupConnectionStateHandlers(guid);

    startConnection(guid);
}

let startConnection = function(guid) {
    connection.start().then(function () {
        console.log("SignalR connected");
        updateConnectionStatus("connected");
        joinRoom(guid);
    }).catch(function (err) {
        console.error("Error starting SignalR connection: " + err.toString());
        updateConnectionStatus("disconnected");
        // Retry connection after 5 seconds
        setTimeout(function() {
            console.log("Retrying connection...");
            startConnection(guid);
        }, 5000);
    });
}

let joinRoom = function(guid) {
    if (connection && connection.state === signalR.HubConnectionState.Connected) {
        connection.invoke("JoinRoom", guid).catch(function (err) {
            console.error("Error joining room: " + err.toString());
        });
    }
}

let setupSignalRHandlers = function(guid) {
    // Room joined successfully
    connection.on("RoomJoined", function (data) {
        console.log("RoomJoined event received:", data);
        console.log("isSuccessful:", data.isSuccessful);
        console.log("people array:", data.people);
        console.log("people count:", data.people ? data.people.length : 0);
        
        if (data.isSuccessful) {
            console.log("About to add people to table");
            addPeopleToTable(data.people)
            setVoteResultInfo(data.voteResultInfo)

            $('#people-dev').append($('#people-dev > tr').sort(sortByIdProp))
            $('#people-test').append($('#people-test > tr').sort(sortByIdProp))
            $('#observers').append($('#observers > tr').sort(sortByIdProp))
            console.log("Finished adding people to table");
        } else if (data.error) {
            console.error("Failed to join room:", data.error);
            alert(data.error);
            // Optionally redirect to home page
            // window.location.href = "/";
        } else {
            console.warn("Room join was not successful but no error provided");
        }
    });

    // Person joined
    connection.on("PersonJoined", function (data) {
        if (enableLog) {
            console.log("PersonJoined", data);
        }
        addPeopleToTable([data.person])
        if (data.vote) {
            setVotes([data.vote])
        }

        if (data.person.personType == "test") {
            $('#people-test').append($('#people-test > tr').sort(sortByIdProp))
        }
        else if (data.person.personType == "obs") {
            $('#observers').append($('#observers > tr').sort(sortByIdProp))
        } else {
            $('#people-dev').append($('#people-dev > tr').sort(sortByIdProp))
        }
    });

    // Person exited
    connection.on("PersonExited", function (person) {
        if (enableLog) {
            console.log("PersonExited", person);
        }
        removePeopleFromTable([person])
    });

    // Vote cast
    connection.on("VoteCast", function (personGuid) {
        if (enableLog) {
            console.log("VoteCast", personGuid);
        }
        $("#" + personGuid + " .mark").text("\u25AE")
    });

    // Vote status update (new event to track voting status)
    connection.on("VoteStatusUpdate", function (voteResultInfo) {
        if (enableLog) {
            console.log("VoteStatusUpdate", voteResultInfo);
        }
        setVoteResultInfo(voteResultInfo)
    });

    // Votes shown
    connection.on("VotesShown", function (voteResultInfo) {
        if (enableLog) {
            console.log("VotesShown", voteResultInfo);
        }
        setVoteResultInfo(voteResultInfo)
    });

    // Votes cleared
    connection.on("VotesCleared", function () {
        if (enableLog) {
            console.log("VotesCleared");
        }
        $("#statistics").hide();
        $("#show-votes-countdown").hide();
        $(".mark").text("")
        $("input[type=radio][name=mark]").prop("checked", false)
        $("#averageMark").text("")
        disableVoting(false)
        stopFireWorks()

        allChart.data.labels = []
        allChart.data.datasets[0].data = []
        allChart.data.datasets[0].backgroundColor = []
        allChart.update()

        devChart.data.labels = []
        devChart.data.datasets[0].data = []
        devChart.data.datasets[0].backgroundColor = []
        devChart.update()

        testChart.data.labels = []
        testChart.data.datasets[0].data = []
        testChart.data.datasets[0].backgroundColor = []
        testChart.update()
    });

    // Countdown update
    connection.on("CountdownUpdate", function (data) {
        if (enableLog) {
            console.log("CountdownUpdate", data);
        }
        if (data.reset) {
            $("#show-votes-countdown").hide();
        } else {
            $("#show-votes-countdown").show();
            $("#countdown").text(data.countdown)
        }
    });

    // Countdown reset
    connection.on("CountdownReset", function (data) {
        if (enableLog) {
            console.log("CountdownReset", data);
        }
        $("#show-votes-countdown").hide();
    });

    // Person type changed
    connection.on("PersonTypeChanged", function (data) {
        if (enableLog) {
            console.log("PersonTypeChanged", data);
        }
        $("#" + data.person.guid).remove()
        addPeopleToTable([data.person])
        setVoteResultInfo(data.voteResultInfo)
    });

    // Health check request
    connection.on("HealthCheckRequest", function () {
        if (enableLog) {
            console.log("HealthCheckRequest");
        }
        connection.invoke("HealthCheck").catch(function (err) {
            console.error("Error responding to health check: " + err.toString());
        });
    });

    // Room configuration updated
    connection.on("RoomConfigurationUpdated", function (data) {
        if (enableLog) {
            console.log("RoomConfigurationUpdated", data);
        }
        
        // Update and cache local configuration
        window.roomConfiguration = data.configuration;
        cacheRoomConfig(data.configuration);
        
        // Show notification
        if (data.updatedBy) {
            console.log(`Room settings updated by ${data.updatedBy}`);
        }
        
        // Clear votes if they are currently shown
        if ($("#statistics").is(":visible")) {
            // Clear votes on the server
            if (connection && connection.state === signalR.HubConnectionState.Connected) {
                connection.invoke("ClearVotes").catch(function (err) {
                    console.error("Error clearing votes after settings change: " + err.toString());
                });
            }
        }
        
        // Update card set if it changed and card details are provided
        if (data.configuration.cardSet && data.cardSetDetails) {
            updateCardUIFromSignalR(data.cardSetDetails);
            updateCardSetMapping(data.cardSetDetails);
            // Clear card sets cache to force refresh on next load
            clearCachedCardSets();
        }
    });
    
    // Ownership transferred
    connection.on("OwnershipTransferred", function (data) {
        if (enableLog) {
            console.log("OwnershipTransferred", data);
        }
        
        // Update UI dynamically without showing popup
        updateOwnershipUI(data.newOwner);
    });
    
    // Room disposed
    connection.on("RoomDisposed", function (data) {
        if (enableLog) {
            console.log("RoomDisposed", data);
        }
        
        alert(data.message || "Room has been closed.");
        window.location.href = "/";
    });
    
    // Set up event handlers for person type changes
    $("input[type=radio][name=personType]").change(function () {
        if (connection && connection.state === signalR.HubConnectionState.Connected) {
            connection.invoke("ChangePersonType", this.value).catch(function (err) {
                console.error("Error changing person type: " + err.toString());
            });
        }
    });

    // Create charts
    allChart = createChart("allChart");
    devChart = createChart("devChart");
    testChart = createChart("testChart");
}

let setupConnectionStateHandlers = function(guid) {
    // Connection closed (will attempt to reconnect automatically)
    connection.onclose(function(error) {
        console.log("SignalR connection closed:", error);
        updateConnectionStatus("disconnected");
    });

    // Connection reconnecting
    connection.onreconnecting(function(error) {
        console.log("SignalR reconnecting...", error);
        updateConnectionStatus("reconnecting");
    });

    // Connection reconnected successfully
    connection.onreconnected(function(connectionId) {
        console.log("SignalR reconnected with connection ID:", connectionId);
        updateConnectionStatus("connected");
        
        // Rejoin the room after reconnection
        console.log("Rejoining room after reconnection...");
        joinRoom(guid);
    });
}

let updateConnectionStatus = function(status) {
    const statusElement = $("#connection-status");
    
    // Create status element if it doesn't exist
    if (statusElement.length === 0) {
        $("body").prepend('<div id="connection-status" class="connection-status"></div>');
    }
    
    const statusDiv = $("#connection-status");
    statusDiv.removeClass("connected disconnected reconnecting");
    
    switch(status) {
        case "connected":
            statusDiv.addClass("connected")
                     .text("Connected")
                     .fadeOut(2000); // Hide after 2 seconds when connected
            break;
        case "disconnected":
            statusDiv.addClass("disconnected")
                     .text("Disconnected - Attempting to reconnect...")
                     .show();
            break;
        case "reconnecting":
            statusDiv.addClass("reconnecting")
                     .text("Reconnecting...")
                     .show();
            break;
    }
}

// Cache for loaded images to avoid reloading
let chartImageCache = {};

// Preload images for the current card set
let preloadCardSetImages = function() {
    if (!window.cardSetMapping) return;
    
    Object.values(window.cardSetMapping).forEach(displayValue => {
        if (displayValue && displayValue.endsWith('.png')) {
            const imagePath = '/' + displayValue;
            if (!chartImageCache[imagePath]) {
                const img = new Image();
                chartImageCache[imagePath] = img;
                img.src = imagePath;
            }
        }
    });
}

let drawImagesOnChart = function(chart) {
    const ctx = chart.chart.ctx;
    const xAxis = chart.scales['x-axis-0'];
    const yAxis = chart.scales['y-axis-0'];
    
    if (!chart.data.labels || chart.data.labels.length === 0) return;
    
    chart.data.labels.forEach((label, index) => {
        if (window.cardSetMapping && window.cardSetMapping[label] && window.cardSetMapping[label].endsWith('.png')) {
            const imagePath = '/' + window.cardSetMapping[label];
            
            // Check if image is already cached
            if (chartImageCache[imagePath] && chartImageCache[imagePath].complete) {
                const img = chartImageCache[imagePath];
                const x = xAxis.getPixelForValue(label);
                const imageSize = 24;
                const imageX = x - imageSize / 2;
                const imageY = xAxis.bottom - imageSize - 5; // Position below the axis
                
                ctx.drawImage(img, imageX, imageY, imageSize, imageSize);
            } else if (!chartImageCache[imagePath]) {
                // Create and cache image element
                const img = new Image();
                chartImageCache[imagePath] = img;
                img.onload = function() {
                    // Redraw the chart to show the newly loaded image
                    chart.update('none');
                };
                img.src = imagePath;
            }
        }
    });
}

let createChart = function (name) {
    const themeColors = getCurrentThemeColors();
    
    return new Chart(name, {
        type: "bar",
        data: {
            labels: [],
            datasets: [{
                backgroundColor: [],
                data: []
            }]
        },
        options: {
            tooltips: {
                callbacks: {
                    label: function (tooltipItem, data) {
                        return tooltipItem.value + "%";
                    }
                },
                backgroundColor: themeColors.bgColor || '#000',
                titleFontColor: themeColors.textColor || '#fff',
                bodyFontColor: themeColors.textColor || '#fff'
            },
            legend: {
                display: false
            },
            scales: {
                yAxes: [{
                    ticks: {
                        beginAtZero: true,
                        suggestedMax: 100,
                        display: false,
                        fontColor: themeColors.textColor || '#666'
                    },
                    gridLines: {
                        display: false
                    }
                }],
                xAxes: [{
                    ticks: {
                        fontSize: 16,
                        fontColor: themeColors.textColor || '#666',
                        callback: function (value) {
                            // For PNG files, return empty string to hide text labels
                            if (window.cardSetMapping && window.cardSetMapping[value] && window.cardSetMapping[value].endsWith('.png')) {
                                return '';
                            }
                            return getDisplayValue(value)
                        }
                    },
                    gridLines: {
                        display: false
                    }
                }]
            },
            animation: {
                onComplete: function() {
                    drawImagesOnChart(this);
                }
            },
            hover: {
                onHover: function(event, activeElements) {
                    // Ensure images are redrawn on hover
                    drawImagesOnChart(this);
                }
            }
        },
        plugins: [{
            afterDraw: function(chart) {
                drawImagesOnChart(chart);
            }
        }]
    })
}

let disableVoting = function (value) {
    $('input[type=radio][name=mark]').prop('disabled', value)
}

let sortByIdProp = function (a, b) {
    let aId = $(a).prop("id")
    let bId = $(b).prop("id")
    if (aId < bId) {
        return -1
    } else if (aId > bId) {
        return 1
    }
    return 0;
}

let setIndividualStatistics = function (marks, highestMark, chart) {
    if (!marks || !chart) return;
    
    let labels = []
    let percentages = []
    let colors = []
    
    // Get theme-appropriate colors
    const themeColors = getCurrentThemeColors();

    for (let mark of marks) {
        labels.push(mark.mark) // Use the actual mark value as label
        percentages.push(mark.percentage)
        if (highestMark == mark.mark) {
            colors.push(themeColors.success || "#28a745")
        }
        else {
            colors.push(themeColors.primary || "#007bff")
        }
    }

    chart.data.labels = labels
    chart.data.datasets[0].data = percentages
    chart.data.datasets[0].backgroundColor = colors
    chart.update()
}

let setStatistics = function (statistics) {
    if (!statistics) return;
    
    try {
    
    if (statistics.marks) {
        setIndividualStatistics(statistics.marks, statistics.highestMark, allChart)
    }
    if (statistics.marksDev) {
        setIndividualStatistics(statistics.marksDev, statistics.highestMarkDev, devChart)
    }
    if (statistics.marksTest) {
        setIndividualStatistics(statistics.marksTest, statistics.highestMarkTest, testChart)
    }

    $("#allAverageMark").text(statistics.averageMark || "")
    $("#devAverageMark").text(statistics.averageMarkDev || "")
    $("#testAverageMark").text(statistics.averageMarkTest || "")

    // Fireworks logic has been moved to setVoteResultInfo where we have access to hasEveryoneVoted
    
    } catch (error) {
        console.error("Error in setStatistics:", error);
        // Continue gracefully - don't let fireworks errors break the statistics display
    }
}

let groupBy = (items, keySelector) => items.reduce(
    (group, arr) => {
        let key = keySelector(arr)
        group[key] = group[key] ?? [];
        group[key].push(arr)
        return group
    },
    {},
);

let stopFireWorks = function () {
    try {        
        fireWorks.stop()        
    } catch {
        // nothing to do
    }
    clearFireWorksIntervals()
}

let clearFireWorksIntervals = function () {
    while (fireWorksIntervals.length > 0) {
        let fireWorksInterval = fireWorksIntervals.pop()
        clearInterval(fireWorksInterval);
    }
}

let setVoteResultInfo = function (voteResultInfo) {
    if (!voteResultInfo) return;
    
    if (voteResultInfo.votes) {
        setVotes(voteResultInfo.votes)
    }
    
    if (voteResultInfo.votingFinished) {
        $("#statistics").show()
        $("#show-votes-countdown").hide()
        if (voteResultInfo.statistics) {
            setStatistics(voteResultInfo.statistics)
            
            // Trigger fireworks if everyone voted the same thing
            // Only when results are shown AND everyone actually voted
            if (voteResultInfo.statistics.marks && voteResultInfo.statistics.marks.length === 1 
                && voteResultInfo.statistics.marks[0].percentage === 100
                && voteResultInfo.hasEveryoneVoted) {
                
                // Count people who can vote (exclude observers)
                let eligiblePeople = $("#people-dev tr, #people-test tr").length;
                
                if (eligiblePeople > 1) {
                    // Everyone voted for the same mark - trigger fireworks!
                    stopFireWorks();
                    let c = 0;
                    fireWorksIntervals.push(setInterval(function () {
                        try {
                            c++;
                            if (!document.hidden) {
                                fireWorks.launch(10)
                            }
                            if (c >= 10) {
                                clearFireWorksIntervals()
                            }
                        }
                        catch {
                            clearFireWorksIntervals()
                        }
                    }, 1000))
                }
            }
        }
        // ONLY disable voting when results are actually shown
        disableVoting(true)
    } else {
        $("#statistics").hide();
        if (voteResultInfo.hasEveryoneVoted) {
            $("#show-votes-countdown").show();
            $("#countdown").text(voteResultInfo.countdown)
            // IMPORTANT: Allow vote changes during countdown - do NOT disable voting
            // disableVoting(true) -- REMOVED
        } else {
            $("#show-votes-countdown").hide();
            // Enable voting when not everyone has voted
            disableVoting(false)
        }
    }
}

let setVotes = function (votes) {
    for (let vote of votes) {
        if ($("#" + vote.guid)[0]) {
            let markElement = $("#" + vote.guid + " .mark")
            if (vote.mark == 'hide') {
                markElement.text("\u25AE")
            }
            else {
                // Use HTML version to support images
                markElement.html(getDisplayValueAsHTML(vote.mark))
            }
        }
    }
}

let addPeopleToTable = function (otherPeople) {
    console.log("addPeopleToTable called with:", otherPeople);
    
    if (!otherPeople || otherPeople.length === 0) {
        console.log("No people to add to table");
        return;
    }
    
    let peopleDev = $("#people-dev")
    let peopleTest = $("#people-test")
    let observers = $("#observers")
    
    console.log("Table elements found:", {
        peopleDev: peopleDev.length,
        peopleTest: peopleTest.length,
        observers: observers.length
    });
    
    for (let otherPerson of otherPeople) {
        console.log("Processing person:", otherPerson);
        console.log("Person details:", {
            guid: otherPerson.guid,
            name: otherPerson.name,
            personType: otherPerson.personType
        });
        
        if (!($("#" + otherPerson.guid)[0])) {
            let tr = $("<tr/>")
            tr.attr("id", otherPerson.guid)

            let tdName = $("<td/>")
            tdName.attr("class", "name")
            tdName.text(unescape(otherPerson.name))

            tr.append(tdName)

            // Default to dev if PersonType is null/empty
            let personType = otherPerson.personType || "dev";
            
            if (personType == "obs") {
                console.log("Adding person to observers table");
                observers.append(tr)
            } else {
                let tdMark = $("<td/>")
                tdMark.attr("class", "mark")
                tr.append(tdMark)

                if (personType == "test") {
                    console.log("Adding person to test table");
                    peopleTest.append(tr)
                } else {
                    console.log("Adding person to dev table (default)");
                    peopleDev.append(tr)
                }
            }
        } else {
            console.log("Person already exists in table:", otherPerson.guid);
        }
    }
}

let removePeopleFromTable = function (otherPeople) {
    for (let otherPerson of otherPeople) {
        $("#" + otherPerson.guid).remove()
    }
}

$(document).ready(function () {
    // Preload images for the current card set
    preloadCardSetImages();
    
    $('input[type=radio][name=mark]').change(function () {
        if (!this.value)
            return;
        
        if (connection && connection.state === signalR.HubConnectionState.Connected) {
            connection.invoke("Vote", this.value).catch(function (err) {
                console.error("Error voting: " + err.toString());
            });
        }
    })

    $('#clear-votes').click(function () {
        stopFireWorks();
        if (connection && connection.state === signalR.HubConnectionState.Connected) {
            connection.invoke("ClearVotes").catch(function (err) {
                console.error("Error clearing votes: " + err.toString());
            });
        }
    })

    $('#show-votes').click(function () {
        console.log("Show votes button clicked");
        stopFireWorks();
        if (connection && connection.state === signalR.HubConnectionState.Connected) {
            console.log("Invoking ForceShowVotes");
            connection.invoke("ForceShowVotes").catch(function (err) {
                console.error("Error showing votes: " + err.toString());
            });
        } else {
            console.error("Connection not ready for ForceShowVotes");
        }
    })

    // Room settings functionality
    $('#room-settings-btn').click(function () {
        showRoomSettings();
    });

    $('#close-settings, #cancel-settings').click(function () {
        hideRoomSettings();
    });

    $('#save-settings').click(function () {
        saveRoomSettings();
    });
    
    $('#transfer-ownership-btn').click(function () {
        transferOwnership();
    });

    // Room creation preferences are handled by the creation modal

    $("#statistics").hide();
    $("#show-votes-countdown").hide();

    try {
        fireWorks = new Fireworks.default($('.fireworks')[0])
    } catch {
        // nothing to do
    }
})

let showRoomSettings = function() {
    $('<div class="room-settings-overlay"></div>').appendTo('body');
    
    // Populate transfer ownership dropdown
    populateOwnershipTransferDropdown();
    
    // Load card sets (prioritize cache)
    loadCardSetsForRoomSettings();
    
    $('#room-settings').show();
}

let hideRoomSettings = function() {
    $('#room-settings').hide();
    $('.room-settings-overlay').remove();
}

let loadRoomSettings = function(config) {
    $('#card-set-select').val(config.cardSet || 'modified-fibonacci');
    $('#countdown-seconds').val(config.countdownSeconds !== undefined ? config.countdownSeconds : 5);
    $('#show-fireworks').prop('checked', config.showFireworks !== false);
    $('#max-participants').val(config.maxParticipants || 50);
}

let saveRoomSettings = function() {
    const countdownSeconds = parseInt($('#countdown-seconds').val());
    const cardSetValue = $('#card-set-select').val();
    
    console.log('=== SAVING ROOM SETTINGS ===');
    console.log('Card set dropdown value:', cardSetValue);
    console.log('Countdown seconds:', countdownSeconds);
    console.log('Show fireworks:', $('#show-fireworks').is(':checked'));
    console.log('Max participants:', parseInt($('#max-participants').val()));
    
    const config = {
        cardSet: cardSetValue,
        countdownSeconds: countdownSeconds,
        showFireworks: $('#show-fireworks').is(':checked'),
        maxParticipants: parseInt($('#max-participants').val())
    };

    console.log('Final config object:', config);

    // Cache the configuration locally
    cacheRoomConfig(config);

    if (connection && connection.state === signalR.HubConnectionState.Connected) {
        console.log('Invoking UpdateRoomConfiguration with config:', config);
        connection.invoke("UpdateRoomConfiguration", config).then(function () {
            console.log('UpdateRoomConfiguration succeeded');
            hideRoomSettings();
            // Update local configuration
            window.roomConfiguration = config;
        }).catch(function (err) {
            console.error("Error updating room configuration: " + err.toString());
            alert("Failed to update room settings. Please try again.");
        });
    } else {
        console.error('SignalR connection not ready. State:', connection ? connection.state : 'No connection');
        alert("Connection error. Please try again.");
    }
}

let updateCardUIFromSignalR = function(cards) {
    // Find the card container
    const cardContainer = document.querySelector('.table.form-row.align-items-center.noselect');
    if (!cardContainer) return;
    
    // Clear existing cards
    cardContainer.innerHTML = '';
    
    // Add new cards
    cards.forEach(card => {
        const span = document.createElement('span');
        
        const input = document.createElement('input');
        input.type = 'radio';
        input.value = card.value;
        input.id = card.value;
        input.name = 'mark';
        
        const label = document.createElement('label');
        label.setAttribute('for', card.value);
        
        // Check if display value is a PNG filename
        if (card.display && card.display.endsWith('.png')) {
            const img = document.createElement('img');
            img.src = '/' + card.display;
            img.alt = card.value;
            img.style.width = '40px';
            img.style.height = '40px';
            label.appendChild(img);
        } else {
            label.textContent = card.display || card.value;
        }
        
        span.appendChild(input);
        span.appendChild(label);
        cardContainer.appendChild(span);
    });
    
    // Re-attach event handlers for the new radio buttons
    $('input[type=radio][name=mark]').change(function () {
        if (!this.value)
            return;
        
        if (connection && connection.state === signalR.HubConnectionState.Connected) {
            connection.invoke("Vote", this.value).catch(function (err) {
                console.error("Error voting: " + err.toString());
            });
        }
    });
}

let updateCardSetMapping = function(cards) {
    // Update the global card set mapping
    window.cardSetMapping = {};
    cards.forEach(card => {
        window.cardSetMapping[card.value] = card.display || card.value;
    });
    
    // Preload images for the new card set
    preloadCardSetImages();
}

let updateOwnershipUI = function(newOwner) {
    const settingsButton = $('#room-settings-btn');
    
    // Use camelCase property name (from JSON serialization)
    const newOwnerGuid = newOwner?.guid;
    
    if (newOwner && newOwnerGuid === personId) {
        // Current user is now the owner - show settings button
        if (settingsButton.length === 0) {
            // Add settings button if it doesn't exist with proper spacing
            const buttonHtml = ' <input id="room-settings-btn" type="button" value="⚙️ Settings" />';
            $('.form-row.align-items-center.noselect').find('input[value="Show votes"]').after(buttonHtml);
            
            // Attach event handler
            $('#room-settings-btn').click(function () {
                showRoomSettings();
            });
        } else {
            // Show existing settings button
            settingsButton.show();
        }
    } else {
        // Current user is no longer the owner - hide settings button
        settingsButton.hide();
    }
}

let populateOwnershipTransferDropdown = function() {
    const dropdown = $('#transfer-ownership-select');
    dropdown.empty();
    dropdown.append('<option value="">Select person to transfer ownership to...</option>');
    
    // Get all people in the room (excluding current user)
    const allPeople = $('#people-dev tr, #people-test tr, #observers tr');
    allPeople.each(function() {
        const currentPersonId = $(this).attr('id');
        const personName = $(this).find('td:first').text();
        
        // Exclude current user by comparing to the global personId variable
        if (currentPersonId && personName && currentPersonId !== personId) {
            dropdown.append(`<option value="${currentPersonId}">${personName}</option>`);
        }
    });
}

let transferOwnership = function() {
    const targetPersonGuid = $('#transfer-ownership-select').val();
    
    if (!targetPersonGuid) {
        return;
    }
    
    if (connection && connection.state === signalR.HubConnectionState.Connected) {
        connection.invoke("TransferOwnership", targetPersonGuid).then(function () {
            hideRoomSettings();
        }).catch(function (err) {
            console.error("Error transferring ownership: " + err.toString());
        });
    }
}

let loadCardSetsForRoomSettings = function() {
    // Try to load from cache first
    const cachedCardSets = getCachedCardSets();
    
    if (cachedCardSets) {
        console.log('Loading card sets from cache');
        populateCardSetDropdown(cachedCardSets);
        return;
    }
    
    // Show loading state
    const cardSetSelect = $('#card-set-select');
    cardSetSelect.html('<option value="">Loading card sets...</option>');
    
    // Load from API if cache is empty or expired
    console.log('Loading card sets from API');
    fetch('/api/cardsets')
        .then(response => {
            if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }
            return response.json();
        })
        .then(cardSets => {
            // Cache the result
            cacheCardSets(cardSets);
            populateCardSetDropdown(cardSets);
            console.log('Card sets loaded and cached successfully');
        })
        .catch(error => {
            console.error('Error loading card sets for room settings:', error);
            cardSetSelect.html('<option value="">Failed to load card sets</option>');
        });
}

let populateCardSetDropdown = function(cardSets) {
    const cardSetSelect = $('#card-set-select');
    cardSetSelect.empty();
    
    cardSets.forEach(cardSet => {
        const option = $('<option></option>');
        option.attr('value', cardSet.value);
        option.text(cardSet.display);
        cardSetSelect.append(option);
    });
    
    // Set the current card set selection if configuration exists
    if (window.roomConfiguration && window.roomConfiguration.cardSet) {
        cardSetSelect.val(window.roomConfiguration.cardSet);
    } else {
        cardSetSelect.val('modified-fibonacci');
    }
}

// Index page functionality
let initializeIndexPage = function() {
    // Function to cache user name synchronously (client + server)
    function cacheUserNameSync(userName) {
        console.log('=== CACHING USER NAME ===');
        console.log('User name to cache:', userName);
        
        // Method 1: Try site.js function
        if (typeof cacheUserName === 'function') {
            try {
                cacheUserName(userName);
                console.log('✓ Method 1: Cached via site.js function');
            } catch (error) {
                console.error('✗ Method 1 failed:', error);
            }
        }
        
        // Method 2: Direct localStorage (always try this as backup)
        try {
            localStorage.setItem('planningpoker_user_name', JSON.stringify(userName));
            console.log('✓ Method 2: Cached via direct localStorage');
        } catch (error) {
            console.error('✗ Method 2 failed:', error);
        }
        
        // Method 3: Try without JSON.stringify
        try {
            localStorage.setItem('planningpoker_user_name_simple', userName);
            console.log('✓ Method 3: Cached simple string version');
        } catch (error) {
            console.error('✗ Method 3 failed:', error);
        }
        
        // Method 4: Update server-side session
        updateServerSideName(userName);
        
        // Verification
        setTimeout(() => {
            try {
                const saved1 = localStorage.getItem('planningpoker_user_name');
                const saved2 = localStorage.getItem('planningpoker_user_name_simple');
                console.log('=== VERIFICATION ===');
                console.log('JSON version saved:', saved1);
                console.log('Simple version saved:', saved2);
                console.log('All localStorage keys:', Object.keys(localStorage));
            } catch (error) {
                console.error('Verification failed:', error);
            }
        }, 10);
    }
    
    // Function to update server-side session name
    function updateServerSideName(userName) {
        console.log('=== UPDATING SERVER-SIDE SESSION ===');
        console.log('Updating server session with name:', userName);
        
        fetch('/api/updatename', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ name: userName })
        })
        .then(response => {
            if (response.ok) {
                return response.json();
            }
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        })
        .then(data => {
            console.log('✓ Server-side session updated:', data);
        })
        .catch(error => {
            console.error('✗ Failed to update server-side session:', error);
        });
    }

    // Functions to handle cached preferences
    function loadCachedPreferences() {
        // Check if the functions from site.js are available
        if (typeof loadCreationPreferences === 'function') {
            const preferences = loadCreationPreferences();
            console.log('Loading cached room creation preferences:', preferences);
            
            // Apply cached preferences to form elements
            const cardSetSelect = document.getElementById('creation-card-set');
            const countdownInput = document.getElementById('creation-countdown-seconds');
            const funRoomNameCheckbox = document.getElementById('creation-use-fun-room-name');
            const fireworksCheckbox = document.getElementById('creation-show-fireworks');
            const maxParticipantsInput = document.getElementById('creation-max-participants');
            
            if (cardSetSelect && preferences.cardSet) {
                cardSetSelect.value = preferences.cardSet;
            }
            if (countdownInput && preferences.countdownSeconds !== undefined) {
                countdownInput.value = preferences.countdownSeconds;
                console.log('Modal: Set countdown to:', preferences.countdownSeconds);
            }
            if (funRoomNameCheckbox && preferences.useFunRoomName !== undefined) {
                funRoomNameCheckbox.checked = preferences.useFunRoomName;
            }
            if (fireworksCheckbox && preferences.showFireworks !== undefined) {
                fireworksCheckbox.checked = preferences.showFireworks;
            }
            if (maxParticipantsInput && preferences.maxParticipants !== undefined) {
                maxParticipantsInput.value = preferences.maxParticipants;
            }
        }
    }
    
    function saveCachedPreferences() {
        const countdownValue = document.getElementById('creation-countdown-seconds').value;
        const preferences = {
            cardSet: document.getElementById('creation-card-set').value,
            countdownSeconds: countdownValue !== '' ? parseInt(countdownValue) : 5,
            useFunRoomName: document.getElementById('creation-use-fun-room-name').checked,
            showFireworks: document.getElementById('creation-show-fireworks').checked,
            maxParticipants: parseInt(document.getElementById('creation-max-participants').value) || 50
        };
        
        console.log('About to save preferences:', preferences);
        
        // Use the caching function from site.js if available
        if (typeof cacheCreationPreferences === 'function') {
            cacheCreationPreferences(preferences);
            console.log('Saved room creation preferences to cache:', preferences);
        }
    }

    function showRoomCreationSettings() {
        const roomCreationSettings = document.getElementById('room-creation-settings');
        const overlay = document.createElement('div');
        overlay.className = 'room-settings-overlay';
        overlay.onclick = hideRoomCreationSettings;
        document.body.appendChild(overlay);
        
        // Load cached preferences when showing the modal
        loadCachedPreferences();
        
        roomCreationSettings.style.display = 'block';
    }

    function hideRoomCreationSettings() {
        const roomCreationSettings = document.getElementById('room-creation-settings');
        roomCreationSettings.style.display = 'none';
        const overlay = document.querySelector('.room-settings-overlay');
        if (overlay) {
            overlay.remove();
        }
    }

    function saveRoomCreationSettings() {
        const roomCreationSettingsBtn = document.getElementById('room-creation-settings-btn');
        
        // Save current preferences to cache
        saveCachedPreferences();
        
        // Visual feedback
        roomCreationSettingsBtn.innerHTML = '✅ Settings Applied';
        setTimeout(() => {
            roomCreationSettingsBtn.innerHTML = '⚙️ More Settings';
        }, 2000);
    }

    function injectRoomCreationValues() {
        const createRoomForm = document.getElementById('createRoom');
        
        // Remove any existing injected fields to avoid duplicates
        const existingFields = createRoomForm.querySelectorAll('.injected-field');
        existingFields.forEach(field => field.remove());

        // Create and inject form fields with current modal values
        const settings = [
            { name: 'cardSet', value: document.getElementById('creation-card-set').value },
            { name: 'countdownSeconds', value: document.getElementById('creation-countdown-seconds').value },
            { name: 'useFunRoomName', value: document.getElementById('creation-use-fun-room-name').checked },
            { name: 'showFireworks', value: document.getElementById('creation-show-fireworks').checked },
            { name: 'maxParticipants', value: document.getElementById('creation-max-participants').value }
        ];

        settings.forEach(setting => {
            const input = document.createElement('input');
            input.type = 'hidden';
            input.name = setting.name;
            input.value = setting.value;
            input.className = 'injected-field';
            createRoomForm.appendChild(input);
        });
    }

    function loadCardSets() {
        // Try to load from cache first (using site.js functions if available)
        if (typeof getCachedCardSets === 'function') {
            const cachedCardSets = getCachedCardSets();
            if (cachedCardSets) {
                console.log('Loading card sets from cache');
                populateCardSetDropdownForCreation(cachedCardSets);
                // Load initial preferences after card sets are populated
                setTimeout(loadInitialPreferences, 100);
                return;
            }
        }
        
        // Load from API if no cache available
        console.log('Loading card sets from API');
        const cardSetSelect = document.getElementById('creation-card-set');
        cardSetSelect.innerHTML = '<option value="">Loading card sets...</option>';
        
        fetch('/api/cardsets')
            .then(response => {
                if (!response.ok) {
                    throw new Error(`HTTP ${response.status}: ${response.statusText}`);
                }
                return response.json();
            })
            .then(cardSets => {
                // Cache the result if caching function is available
                if (typeof cacheCardSets === 'function') {
                    cacheCardSets(cardSets);
                }
                
                populateCardSetDropdownForCreation(cardSets);
                // Load initial preferences after card sets are populated
                setTimeout(loadInitialPreferences, 100);
            })
            .catch(error => {
                console.error('Error loading card sets:', error);
                cardSetSelect.innerHTML = '<option value="">Failed to load card sets</option>';
            });
    }
    
    function populateCardSetDropdownForCreation(cardSets) {
        const cardSetSelect = document.getElementById('creation-card-set');
        cardSetSelect.innerHTML = ''; // Clear existing options
        
        cardSets.forEach(cardSet => {
            const option = document.createElement('option');
            option.value = cardSet.value;
            option.textContent = cardSet.display;
            cardSetSelect.appendChild(option);
        });
    }
    
    function loadCachedUserName() {
        // Load cached user name if available
        console.log('Attempting to load cached user name...');
        
        if (typeof getCachedUserName === 'function') {
            const cachedName = getCachedUserName();
            console.log('getCachedUserName returned:', cachedName);
            
            if (cachedName) {
                const nameInput = document.getElementById('name');
                console.log('Name input element:', nameInput);
                console.log('Current name input value:', nameInput?.value);
                
                if (nameInput && !nameInput.value.trim()) {
                    nameInput.value = cachedName;
                    console.log('Successfully loaded cached user name:', cachedName);
                } else if (nameInput) {
                    console.log('Name input already has a value, not overwriting');
                }
            } else {
                console.log('No cached user name found');
            }
        } else {
            console.log('getCachedUserName function not available, trying fallback...');
            // Fallback: try to read directly from localStorage
            try {
                const cachedName = localStorage.getItem('planningpoker_user_name');
                if (cachedName) {
                    const parsedName = JSON.parse(cachedName);
                    const nameInput = document.getElementById('name');
                    if (nameInput && !nameInput.value.trim()) {
                        nameInput.value = parsedName;
                        console.log('Loaded cached user name via fallback:', parsedName);
                    }
                }
            } catch (error) {
                console.log('Error loading cached user name:', error);
            }
        }
    }
    
    function loadInitialPreferences() {
        // Load cached preferences and apply default values on page load
        if (typeof loadCreationPreferences === 'function') {
            const preferences = loadCreationPreferences();
            console.log('Loading initial room creation preferences:', preferences);
            
            // Apply cached preferences to form elements
            const cardSetSelect = document.getElementById('creation-card-set');
            const countdownInput = document.getElementById('creation-countdown-seconds');
            const funRoomNameCheckbox = document.getElementById('creation-use-fun-room-name');
            const fireworksCheckbox = document.getElementById('creation-show-fireworks');
            const maxParticipantsInput = document.getElementById('creation-max-participants');
            
            if (cardSetSelect && preferences.cardSet) {
                cardSetSelect.value = preferences.cardSet;
            }
            if (countdownInput && preferences.countdownSeconds !== undefined) {
                countdownInput.value = preferences.countdownSeconds;
                console.log('Set countdown to:', preferences.countdownSeconds);
            }
            if (funRoomNameCheckbox) {
                funRoomNameCheckbox.checked = preferences.useFunRoomName || false;
            }
            if (fireworksCheckbox) {
                fireworksCheckbox.checked = preferences.showFireworks !== undefined ? preferences.showFireworks : true;
            }
            if (maxParticipantsInput && preferences.maxParticipants !== undefined) {
                maxParticipantsInput.value = preferences.maxParticipants;
            }
        }
    }

    // Helper functions for form validation
    function showError(errorId, message) {
        const errorElement = document.getElementById(errorId);
        if (errorElement) {
            errorElement.textContent = message;
            errorElement.style.display = 'inline';
        }
    }
    
    function clearFormErrors() {
        const errorElements = document.querySelectorAll('[id$="-error"]');
        errorElements.forEach(element => {
            element.style.display = 'none';
            element.textContent = '';
        });
    }

    // Initialize page functionality
    const roomCreationSettingsBtn = document.getElementById('room-creation-settings-btn');
    const roomCreationSettings = document.getElementById('room-creation-settings');
    const closeCreationSettings = document.getElementById('close-creation-settings');
    const cancelCreationSettings = document.getElementById('cancel-creation-settings');
    const saveCreationSettings = document.getElementById('save-creation-settings');
    const createRoomForm = document.getElementById('createRoom');

    if (roomCreationSettingsBtn) {
        roomCreationSettingsBtn.addEventListener('click', function() {
            if (roomCreationSettings.style.display === 'block') {
                hideRoomCreationSettings();
            } else {
                showRoomCreationSettings();
            }
        });
    }

    if (closeCreationSettings) {
        closeCreationSettings.addEventListener('click', function() {
            hideRoomCreationSettings();
        });
    }

    if (cancelCreationSettings) {
        cancelCreationSettings.addEventListener('click', function() {
            hideRoomCreationSettings();
        });
    }

    if (saveCreationSettings) {
        saveCreationSettings.addEventListener('click', function() {
            saveRoomCreationSettings();
            hideRoomCreationSettings();
        });
    }

    // Form validation and submission handling
    if (createRoomForm) {
        createRoomForm.addEventListener('submit', function(e) {
            console.log('=== FORM SUBMITTED ===');
            console.log('Submitter:', e.submitter);
            console.log('Form action:', e.submitter?.formAction);
            
            // Validate form based on action
            const isCreateRoom = e.submitter && e.submitter.formAction && e.submitter.formAction.includes('CreateRoom');
            const isJoinRoom = e.submitter && e.submitter.formAction && e.submitter.formAction.includes('JoinRoom');
            
            // Clear previous errors
            clearFormErrors();
            
            let hasErrors = false;
            
            // Validate name for both create and join
            const nameInput = document.getElementById('name');
            if (!nameInput || !nameInput.value.trim()) {
                showError('name-error', 'Please enter your name');
                hasErrors = true;
            }
            
            // Validate room name for join room
            if (isJoinRoom) {
                const roomNameInput = document.getElementById('roomName');
                if (!roomNameInput || !roomNameInput.value.trim()) {
                    showError('room-name-error', 'Please enter a room name');
                    hasErrors = true;
                }
            }
            
            // Prevent form submission if there are errors
            if (hasErrors) {
                e.preventDefault();
                return;
            }
            
            // Cache user name for both create and join actions
            if (nameInput && nameInput.value.trim()) {
                const userName = nameInput.value.trim();
                cacheUserNameSync(userName);
            }
            
            // Only inject room creation values when creating a room (not joining)
            if (isCreateRoom) {
                injectRoomCreationValues();
            }
        });
    }

    // Load card sets from server on page load
    loadCardSets();
    
    // Load cached user name on page load (with slight delay to ensure site.js is loaded)
    setTimeout(loadCachedUserName, 100);
    
    // Auto-hide server error message after 8 seconds
    const serverError = document.getElementById('server-error');
    if (serverError) {
        setTimeout(() => {
            serverError.style.transition = 'opacity 0.5s ease';
            serverError.style.opacity = '0';
            setTimeout(() => {
                serverError.style.display = 'none';
            }, 500);
        }, 8000);
    }
    
    // Save user name with multiple event handlers for maximum reliability
    const nameInput = document.getElementById('name');
    
    if (nameInput) {
        // Cache on blur (when user leaves field)
        nameInput.addEventListener('blur', function() {
            if (this.value.trim()) {
                console.log('=== BLUR EVENT ===');
                cacheUserNameSync(this.value.trim());
            }
        });
        
        // Cache on input (as user types - debounced)
        let inputTimeout;
        nameInput.addEventListener('input', function() {
            clearTimeout(inputTimeout);
            if (this.value.trim()) {
                inputTimeout = setTimeout(() => {
                    console.log('=== INPUT EVENT (DEBOUNCED) ===');
                    cacheUserNameSync(this.value.trim());
                }, 500); // Wait 500ms after user stops typing
            }
        });
        
        // Cache on keyup (immediate for Enter key)
        nameInput.addEventListener('keyup', function(e) {
            if (e.key === 'Enter' && this.value.trim()) {
                console.log('=== ENTER KEY PRESSED ===');
                cacheUserNameSync(this.value.trim());
            }
        });
    }
}