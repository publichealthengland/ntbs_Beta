Feature: Notification input errors
  Happy and error paths for notification creation
  Notification deletion

  Background: Create new draft notification
    Given I navigate to the app
    Given I have logged in as BirminghamServiceUser
    Given I am on the Search page
    When I enter 1 into 'SearchParameters_IdFilter'
    And I click on the 'search-button' button
    Then I should be on the Search page
    When I click on the 'create-button' button
    Then A new notification should have been created
    And I should be on the PatientDetails page

  Scenario: Create and submit notification without content
    When I click on the 'submit-button' button
    Then I should be on the PatientDetails page
    And I should see all submission error messages
    
  Scenario: NHS number validation works correctly
    When I enter 0000000000 into 'PatientDetails_NhsNumber'
    And I click on the 'save-button' button
    Then I can see the error 'This NHS number is not valid. If you do not know the NHS number please select "Not Known"'
    
    When I enter 100100100 into 'PatientDetails_NhsNumber'
    And I click on the 'save-button' button
    Then I can see the error 'NHS number needs to be 10 digits long'

    When I enter TreeSurgeon into 'PatientDetails_NhsNumber'
    And I click on the 'save-button' button
    Then I can see the error 'NHS number can only contain digits 0-9'

    When I enter 4333222111 into 'PatientDetails_NhsNumber'
    And I click on the 'save-button' button
    Then I can see the error 'This NHS number is not valid. Confirm you have entered it correctly'
    
  Scenario: Clinical date verification shows correct errors
    # Some clinical date validation requires a date of birth to compare against
    When I enter '04/08/2011' into date fields with id 'FormattedDob'
    And I click on the 'save-button' button
    And I click 'Clinical details' on the navigation bar
    
    When I check 'symptomatic-yes'
    And I enter '01/08/2011' into date fields with id 'FormattedSymptomDate'
    And I click on the 'save-button' button
    Then I can see the error 'Symptom onset date must be later than date of birth'
    
    When I enter '01/08/1977' into date fields with id 'FormattedSymptomDate'
    And I click on the 'save-button' button
    Then I can see the error 'Symptom onset date must not be before 01/01/2010'

    When I enter 1 into 'FormattedSymptomDate_Day'
    And I enter 8 into 'FormattedSymptomDate_Month'
    And I enter  into 'FormattedSymptomDate_Year'
    And I click on the 'save-button' button
    Then I can see the error 'Symptom onset date does not have a valid date selection'

  Scenario: Clinical date verification shows correct warnings
    When I click 'Clinical details' on the navigation bar
    When I check 'symptomatic-yes'
    And I enter '01/08/2012' into date fields with id 'FormattedSymptomDate'
    
    When I enter '14/07/2010' into date fields with id 'FormattedFirstPresentationDate'
    Then I see the warning 'Presentation to any health service is earlier than Symptom onset date' for 'date-warning-text-1'
    
    When I enter '12/01/2009' into date fields with id 'FormattedTBServiceReferralDate'
    Then I see the warning 'Referral to TB service received is earlier than Presentation to any health service' for 'date-warning-text-2'
    
    When I enter '09/11/2010' into date fields with id 'FormattedTbServicePresentationDate'
    Then I see the warning 'Presentation to TB service is earlier than Symptom onset date' for 'date-warning-text-3'
   
    When I enter '09/11/2012' into date fields with id 'FormattedFirstPresentationDate'
    When I enter '16/10/2012' into date fields with id 'FormattedTbServicePresentationDate'
    Then I see the warning 'Presentation to TB service is earlier than Presentation to any health service' for 'date-warning-text-3'

    When I enter '09/06/2012' into date fields with id 'FormattedFirstPresentationDate'
    When I enter '11/09/2012' into date fields with id 'FormattedDiagnosisDate'
    Then I see the warning 'Diagnosis date is earlier than Presentation to TB service' for 'date-warning-text-4'

    When I check 'treatment-yes'
    And I enter '30/08/2012' into date fields with id 'FormattedTreatmentDate'
    Then I see the warning 'Treatment start date is earlier than Diagnosis date' for 'date-warning-text-5'
    
    When I check 'home-visit-yes'
    And I enter '01/09/2012' into date fields with id 'FormattedHomeVisitDate'
    Then I see the warning 'First home visit date is earlier than Diagnosis date' for 'date-warning-text-6'

    When I check 'home-visit-yes'
    When I enter '16/01/2010' into date fields with id 'FormattedTbServicePresentationDate'
    And I enter '20/01/2010' into date fields with id 'FormattedHomeVisitDate'
    Then I see the warning 'First home visit date is earlier than Treatment start date' for 'date-warning-text-6'
    
  Scenario: Contact tracing verification shows correct errors
    When I click 'Contact tracing' on the navigation bar
    When I enter 3 into 'ContactTracing_AdultsIdentified'
    And I enter 5 into 'ContactTracing_AdultsScreened'
    And I click on the 'save-button' button
    And I click on the 'save-button' button
    Then I can see the error 'Adults screened must not be greater than the number of adults identified'
    
    When I enter 3 into 'ContactTracing_AdultsScreened'
    And I enter 2 into 'ContactTracing_AdultsActiveTB'
    And I enter 2 into 'ContactTracing_AdultsLatentTB'
    And I click on the 'save-button' button
    And I click on the 'save-button' button
    Then I can see the error 'Adults with active TB must not be greater than number of adults screened minus those with latent TB'
    And I can see the error 'Adults with latent TB must not be greater than number of adults screened minus those with active TB'
    
    When I enter 12 into 'ContactTracing_ChildrenIdentified'
    And I enter 4 into 'ContactTracing_ChildrenStartedTreatment'
    And I enter 6 into 'ContactTracing_ChildrenFinishedTreatment'
    And I click on the 'save-button' button
    And I click on the 'save-button' button
    Then I can see the error 'Children that have started treatment must not be greater than number of children with latent TB'
    And I can see the error 'Children that have finished treatment must not be greater than number of children that started treatment'