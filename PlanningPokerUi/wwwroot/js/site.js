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
        return window.cardSetMapping[mark]
    }
    return mark
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
                            return getDisplayValue(value)
                        }
                    },
                    gridLines: {
                        display: false
                    }
                }]
            }
        }
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
        labels.push(getDisplayValue(mark.mark))
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
                markElement.text(getDisplayValue(vote.mark))
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

    $("#statistics").hide();
    $("#show-votes-countdown").hide();

    try {
        fireWorks = new Fireworks.default($('.fireworks')[0])
    } catch {
        // nothing to do
    }
})