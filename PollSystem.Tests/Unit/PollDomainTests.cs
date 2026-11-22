using PollSystem.Extensions;
using PollSystem.Models.Common;
using PollSystem.Models.Entities;
using PollSystem.Models.Exceptions;

namespace PollSystem.Tests.Unit
{
    public class PollDomainTests
    {
        [Fact]
        public void CastVote_ValidEarlyBirdVote_DeductsPointsAndSetsEarlyBird()
        {
            var user = new User("alice", "hashed_password", 100);
            var poll = new Poll
            {
                Question = "Best backend framework?",
                Category = "Technology",
                Options = new List<PollOption>
                {
                    new("ASP.NET Core") { Position = 0 },
                    new("Spring Boot") { Position = 1 }
                },
                ClosingTime = DateTime.UtcNow.AddHours(1)
            };

            poll.CastVote(user, new Vote(user.Id, new List<int> { 0 }, 25));

            Assert.Equal(75, user.PointsBalance);
            Assert.Single(poll.Votes);
            Assert.True(poll.Votes[0].IsEarlyBird);
        }

        [Fact]
        public void CastVote_LateVote_DoesNotSetEarlyBird()
        {
            var user = new User("alice", "hashed_password", 100);
            var poll = new Poll
            {
                Question = "Late vote test?",
                Category = "Fun",
                CreatedAt = DateTime.UtcNow.AddMinutes(-50),
                ClosingTime = DateTime.UtcNow.AddMinutes(10),
                Options = new List<PollOption>
                {
                    new("Yes") { Position = 0 },
                    new("No") { Position = 1 }
                }
            };

            poll.CastVote(user, new Vote(user.Id, new List<int> { 0 }, 10));

            Assert.Single(poll.Votes);
            Assert.False(poll.Votes[0].IsEarlyBird);
        }

        [Fact]
        public void CastVote_ClosedPoll_ThrowsPollDomainException()
        {
            var user = new User("alice", "hashed_password", 100);
            var poll = new Poll
            {
                ClosingTime = DateTime.UtcNow.AddMinutes(-1),
                Options = new List<PollOption> { new("Yes") { Position = 0 } }
            };

            var ex = Assert.Throws<PollDomainException>(() =>
                poll.CastVote(user, new Vote(user.Id, new List<int> { 0 }, 10)));

            Assert.Equal("POLL_CLOSED", ex.ErrorCode);
            Assert.Equal(poll.Id, ex.PollId);
        }

        [Fact]
        public void CastVote_EmptyOptions_ThrowsArgumentException()
        {
            var user = new User("alice", "hashed_password", 100);
            var poll = new Poll
            {
                ClosingTime = DateTime.UtcNow.AddHours(1),
                Options = new List<PollOption> { new("Yes") { Position = 0 } }
            };

            Assert.Throws<ArgumentException>(() =>
                poll.CastVote(user, new Vote(user.Id, new List<int>(), 0)));
        }

        [Fact]
        public void CastVote_MultipleOptionsWhenDisallowed_ThrowsPollDomainException()
        {
            var user = new User("alice", "hashed_password", 100);
            var poll = new Poll
            {
                AllowMultiple = false,
                ClosingTime = DateTime.UtcNow.AddHours(1),
                Options = new List<PollOption>
                {
                    new("A") { Position = 0 },
                    new("B") { Position = 1 }
                }
            };

            var ex = Assert.Throws<PollDomainException>(() =>
                poll.CastVote(user, new Vote(user.Id, new List<int> { 0, 1 }, 0)));

            Assert.Equal("MULTIPLE_CHOICES_NOT_ALLOWED", ex.ErrorCode);
            Assert.Equal(poll.Id, ex.PollId);
        }

        [Fact]
        public void CastVote_OutOfRangeOptionIndex_ThrowsArgumentOutOfRangeException()
        {
            var user = new User("alice", "hashed_password", 100);
            var poll = new Poll
            {
                ClosingTime = DateTime.UtcNow.AddHours(1),
                Options = new List<PollOption> { new("A") { Position = 0 } }
            };

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                poll.CastVote(user, new Vote(user.Id, new List<int> { 5 }, 0)));
        }

        [Fact]
        public void CastVote_InsufficientPoints_ThrowsPollDomainException()
        {
            var user = new User("alice", "hashed_password", 15);
            var poll = new Poll
            {
                ClosingTime = DateTime.UtcNow.AddHours(1),
                Options = new List<PollOption> { new("A") { Position = 0 } }
            };

            var ex = Assert.Throws<PollDomainException>(() =>
                poll.CastVote(user, new Vote(user.Id, new List<int> { 0 }, 30)));

            Assert.Equal("INSUFFICIENT_POINTS", ex.ErrorCode);
            Assert.Equal(poll.Id, ex.PollId);
        }

        [Fact]
        public void Boost_ActiveUnboostedPollWithEnoughPoints_DeductsPointsAndMarksBoosted()
        {
            var user = new User("alice", "hash", 100);
            var poll = new Poll
            {
                ClosingTime = DateTime.UtcNow.AddHours(1)
            };

            poll.Boost(user, 30);

            Assert.True(poll.IsBoosted);
            Assert.Equal(70, user.PointsBalance);
        }

        [Fact]
        public void Boost_ClosedPoll_ThrowsPollDomainException()
        {
            var user = new User("alice", "hash", 100);
            var poll = new Poll
            {
                ClosingTime = DateTime.UtcNow.AddMinutes(-1)
            };

            var ex = Assert.Throws<PollDomainException>(() => poll.Boost(user, 30));

            Assert.Equal("POLL_CLOSED", ex.ErrorCode);
            Assert.Equal(poll.Id, ex.PollId);
            Assert.Equal(100, user.PointsBalance);
        }

        [Fact]
        public void Boost_DailyChallengePoll_ThrowsPollDomainException()
        {
            var user = new User("alice", "hash", 100);
            var poll = new Poll
            {
                ClosingTime = DateTime.UtcNow.AddHours(1),
                IsDailyChallenge = true
            };

            var ex = Assert.Throws<PollDomainException>(() => poll.Boost(user, 30));

            Assert.Equal("DAILY_CHALLENGE_CANNOT_BE_BOOSTED", ex.ErrorCode);
            Assert.Equal(poll.Id, ex.PollId);
            Assert.Equal(100, user.PointsBalance);
        }

        [Fact]
        public void Boost_AlreadyBoostedPoll_ThrowsPollDomainException()
        {
            var user = new User("alice", "hash", 100);
            var poll = new Poll
            {
                ClosingTime = DateTime.UtcNow.AddHours(1),
                IsBoosted = true
            };

            var ex = Assert.Throws<PollDomainException>(() => poll.Boost(user, 30));

            Assert.Equal("POLL_ALREADY_BOOSTED", ex.ErrorCode);
            Assert.Equal(poll.Id, ex.PollId);
            Assert.Equal(100, user.PointsBalance);
        }

        [Fact]
        public void Boost_InsufficientPoints_ThrowsPollDomainException()
        {
            var user = new User("alice", "hash", 20);
            var poll = new Poll
            {
                ClosingTime = DateTime.UtcNow.AddHours(1)
            };

            var ex = Assert.Throws<PollDomainException>(() => poll.Boost(user, 30));

            Assert.Equal("INSUFFICIENT_POINTS", ex.ErrorCode);
            Assert.Equal(poll.Id, ex.PollId);
            Assert.Equal(20, user.PointsBalance);
        }

        [Fact]
        public void Resolve_PollNotClosed_ThrowsPollDomainException()
        {
            var poll = new Poll
            {
                ClosingTime = DateTime.UtcNow.AddMinutes(15)
            };

            var ex = Assert.Throws<PollDomainException>(() => poll.Resolve());

            Assert.Equal("POLL_NOT_CLOSED", ex.ErrorCode);
            Assert.Equal(poll.Id, ex.PollId);
        }

        [Fact]
        public void Resolve_AlreadyResolvedPoll_ReturnsEmptyDictionary()
        {
            var poll = new Poll
            {
                ClosingTime = DateTime.UtcNow.AddMinutes(-5),
                IsResolved = true
            };

            var payouts = poll.Resolve();

            Assert.Empty(payouts);
        }

        [Fact]
        public void Resolve_StandardEarlyBirdAndDailyChallengeWinners_CalculatesCorrectPayouts()
        {
            var earlyWinner = new User("early", "hash", 100);
            var standardWinner = new User("standard", "hash", 100);
            var loser = new User("loser", "hash", 100);

            var standardPoll = new Poll
            {
                CreatedAt = DateTime.UtcNow.AddMinutes(-50),
                ClosingTime = DateTime.UtcNow.AddMinutes(10),
                Options = new List<PollOption>
                {
                    new("Option 0") { Position = 0 },
                    new("Option 1") { Position = 1 }
                }
            };

            standardPoll.Votes.Add(new Vote(earlyWinner.Id, new List<int> { 0 }, 20) { IsEarlyBird = true });
            standardPoll.Votes.Add(new Vote(standardWinner.Id, new List<int> { 0 }, 20) { IsEarlyBird = false });
            standardPoll.Votes.Add(new Vote(loser.Id, new List<int> { 1 }, 20) { IsEarlyBird = false });
            standardPoll.ClosingTime = DateTime.UtcNow.AddSeconds(-1);

            var standardPayouts = standardPoll.Resolve();

            Assert.True(standardPoll.IsResolved);
            Assert.Equal(50, standardPayouts[earlyWinner.Id]);
            Assert.Equal(40, standardPayouts[standardWinner.Id]);
            Assert.False(standardPayouts.ContainsKey(loser.Id));

            var dailyPoll = new Poll
            {
                IsDailyChallenge = true,
                ClosingTime = DateTime.UtcNow.AddSeconds(-1),
                Options = new List<PollOption>
                {
                    new("Yes") { Position = 0 },
                    new("No") { Position = 1 }
                }
            };
            dailyPoll.Votes.Add(new Vote(standardWinner.Id, new List<int> { 0 }, 20));

            var dailyPayouts = dailyPoll.Resolve();

            Assert.Equal(60, dailyPayouts[standardWinner.Id]);
        }

        [Fact]
        public void PollEqualityAndComparison_BehaveAsExpected()
        {
            var earlier = new Poll { ClosingTime = DateTime.UtcNow.AddMinutes(5) };
            var later = new Poll { ClosingTime = DateTime.UtcNow.AddMinutes(10) };

            Assert.True(earlier.Equals(earlier));
            Assert.False(earlier.Equals(later));
            Assert.False(earlier.Equals(null));
            Assert.True(earlier.CompareTo(later) < 0);
        }

        [Fact]
        public void EntityConstructorsAndExtensions_ValidateInputs()
        {
            Assert.Throws<ArgumentException>(() => new User("", "hash"));
            Assert.Throws<ArgumentException>(() => new User("user", ""));
            Assert.Throws<ArgumentException>(() => new Comment(Guid.NewGuid(), "", "text"));
            Assert.Throws<ArgumentException>(() => new Comment(Guid.NewGuid(), "author", ""));
            Assert.Throws<ArgumentException>(() => new Notification(Guid.NewGuid(), ""));

            var follower = new User("follower", "hash");
            var followedId = Guid.NewGuid();
            follower.Following.Add(new UserFollow { FollowerId = follower.Id, FollowedId = followedId });

            Assert.True(follower.IsFollowing(followedId));
            Assert.False(follower.IsFollowing(Guid.NewGuid()));
        }

        [Fact]
        public void OperationResult_Map_ExecutesMatchingBranchForEntity()
        {
            var user = new User("alice", "hash", 100);
            var success = OperationResult<User>.Success(user);
            var failure = OperationResult<User>.Failure("User not found.");

            var okText = success.Map(u => $"{u.Username}:{u.PointsBalance}", err => $"err:{err}");
            var failText = failure.Map(u => $"{u.Username}:{u.PointsBalance}", err => $"err:{err}");

            Assert.Equal("alice:100", okText);
            Assert.Equal("err:User not found.", failText);
        }
    }
}