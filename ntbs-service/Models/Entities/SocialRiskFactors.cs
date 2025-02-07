﻿using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using EFAuditer;
using Microsoft.EntityFrameworkCore;
using ntbs_service.Models.Enums;
using ntbs_service.Models.Validations;

namespace ntbs_service.Models.Entities
{
    [Owned]
    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    [Display(Name = "Social risk factors")]
    public partial class SocialRiskFactors : ModelBase, IOwnedEntityForAuditing
    {
        public SocialRiskFactors()
        {
            RiskFactorHomelessness = new RiskFactorDetails(RiskFactorType.Homelessness);
            RiskFactorDrugs = new RiskFactorDetails(RiskFactorType.Drugs);
            RiskFactorImprisonment = new RiskFactorDetails(RiskFactorType.Imprisonment);
            RiskFactorSmoking = new RiskFactorDetails(RiskFactorType.Smoking);
        }

        [Display(Name = "Is the patient’s ability to self-administer treatment affected by alcohol misuse or abuse?")]
        public Status? AlcoholMisuseStatus { get; set; }
        [Display(Name = "Is the patient’s ability to self-administer treatment affected by mental health illness?")]
        public Status? MentalHealthStatus { get; set; }
        [Display(Name = "Is the patient an asylum seeker?")]
        public Status? AsylumSeekerStatus { get; set; }
        [Display(Name = "Is the patient an immigration removal centre detainee?")]
        public Status? ImmigrationDetaineeStatus { get; set; }

        [Display(Name = "History of smoking")]
        [ValidationChild]
        public virtual RiskFactorDetails RiskFactorSmoking { get; set; }
        [Display(Name = "History of drug misuse")]
        [ValidationChild]
        public virtual RiskFactorDetails RiskFactorDrugs { get; set; }
        [Display(Name = "History of homelessness")]
        [ValidationChild]
        public virtual RiskFactorDetails RiskFactorHomelessness { get; set; }
        [Display(Name = "History of imprisonment")]
        [ValidationChild]
        public virtual RiskFactorDetails RiskFactorImprisonment { get; set; }

        string IOwnedEntityForAuditing.RootEntityType => RootEntities.Notification;
    }
}
