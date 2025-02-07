﻿using System;
using System.Linq.Expressions;
using EFAuditer;
using ntbs_service.Helpers;
using ntbs_service.Models.Enums;
using ntbs_service.Services;

namespace ntbs_service.Models.Entities.Alerts
{
    // ReSharper disable once ClassNeverInstantiated.Global - actually instantiated in DataQualityAlertsJob
    public class DataQualityTreatmentOutcome36 : Alert, IHasRootEntityForAuditing
    {
        public static readonly Expression<Func<Notification, bool>> NotificationInQualifyingRangeExpression =
            // This corresponds to n.ClinicalDetails.StartingDate but we can't use it directly as LINQ->SQL gives up
            n => (n.ClinicalDetails.TreatmentStartDate ?? n.ClinicalDetails.DiagnosisDate) < DateTime.Today.AddYears(-3)
                 && n.NotificationStatus == NotificationStatus.Notified;

        public static readonly Func<Notification, bool> NotificationInRangeQualifies =
            n => TreatmentOutcomesHelper.IsTreatmentOutcomeMissingAtXYears(n, 3);

        public static readonly Func<Notification, bool> NotificationQualifies =
            n => NotificationInQualifyingRangeExpression.Compile()(n)
                 && NotificationInRangeQualifies(n);

        public override string Action => "Please provide an outcome with appropriate date";

        public override string ActionLink =>
            RouteHelper.GetNotificationOverviewPathWithSectionAnchor(
                // ReSharper disable once PossibleInvalidOperationException
                NotificationId.Value,
                NotificationSubPaths.EditTreatmentEvents);

        public DataQualityTreatmentOutcome36()
        {
            AlertType = AlertType.DataQualityTreatmentOutcome36;
        }
        public string RootEntityType => RootEntities.Notification;
        public string RootId => NotificationId.ToString();
    }
}
