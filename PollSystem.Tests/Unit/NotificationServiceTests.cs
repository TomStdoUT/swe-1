using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using PollSystem.Models.Entities;
using PollSystem.Repositories.Interfaces;
using PollSystem.Services;
using Xunit;

namespace PollSystem.Tests.Unit
{
    public class NotificationServiceTests
    {
        private readonly Mock<INotificationRepository> _notificationRepo;
        private readonly Mock<IUnitOfWork> _unitOfWork;
        private readonly NotificationService _sut;

        public NotificationServiceTests()
        {
            _notificationRepo = new Mock<INotificationRepository>();
            _unitOfWork = new Mock<IUnitOfWork>();
            _sut = new NotificationService(_notificationRepo.Object, _unitOfWork.Object);
        }

        [Fact]
        public async Task CreateNotificationAsync_ValidData_AddsAndSavesNotification()
        {
            var userId = Guid.NewGuid();

            var notification = await _sut.CreateNotificationAsync(userId, "Poll ended!");

            Assert.Equal(userId, notification.TargetUserId);
            Assert.Equal("Poll ended!", notification.Content);
            _notificationRepo.Verify(r => r.Add(It.IsAny<Notification>()), Times.Once);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void GetUserNotifications_ReturnsNotificationsFromRepository()
        {
            var userId = Guid.NewGuid();
            var expected = new List<Notification>
            {
                new(userId, "First notification"),
                new(userId, "Second notification")
            };
            _notificationRepo.Setup(r => r.GetByUserId(userId)).Returns(expected);

            var result = _sut.GetUserNotifications(userId).ToList();

            Assert.Equal(2, result.Count);
            Assert.Equal("First notification", result[0].Content);
        }
    }
}