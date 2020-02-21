﻿using System;
using System.Collections.Generic;
using ntbs_integration_tests.LabResultsPage;
using ntbs_integration_tests.NotificationPages;
using ntbs_integration_tests.TransferPage;
using ntbs_service.DataAccess;
using ntbs_service.Models.Entities;
using ntbs_service.Models.Enums;
using ntbs_service.Models.ReferenceEntities;
using ntbs_service.Services;

namespace ntbs_integration_tests.Helpers
{
    public static class Utilities
    {
        public const int DRAFT_ID = 10001;
        public const int NOTIFIED_ID = 10002;
        public const int DENOTIFIED_ID = 10003;
        public const int NOTIFIED_ID_WITH_NOTIFICATION_DATE = 10004;
        public const int NEW_ID = 10005;

        public const int DENOTIFY_WITH_DESCRIPTION = 10010;
        public const int DENOTIFY_NO_DESCRIPTION = 10011;

        public const int DELETE_WITH_DESCRIPTION = 10020;
        public const int DELETE_NO_DESCRIPTION = 10021;

        public const int PATIENT_GROUPED_NOTIFIED_NOTIFICATION_SHARED_NHS_NUMBER = 10030;
        public const int PATIENT_GROUPED_DENOTIFIED_NOTIFICATION_SHARED_NHS_NUMBER = 10031;
        public const int PATIENT_DRAFT_NOTIFICATION_SHARED_NHS_NUMBER = 10032;
        public const int PATIENT_NOTIFIED_NOTIFICATION_SHARED_NHS_NUMBER = 10033;

        public const int NOTIFIED_WITH_TBSERVICE = 10041;
        public const int MDR_DETAILS_EXIST = 10050;

        public const int NOTIFICATION_WITH_MANUAL_TESTS = 10051;

        public const int NOTIFICATION_WITH_VENUES = 10060;
        public const int NOTIFICATION_WITH_ADDRESSES = 10061;

        public const int NOTIFICATION_WITH_TREATMENT_EVENTS = 10070;
        public const int NOTIFICATION_FOR_ADD_TREATMENT_OUTCOME = 10071;
        public const int NOTIFICATION_FOR_ADD_TREATMENT_RESTART = 10072;

        public const int CLINICAL_NOTIFICATION_EXTRA_PULMONARY_ID = 10080;

        public const int LATE_DOB_ID = 10090;
        public const int NOTIFICATION_DATE_TODAY = 10095;
        public const int NOTIFICATION_DATE_OVER_YEAR_AGO = 10096;
        public const int LINKED_NOTIFICATION_ABINGDON_TB_SERVICE = 10097;
        public const int LINK_NOTIFICATION_ROYAL_FREE_LONDON_TB_SERVICE = 10098;

        public const int NOTIFIED_ID_WITH_TRANSFER_REQUEST_TO_REJECT = 10091;
        public const int NOTIFICATION_WITH_TRANSFER_REQUEST_TO_ACCEPT = 10092;
        

        public static int SPECIMEN_MATCHING_NOTIFICATION_ID1 = MockSpecimenService.MockSpecimenNotificationId1; // 10100
        public static int SPECIMEN_MATCHING_NOTIFICATION_ID2 = MockSpecimenService.MockSpecimenNotificationId2; // 10101
        public static int SPECIMEN_MATCHING_NOTIFICATION_ID3 = MockSpecimenService.MockSpecimenNotificationId3; // 10102
        public static int SPECIMEN_MATCHING_NOTIFICATION_ID4 = MockSpecimenService.MockSpecimenNotificationId4; // 10103
        public const int SPECIMEN_MATCHING_MANUAL_MATCH_NOTIFICATION_ID = 10104;

        public const int NOTIFICATION_ID_WITH_MBOVIS_OTHER_CASE_ENTITIES = 10130;
        public const int NOTIFICATION_ID_WITH_MBOVIS_OTHER_CASE_NO_ENTITIES = 10131;
        public const int NOTIFICATION_ID_WITH_MBOVIS_MILK_ENTITIES = 10132;
        public const int NOTIFICATION_ID_WITH_MBOVIS_MILK_NO_ENTITIES = 10133;
        public const int NOTIFICATION_ID_WITH_MBOVIS_OCCUPATION_ENTITIES = 10134;
        public const int NOTIFICATION_ID_WITH_MBOVIS_OCCUPATION_NO_ENTITIES = 10135;

        public const int ALERT_ID = 20001;
        public const int TRANSFER_ALERT_ID = 20002;
        public const int TRANSFER_ALERT_ID_TO_ACCEPT = 20003;

        public const int TRANSFER_ALERT_ID_TO_REJECT = 20004;
        public const int TRANSFER_REJECTED_ID = 20005;
        public const int TRANSFER_ALERT_ID_TO_ACCEPT_2 = 20006;

        public const int PATIENT_NOTIFICATION_GROUP_ID = 30001;
            
        public const int OVERVIEW_NOTIFICATION_GROUP_ID = 30002;

        // Below generated by http://danielbayley.uk/nhs-number/
        public const string NHS_NUMBER_SHARED = "6345444995";


        // These IDs match actual reference data - see app db seeding
        public const string HOSPITAL_FLEETWOOD_HOSPITAL_ID = "1EE2B39A-428F-44C7-B4BB-000649636591";
        public const string HOSPITAL_ABINGDON_COMMUNITY_HOSPITAL_ID = "93FA0A6C-474D-4AE8-AF23-952076F96336";
        public const string HOSPITAL_FAKE_ID = "f9454382-9fbd-4524-8b65-000000000000";
        public const string TBSERVICE_ROYAL_DERBY_HOSPITAL_ID = "TBS0181";
        public const string TBSERVICE_ROYAL_FREE_LONDON_TB_SERVICE_ID = "TBS0182";
        public const string TBSERVICE_ABINGDON_COMMUNITY_HOSPITAL_ID = "TBS0001";
        public const string PERMITTED_SERVICE_CODE = "TBS0008";
        public const string UNPERMITTED_SERVICE_CODE = "TBS0009";
        public const string PERMITTED_PHEC_CODE = "E45000019";
        public const string UNPERMITTED_PHEC_CODE = "E45000020";
        public const string PERMITTED_POSTCODE = "TW153AA";
        public const string UNPERMITTED_POSTCODE = "NW51TL";
        public const string CASEMANAGER_ABINGDON_EMAIL = "pheNtbs_nhsUser2@ntbs.phe.com";
        public const string CASEMANAGER_ABINGDON_EMAIL2 = "pheNtbs_nhsUser3@ntbs.phe.com";

        public static void SeedDatabase(NtbsContext context)
        {
            // General purpose entities shared between tests
            context.Notification.AddRange(GetSeedingNotifications());
            context.PostcodeLookup.AddRange(GetTestPostcodeLookups());
            context.NotificationGroup.AddRange(GetTestNotificationGroups());
            context.User.AddRange(GetCaseManagers());
            context.CaseManagerTbService.AddRange(GetCaseManagerTbServicesJoinEntries());
            context.Alert.AddRange(GetSeedingAlerts());

            // Entities required for specific test suites
            context.Notification.AddRange(OverviewPageTests.GetSeedingNotifications());
            context.Notification.AddRange(DenotifyPageTests.GetSeedingNotifications());
            context.Notification.AddRange(DeletePageTests.GetSeedingNotifications());
            context.Notification.AddRange(PatientPageTests.GetSeedingNotifications());
            context.Notification.AddRange(HospitalDetailsPageTests.GetSeedingNotifications());
            context.Notification.AddRange(ManualTestResultEditPagesTests.GetSeedingNotifications());
            context.Notification.AddRange(SocialContextVenueEditPageTests.GetSeedingNotifications());
            context.Notification.AddRange(SocialContextAddressEditPageTests.GetSeedingNotifications());
            context.Notification.AddRange(TreatmentEventEditPageTests.GetSeedingNotifications());
            context.Notification.AddRange(ClinicalDetailsPageTests.GetSeedingNotifications());
            context.Notification.AddRange(ActionTransferPageTests.GetSeedingNotifications());
            context.Notification.AddRange(LabResultsPageTests.GetSeedingNotifications());
            context.Notification.AddRange(MBovisExposureToKnownCasesPageTests.GetSeedingNotifications());
            context.Notification.AddRange(MBovisUnpasteurisedMilkConsumptionPageTests.GetSeedingNotifications());
            context.Notification.AddRange(MBovisOccupationExposurePageTests.GetSeedingNotifications());

            context.TreatmentOutcome.AddRange(TreatmentEventEditPageTests.GetSeedingOutcomes());

            context.SaveChanges();
        }

        private static IEnumerable<NotificationGroup> GetTestNotificationGroups()
        {
            return new List<NotificationGroup>
            {
                new NotificationGroup { NotificationGroupId = PATIENT_NOTIFICATION_GROUP_ID },
                new NotificationGroup { NotificationGroupId = OVERVIEW_NOTIFICATION_GROUP_ID}
            };
        }

        // Unlike other reference data, these are not seeded via fluent migrator so we need to add some test postcodes manually
        private static IEnumerable<PostcodeLookup> GetTestPostcodeLookups()
        {
            return new List<PostcodeLookup>
            {
                // Matches permitted PHEC_CODE
                new PostcodeLookup
                {
                    Postcode = PERMITTED_POSTCODE, LocalAuthorityCode = "E10000030", CountryCode = "E92000001"
                },
                // Matches unpermitted PHEC_CODE
                new PostcodeLookup
                {
                    Postcode = UNPERMITTED_POSTCODE, LocalAuthorityCode = "E09000007", CountryCode = "E92000001"
                }
            };
        }

        private static IEnumerable<User> GetCaseManagers()
        {
            return new List<User>
            {
                new User
                {
                    Username = CASEMANAGER_ABINGDON_EMAIL,
                    GivenName = "TestCase",
                    FamilyName = "TestManager",
                    AdGroups = "Global.NIS.NTBS.Service_Abingdon",
                    IsActive = true,
                    IsCaseManager = true
                },
                new User
                {
                    Username = CASEMANAGER_ABINGDON_EMAIL2,
                    GivenName = "TestCase2",
                    FamilyName = "TestManager",
                    AdGroups = "Global.NIS.NTBS.Service_Abingdon",
                    IsActive = true,
                    IsCaseManager = true
                }
            };
        }

        private static IEnumerable<CaseManagerTbService> GetCaseManagerTbServicesJoinEntries()
        {
            return new List<CaseManagerTbService>
            {
                new CaseManagerTbService
                {
                    TbServiceCode = TBSERVICE_ABINGDON_COMMUNITY_HOSPITAL_ID,
                    CaseManagerUsername = CASEMANAGER_ABINGDON_EMAIL
                },
                new CaseManagerTbService
                {
                    TbServiceCode = TBSERVICE_ABINGDON_COMMUNITY_HOSPITAL_ID,
                    CaseManagerUsername = CASEMANAGER_ABINGDON_EMAIL2
                }
            };
        }

        private static IEnumerable<Notification> GetSeedingNotifications()
        {
            return new List<Notification>
            {
                new Notification
                {
                    NotificationId = DRAFT_ID,
                    NotificationStatus = NotificationStatus.Draft,
                    DrugResistanceProfile = new DrugResistanceProfile
                    {
                        DrugResistanceProfileString = "RR/MDR/XDR",
                        Species = "M. bovis"
                    },
                    ClinicalDetails = new ClinicalDetails
                    {
                        IsMDRTreatment = true
                    }
                },
                new Notification
                {
                    NotificationId = NOTIFIED_ID,
                    NotificationStatus = NotificationStatus.Notified,
                    // Requires a notification site to pass full validation
                    NotificationSites =
                        new List<NotificationSite>
                        {
                            new NotificationSite {NotificationId = NOTIFIED_ID, SiteId = (int)SiteId.PULMONARY}
                        },
                    HospitalDetails = new HospitalDetails
                    {
                        TBServiceCode = TBSERVICE_ABINGDON_COMMUNITY_HOSPITAL_ID,
                        HospitalId = Guid.Parse(HOSPITAL_ABINGDON_COMMUNITY_HOSPITAL_ID),
                        CaseManagerUsername = CASEMANAGER_ABINGDON_EMAIL
                    },
                    PatientDetails = new PatientDetails
                    {
                        Dob = new DateTime(1970, 1, 1)
                    },
                    ClinicalDetails = new ClinicalDetails {IsMDRTreatment = true},
                    DrugResistanceProfile = new DrugResistanceProfile {Species = "M. bovis"}
                },
                new Notification
                {
                    NotificationId = DENOTIFIED_ID,
                    NotificationStatus = NotificationStatus.Denotified,
                    DenotificationDetails = new DenotificationDetails
                    {
                        Reason = DenotificationReason.Other,
                        OtherDescription = "a great reason"
                    },
                    // Requires a notification site to pass full validation
                    NotificationSites = new List<NotificationSite>
                    {
                        new NotificationSite {NotificationId = DENOTIFIED_ID, SiteId = (int)SiteId.PULMONARY}
                    },
                    DrugResistanceProfile = new DrugResistanceProfile
                    {
                        DrugResistanceProfileString = "RR/MDR/XDR",
                        Species = "M. bovis"
                    },
                    ClinicalDetails = new ClinicalDetails
                    {
                        IsMDRTreatment = true
                    }
                },
                new Notification
                {
                    NotificationId = MDR_DETAILS_EXIST,
                    NotificationStatus = NotificationStatus.Notified,
                    // Requires a notification site to pass full validation
                    NotificationSites =
                        new List<NotificationSite>
                        {
                            new NotificationSite
                            {
                                NotificationId = MDR_DETAILS_EXIST, SiteId = (int)SiteId.PULMONARY
                            }
                        },
                    MDRDetails = new MDRDetails {ExposureToKnownCaseStatus = Status.Yes, RelationshipToCase = "test"},
                    ClinicalDetails = new ClinicalDetails {IsMDRTreatment = true}
                }
            };
        }

        private static IEnumerable<Alert> GetSeedingAlerts()
        {
            return new List<Alert>
            {
                new TestAlert
                {
                    AlertId = ALERT_ID,
                    AlertStatus = AlertStatus.Open,
                    TbServiceCode = PERMITTED_SERVICE_CODE,
                    CreationDate = DateTime.Now,
                    NotificationId = NOTIFIED_ID,
                    AlertType = AlertType.Test
                },
                new TransferAlert
                {
                    AlertType = AlertType.TransferRequest,
                    AlertId = TRANSFER_ALERT_ID,
                    NotificationId = NOTIFIED_ID,
                    TbServiceCode = PERMITTED_SERVICE_CODE,
                    AlertStatus = AlertStatus.Open
                },
                new TransferAlert
                {
                    AlertType = AlertType.TransferRequest,
                    AlertId = TRANSFER_ALERT_ID_TO_ACCEPT,
                    NotificationId = NOTIFIED_ID_WITH_NOTIFICATION_DATE,
                    TbServiceCode = TBSERVICE_ABINGDON_COMMUNITY_HOSPITAL_ID,
                    CaseManagerUsername = CASEMANAGER_ABINGDON_EMAIL,
                    AlertStatus = AlertStatus.Open
                },
                new TransferAlert
                {
                    AlertType = AlertType.TransferRequest,
                    AlertId = TRANSFER_ALERT_ID_TO_REJECT,
                    NotificationId = NOTIFIED_ID_WITH_TRANSFER_REQUEST_TO_REJECT,
                    TbServiceCode = TBSERVICE_ABINGDON_COMMUNITY_HOSPITAL_ID,
                    CaseManagerUsername = CASEMANAGER_ABINGDON_EMAIL,
                    AlertStatus = AlertStatus.Open
                },
                new TransferAlert
                {
                    AlertType = AlertType.TransferRequest,
                    AlertId = TRANSFER_ALERT_ID_TO_ACCEPT_2,
                    NotificationId = NOTIFICATION_WITH_TRANSFER_REQUEST_TO_ACCEPT,
                    TbServiceCode = TBSERVICE_ABINGDON_COMMUNITY_HOSPITAL_ID,
                    CaseManagerUsername = CASEMANAGER_ABINGDON_EMAIL,
                    AlertStatus = AlertStatus.Open
                },
                new TransferRejectedAlert
                {
                    AlertType = AlertType.TransferRejected,
                    AlertId = TRANSFER_REJECTED_ID,
                    NotificationId = NOTIFIED_ID,
                    TbServiceCode = TBSERVICE_ABINGDON_COMMUNITY_HOSPITAL_ID,
                    AlertStatus = AlertStatus.Open
                }
            };
        }

        public static void SetServiceCodeForNotification(NtbsContext context, int notificationId, string code)
        {
            var notification = context.Notification.Find(notificationId);
            notification.HospitalDetails.TBServiceCode = code;
            context.SaveChanges();
        }

        public static void SetPostcodeForNotification(NtbsContext context, int notificationId, string code)
        {
            var notification = context.Notification.Find(notificationId);
            notification.PatientDetails.PostcodeToLookup = code;
            context.SaveChanges();
        }
    }
}
