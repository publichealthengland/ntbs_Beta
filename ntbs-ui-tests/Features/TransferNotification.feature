Feature: Transfer notification
  
  Background: Create a transfer request
    Given I navigate to the app
    And I have logged in as BirminghamServiceUser
    And I am on seeded 'MAXIMUM_DETAILS' notification overview page
    Then I can see the value 'Birmingham & Solihull' for the field 'tb-service' in the 'HospitalDetails' overview section

    When I expand manage notification section
    And I click on the 'transfer-button' button
    And I select West Yorkshire from input list 'selectTBService'
    And I select Leeds UITester for 'TransferRequest_CaseManagerId'
    And I check 'transfer-radio-Other'
    And I enter Patient likes travel into 'TransferRequest_OtherReasonDescription'
    And I enter I hope your service is doing well into 'TransferRequest_TransferRequestNote'
    And I click on the 'confirm-transfer-button' button
    Then I should see the Notification
    And An alert has been created for the notification with type TransferRequest

  @NormalAuth
  Scenario: Pending transfer page has correct values
    When I expand manage notification section
    And I click on the 'transfer-button' button

    Then I can see the value Yorkshire and Humber for element with id 'transfer-region'
    Then I can see the value West Yorkshire for element with id 'transfer-tb-service'
    Then I can see the value Leeds UITester for element with id 'transfer-case-manager'

  @CookieOverride
  Scenario: Pending transfer page has correct values - Cookie Override
    When I expand manage notification section
    And I click on the 'transfer-button' button

    Then I can see the value Yorkshire and Humber for element with id 'transfer-region'
    Then I can see the value West Yorkshire for element with id 'transfer-tb-service'
    
  @NormalAuth
  Scenario: Accept transfer of notification between services
    When I log out

    Given I choose to log in with a different account
    Given I have logged in as LeedsServiceUser
    When I navigate to the url of the current notification
    And I take action on the alert with title Transfer request

    Then I can see the value Birmingham & Solihull for 'Sending TB service' transfer information
    Then I can see the value Birmingham UITester for 'Sending case manager' transfer information
    Then I can see the value Other - Patient likes travel for 'Reason for transfer' transfer information
    Then I can see the value I hope your service is doing well for 'Note accompanying transfer' transfer information

    When I check 'accept-transfer-input'
    And I click on the 'submit-transfer-button' button
    And I click on the 'return-to-notification' button

    Then I should see the Notification
    Then I can see the value 'West Yorkshire' for the field 'tb-service' in the 'HospitalDetails' overview section
    Then I can see no value for the field 'local-patient-id' in the 'PatientDetails' overview section
    Then I can see no value for the field 'consultant' in the 'HospitalDetails' overview section

  @CookieOverride
  Scenario: Accept transfer of notification between services - Cookie Override
    When I navigate to the url of the current notification
    And I take action on the alert with title Transfer request

    Then I can see the value Birmingham & Solihull for 'Sending TB service' transfer information
    Then I can see the value Other - Patient likes travel for 'Reason for transfer' transfer information
    Then I can see the value I hope your service is doing well for 'Note accompanying transfer' transfer information

    When I check 'accept-transfer-input'
    And I click on the 'submit-transfer-button' button
    And I click on the 'return-to-notification' button

    Then I should see the Notification
    Then I can see the value 'West Yorkshire' for the field 'tb-service' in the 'HospitalDetails' overview section
    Then I can see no value for the field 'local-patient-id' in the 'PatientDetails' overview section
    Then I can see no value for the field 'consultant' in the 'HospitalDetails' overview section

  @NormalAuth
  Scenario: Decline transfer of notification between services
    When I log out

    Given I choose to log in with a different account
    Given I have logged in as LeedsServiceUser
    When I navigate to the url of the current notification
    And I take action on the alert with title Transfer request

    And I check 'decline-transfer-input'
    And I enter Do not want this patient here into 'TransferRequest_DeclineTransferReason'
    And I click on the 'submit-transfer-button' button
    And I click on the 'return-to-homepage' button
    When I log out

    Given I choose to log in with a different account
    Given I have logged in as BirminghamServiceUser

    When I navigate to the url of the current notification
    Then I can see the value '3455' for the field 'local-patient-id' in the 'PatientDetails' overview section
    Then I can see the value 'Dr Frank Lotchewski' for the field 'consultant' in the 'HospitalDetails' overview section
    When I take action on the alert with title Transfer rejected

    Then I should be on the TransferDeclined page
    Then I can see 'Do not want this patient here' as the rejection note

  @CookieOverride
  Scenario: Decline transfer of notification between services - Cookie Override
    When I navigate to the url of the current notification
    And I take action on the alert with title Transfer request

    And I check 'decline-transfer-input'
    And I enter Do not want this patient here into 'TransferRequest_DeclineTransferReason'
    And I click on the 'submit-transfer-button' button
    And I click on the 'return-to-homepage' button

    When I navigate to the url of the current notification
    Then I can see the value '3455' for the field 'local-patient-id' in the 'PatientDetails' overview section
    Then I can see the value 'Dr Frank Lotchewski' for the field 'consultant' in the 'HospitalDetails' overview section
    When I take action on the alert with title Transfer rejected

    Then I should be on the TransferDeclined page
    Then I can see 'Do not want this patient here' as the rejection note