Feature: DummyJSON API
  As a test analyst
  I want to validate the public API contract
  So that key read and simulated write operations are covered

  Background:
    Given the DummyJSON API is available

  @auth
  Scenario: Authenticate and store the access token
    When I authenticate as the sample user
    Then the response is successful
    And the access token is stored

  @auth
  Scenario: Validate the current authenticated user
    Given I am authenticated as the sample user
    When I request the current authenticated user
    Then the response is successful
    And the authenticated user has id 1, first name "Emily", and last name "Johnson"

  @users
  Scenario: Count users with red hair
    When I retrieve all users and users filtered by red hair
    Then the response is successful
    And the red hair filter returns the same users as the full user list

  @users
  Scenario: Add a user with a complete address
    When I add a user with a complete address
    Then the response is successful
    And the created user response matches the submitted details

  @products
  Scenario: Validate the iPhone 13 Pro price
    When I search for "iPhone 13 Pro"
    Then the response is successful
    And the iPhone 13 Pro price is 1099.99

  @products
  Scenario: Retrieve only the fifteenth product with pagination
    When I request one product starting at offset 14
    Then the response is successful
    And the response contains only product 15 "Wooden Bathroom Sink With Mirror" priced 799.99

  @products
  Scenario: Update the title of product 121
    When I update product 121 with title "Updated product 121 title"
    Then the response is successful
    And the updated product title is "Updated product 121 title"

  @carts
  Scenario: Add five products and validate the total discount percentage
    When I add five products to a cart
    Then the response is successful
    And the cart contains five products and a valid aggregate discount percentage

  @carts
  Scenario: Delete a cart
    When I delete cart 1
    Then the response is successful
    And the cart deletion is marked as simulated and successful
