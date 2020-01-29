﻿using System.ComponentModel.DataAnnotations;

namespace ntbs_service.Models.Enums
{
    public enum AlertType
    {
        [Display(Name = "Enhanced surveillance - MDR")]
        EnhancedSurveillanceMDR,
        EnhancedSurveillanceMBovis,
        MissingTreatmentOutcome,
        UnmatchedLabResult,
        [Display(Name = "Transfer Request")]
        TransferRequest,
        [Display(Name = "Transfer rejected")]
        TransferRejected,
        DataQualityDraft,
        DataQualityBirthCountry,
        DataQualityClinicalDates,
        DataQualityCluster,
        SocialContext,
        [Display(Name = "Test Alert")]
        Test
    }
}
