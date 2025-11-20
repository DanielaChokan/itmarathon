@api @user-deletion
Feature: User Deletion API
  As a room administrator
  I want to delete users from my room
  So that I can manage room participants

Background:
  Given the API is available

Rule: Successful User Deletion

  @positive
  Scenario: Admin successfully deletes a regular user
    Given I am a room admin with multiple users
    And I have a regular user to delete
    When I delete the user as admin
    Then the request should return status 200
    And the user should be removed from the room
    And the room should have one less participant

  @positive
  Scenario: Admin deletes user from room with minimum users
    Given I am a room admin
    And there is exactly one regular user in the room
    When I delete the user as admin
    Then the request should return status 200
    And only admin should remain in the room

Rule: Authorization and Permission Checks

  @negative @authorization
  Scenario: Regular user cannot delete other users
    Given I am a regular user in a room
    When I try to delete another user
    Then the request should fail with status 403
    And the error should mention "not an administrator"

  @negative @authorization
  Scenario: User from different room cannot delete users
    Given I am an admin of one room
    And there is a user in a different room
    When I try to delete the user from different room
    Then the request should fail with status 403
    And the error should mention "different rooms"

  @negative @authorization
  Scenario: Admin cannot delete themselves
    Given I am a room admin
    When I try to delete myself
    Then the request should fail with status 400
    And the error should mention "cannot delete themselves"

Rule: Validation Scenarios

  @negative @validation
  Scenario: Delete user with invalid user code
    Given I have an invalid user code
    When I try to delete a user with invalid code
    Then the request should fail with status 404
    And the error should mention "User with such code not found"

  @negative @validation
  Scenario: Delete non-existent user
    Given I am a room admin
    When I try to delete a user that does not exist
    Then the request should fail with status 404
    And the error should mention "User with the specified Id was not found"

  @negative @validation
  Scenario: Delete user from closed room
    Given I am a room admin
    And the room is closed
    When I try to delete a user
    Then the request should fail with status 400
    And the error should mention "Room is already closed"

Rule: Edge Cases

  @negative
  Scenario: Delete user with gift assignment after draw
    Given I am a room admin with drawn names
    And I have a regular user with gift assignment
    When I delete the user as admin
    Then the request should fail with status 400
    And the error should mention "Room is already closed"

  @negative
  Scenario: Delete user with special characters in userCode
    Given I have a user code with special characters
    When I try to delete a user
    Then the request should fail with status 404

  @positive
  Scenario: Delete multiple users sequentially
    Given I am a room admin with 5 users
    When I delete 3 users one by one
    Then all deletions should succeed with status 200
    And the room should have 3 participants remaining