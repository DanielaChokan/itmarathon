using CSharpFunctionalExtensions;
using Epam.ItMarathon.ApiService.Application.Tests;
using Epam.ItMarathon.ApiService.Application.UseCases.User.Commands;
using Epam.ItMarathon.ApiService.Application.UseCases.User.Handlers;
using Epam.ItMarathon.ApiService.Domain.Abstract;
using Epam.ItMarathon.ApiService.Domain.Aggregate.Room;
using Epam.ItMarathon.ApiService.Domain.Shared.ValidationErrors;
using FluentAssertions;
using FluentValidation.Results;
using NSubstitute;

namespace Epam.ItMarathon.ApiService.Application.Tests.UserCases.Commands
{
    /// <summary>
    /// Unit tests for the <see cref="DeleteUserHandler"/> class.
    /// </summary>
    public class DeleteUserHandlerTests
    {
        private readonly IRoomRepository _roomRepositoryMock;
        private readonly IUserReadOnlyRepository _userReadOnlyRepositoryMock;
        private readonly DeleteUserHandler _handler;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteUserHandlerTests"/> class with mocked dependencies.
        /// </summary>
        public DeleteUserHandlerTests()
        {
            _roomRepositoryMock = Substitute.For<IRoomRepository>();
            _userReadOnlyRepositoryMock = Substitute.For<IUserReadOnlyRepository>();
            _handler = new DeleteUserHandler(_roomRepositoryMock, _userReadOnlyRepositoryMock);
        }

        /// <summary>
        /// Позитивний тест: успішне видалення користувача
        /// </summary>
        [Fact]
        public async Task Handle_ShouldDeleteUser_WhenAllConditionsAreMet()
        {
            // Arrange
            var adminUser = DataFakers.UserFaker
                .RuleFor(u => u.Id, _ => 1UL)
                .RuleFor(u => u.IsAdmin, _ => true)
                .RuleFor(u => u.RoomId, _ => 1UL)
                .Generate();
            
            var userToDelete = DataFakers.UserFaker
                .RuleFor(u => u.Id, _ => 2UL)
                .RuleFor(u => u.IsAdmin, _ => false)
                .RuleFor(u => u.RoomId, _ => 1UL)
                .Generate();

            var room = DataFakers.RoomFaker
                .RuleFor(r => r.Id, _ => 1UL)
                .RuleFor(r => r.ClosedOn, _ => (DateTime?)null)
                .RuleFor(r => r.Users, _ => new List<Domain.Entities.User.User> { adminUser, userToDelete })
                .Generate();

            var command = new DeleteUserRequest(adminUser.AuthCode, userToDelete.Id);

            _userReadOnlyRepositoryMock
                .GetByCodeAsync(adminUser.AuthCode, Arg.Any<CancellationToken>(), includeRoom: true, includeWishes: false)
                .Returns(adminUser);

            _roomRepositoryMock
                .GetByUserCodeAsync(adminUser.AuthCode, Arg.Any<CancellationToken>())
                .Returns(room);

            _roomRepositoryMock
                .UpdateAsync(Arg.Any<Room>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(Result.Success()));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Users.Should().NotContain(u => u.Id == userToDelete.Id);
            await _roomRepositoryMock.Received(1).UpdateAsync(Arg.Any<Room>(), Arg.Any<CancellationToken>());
        }

        /// <summary>
        /// Користувача з userCode не знайдено
        /// </summary>
        [Fact]
        public async Task Handle_ShouldReturnNotFoundError_WhenUserCodeNotFound()
        {
            // Arrange
            var command = new DeleteUserRequest("invalid-code", 1);

            _userReadOnlyRepositoryMock
                .GetByCodeAsync(command.UserCode, Arg.Any<CancellationToken>(), includeRoom: true, includeWishes: false)
                .Returns(Result.Failure<Domain.Entities.User.User, ValidationResult>(
                    new NotFoundError([new ValidationFailure("userCode", "User with such code not found")])));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<NotFoundError>();
            result.Error.Errors.Should().Contain(e => e.PropertyName == "userCode");
        }

        /// <summary>
        /// Користувач з userCode не адміністратор
        /// </summary>
        [Fact]
        public async Task Handle_ShouldReturnForbiddenError_WhenUserIsNotAdmin()
        {
            // Arrange
            var regularUser = DataFakers.UserFaker
                .RuleFor(u => u.IsAdmin, _ => false)
                .Generate();

            var command = new DeleteUserRequest(regularUser.AuthCode, 2);

            _userReadOnlyRepositoryMock
                .GetByCodeAsync(regularUser.AuthCode, Arg.Any<CancellationToken>(), includeRoom: true, includeWishes: false)
                .Returns(regularUser);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<ForbiddenError>();
            result.Error.Errors.Should().Contain(e => e.PropertyName == "userCode");
        }

        /// <summary>
        /// Користувача з id не знайдено
        /// </summary>
        [Fact]
        public async Task Handle_ShouldReturnNotFoundError_WhenUserIdNotFound()
        {
            // Arrange
            var adminUser = DataFakers.UserFaker
                .RuleFor(u => u.IsAdmin, _ => true)
                .Generate();

            var room = DataFakers.RoomFaker
                .RuleFor(r => r.Users, _ => new List<Domain.Entities.User.User> { adminUser })
                .Generate();

            var command = new DeleteUserRequest(adminUser.AuthCode, 999UL);

            _userReadOnlyRepositoryMock
                .GetByCodeAsync(adminUser.AuthCode, Arg.Any<CancellationToken>(), includeRoom: true, includeWishes: false)
                .Returns(adminUser);

            _roomRepositoryMock
                .GetByUserCodeAsync(adminUser.AuthCode, Arg.Any<CancellationToken>())
                .Returns(room);

            // Mock GetByIdAsync щоб повернути NotFound
            _userReadOnlyRepositoryMock
                .GetByIdAsync(999UL, Arg.Any<CancellationToken>(), includeRoom: true, includeWishes: false)
                .Returns(Result.Failure<Domain.Entities.User.User, ValidationResult>(
                    new NotFoundError([new ValidationFailure("id", "User not found")])));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<NotFoundError>();
            result.Error.Errors.Should().Contain(e => e.PropertyName == "id");
        }

        /// <summary>
        /// Користувач з userCode і id належать до різних кімнат
        /// </summary>
        [Fact]
        public async Task Handle_ShouldReturnForbiddenError_WhenUsersInDifferentRooms()
        {
            // Arrange
            var adminUser = DataFakers.UserFaker
                .RuleFor(u => u.Id, _ => 1UL)
                .RuleFor(u => u.IsAdmin, _ => true)
                .RuleFor(u => u.RoomId, _ => 1UL)
                .Generate();

            var userInDifferentRoom = DataFakers.UserFaker
                .RuleFor(u => u.Id, _ => 2UL)
                .RuleFor(u => u.IsAdmin, _ => false)
                .RuleFor(u => u.RoomId, _ => 2UL) // Інша кімната
                .Generate();

            var room = DataFakers.RoomFaker
                .RuleFor(r => r.Id, _ => 1UL)
                .RuleFor(r => r.Users, _ => new List<Domain.Entities.User.User> { adminUser })
                .Generate();

            var command = new DeleteUserRequest(adminUser.AuthCode, userInDifferentRoom.Id);

            _userReadOnlyRepositoryMock
                .GetByCodeAsync(adminUser.AuthCode, Arg.Any<CancellationToken>(), includeRoom: true, includeWishes: false)
                .Returns(adminUser);

            _roomRepositoryMock
                .GetByUserCodeAsync(adminUser.AuthCode, Arg.Any<CancellationToken>())
                .Returns(room);

            // Mock GetByIdAsync щоб повернути користувача з іншої кімнати
            _userReadOnlyRepositoryMock
                .GetByIdAsync(userInDifferentRoom.Id, Arg.Any<CancellationToken>(), includeRoom: true, includeWishes: false)
                .Returns(userInDifferentRoom);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<ForbiddenError>();
            result.Error.Errors.Should().Contain(e => e.PropertyName == "id");
            result.Error.Errors.Should().Contain(e => e.ErrorMessage.Contains("different rooms"));
        }

        /// <summary>
        /// Адміністратор намагається видалити сам себе
        /// </summary>
        [Fact]
        public async Task Handle_ShouldReturnBadRequestError_WhenAdminTriesToDeleteThemselves()
        {
            // Arrange
            var adminUser = DataFakers.UserFaker
                .RuleFor(u => u.Id, _ => 1UL)
                .RuleFor(u => u.IsAdmin, _ => true)
                .RuleFor(u => u.RoomId, _ => 1UL)
                .Generate();

            var room = DataFakers.RoomFaker
                .RuleFor(r => r.Id, _ => 1UL)
                .RuleFor(r => r.ClosedOn, _ => (DateTime?)null)
                .RuleFor(r => r.Users, _ => new List<Domain.Entities.User.User> { adminUser })
                .Generate();

            var command = new DeleteUserRequest(adminUser.AuthCode, adminUser.Id);

            _userReadOnlyRepositoryMock
                .GetByCodeAsync(adminUser.AuthCode, Arg.Any<CancellationToken>(), includeRoom: true, includeWishes: false)
                .Returns(adminUser);

            _roomRepositoryMock
                .GetByUserCodeAsync(adminUser.AuthCode, Arg.Any<CancellationToken>())
                .Returns(room);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<BadRequestError>();
            result.Error.Errors.Should().Contain(e => e.PropertyName == "id");
        }

        /// <summary>
        /// Кімната вже закрита
        /// </summary>
        [Fact]
        public async Task Handle_ShouldReturnBadRequestError_WhenRoomIsClosed()
        {
            // Arrange
            var adminUser = DataFakers.UserFaker
                .RuleFor(u => u.Id, _ => 1UL)
                .RuleFor(u => u.IsAdmin, _ => true)
                .RuleFor(u => u.RoomId, _ => 1UL)
                .Generate();

            var userToDelete = DataFakers.UserFaker
                .RuleFor(u => u.Id, _ => 2UL)
                .RuleFor(u => u.IsAdmin, _ => false)
                .RuleFor(u => u.RoomId, _ => 1UL)
                .Generate();

            var room = DataFakers.RoomFaker
                .RuleFor(r => r.Id, _ => 1UL)
                .RuleFor(r => r.ClosedOn, _ => DateTime.UtcNow.AddDays(-1)) // Кімната закрита
                .RuleFor(r => r.Users, _ => new List<Domain.Entities.User.User> { adminUser, userToDelete })
                .Generate();

            var command = new DeleteUserRequest(adminUser.AuthCode, userToDelete.Id);

            _userReadOnlyRepositoryMock
                .GetByCodeAsync(adminUser.AuthCode, Arg.Any<CancellationToken>(), includeRoom: true, includeWishes: false)
                .Returns(adminUser);

            _roomRepositoryMock
                .GetByUserCodeAsync(adminUser.AuthCode, Arg.Any<CancellationToken>())
                .Returns(room);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<BadRequestError>();
            result.Error.Errors.Should().Contain(e => e.PropertyName == "room.ClosedOn");
        }
    }
}