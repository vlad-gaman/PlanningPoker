// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your Javascript code.
let personId = ""
let webSocket
let chart

let createWebSocket = function (hostname, port, guid) {
    let uri = hostname + (port ? ":" + port : "") + "/ws/" + guid;
    let ws;
    try {
        ws = new WebSocket("ws://" + uri)
    }
    catch (err) {
        ws = new WebSocket("wss://" + uri)
    }

    if (ws.readyState == ws.OPEN) {
        return ws
    }
    else {
        ws = new WebSocket("wss://" + uri)
        return ws;
    }

}

let connectToRoom = function (guid, personGuid) {
    webSocket = createWebSocket(window.location.hostname, window.location.port, guid);
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

                    $('#people').append($('#people > tr').sort(sortByIdProp))
                    $('#observers').append($('#observers > tr').sort(sortByIdProp))
                }
                break
            case "Joined":
                addPeopleToTable([object.Person])
                if (object.Vote) {
                    setVotes([object.Vote])
                }

                if (!object.IsObserver) {
                    $('#people').append($('#people > tr').sort(sortByIdProp))
                } else {
                    $('#observers').append($('#observers > tr').sort(sortByIdProp))
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

                chart.data.labels = []
                chart.data.datasets[0].data = []
                chart.data.datasets[0].backgroundColor = []
                chart.update()
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
                $("#" + object.Person.Guid).remove()
                addPeopleToTable([object.Person])
                setVoteResultInfo(object.VoteResultInfo)
                break
        }
    }

    $("#isObserver").change(function () {
        let message = {
            Verb: "ObserverChange",
            Object: {
                IsObserver: this.checked
            }
        }

        let messageAsString = JSON.stringify(message)
        webSocket.send(messageAsString)
    })

    chart = new Chart("myChart", {
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

let setStatistics = function (statistics) {
    let labels = []
    let percentages = []
    let colors = []
    for (let mark of statistics.Marks) {
        labels.push(mark.Mark)
        percentages.push(mark.Percentage)
        if (statistics.HighestMark == mark.Mark) {
            colors.push("green")
        }
        else {
            colors.push("blue")
        }
    }

    $("#averageMark").text(statistics.AverageMark)

    chart.data.labels = labels
    chart.data.datasets[0].data = percentages
    chart.data.datasets[0].backgroundColor = colors
    chart.update()
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
    let people = $("#people")
    let observers = $("#observers")
    for (let otherPerson of otherPeople) {        
        if (!($("#" + otherPerson.Guid)[0])) {
            let tr = $("<tr/>")
            tr.attr("id", otherPerson.Guid)

            let tdName = $("<td/>")
            tdName.attr("class", "name")
            tdName.text(otherPerson.Name)

            tr.append(tdName)

            if (otherPerson.IsObserver) {
                observers.append(tr)
            } else {
                let tdMark = $("<td/>")
                tdMark.attr("class", "mark")

                
                tr.append(tdMark)
                people.append(tr)
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
        let message = {
            verb: "ClearVotes"
        }

        let messageAsString = JSON.stringify(message)
        webSocket.send(messageAsString)
    })

    $('#show-votes').click(function () {
        let message = {
            verb: "ShowVotes"
        }

        let messageAsString = JSON.stringify(message)
        webSocket.send(messageAsString)
    })

    $("#statistics").hide();
    $("#show-votes-countdown").hide();
})