﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using EFAuditer;
using ExpressiveAnnotations.Attributes;
using Newtonsoft.Json.Serialization;
using ntbs_service.Models.ReferenceEntities;
using ntbs_service.Models.Validations;

namespace ntbs_service.Models.Entities
{
    [Display(Name = "Hospital details")]
    public partial class HospitalDetails : ModelBase, IOwnedEntityForAuditing
    {
        public int NotificationId { get; set; }

        [MaxLength(200)]
        [RegularExpression(ValidationRegexes.CharacterValidationAsciiBasic, ErrorMessage = ValidationMessages.InvalidCharacter)]
        [Display(Name = "Consultant")]
        public string Consultant { get; set; }

        [AssertThat("CaseManagerId == ExistingCaseManagerId || CaseManagerAllowedForTbService", ErrorMessage = ValidationMessages.CaseManagerMustBeAllowedForSelectedTbService)]
        [Display(Name = "Case Manager")]
        public int? CaseManagerId { get; set; }
        public virtual User CaseManager { get; set; }

        [RequiredIf(@"ShouldValidateFull", ErrorMessage = ValidationMessages.FieldRequired)]
        [Display(Name = "TB Service")]
        public string TBServiceCode { get; set; }
        public virtual TBService TBService { get; set; }

        [RequiredIf(@"ShouldValidateFull", ErrorMessage = ValidationMessages.FieldRequired)]
        [AssertThat(@"HospitalBelongsToTbService", ErrorMessage = ValidationMessages.HospitalMustBelongToSelectedTbSerice)]
        [Display(Name = "Hospital")]
        public Guid? HospitalId { get; set; }
        public virtual Hospital Hospital { get; set; }

        [Display(Name = "TB Service record is shared with")]
        public string SecondaryTBServiceCode { get; set; }
        public virtual TBService SecondaryTBService { get; set; }

        [Display(Name = "Reason for TB Service share")]
        public string ReasonForTBServiceShare { get; set; }

        [NotMapped]
        public bool HospitalBelongsToTbService => Hospital?.TBService == TBService;

        [NotMapped]
        public bool CaseManagerAllowedForTbService
        {
            get
            {
                // If email not set, or TBService missing (ergo navigation properties not yet retrieved) pass validation
                if (CaseManagerId == null || TBService == null)
                {
                    return true;
                }

                if (CaseManager?.CaseManagerTbServices == null || CaseManager.CaseManagerTbServices.Count == 0)
                {
                    return false;
                }

                return CaseManager.CaseManagerTbServices.Any(c => c.TbService == TBService);
            }
        }


        [NotMapped]
        [Display(Name = "Case Manager")]
        public string CaseManagerName => CaseManager?.DisplayName;
        
        [NotMapped]
        public int? ExistingCaseManagerId { get; set; }

        string IOwnedEntityForAuditing.RootEntityType => RootEntities.Notification;
    }
}
