using System;
using System.Collections.Generic;
using PollSystem.Extensions;
using PollSystem.Models.Common;
using PollSystem.Models.Entities;
using Xunit;

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
        public void CastVote_ClosedPoll_ThrowsInvalidOperationException()
        {
            var user = new User("alice", "hashed_password", 100);
            var poll = new Poll
            {
                ClosingTime = DateTime.UtcNow.AddMinutes(-1),
                Options = new List<PollOption> { new("Yes") { Position = 0 } }
            };

            Assert.Throws<InvalidOperationException>(() =>
                poll.CastVote(user, new Vote(user.Id, new List<int> { 0 }, 10)));
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
        public void CastVote_MultipleOptionsWhenDisallowed_ThrowsInvalidOperationException()
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

            Assert.Throws<InvalidOperationException>(() =>
                poll.CastVote(user, new Vote(user.Id, new List<int> { 0, 1 }, 0)));
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
        public void CastVote_InsufficientPoints_ThrowsInvalidOperationException()
        {
            var user = new User("alice", "hashed_password", 15);
            var poll = new Poll
            {
                ClosingTime = DateTime.UtcNow.AddHours(1),
                Options = new List<PollOption> { new("A") { Position = 0 } }
            };

            Assert.Throws<InvalidOperationException>(() =>
                poll.CastVote(user, new Vote(user.Id, new List<int> { 0 }, 30)));
        }

        [Fact]
        public void Resolve_PollNotClosed_ThrowsInvalidOperationException()
        {
            var poll = new Poll
            {
                ClosingTime = DateTime.UtcNow.AddMinutes(15)
            };

            Assert.Throws<InvalidOperationException>(() => poll.Resolve());
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
        public void OperationResult_Map_TransformsSuccessAndFailureCorrectly()
        {
            var success = OperationResult<int>.Success(42);
            var failure = OperationResult<int>.Failure("Failed operation");

            var successOutput = success.Map(val => $"OK:{val}", err => $"ERR:{err}");
            var failureOutput = failure.Map(val => $"OK:{val}", err => $"ERR:{err}");

            Assert.Equal("OK:42", successOutput);
            Assert.Equal("ERR:Failed operation", failureOutput);
        }
    }
}