﻿using EFAuditer;
using ntbs_service.Helpers;
using ntbs_service.Models.Enums;

namespace ntbs_service.Models.Entities.Alerts

{
    public class MBovisAlert : Alert, IHasRootEntityForAuditing
    {
        public override string Action => "Please complete enhanced surveillance questionnaire";
        public override string ActionLink => RouteHelper.GetNotificationOverviewPathWithSectionAnchor(
            NotificationId.GetValueOrDefault(),
            NotificationSubPaths.EditMBovisExposureToKnownCases);

        public MBovisAlert()
        {
            AlertType = AlertType.EnhancedSurveillanceMBovis;
        }
        public string RootEntityType => RootEntities.Notification;
        public string RootId => NotificationId.ToString();
    }
}
