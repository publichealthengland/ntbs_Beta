using System;
using System.Linq.Expressions;
using ntbs_service.Helpers;
using ntbs_service.Models.Enums;

namespace ntbs_service.Models.Entities.Alerts

{
    public class DataQualityChildECMLevel : Alert
    {
        private const int MinNumberDaysDraftForAlert = 45;

        public static readonly Expression<Func<Notification, bool>> NotificationQualifiesExpression =
            n => n.NotificationStatus == NotificationStatus.Notified
                 && n.CreationDate < DateTime.Now.AddDays(-MinNumberDaysDraftForAlert)
                 && (n.PatientDetails.Dob.Value.Month * 100 + n.PatientDetails.Dob.Value.Day > n.NotificationDate.Value.Month * 100 + n.NotificationDate.Value.Day
                     ? (n.NotificationDate.Value.Year - n.PatientDetails.Dob.Value.Year - 1) < 16
                     : n.NotificationDate.Value.Year - n.PatientDetails.Dob.Value.Year < 16)
                 && n.ClinicalDetails.EnhancedCaseManagementLevel == 0;

        public static readonly Func<Notification, bool> NotificationQualifies =
            NotificationQualifiesExpression.Compile();

        public override string Action => "Please review whether the value given for ECM is correct.";

        public override string ActionLink => RouteHelper.GetNotificationPath(
            NotificationId.GetValueOrDefault(),
            NotificationSubPaths.EditClinicalDetails);

        public DataQualityChildECMLevel()
        {
            AlertType = AlertType.DataQualityChildECMLevel;
        }
    }
}
