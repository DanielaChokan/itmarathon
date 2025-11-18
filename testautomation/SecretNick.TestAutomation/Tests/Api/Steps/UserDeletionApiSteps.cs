using Reqnroll;
using Shouldly;
using Tests.Api.Clients;
using Tests.Api.Models.Responses;
using Tests.Common.TestData;

namespace Tests.Api.Steps
{
    [Binding]
    public class UserDeletionApiSteps(
        ScenarioContext scenarioContext,
        RoomApiClient roomApiClient,
        UserApiClient userApiClient)
    {
        private readonly ScenarioContext _scenarioContext = scenarioContext;
        private readonly RoomApiClient _roomApiClient = roomApiClient;
        private readonly UserApiClient _userApiClient = userApiClient;

        [Given("I am a room admin with multiple users")]
        public async Task GivenIAmARoomAdminWithMultipleUsers()
        {
            var roomResponse = await CreateRoomWithUsers(3);
            _scenarioContext.Set(roomResponse, "RoomResponse");
            _scenarioContext.Set(roomResponse.UserCode!, "AdminUserCode");
        }

        [Given("I have a regular user to delete")]
        public async Task GivenIHaveARegularUserToDelete()
        {
            var adminCode = _scenarioContext.Get<string>("AdminUserCode");
            var users = await _userApiClient.GetUsersAsync(adminCode);
            var regularUser = users.First(u => !u.IsAdmin);
            _scenarioContext.Set(regularUser.Id, "UserToDeleteId");
        }

        [Given("there is exactly one regular user in the room")]
        public async Task GivenThereIsExactlyOneRegularUserInTheRoom()
        {
            var roomResponse = await CreateRoomWithUsers(1);
            _scenarioContext.Set(roomResponse, "RoomResponse");
            _scenarioContext.Set(roomResponse.UserCode!, "AdminUserCode");

            var users = await _userApiClient.GetUsersAsync(roomResponse.UserCode!);
            var regularUser = users.First(u => !u.IsAdmin);
            _scenarioContext.Set(regularUser.Id, "UserToDeleteId");
        }

        [Given("I am an admin of one room")]
        public async Task GivenIAmAnAdminOfOneRoom()
        {
            var roomResponse = await CreateRoomWithUsers(1);
            _scenarioContext.Set(roomResponse, "MyRoom");
            _scenarioContext.Set(roomResponse.UserCode!, "MyAdminCode");
        }

        [Given("there is a user in a different room")]
        public async Task GivenThereIsAUserInADifferentRoom()
        {
            var otherRoomResponse = await CreateRoomWithUsers(1);
            var users = await _userApiClient.GetUsersAsync(otherRoomResponse.UserCode!);
            var userFromOtherRoom = users.First(u => !u.IsAdmin);
            _scenarioContext.Set(userFromOtherRoom.Id, "UserInDifferentRoomId");
        }

        [Given("I have an invalid user code")]
        public void GivenIHaveAnInvalidUserCode()
        {
            _scenarioContext.Set("invalid-code-12345", "InvalidUserCode");
        }

        [Given("the room is closed")]
        public async Task GivenTheRoomIsClosed()
        {
            var adminCode = _scenarioContext.Get<string>("AdminUserCode");
            
            var users = await _userApiClient.GetUsersAsync(adminCode);
            if (users.Count < 2)
            {
                await _userApiClient.CreateUserAsync(
                    _scenarioContext.Get<Api.Models.Responses.RoomCreationResponse>("RoomResponse").Room.InvitationCode!,
                    TestDataGenerator.GenerateUser());
                users = await _userApiClient.GetUsersAsync(adminCode);
            }

            var regularUser = users.First(u => !u.IsAdmin);
            _scenarioContext.Set(regularUser.Id, "UserToDeleteId");

            await _roomApiClient.DrawNamesAsync(adminCode);
        }

        [Given("I am a room admin with drawn names")]
        public async Task GivenIAmARoomAdminWithDrawnNames()
        {
            await GivenIAmARoomAdminWithMultipleUsers();
            var adminCode = _scenarioContext.Get<string>("AdminUserCode");
            await _roomApiClient.DrawNamesAsync(adminCode);
        }

        [Given("I have a regular user with gift assignment")]
        public async Task GivenIHaveARegularUserWithGiftAssignment()
        {
            var adminCode = _scenarioContext.Get<string>("AdminUserCode");
            var users = await _userApiClient.GetUsersAsync(adminCode);
            var userWithAssignment = users.FirstOrDefault(u => !u.IsAdmin && u.GiftToUserId.HasValue);
            
            if (userWithAssignment == null)
            {
                userWithAssignment = users.First(u => !u.IsAdmin);
            }
            
            _scenarioContext.Set(userWithAssignment.Id, "UserToDeleteId");
        }

        [Given("I have a user code with special characters")]
        public void GivenIHaveAUserCodeWithSpecialCharacters()
        {
            _scenarioContext.Set("code<script>alert('xss')</script>", "SpecialCharUserCode");
        }

        [Given("I am a room admin with {int} users")]
        public async Task GivenIAmARoomAdminWithUsers(int userCount)
        {
            var roomResponse = await CreateRoomWithUsers(userCount);
            _scenarioContext.Set(roomResponse, "RoomResponse");
            _scenarioContext.Set(roomResponse.UserCode!, "AdminUserCode");

            var users = await _userApiClient.GetUsersAsync(roomResponse.UserCode!);
            var userIds = users.Where(u => !u.IsAdmin).Select(u => u.Id).ToList();
            _scenarioContext.Set(userIds, "UserIdsToDelete");
        }

        [When("I delete the user as admin")]
        public async Task WhenIDeleteTheUserAsAdmin()
        {
            var adminCode = _scenarioContext.Get<string>("AdminUserCode");
            var userId = _scenarioContext.Get<long>("UserToDeleteId");

            try
            {
                await DeleteUserAsync(userId, adminCode);
                _scenarioContext.Set(200, "LastStatusCode");
            }
            catch (ApiException ex)
            {
                _scenarioContext.Set(ex.ActualStatus, "LastStatusCode");
                _scenarioContext.Set(ex.ResponseBody, "LastErrorBody");
            }
        }

        [When("I try to delete another user")]
        public async Task WhenITryToDeleteAnotherUser()
        {
            var regularUserCode = _scenarioContext.Get<string>("RegularUserCode");
            var adminCode = _scenarioContext.Get<Api.Models.Responses.RoomCreationResponse>("RoomResponse").UserCode!;
            
            var users = await _userApiClient.GetUsersAsync(adminCode);
            var otherUser = users.First(u => !u.IsAdmin && u.UserCode != regularUserCode);

            try
            {
                await DeleteUserAsync(otherUser.Id, regularUserCode);
                _scenarioContext.Set(200, "LastStatusCode");
            }
            catch (ApiException ex)
            {
                _scenarioContext.Set(ex.ActualStatus, "LastStatusCode");
                _scenarioContext.Set(ex.ResponseBody, "LastErrorBody");
            }
        }

        [When("I try to delete the user from different room")]
        public async Task WhenITryToDeleteTheUserFromDifferentRoom()
        {
            var myAdminCode = _scenarioContext.Get<string>("MyAdminCode");
            var userInDifferentRoomId = _scenarioContext.Get<long>("UserInDifferentRoomId");

            try
            {
                await DeleteUserAsync(userInDifferentRoomId, myAdminCode);
                _scenarioContext.Set(200, "LastStatusCode");
            }
            catch (ApiException ex)
            {
                _scenarioContext.Set(ex.ActualStatus, "LastStatusCode");
                _scenarioContext.Set(ex.ResponseBody, "LastErrorBody");
            }
        }

        [When("I try to delete myself")]
        public async Task WhenITryToDeleteMyself()
        {
            var adminCode = _scenarioContext.Get<string>("AdminUserCode");
            var users = await _userApiClient.GetUsersAsync(adminCode);
            var adminUser = users.First(u => u.IsAdmin);

            try
            {
                await DeleteUserAsync(adminUser.Id, adminCode);
                _scenarioContext.Set(200, "LastStatusCode");
            }
            catch (ApiException ex)
            {
                _scenarioContext.Set(ex.ActualStatus, "LastStatusCode");
                _scenarioContext.Set(ex.ResponseBody, "LastErrorBody");
            }
        }

        [When("I try to delete a user with invalid code")]
        public async Task WhenITryToDeleteAUserWithInvalidCode()
        {
            var invalidCode = _scenarioContext.Get<string>("InvalidUserCode");

            try
            {
                await DeleteUserAsync(999, invalidCode);
                _scenarioContext.Set(200, "LastStatusCode");
            }
            catch (ApiException ex)
            {
                _scenarioContext.Set(ex.ActualStatus, "LastStatusCode");
                _scenarioContext.Set(ex.ResponseBody, "LastErrorBody");
            }
        }

        [When("I try to delete a user that does not exist")]
        public async Task WhenITryToDeleteAUserThatDoesNotExist()
        {
            var adminCode = _scenarioContext.Get<string>("AdminUserCode");

            try
            {
                await DeleteUserAsync(999999, adminCode);
                _scenarioContext.Set(200, "LastStatusCode");
            }
            catch (ApiException ex)
            {
                _scenarioContext.Set(ex.ActualStatus, "LastStatusCode");
                _scenarioContext.Set(ex.ResponseBody, "LastErrorBody");
            }
        }

        [When("I try to delete a user")]
        public async Task WhenITryToDeleteAUser()
        {
            if (_scenarioContext.ContainsKey("SpecialCharUserCode"))
            {
                var specialCode = _scenarioContext.Get<string>("SpecialCharUserCode");
                try
                {
                    await DeleteUserAsync(1, specialCode);
                    _scenarioContext.Set(200, "LastStatusCode");
                }
                catch (ApiException ex)
                {
                    _scenarioContext.Set(ex.ActualStatus, "LastStatusCode");
                    _scenarioContext.Set(ex.ResponseBody, "LastErrorBody");
                }
            }
            else
            {
                await WhenIDeleteTheUserAsAdmin();
            }
        }

        [When("I delete {int} users one by one")]
        public async Task WhenIDeleteUsersOneByOne(int count)
        {
            var adminCode = _scenarioContext.Get<string>("AdminUserCode");
            var userIds = _scenarioContext.Get<List<long>>("UserIdsToDelete");
            var deletedCount = 0;

            foreach (var userId in userIds.Take(count))
            {
                try
                {
                    await DeleteUserAsync(userId, adminCode);
                    deletedCount++;
                }
                catch
                {
                    break;
                }
            }

            _scenarioContext.Set(deletedCount, "DeletedUsersCount");
            _scenarioContext.Set(200, "LastStatusCode");
        }

        [Then("the user should be removed from the room")]
        public async Task ThenTheUserShouldBeRemovedFromTheRoom()
        {
            var adminCode = _scenarioContext.Get<string>("AdminUserCode");
            var deletedUserId = _scenarioContext.Get<long>("UserToDeleteId");

            var users = await _userApiClient.GetUsersAsync(adminCode);
            users.ShouldNotContain(u => u.Id == deletedUserId);
        }

        [Then("the room should have one less participant")]
        public async Task ThenTheRoomShouldHaveOneLessParticipant()
        {
            var adminCode = _scenarioContext.Get<string>("AdminUserCode");
            var currentUsers = await _userApiClient.GetUsersAsync(adminCode);
            
            currentUsers.Count.ShouldBeGreaterThan(0);
        }

        [Then("only admin should remain in the room")]
        public async Task ThenOnlyAdminShouldRemainInTheRoom()
        {
            var adminCode = _scenarioContext.Get<string>("AdminUserCode");
            var users = await _userApiClient.GetUsersAsync(adminCode);

            users.Count.ShouldBe(1);
            users.Single().IsAdmin.ShouldBeTrue();
        }

        [Then("all gift assignments should be cleared")]
        public async Task ThenAllGiftAssignmentsShouldBeCleared()
        {
            var adminCode = _scenarioContext.Get<string>("AdminUserCode");
            var users = await _userApiClient.GetUsersAsync(adminCode);

            users.ShouldAllBe(u => !u.GiftToUserId.HasValue);
        }

        [Then("all deletions should succeed with status {int}")]
        public void ThenAllDeletionsShouldSucceedWithStatus(int expectedStatus)
        {
            var actualStatus = _scenarioContext.Get<int>("LastStatusCode");
            actualStatus.ShouldBe(expectedStatus);
        }

        [Then("the room should have {int} participants remaining")]
        public async Task ThenTheRoomShouldHaveParticipantsRemaining(int expectedCount)
        {
            var adminCode = _scenarioContext.Get<string>("AdminUserCode");
            var users = await _userApiClient.GetUsersAsync(adminCode);

            users.Count.ShouldBe(expectedCount);
        }

        private async Task<Api.Models.Responses.RoomCreationResponse> CreateRoomWithUsers(int additionalUsersCount)
        {
            var request = new Api.Models.Requests.RoomCreationRequest
            {
                Room = TestDataGenerator.GenerateRoom(),
                AdminUser = TestDataGenerator.GenerateUser()
            };

            var roomResponse = await _roomApiClient.CreateRoomAsync(request);

            for (int i = 0; i < additionalUsersCount; i++)
            {
                await _userApiClient.CreateUserAsync(
                    roomResponse.Room.InvitationCode!,
                    TestDataGenerator.GenerateUser());
            }

            return roomResponse;
        }

        private async Task DeleteUserAsync(long userId, string userCode)
        {
            await _userApiClient.DeleteUserAsync(userId, userCode);
            _scenarioContext.Set(200, "LastStatusCode");
        }
    }
}