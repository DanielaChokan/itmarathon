@ui @user-deletion
Feature: User Deletion UI
  As a room administrator
  I want to delete users from the room page
  So that I can manage participants through the interface

Background:
  Given the API is available

Rule: Delete User Flow

  @positive
  Scenario: Admin deletes user from participants list
    Given a room exists with 3 participants via API
    And I am on the home page
    When I navigate to room page with admin code
    Then I should see participants count 3
    
    When I click delete button for second participant
    And I confirm deletion in modal
    Then I should see success message
    And I should see participants count 2
    And deleted user should not be in the list

  @positive
  Scenario: Admin deletes user and list updates
    Given a room exists with 4 participants via API
    And I am on the home page
    When I navigate to room page with admin code
    And I click delete button for a participant
    And I confirm deletion in modal
    Then I should see participants count 3
    And participant names should update correctly

  @negative
  Scenario: Admin cancels user deletion
    Given a room exists with 3 participants via API
    And I am on the home page
    When I navigate to room page with admin code
    And I click delete button for a participant
    And I cancel deletion in modal
    Then I should see participants count 3
    And all participants should remain in the list

Rule: UI Validation

  @positive
  Scenario: Delete button only visible to admin
    Given a room exists with 2 participants via API
    And I am on the home page
    When I navigate to room page with regular user code
    Then I should not see delete buttons

  @positive
  Scenario: Delete last regular user
    Given a room exists with 2 participants via API
    And I am on the home page
    When I navigate to room page with admin code
    Then I should see participants count 2
    
    When I click delete button for first participant
    And I confirm deletion in modal
    Then I should see success message
    And only admin should be visible

Rule: Error Handling

  @negative
  Scenario: Delete user when room is closed
    Given a room exists with 3 participants via API
    And names are drawn via API
    And I am on the home page
    When I navigate to room page with admin code
    And I try to delete a participant
    Then I should see error message
    And I should see participants count 3

  @negative @ignore
  Scenario: Handle deletion failure gracefully
    Given a room exists with 2 participants via API
    And I am on the home page
    And API will fail on next delete request
    When I navigate to room page with admin code
    And I click delete button for a participant
    And I confirm deletion in modal
    Then I should see error message "Something went wrong"
    And I should see participants count 2