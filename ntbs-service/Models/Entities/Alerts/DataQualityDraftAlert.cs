﻿using System;
using System.Linq.Expressions;
using EFAuditer;
using ntbs_service.Helpers;
using ntbs_service.Models.Enums;

namespace ntbs_service.Models.Entities.Alerts

{
    public class DataQualityDraftAlert : Alert, IHasRootEntityForAuditing
    {
        private const int MinNumberDaysDraftForAlert = 90;

        public static readonly Expression<Func<Notification, bool>> NotificationQualifiesExpression =
            n => n.NotificationStatus == NotificationStatus.Draft &&
                 n.CreationDate < DateTime.Now.AddDays(-MinNumberDaysDraftForAlert);

        public static readonly Func<Notification, bool> NotificationQualifies =
            NotificationQualifiesExpression.Compile();

        public override string Action => "Please review and action.";

        public override string ActionLink => RouteHelper.GetNotificationPath(
            NotificationId.GetValueOrDefault(),
            NotificationSubPaths.EditPatientDetails);

        public DataQualityDraftAlert()
        {
            AlertType = AlertType.DataQualityDraft;
        }
        public string RootEntityType => RootEntities.Notification;
        public string RootId => NotificationId.ToString();
    }
}
