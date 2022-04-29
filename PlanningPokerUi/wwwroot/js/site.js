// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your Javascript code.
var personId = ""
var webSocket
var chart

var connectToRoom = function (guid, personGuid) {
    var url = "ws://" + window.location.hostname + (window.location.port ? ":" + window.location.port : "") + "/ws/" + guid
    webSocket = new WebSocket(url)
    webSocket.bufferedAmount = 1024 * 20;
    personId = personGuid

    webSocket.onopen = function (event) {
        var message = {
            verb: "Join",
            object: guid
        }

        var messageAsString = JSON.stringify(message)

        webSocket.send(messageAsString)
    }

    webSocket.onmessage = function (event) {
        if (event.data instanceof Blob) {
            event.data.text().then(text => handleMessage(text))
        } else {
            handleMessage(event.data)
        }
    }

    var handleMessage = function (data) {
        var messsage = JSON.parse(data)
        console.log(messsage)
        var object = messsage.Object
        switch (messsage.Verb) {
            case "IsJoined":
                if (object.IsSuccessful) {
                    addPeopleToTable(object.People)
                    setVotes(object.Votes)

                    $('#people').append($('#people > tr').sort(sortByIdProp))

                    $('#observers').append($('#observers > tr').sort(sortByIdProp))
                }
                break
            case "Joined":
                addPeopleToTable([object])

                if (!object.IsObserver) {
                    $("#averageMark").text("")
                    chart.data.labels = []
                    chart.data.datasets[0].data = []
                    chart.data.datasets[0].backgroundColor = []
                    chart.update()

                    for (let a of $(".mark")) {
                        let b = $(a)
                        if (b.text()) {
                            b.text("\u25AE")
                        }
                    }
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
                setVotes(object.Votes)
                setStatistics(object.Statistics)
                break
            case "ClearVote":
                $(".mark").text("")
                $("input[type=radio][name=mark]").prop("checked", false)
                $("#averageMark").text("")

                chart.data.labels = []
                chart.data.datasets[0].data = []
                chart.data.datasets[0].backgroundColor = []
                chart.update()
                break
            case "ObserverChange":
                $("#" + object.Guid).remove()
                addPeopleToTable([object])
                break
        }
    }

    $("#isObserver").change(function () {
        var message = {
            Verb: "ObserverChange",
            Object: {
                IsObserver: this.checked
            }
        }

        var messageAsString = JSON.stringify(message)
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

var sortByIdProp = function (a, b) {
    var aId = $(a).prop("id")
    var bId = $(b).prop("id")
    if (aId < bId) {
        return -1
    } else if (aId > bId) {
        return 1
    }
    return 0;
}

var setStatistics = function (statistics) {
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

var setVotes = function (votes) {
    for (let vote of votes) {
        if ($("#" + vote.Guid)[0]) {
            var markElement = $("#" + vote.Guid + " .mark")
            if (vote.Mark == 'coffee') {
                markElement.text("\u2615")
            }
            else if (vote.Mark == '0.5') {
                markElement.text("\u00BD")
            }
            else if (vote.Mark == 'hide')
            {
                markElement.text("\u25AE")
            }
            else {
                markElement.text(vote.Mark)
            }
        }
    }
}

var addPeopleToTable = function (otherPeople) {
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

var removePeopleFromTable = function (otherPeople) {
    for (let otherPerson of otherPeople) {
        $("#" + otherPerson.Guid).remove()
    }
}

$(document).ready(function () {
    $('input[type=radio][name=mark]').change(function () {
        if (!this.value)
            return;
        var message = {
            Verb: "Vote",
            Object: this.value
        }

        var messageAsString = JSON.stringify(message)
        webSocket.send(messageAsString)
    })

    $('#clear-votes').click(function () {
        var message = {
            verb: "ClearVotes"
        }

        var messageAsString = JSON.stringify(message)
        webSocket.send(messageAsString)
    })

    $('#show-votes').click(function () {
        var message = {
            verb: "ShowVotes"
        }

        var messageAsString = JSON.stringify(message)
        webSocket.send(messageAsString)
    })
})