using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace PlanningPokerUi.Models
{
    public class Room
    {
        private readonly ConcurrentDictionary<Guid, Person> _people;
        private readonly ConcurrentDictionary<Guid, string> _votes;
        public readonly MyTimer Timer;
        public VoteResultInfo VoteResultInfo { get; private set; }

        public Room(Person person)
        {
            _people = new ConcurrentDictionary<Guid, Person>();
            _votes = new ConcurrentDictionary<Guid, string>();
            Timer = new MyTimer(1000);
            AddPerson(person);
        }

        public string Guid { get; set; }

        public void AddPerson(Person person)
        {
            _people.TryAdd(person.Guid, person);
        }

        public void RemovePerson(Person person)
        {
            if (_people.TryRemove(person.Guid, out _))
            {
                _votes.TryRemove(person.Guid, out _);
            }
        }

        public bool IsPersonHere(Person person)
        {
            return _people.TryGetValue(person.Guid, out _);
        }

        public IEnumerable<Person> GetPeople()
        {
            return _people.Select(p => p.Value);
        }

        public void Vote(Guid personGuid, string mark)
        {
            _votes.AddOrUpdate(personGuid, mark, (guid, oldMark) => mark);
        }

        public bool DidEveryoneVote()
        {
            return !_people.Any(p => !p.Value.IsObserver && !_votes.ContainsKey(p.Key));
        }

        public void ClearVotes()
        {
            _votes.Clear();
            VoteResultInfo = null;
            Timer.ClearElapsed();
        }

        public bool IsVotingEnabled()
        {
            return VoteResultInfo == null;
        }

        public IEnumerable<Vote> AllVotes()
        {
            return _votes.Where(p =>
            {
                _people.TryGetValue(p.Key, out var person);
                return !person.IsObserver;
            }).Select(p => new Vote()
            {
                Guid = p.Key, 
                Mark = p.Value
            });
        }

        public VoteResultInfo GenerateResultsAndStatistics()
        {
            var votes = AllVotes();
            var groupedVotes = votes.GroupBy(p => p.Mark).Select(group => new MarkPercentage
            {
                Mark = group.Key,
                Percentage = (group.Count() / (decimal)votes.Count()) * 100
            });
            var valueMarks = votes.Where(a =>
            {
                return decimal.TryParse(a.Mark, out _);
            });

            var maxPercent = groupedVotes.OrderByDescending(a => a.Percentage).FirstOrDefault();
            var otherHighestVote = groupedVotes.FirstOrDefault(a => a.Percentage == maxPercent.Percentage && a.Mark != maxPercent.Mark);

            VoteResultInfo = new VoteResultInfo()
            {
                VotingFinished = true,
                Votes = votes.ToList(),
                Statistics = new Statistics()
                {
                    Marks = groupedVotes.OrderBy(a =>
                    {
                        if (decimal.TryParse(a.Mark, out var markDecimal))
                        {
                            return markDecimal;
                        }
                        else if (a.Mark == "?")
                        {
                            return 101;
                        }
                        else if (a.Mark == "coffee")
                        {
                            return 102;
                        }
                        return 103;
                    }).ToList(),
                    HighestMark = otherHighestVote == null ? maxPercent?.Mark : null,
                    AverageMark = valueMarks.Any() ? (decimal?)valueMarks.Average(a => decimal.Parse(a.Mark)) : null,
                }
            };

            return VoteResultInfo;
        }

        public VoteResultInfo GetCurrentStatus()
        {
            if (VoteResultInfo != null)
            {
                return VoteResultInfo;
            }

            return new VoteResultInfo()
            {
                Votes = AllVotes().Select(t => new Vote()
                {
                    Guid = t.Guid,
                    Mark = "hide"
                }).ToList(),
                HasEveryoneVoted = DidEveryoneVote(),
                Countdown = Timer.Countdown
            };
        }

        public Vote? GetVote(Guid guid)
        {
            if (VoteResultInfo != null)
            {
                return VoteResultInfo.Votes.FirstOrDefault(v => v.Guid == guid);
            }

            var mark = AllVotes().FirstOrDefault(t => t.Guid == guid);

            return mark == default(Vote) ? null: new Vote()
            {
                Guid = guid,
                Mark = "hide"
            };
        }
    }
}
