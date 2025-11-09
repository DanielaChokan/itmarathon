using CSharpFunctionalExtensions;
using Epam.ItMarathon.ApiService.Application.UseCases.User.Commands;
using Epam.ItMarathon.ApiService.Domain.Abstract;
using Epam.ItMarathon.ApiService.Domain.Shared.ValidationErrors;
using FluentValidation.Results;
using MediatR;
using RoomAggregate = Epam.ItMarathon.ApiService.Domain.Aggregate.Room.Room;

namespace Epam.ItMarathon.ApiService.Application.UseCases.User.Handlers
{
    /// <summary>
    /// Handler for deleting a user from a room.
    /// </summary>
    public class DeleteUserHandler(IRoomRepository roomRepository, IUserReadOnlyRepository userReadOnlyRepository)
        : IRequestHandler<DeleteUserRequest, Result<RoomAggregate, ValidationResult>>
    {
        public async Task<Result<RoomAggregate, ValidationResult>> Handle(DeleteUserRequest request,
    CancellationToken cancellationToken)
        {
            // 1. Отримати користувача з userCode
            var authUserResult = await userReadOnlyRepository.GetByCodeAsync(
                request.UserCode,
                cancellationToken,
                includeRoom: true,
                includeWishes: false);

            if (authUserResult.IsFailure)
            {
                return authUserResult.ConvertFailure<RoomAggregate>();
            }

            var authUser = authUserResult.Value;

            // 2. Перевірити, чи користувач є адміністратором
            if (!authUser.IsAdmin)
            {
                return Result.Failure<RoomAggregate, ValidationResult>(new ForbiddenError([
                    new ValidationFailure("userCode", "User is not an administrator.")
                ]));
            }

            // 3. Отримати кімнату за userCode
            var roomResult = await roomRepository.GetByUserCodeAsync(request.UserCode, cancellationToken);
            if (roomResult.IsFailure)
            {
                return roomResult;
            }

            var room = roomResult.Value;

            // 4. Перевірити, чи адмін не намагається видалити себе
            if (authUser.Id == request.UserId)
            {
                return Result.Failure<RoomAggregate, ValidationResult>(new BadRequestError([
                    new ValidationFailure("id", "Administrator cannot delete themselves.")
                ]));
            }

            // 5. Знайти користувача для видалення в кімнаті
            var userToDelete = room.Users.FirstOrDefault(u => u.Id == request.UserId);
            if (userToDelete is null)
            {
                // Перевіряємо чи користувач взагалі існує в БД
                var userExistsResult = await userReadOnlyRepository.GetByIdAsync(
                    request.UserId!.Value,
                    cancellationToken,
                    includeRoom: true,
                    includeWishes: false);

                if (userExistsResult.IsFailure)
                {
                    // Користувача не існує взагалі
                    return Result.Failure<RoomAggregate, ValidationResult>(new NotFoundError([
                        new ValidationFailure("id", "User with the specified Id was not found.")
                    ]));
                }

                // Користувач існує, перевіряємо чи він в іншій кімнаті
                if (userExistsResult.Value.RoomId != authUser.RoomId)
                {
                    return Result.Failure<RoomAggregate, ValidationResult>(new ForbiddenError([
                        new ValidationFailure("id", "User with userCode and user with Id belong to different rooms.")
                    ]));
                }

                // Користувач в тій же кімнаті, але не в колекції room.Users - помилка даних
                return Result.Failure<RoomAggregate, ValidationResult>(new BadRequestError([
                    new ValidationFailure("id", "Data inconsistency detected. User exists in room but not loaded properly.")
                ]));
            }

            // 6. Видалити користувача з кімнати
            var deleteResult = room.DeleteUser(request.UserId);
            if (deleteResult.IsFailure)
            {
                return deleteResult;
            }

            // 7. Оновити кімнату в репозиторії
            var updateResult = await roomRepository.UpdateAsync(room, cancellationToken);
            if (updateResult.IsFailure)
            {
                return Result.Failure<RoomAggregate, ValidationResult>(new BadRequestError([
                    new ValidationFailure(string.Empty, updateResult.Error)
                ]));
            }

            // 8. Повернути оновлену кімнату
            return room;
        }
    }
}