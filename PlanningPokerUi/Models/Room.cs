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
        public readonly MyTimer VotingTimer;
        public readonly MyTimer HealthCheckTimer;
        public VoteResultInfo VoteResultInfo { get; private set; }

        public Room(Person person, RoomConfiguration configuration = null)
        {
            _people = new ConcurrentDictionary<Guid, Person>();
            _votes = new ConcurrentDictionary<Guid, string>();
            
            Configuration = configuration ?? new RoomConfiguration();
            Owner = person; // First person to create room is the owner
            
            VotingTimer = new MyTimer(Configuration.CountdownInterval);
            VotingTimer.MaxTriggers = Configuration.CountdownSeconds;
            
            HealthCheckTimer = new MyTimer(Configuration.HealthCheckInterval);
            
            AddPerson(person);
        }

        public string Guid { get; set; }
        public RoomConfiguration Configuration { get; set; }
        public Person Owner { get; set; }
        
        // Backward compatibility
        public string CardSet 
        { 
            get => Configuration.CardSet; 
            set => Configuration.CardSet = value; 
        }

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
            var eligiblePeople = _people.Values.Where(p => 
                p.PersonType != "obs" || (p.PersonType == "obs" && Configuration.AllowObserverVoting));
            
            return eligiblePeople.All(p => _votes.ContainsKey(p.Guid));
        }

        public void ClearVotes()
        {
            _votes.Clear();
            VoteResultInfo = null;
            VotingTimer.ClearElapsed();
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
                return person.PersonType != "obs";
            }).Select(p =>
            {
                _people.TryGetValue(p.Key, out var person);
                return new Vote()
                {
                    Guid = p.Key,
                    Mark = p.Value,
                    PersonType = person.PersonType
                };
            });
        }

        public IEnumerable<Person> GetNotConnected()
        {
            return _people.Values.Where(p => !p.IsConnected);
        }

        public IEnumerable<Person> AllPeople()
        {
            return _people.Values;
        }

        public void UpdateConfiguration(RoomConfiguration newConfiguration)
        {
            // Only update properties that are actually being changed
            if (!string.IsNullOrEmpty(newConfiguration.CardSet))
                Configuration.CardSet = newConfiguration.CardSet;
            
            if (newConfiguration.CountdownSeconds >= 0)
                Configuration.CountdownSeconds = newConfiguration.CountdownSeconds;
            
            if (newConfiguration.MaxParticipants > 0)
                Configuration.MaxParticipants = newConfiguration.MaxParticipants;
            
            // Always update these boolean properties
            Configuration.ShowFireworks = newConfiguration.ShowFireworks;
            Configuration.AutoShowVotes = newConfiguration.AutoShowVotes;
            
            // Update timer configurations
            VotingTimer.MaxTriggers = Configuration.CountdownSeconds;
        }

        public bool CanPersonVote(Person person)
        {
            if (person.PersonType == "obs" && !Configuration.AllowObserverVoting)
                return false;
                
            return true;
        }
        
        public bool TransferOwnership(Person newOwner)
        {
            if (newOwner == null || !IsPersonHere(newOwner))
                return false;
                
            Owner = newOwner;
            return true;
        }
        
        public Person TransferOwnershipRandomly()
        {
            var eligiblePeople = GetPeople().Where(p => p.Guid != Owner?.Guid && p.IsConnected).ToList();
            
            if (!eligiblePeople.Any())
                return null; // No one left to transfer to
                
            var random = new Random();
            var newOwner = eligiblePeople[random.Next(eligiblePeople.Count())];
            Owner = newOwner;
            return newOwner;
        }

        public VoteResultInfo GenerateResultsAndStatistics()
        {
            var votes = AllVotes();
            var groupedVotes = GroupVotes(votes);
            var groupedVotesDev = GroupVotes(votes.Where(v => v.PersonType == "dev"));
            var groupedVotesTest = GroupVotes(votes.Where(v => v.PersonType == "test"));

            var valueMarks = votes.Where(a =>
            {
                return decimal.TryParse(a.Mark, out _);
            });

            var maxPercent = groupedVotes.OrderByDescending(a => a.Percentage).FirstOrDefault();
            var maxPercentDev = groupedVotesDev.OrderByDescending(a => a.Percentage).FirstOrDefault();
            var maxPercentTest = groupedVotesTest.OrderByDescending(a => a.Percentage).FirstOrDefault();

            var otherHighestVote = GetOtherHighestVote(groupedVotes, maxPercent);
            var otherHighestVoteDev = GetOtherHighestVote(groupedVotesDev, maxPercentDev);
            var otherHighestVoteTest = GetOtherHighestVote(groupedVotesTest, maxPercentTest);

            VoteResultInfo = new VoteResultInfo()
            {
                VotingFinished = true,
                Votes = votes.ToList(),
                Statistics = new Statistics()
                {
                    Marks = GetMarksOrdered(groupedVotes),
                    HighestMark = GetHighestMark(maxPercent, otherHighestVote),
                    AverageMark = GetAverageMark(valueMarks),

                    MarksDev = GetMarksOrdered(groupedVotesDev),
                    HighestMarkDev = GetHighestMark(maxPercentDev, otherHighestVoteDev),
                    AverageMarkDev = GetAverageMark(valueMarks.Where(v => v.PersonType == "dev")),

                    MarksTest = GetMarksOrdered(groupedVotesTest),
                    HighestMarkTest = GetHighestMark(maxPercentTest, otherHighestVoteTest),
                    AverageMarkTest = GetAverageMark(valueMarks.Where(v => v.PersonType == "test"))
                }
            };

            return VoteResultInfo;
        }

        private static MarkPercentage GetOtherHighestVote(IEnumerable<MarkPercentage> groupedVotes, MarkPercentage maxPercent)
        {
            return groupedVotes.FirstOrDefault(a => a.Percentage == maxPercent.Percentage && a.Mark != maxPercent.Mark);
        }

        private static string GetHighestMark(MarkPercentage maxPercent, MarkPercentage otherHighestVote)
        {
            return otherHighestVote == null ? maxPercent?.Mark : null;
        }

        private static decimal? GetAverageMark(IEnumerable<Vote> valueMarks)
        {
            return valueMarks.Any() ? (decimal?)valueMarks.Average(a => decimal.Parse(a.Mark)) : null;
        }

        private static List<MarkPercentage> GetMarksOrdered(IEnumerable<MarkPercentage> groupedVotes)
        {
            return groupedVotes.OrderBy(a =>
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
            }).ToList();
        }

        private static IEnumerable<MarkPercentage> GroupVotes(IEnumerable<Vote> votes)
        {
            return votes.GroupBy(p => p.Mark).Select(group => new MarkPercentage
            {
                Mark = group.Key,
                Percentage = (group.Count() / (decimal)votes.Count()) * 100
            });
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
                Countdown = VotingTimer.Countdown
            };
        }

        public Vote? GetVote(Guid guid)
        {
            if (VoteResultInfo != null)
            {
                return VoteResultInfo.Votes.FirstOrDefault(v => v.Guid == guid);
            }

            var mark = AllVotes().FirstOrDefault(t => t.Guid == guid);

            return mark == default(Vote) ? null : new Vote()
            {
                Guid = guid,
                Mark = "hide"
            };
        }
    }
}
