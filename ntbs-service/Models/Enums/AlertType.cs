using System.ComponentModel.DataAnnotations;

namespace ntbs_service.Models.Enums
{
    public enum AlertType
    {
        [Display(Name = "RR/MDR/XDR-TB")]
        EnhancedSurveillanceMDR,
        [Display(Name = "M. bovis")]
        EnhancedSurveillanceMBovis,
        [Display(Name = "Unmatched specimen")]
        UnmatchedLabResult,
        [Display(Name = "Transfer request")]
        TransferRequest,
        [Display(Name = "Transfer rejected")]
        TransferRejected,
        [Display(Name = "Draft record open for more than 90 days")]
        DataQualityDraft,
        [Display(Name = "Unknown country of birth")]
        DataQualityBirthCountry,
        [Display(Name = "Clinical dates out of sequence")]
        DataQualityClinicalDates,
        [Display(Name = "Notification is in a cluster")]
        DataQualityCluster,
        [Display(Name = "Missing 12 month treatment outcome ")]
        DataQualityTreatmentOutcome12,
        [Display(Name = "Missing 24 month treatment outcome")]
        DataQualityTreatmentOutcome24,
        [Display(Name = "Missing 36 month treatment outcome")]
        DataQualityTreatmentOutcome36,
        [Display(Name = "Inconsistent values for social risk factors and DOT")]
        DataQualityDotVotAlert,
        [Display(Name = "Potential duplicate notification")]
        DataQualityPotientialDuplicate,
        [Display(Name = "ECM value not recorded for child")]
        DataQualityChildECMLevel,
        [Display(Name = "Test Alert")]
        Test
    }
}
