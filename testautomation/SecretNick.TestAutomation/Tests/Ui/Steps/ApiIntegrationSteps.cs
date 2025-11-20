using Microsoft.Playwright;
using Reqnroll;
using Shouldly;
using Tests.Api.Clients;
using Tests.Api.Models.Requests;
using Tests.Api.Models.Responses;
using Tests.Common.TestData;
using Tests.Core.Configuration;

namespace Tests.Ui.Steps
{
    [Binding]
    public class ApiIntegrationSteps(
        ScenarioContext scenarioContext,
        RoomApiClient roomApiClient,
        UserApiClient userApiClient,
        IPage page,
        IConfigurationManager configurationManager) : UiStepsBase(page)
    {
        private readonly IPage _page = page;
        private readonly IConfigurationManager _configurationManager = configurationManager;
        [Given("a room exists via API")]
        public async Task GivenARoomExistsViaAPI()
        {
            await CreateRoomWithParticipants(1);
        }

        [Given("a room exists with {int} participants via API")]
        [Given("a room exists with {int} participant via API")]
        public async Task GivenARoomExistsWithParticipantsViaAPI(int participantCount)
        {
            await CreateRoomWithParticipants(participantCount);
        }

        private async Task CreateRoomWithParticipants(int participantCount)
        {
            var roomRequest = new RoomCreationRequest
            {
                Room = TestDataGenerator.GenerateRoom(),
                AdminUser = TestDataGenerator.GenerateUser(wantSurprise: false)
            };

            var roomResponse = await roomApiClient.CreateRoomAsync(roomRequest);

            scenarioContext.Set(roomResponse, "RoomCreationResponse");
            scenarioContext.Set(roomResponse, "RoomResponse");
            scenarioContext.Set(roomResponse.Room.InvitationCode, "InvitationCode");
            scenarioContext.Set(roomResponse.UserCode, "AdminCode");

            var participantNames = new List<string>
            {
                $"{roomRequest.AdminUser.FirstName} {roomRequest.AdminUser.LastName}"
            };

            for (int i = 1; i < participantCount; i++)
            {
                var participant = TestDataGenerator.GenerateUser(wantSurprise: i % 2 == 0);
                _ = await userApiClient.CreateUserAsync(
                    roomResponse.Room.InvitationCode!,
                    participant);

                participantNames.Add($"{participant.FirstName} {participant.LastName}");
            }

            scenarioContext.Set(participantNames, "ParticipantNames");
        }

        [When("I add {int} participants via API")]
        public async Task WhenIAddParticipantsViaAPI(int count)
        {
            var invitationCode = scenarioContext.Get<string>("RoomLink").Split('/').Last();
            var participantNames = scenarioContext.ContainsKey("ParticipantNames")
                ? scenarioContext.Get<List<string>>("ParticipantNames")
                : ["Admin User"];

            for (int i = 0; i < count; i++)
            {
                var participant = TestDataGenerator.GenerateUser(wantSurprise: i % 2 == 0);

                await userApiClient.CreateUserAsync(invitationCode, participant);
                participantNames.Add($"{participant.FirstName} {participant.LastName}");
            }

            scenarioContext.Set(participantNames, "ParticipantNames");
        }

        [When("I add participant via API")]
        public async Task WhenIAddParticipantViaAPI()
        {
            await WhenIAddParticipantsViaAPI(1);
        }

        [When("all users draw names via API")]
        public async Task WhenAllUsersDrawNamesViaAPI()
        {
            var adminCode = scenarioContext.Get<string>("AdminCode");
            await roomApiClient.DrawRoomAsync(adminCode);
        }

        [Then("participants should have valid pairings via API")]
        public async Task ThenParticipantsShouldHaveValidPairingsViaAPI()
        {
            var adminCode = scenarioContext.Get<string>("AdminCode");
            var users = await userApiClient.GetUsersAsync(adminCode);

            users.Count.ShouldBeGreaterThan(2, "Should have at least 3 participants");

            var givers = new HashSet<long>();
            var receivers = new HashSet<long>();

            foreach (var user in users)
            {
                user.GiftToUserId.ShouldNotBeNull("Each participant should have a giftee");
                user.Id.ShouldNotBe(user.GiftToUserId.Value,
                    "Participant should not gift to themselves");

                givers.Add(user.Id);
                receivers.Add(user.GiftToUserId.Value);
            }

            givers.Count.ShouldBe(users.Count, "Each participant should be a giver");
            receivers.Count.ShouldBe(users.Count, "Each participant should receive a gift");
        }

        [When("I navigate to room page with regular user code")]
        public async Task WhenINavigateToRoomPageWithRegularUserCode()
        {
            var invitationCode = scenarioContext.Get<string>("InvitationCode");

            var regularUser = TestDataGenerator.GenerateUser();
            var userResponse = await userApiClient.CreateUserAsync(invitationCode, regularUser);

            scenarioContext.Set(userResponse.UserCode!, "RegularUserCode");

            var regularUserCode = userResponse.UserCode!;
            var baseUrl = _configurationManager.Settings.BaseUrls.Ui;
            await _page.GotoAsync($"{baseUrl}/room/{regularUserCode}");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
    }
}
