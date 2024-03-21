// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your Javascript code.
let personId = ""
let webSocket
let allChart
let devChart
let testChart
let fireWorks
let fireWorksIntervals = []

let createWebSocket = function (hostname, port, protocol, guid) {
    let uri = hostname + (port ? ":" + port : "") + "/ws/" + guid;
    let wsProtocol = protocol.startsWith("https") ? "wss" : "ws";
    let ws = new WebSocket(wsProtocol + "://" + uri);
    return ws;
}

let connectToRoom = function (guid, personGuid) {
    webSocket = createWebSocket(window.location.hostname, window.location.port, window.location.protocol, guid);
    webSocket.bufferedAmount = 1024 * 20;
    personId = personGuid

    webSocket.onopen = function (event) {
        let message = {
            verb: "Join",
            object: guid
        }

        let messageAsString = JSON.stringify(message)

        webSocket.send(messageAsString)
    }

    webSocket.onmessage = function (event) {
        if (event.data instanceof Blob) {
            event.data.text().then(text => handleMessage(text))
        } else {
            handleMessage(event.data)
        }
    }

    let handleMessage = function (data) {
        let messsage = JSON.parse(data)
        console.log(messsage)
        let object = messsage.Object
        switch (messsage.Verb) {
            case "IsJoined":
                if (object.IsSuccessful) {
                    addPeopleToTable(object.People)
                    setVoteResultInfo(object.VoteResultInfo)

                    $('#people-dev').append($('#people-dev > tr').sort(sortByIdProp))
                    $('#people-test').append($('#people-test > tr').sort(sortByIdProp))
                    $('#observers').append($('#observers > tr').sort(sortByIdProp))
                }
                break
            case "Joined":
                addPeopleToTable([object.Person])
                if (object.Vote) {
                    setVotes([object.Vote])
                }

                if (object.PersonType == "test") {
                    $('#people-test').append($('#people-test > tr').sort(sortByIdProp))
                }
                else if (object.PersonType == "obs") {
                    $('#observers').append($('#observers > tr').sort(sortByIdProp))
                } else {
                    $('#people-dev').append($('#people-dev > tr').sort(sortByIdProp))
                }
                break
            case "Exited":
                removePeopleFromTable([object])
                break
            case "Disconnected":
                window.location.href = "/"
                break
            case "AVote":
                $("#" + object + " .mark").text("\u25AE")
                break
            case "ShowVotes":
                setVoteResultInfo(object)
                break
            case "ClearVote":
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
                break
            case "Countdown":
                if (object.Reset) {
                    $("#show-votes-countdown").hide();
                } else {
                    $("#show-votes-countdown").show();
                    $("#countdown").text(object.Countdown)
                }
                break
            case "ObserverChange":
            case "PersonTypeChange":
                $("#" + object.Person.Guid).remove()
                addPeopleToTable([object.Person])
                setVoteResultInfo(object.VoteResultInfo)
                break
            case "HealthCheck":
                let message = {
                    verb: "Healthy"
                }

                let messageAsString = JSON.stringify(message)
                webSocket.send(messageAsString)
                break
        }
    }

    $("input[type=radio][name=personType]").change(function () {
        let message = {
            Verb: "PersonTypeChange",
            Object: {
                PersonType: this.value
            }
        }

        let messageAsString = JSON.stringify(message)
        webSocket.send(messageAsString)
    })

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
                                if (value == '0.5') {
                                    return '\u00BD'
                                } else if (value == 'coffee') {
                                    return '\u2615'
                                }
                                return value
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

    allChart = createChart("allChart")
    devChart = createChart("devChart")
    testChart = createChart("testChart")
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
    let labels = []
    let percentages = []
    let colors = []

    for (let mark of marks) {
        labels.push(mark.Mark)
        percentages.push(mark.Percentage)
        if (highestMark == mark.Mark) {
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
    setIndividualStatistics(statistics.Marks, statistics.HighestMark, allChart)
    setIndividualStatistics(statistics.MarksDev, statistics.HighestMarkDev, devChart)
    setIndividualStatistics(statistics.MarksTest, statistics.HighestMarkTest, testChart)

    $("#allAverageMark").text(statistics.AverageMark)
    $("#devAverageMark").text(statistics.AverageMarkDev)
    $("#testAverageMark").text(statistics.AverageMarkTest)

    let marksElements = $(".mark").toArray()
    let numberOfVotes = marksElements.length
    let votesGrouped = groupBy(marksElements, e => $(e).text());


    if (statistics.Marks.length > 0
        && statistics.Marks[0].Percentage == 100
        && statistics.Marks.length == 1
        && votesGrouped[statistics.Marks[0].Mark].length == numberOfVotes)
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
    setVotes(voteResultInfo.Votes)
    if (voteResultInfo.VotingFinished) {
        $("#statistics").show()
        $("#show-votes-countdown").hide()
        setStatistics(voteResultInfo.Statistics)
        disableVoting(true)
    } else {
        $("#statistics").hide();
        if (voteResultInfo.HasEveryoneVoted) {
            $("#show-votes-countdown").show();
            $("#countdown").text(voteResultInfo.Countdown)
        } else {
            $("#show-votes-countdown").hide();
        }

    }
}

let setVotes = function (votes) {
    for (let vote of votes) {
        if ($("#" + vote.Guid)[0]) {
            let markElement = $("#" + vote.Guid + " .mark")
            if (vote.Mark == 'coffee') {
                markElement.text("\u2615")
            }
            else if (vote.Mark == '0.5') {
                markElement.text("\u00BD")
            }
            else if (vote.Mark == 'hide') {
                markElement.text("\u25AE")
            }
            else {
                markElement.text(vote.Mark)
            }
        }
    }
}

let addPeopleToTable = function (otherPeople) {
    let peopleDev = $("#people-dev")
    let peopleTest = $("#people-test")
    let observers = $("#observers")
    for (let otherPerson of otherPeople) {
        if (!($("#" + otherPerson.Guid)[0])) {
            let tr = $("<tr/>")
            tr.attr("id", otherPerson.Guid)

            let tdName = $("<td/>")
            tdName.attr("class", "name")
            tdName.text(otherPerson.Name)

            tr.append(tdName)

            if (otherPerson.PersonType == "obs") {
                observers.append(tr)
            } else {
                let tdMark = $("<td/>")
                tdMark.attr("class", "mark")
                tr.append(tdMark)

                if (otherPerson.PersonType == "test") {
                    peopleTest.append(tr)
                } else {
                    peopleDev.append(tr)
                }
            }
        }
    }
}

let removePeopleFromTable = function (otherPeople) {
    for (let otherPerson of otherPeople) {
        $("#" + otherPerson.Guid).remove()
    }
}

$(document).ready(function () {
    $('input[type=radio][name=mark]').change(function () {
        if (!this.value)
            return;
        let message = {
            Verb: "Vote",
            Object: this.value
        }

        let messageAsString = JSON.stringify(message)
        webSocket.send(messageAsString)
    })

    $('#clear-votes').click(function () {
        stopFireWorks();
        let message = {
            verb: "ClearVotes"
        }

        let messageAsString = JSON.stringify(message)
        webSocket.send(messageAsString)
    })

    $('#show-votes').click(function () {
        stopFireWorks();
        let message = {
            verb: "ForceShowVotes"
        }

        let messageAsString = JSON.stringify(message)
        webSocket.send(messageAsString)
    })

    $("#statistics").hide();
    $("#show-votes-countdown").hide();

    try {
        fireWorks = new Fireworks.default($('.fireworks')[0])
    } catch {
        // nothing to do
    }
})