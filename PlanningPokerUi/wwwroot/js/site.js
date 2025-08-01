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
        
        // Update local configuration
        window.roomConfiguration = data.configuration;
        
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
                }
            },
            legend: {
                display: false
            },
            scales: {
                yAxes: [{
                    ticks: {
                        beginAtZero: true,
                        suggestedMax: 100,
                        display: false
                    },
                    gridLines: {
                        display: false
                    }
                }],
                xAxes: [{
                    ticks: {
                        fontSize: 16,
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

    for (let mark of marks) {
        labels.push(mark.mark) // Use the actual mark value as label
        percentages.push(mark.percentage)
        if (highestMark == mark.mark) {
            colors.push("green")
        }
        else {
            colors.push("blue")
        }
    }

    chart.data.labels = labels
    chart.data.datasets[0].data = percentages
    chart.data.datasets[0].backgroundColor = colors
    chart.update()
}

let setStatistics = function (statistics) {
    if (!statistics) return;
    
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

    let marksElements = $(".mark").toArray()
    let numberOfVotes = marksElements.length
    let votesGrouped = groupBy(marksElements, e => $(e).text());

    if (statistics.marks && statistics.marks.length > 0
        && statistics.marks[0].percentage == 100
        && statistics.marks.length == 1
        && votesGrouped[getDisplayValue(statistics.marks[0].mark)].length == numberOfVotes
        && numberOfVotes > 1)
    {
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
        }
        disableVoting(true)
    } else {
        $("#statistics").hide();
        if (voteResultInfo.hasEveryoneVoted) {
            $("#show-votes-countdown").show();
            $("#countdown").text(voteResultInfo.countdown)
        } else {
            $("#show-votes-countdown").hide();
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

    // Initialize room settings if configuration exists
    if (typeof window.roomConfiguration !== 'undefined') {
        loadRoomSettings(window.roomConfiguration);
    }

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
    
    // Load card sets from API
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
    const config = {
        cardSet: $('#card-set-select').val(),
        countdownSeconds: countdownSeconds,
        autoShowVotes: countdownSeconds === 0, // Auto-enable when countdown is 0
        showFireworks: $('#show-fireworks').is(':checked'),
        maxParticipants: parseInt($('#max-participants').val())
    };

    if (connection && connection.state === signalR.HubConnectionState.Connected) {
        connection.invoke("UpdateRoomConfiguration", config).then(function () {
            hideRoomSettings();
            // Update local configuration
            window.roomConfiguration = config;
        }).catch(function (err) {
            console.error("Error updating room configuration: " + err.toString());
            alert("Failed to update room settings. Please try again.");
        });
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
    fetch('/api/cardsets')
        .then(response => response.json())
        .then(cardSets => {
            const cardSetSelect = $('#card-set-select');
            cardSetSelect.empty(); // Clear existing options
            
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
                // Default to modified-fibonacci if no configuration
                cardSetSelect.val('modified-fibonacci');
            }
        })
        .catch(error => {
            console.error('Error loading card sets for room settings:', error);
            // Keep the loading message if API fails
            $('#card-set-select').html('<option value="">Failed to load card sets</option>');
        });
}