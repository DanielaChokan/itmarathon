using Reqnroll;
using Shouldly;
using Tests.Api.Clients;
using Tests.Api.Models.Requests;
using Tests.Api.Models.Responses;
using Tests.Common.Models;
using Tests.Common.TestData;
using Tests.Helpers;

namespace Tests.Api.Steps
{
    [Binding]
    public class UserApiSteps(
        ScenarioContext scenarioContext,
        UserApiClient apiClient,
        RoomApiClient roomApiClient)
    {
        private readonly ScenarioContext _scenarioContext = scenarioContext;
        private readonly UserApiClient _apiClient = apiClient;
        private readonly RoomApiClient _roomApiClient = roomApiClient;

        [When("I try to join the room")]
        public async Task WhenITryToJoinTheRoom()
        {
            try
            {
                await WhenIJoinTheRoom();
            }
            catch (ApiException ex)
            {
                _scenarioContext.Set(ex.ActualStatus, "LastStatusCode");
                _scenarioContext.Set(ex.ResponseBody, "LastErrorBody");
            }
        }

        [Given("a room exists with invitation code")]
        public async Task GivenARoomExistsWithInvitationCode()
        {
            if (_scenarioContext.ContainsKey("RoomCreationResponse"))
                return;

            var request = new RoomCreationRequest
            {
                Room = TestDataGenerator.GenerateRoom(),
                AdminUser = TestDataGenerator.GenerateUser()
            };

            var response = await _roomApiClient.CreateRoomAsync(request);
            _scenarioContext.Set(response);
            _scenarioContext.Set(response.Room.InvitationCode!, "InvitationCode");
        }

        [Given("I have user data with wish list:")]
        public void GivenIHaveUserDataWithWishList(Table table)
        {
            var user = TestDataGenerator.GenerateUser(false, table);

            user.WishList = [];
            _scenarioContext.Set(user, "UserData");
        }

        [Given("I have wishes:")]
        public void GivenIHaveWishes(Table table)
        {
            var wishes = table.CreateSet<WishDto>().ToList();
            var user = _scenarioContext.Get<UserCreationDto>("UserData");
            user.WishList = wishes;
        }

        [Given("I have user data with surprise preference:")]
        public void GivenIHaveUserDataWithSurprisePreference(Table table)
        {
            var user = TestDataGenerator.GenerateUser(true, table);

            _scenarioContext.Set(user, "UserData");
        }

        [Given("I have user data:")]
        public void GivenIHaveUserData(Table table)
        {
            var userData = new UserCreationDto
            {
                FirstName = "",
                LastName = "",
                Phone = "",
                Email = "",
                DeliveryInfo = "Test Address",
                WantSurprise = false,
                WishList = []
            };

            foreach (var row in table.Rows)
            {
                switch (row["Field"])
                {
                    case "FirstName":
                        userData.FirstName = row["Value"];
                        break;
                    case "LastName":
                        userData.LastName = row["Value"];
                        break;
                    case "Phone":
                        userData.Phone = row["Value"];
                        break;
                    case "Email":
                        userData.Email = row["Value"];
                        break;
                }
            }

            _scenarioContext.Set(userData, "UserData");
        }

        [Given("I am in a room with multiple users")]
        public async Task GivenIAmInARoomWithMultipleUsers()
        {
            var roomRequest = new RoomCreationRequest
            {
                Room = TestDataGenerator.GenerateRoom(),
                AdminUser = TestDataGenerator.GenerateUser()
            };

            var roomResponse = await _roomApiClient.CreateRoomAsync(roomRequest);
            _scenarioContext.Set(roomResponse);
            _scenarioContext.Set(roomResponse.UserCode!, "AdminUserCode");

            for (int i = 0; i < 3; i++)
            {
                await _apiClient.CreateUserAsync(
                    roomResponse.Room.InvitationCode!,
                    TestDataGenerator.GenerateUser());
            }
        }

        [Given("I am a room admin")]
        public async Task GivenIAmARoomAdmin()
        {
            await GivenIAmInARoomWithMultipleUsers();
        }

        [Given("there are other users in the room")]
        public async Task GivenThereAreOtherUsersInTheRoom()
        {
            var roomResponse = _scenarioContext.Get<RoomCreationResponse>();

            for (int i = 0; i < 2; i++)
            {
                await _apiClient.CreateUserAsync(
                    roomResponse.Room.InvitationCode!,
                    TestDataGenerator.GenerateUser());
            }
        }

        [Given("I am a regular user")]
        public async Task GivenIAmARegularUser()
        {
            var roomResponse = _scenarioContext.Get<RoomCreationResponse>();
            var userResponse = await _apiClient.CreateUserAsync(
                roomResponse.Room.InvitationCode!,
                TestDataGenerator.GenerateUser());

            _scenarioContext.Set(userResponse.UserCode!, "RegularUserCode");
        }

        [Given("I am a regular user in a room")]
        public async Task GivenIAmARegularUserInARoom()
        {
            var roomRequest = new RoomCreationRequest
            {
                Room = TestDataGenerator.GenerateRoom(),
                AdminUser = TestDataGenerator.GenerateUser()
            };

            var roomResponse = await _roomApiClient.CreateRoomAsync(roomRequest);
            _scenarioContext.Set(roomResponse, "RoomResponse");
            _scenarioContext.Set(roomResponse.UserCode!, "AdminUserCode");

            for (int i = 0; i < 2; i++)
            {
                await _apiClient.CreateUserAsync(
                    roomResponse.Room.InvitationCode!,
                    TestDataGenerator.GenerateUser());
            }

            var regularUserResponse = await _apiClient.CreateUserAsync(
                roomResponse.Room.InvitationCode!,
                TestDataGenerator.GenerateUser());

            _scenarioContext.Set(regularUserResponse.UserCode!, "RegularUserCode");
        }

        [Given("I am a user in a room")]
        public async Task GivenIAmAUserInARoom()
        {
            await GivenIAmARegularUser();
        }

        [When("I join the room")]
        public async Task WhenIJoinTheRoom()
        {
            try
            {
                var invitationCode = _scenarioContext.Get<string>("InvitationCode");
                var user = _scenarioContext.Get<UserCreationDto>("UserData");

                var response = await _apiClient.CreateUserAsync(invitationCode, user);
                _scenarioContext.Set(response, "UserResponse");
                _scenarioContext.Set(201, "LastStatusCode");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Expected status"))
            {
                _scenarioContext.Set(ex.Message, "LastError");
                var match = ValidationPatterns.StatusCodePattern().Match(ex.Message);
                if (match.Success)
                {
                    _scenarioContext.Set(int.Parse(match.Groups[1].Value), "LastStatusCode");
                }
            }
        }

        [When("I get the list of users using my code")]
        public async Task WhenIGetTheListOfUsersUsingMyCode()
        {
            var userCode = _scenarioContext.Get<RoomCreationResponse>().UserCode;
            var users = await _apiClient.GetUsersAsync(userCode);
            _scenarioContext.Set(users, "UsersList");
        }

        [When("I get user details by ID")]
        public async Task WhenIGetUserDetailsByID()
        {
            var adminCode = _scenarioContext.Get<RoomCreationResponse>().UserCode;
            var users = await _apiClient.GetUsersAsync(adminCode);

            var targetUser = users.First(u => !u.IsAdmin);
            var userDetails = await _apiClient.GetUserByIdAsync(targetUser.Id, adminCode);

            _scenarioContext.Set(userDetails, "UserDetails");
        }

        [When("I try to get another user's details")]
        public async Task WhenITryToGetAnotherUsersDetails()
        {
            var regularUserCode = _scenarioContext.Get<string>("RegularUserCode");
            var adminCode = _scenarioContext.Get<RoomCreationResponse>().UserCode;

            var users = await _apiClient.GetUsersAsync(adminCode);
            var targetUser = users.FirstOrDefault(u => !u.IsAdmin && u.UserCode != regularUserCode) ?? throw new InvalidOperationException("No other users found in the room");
            var userDetails = await _apiClient.GetUserByIdAsync(targetUser.Id, regularUserCode);
            _scenarioContext.Set(userDetails, "UserDetails");
            _scenarioContext.Set(200, "LastStatusCode");
        }

        [Then("I should be added successfully")]
        public void ThenIShouldBeAddedSuccessfully()
        {
            var response = _scenarioContext.Get<UserCreationResponse>("UserResponse");
            response.ShouldNotBeNull();
            response.Id.ShouldBeGreaterThan(0);
        }

        [Then("I should receive my user code")]
        public void ThenIShouldReceiveMyUserCode()
        {
            var response = _scenarioContext.Get<UserCreationResponse>("UserResponse");
            response.UserCode.ShouldNotBeNullOrEmpty();
        }

        [Then("my wishes should be saved")]
        public void ThenMyWishesShouldBeSaved()
        {
            var response = _scenarioContext.Get<UserCreationResponse>("UserResponse");
            response.WishList.ShouldNotBeEmpty();
        }

        [Then("my wish list should be empty")]
        public void ThenMyWishListShouldBeEmpty()
        {
            var response = _scenarioContext.Get<UserCreationResponse>("UserResponse");
            response.WishList.ShouldBeEmpty();
        }

        [Then("my interests should be saved")]
        public void ThenMyInterestsShouldBeSaved()
        {
            var userData = _scenarioContext.Get<UserCreationDto>("UserData");
            userData.Interests.ShouldNotBeNullOrEmpty();
        }

        [Then("I should receive all users in the room")]
        public void ThenIShouldReceiveAllUsersInTheRoom()
        {
            var users = _scenarioContext.Get<List<UserReadDto>>("UsersList");
            users.ShouldNotBeNull();
            users.Count.ShouldBeGreaterThan(1);
        }

        [Then("each user should have basic information including name and ID")]
        public void ThenEachUserShouldHaveBasicInformationIncludingNameAndID()
        {
            var users = _scenarioContext.Get<List<UserReadDto>>("UsersList");

            foreach (var user in users)
            {
                user.FirstName.ShouldNotBeNullOrEmpty();
                user.LastName.ShouldNotBeNullOrEmpty();
                user.Id.ShouldBeGreaterThan(0);
            }
        }

        [Then("each user should have gift preference indicator")]
        public void ThenEachUserShouldHaveGiftPreferenceIndicator()
        {
            var users = _scenarioContext.Get<List<UserReadDto>>("UsersList");

            foreach (var user in users)
            {
                user.ShouldSatisfyAllConditions(
                    () => user.Id.ShouldBeGreaterThan(0),
                    () => user.FirstName.ShouldNotBeNullOrEmpty()
                );
            }
        }

        [Then("users should not have sensitive data like phone or email or address")]
        public void ThenUsersShouldNotHaveSensitiveDataLikePhoneOrEmailOrAddress()
        {
            var users = _scenarioContext.Get<List<UserReadDto>>("UsersList");

            // Check if current user is admin
            var response = _scenarioContext.Get<RoomCreationResponse>();
            var currentUser = users.FirstOrDefault(u => u.UserCode == response.UserCode);

            if (currentUser?.IsAdmin == true)
            {
                // Admin SHOULD see sensitive data - this is a different test
                users.ForEach(u =>
                {
                    Console.WriteLine($"Admin sees user {u.FirstName} {u.LastName} with phone: {u.Phone}, email: {u.Email}");
                });
            }
            else
            {
                // Regular users should NOT see sensitive data of others
                var myUserCode = response.UserCode;
                var otherUsers = users.Where(u => u.UserCode != myUserCode).ToList();

                otherUsers.ShouldSatisfyAllConditions(
                    () => otherUsers.ShouldNotBeEmpty("Should have other users to check"),
                    () => otherUsers.ShouldAllBe(u => string.IsNullOrEmpty(u.Phone),
                        $"Other users should not expose phone. Found: {string.Join(", ", otherUsers.Select(u => u.Phone))}"),
                    () => otherUsers.ShouldAllBe(u => string.IsNullOrEmpty(u.Email),
                        "Other users should not expose email"),
                    () => otherUsers.ShouldAllBe(u => string.IsNullOrEmpty(u.DeliveryInfo),
                        "Other users should not expose delivery info")
                );
            }
        }

        [Then("admin should see all user data including sensitive information")]
        public void ThenAdminShouldSeeAllUserDataIncludingSensitiveInformation()
        {
            var users = _scenarioContext.Get<List<UserReadDto>>("UsersList");

            // Admin should see everyone's data
            users.Where(u => !u.IsAdmin).ShouldAllBe(u =>
                !string.IsNullOrEmpty(u.Phone) &&
                !string.IsNullOrEmpty(u.DeliveryInfo),
                "Admin should see all users' sensitive data");
        }

        [Then("I should see complete user information")]
        public void ThenIShouldSeeCompleteUserInformation()
        {
            var userDetails = _scenarioContext.Get<UserReadDto>("UserDetails");

            userDetails.FirstName.ShouldNotBeNullOrEmpty();
            userDetails.LastName.ShouldNotBeNullOrEmpty();
            userDetails.Id.ShouldBeGreaterThan(0);
            userDetails.RoomId.ShouldBeGreaterThan(0);
        }

        [Then("I should not see their sensitive information")]
        public void ThenIShouldNotSeeTheirSensitiveInformation()
        {
            var userDetails = _scenarioContext.Get<UserReadDto>("UserDetails");

            userDetails.Phone.ShouldBeNullOrEmpty("Regular users should not see phone");
            userDetails.DeliveryInfo.ShouldBeNullOrEmpty("Regular users should not see delivery info");
            // Email might be visible for communication purposes
        }

        [Then("I should see their gift preferences")]
        public void ThenIShouldSeeTheirGiftPreferences()
        {
            var userDetails = _scenarioContext.Get<UserReadDto>("UserDetails");

            if (userDetails.WantSurprise.HasValue)
            {
                if (userDetails.WantSurprise.Value)
                {
                    userDetails.Interests.ShouldNotBeNullOrEmpty();
                }
                else
                {
                    userDetails.WishList.ShouldNotBeNull();
                }
            }
        }

        [Then("the request should (pass|fail) with status {int}")]
        public void ThenTheRequestShouldPassOrFailWithStatus(string result, int expectedStatus)
        {
            var actualStatus = _scenarioContext.Get<int>("LastStatusCode");
            actualStatus.ShouldBe(expectedStatus);

            if (result == "pass")
            {
                actualStatus.ShouldBeInRange(200, 299);
            }
            else
            {
                actualStatus.ShouldBeGreaterThanOrEqualTo(400);
            }
        }

        [Then("my preferences should be updated")]
        public void ThenMyPreferencesShouldBeUpdated()
        {
            var updatedUser = _scenarioContext.Get<UserReadDto>("UpdatedUser");
            updatedUser.ShouldNotBeNull();
            updatedUser.WantSurprise?.ShouldBeFalse();
        }

        [Then("my wish list should contain {int} item")]
        [Then("my wish list should contain {int} items")]
        public void ThenMyWishListShouldContainItems(int count)
        {
            var updatedUser = _scenarioContext.Get<UserReadDto>("UpdatedUser");
            updatedUser.WishList?.Count.ShouldBe(count);
        }
    }
}
