using Microsoft.Playwright;
using Reqnroll;
using Shouldly;
using Tests.Api.Clients;
using Tests.Ui.Pages;

namespace Tests.Ui.Steps
{
    [Binding]
    public class UserDeletionUiSteps(
        ScenarioContext scenarioContext,
        IPage page,
        RoomApiClient roomApiClient) : UiStepsBase(page)
    {
        private readonly ScenarioContext _scenarioContext = scenarioContext;
        private readonly RoomApiClient _roomApiClient = roomApiClient;

        [When("I click delete button for second participant")]
        [When("I click delete button for a participant")]
        public async Task WhenIClickDeleteButtonForParticipant()
        {
            var deleteButtons = await GetRoomPage().GetDeleteButtonsAsync();
            deleteButtons.Count.ShouldBeGreaterThan(0, "No delete buttons found");

            var participantName = await GetRoomPage().GetParticipantNameForDeleteButton(0);
            _scenarioContext.Set(participantName, "DeletedParticipantName");

            await GetRoomPage().ClickDeleteButtonAsync(0);
            await Task.Delay(500);
        }

        [When("I click delete button for first participant")]
        public async Task WhenIClickDeleteButtonForFirstParticipant()
        {
            await WhenIClickDeleteButtonForParticipant();
        }

        [When("I confirm deletion in modal")]
        public async Task WhenIConfirmDeletionInModal()
        {
            await GetRoomPage().ConfirmDeletionAsync();
            await Task.Delay(1000);
        }

        [When("I cancel deletion in modal")]
        public async Task WhenICancelDeletionInModal()
        {
            await GetRoomPage().CancelDeletionAsync();
            await Task.Delay(500);
        }

        [When("I try to delete a participant")]
        public async Task WhenITryToDeleteAParticipant()
        {
            await WhenIClickDeleteButtonForParticipant();
            await WhenIConfirmDeletionInModal();
        }

        [Given("names are drawn via API")]
        public async Task GivenNamesAreDrawnViaApi()
        {
            var adminCode = _scenarioContext.Get<string>("AdminCode");
            await _roomApiClient.DrawNamesAsync(adminCode);
        }

        [Given("API will fail on next delete request")]
        public void GivenApiWillFailOnNextDeleteRequest()
        {
            _scenarioContext.Set(true, "SimulateApiFailure");
        }

        [Then("I should see success message")]
        public async Task ThenIShouldSeeSuccessMessage()
        {
            await Task.Delay(1000); // Wait for toast to appear
            
            var toastVisible = await GetRoomPage().IsToastVisibleAsync("success");
            toastVisible.ShouldBeTrue("Success toast should be visible");
        }

        [Then("deleted user should not be in the list")]
        public async Task ThenDeletedUserShouldNotBeInTheList()
        {
            var deletedName = _scenarioContext.Get<string>("DeletedParticipantName");
            var participants = await GetRoomPage().GetAllParticipantNamesAsync();
            
            participants.ShouldNotContain(deletedName);
        }

        [Then("participant names should update correctly")]
        public async Task ThenParticipantNamesShouldUpdateCorrectly()
        {
            var participants = await GetRoomPage().GetAllParticipantNamesAsync();
            participants.ShouldNotBeEmpty();
            participants.ShouldAllBe(name => !string.IsNullOrWhiteSpace(name));
        }

        [Then("all participants should remain in the list")]
        public async Task ThenAllParticipantsShouldRemainInTheList()
        {
            var currentCount = await GetRoomPage().GetParticipantsCountAsync();
            var expectedCount = 3;
            currentCount.ShouldBe(expectedCount);
        }

        [Then("I should not see delete buttons")]
        public async Task ThenIShouldNotSeeDeleteButtons()
        {
            var deleteButtons = await GetRoomPage().GetDeleteButtonsAsync();
            deleteButtons.Count.ShouldBe(0, "Delete buttons should not be visible to regular users");
        }

        [Then("only admin should be visible")]
        public async Task ThenOnlyAdminShouldBeVisible()
        {
            var participants = await GetRoomPage().GetAllParticipantNamesAsync();
            participants.Count.ShouldBe(1);
        }

        [Then("I should see error message")]
        public async Task ThenIShouldSeeErrorMessage()
        {
            await ThenIShouldSeeErrorMessageWithText(string.Empty);
        }

        [Then("I should see error message {string}")]
        public async Task ThenIShouldSeeErrorMessageWithText(string expectedMessage)
        {
            await Task.Delay(1000); // Wait for toast to appear
            
            var toastVisible = await GetRoomPage().IsToastVisibleAsync("error");
            toastVisible.ShouldBeTrue("Error toast should be visible");

            if (!string.IsNullOrEmpty(expectedMessage))
            {
                var toastText = await GetRoomPage().GetToastTextAsync();
                toastText.ShouldContain(expectedMessage, Case.Insensitive);
            }
        }
    }
}