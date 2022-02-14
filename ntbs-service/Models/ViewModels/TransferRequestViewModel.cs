﻿using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using ExpressiveAnnotations.Attributes;
using Microsoft.AspNetCore.Mvc;
using ntbs_service.Models.Entities;
using ntbs_service.Models.Enums;
using ntbs_service.Models.ReferenceEntities;
using ntbs_service.Models.Validations;

namespace ntbs_service.Models.ViewModels
{
    public class TransferRequestViewModel
    {
        [BindProperty]
        [Display(Name = "TB Service")]
        [Required(ErrorMessage = ValidationMessages.FieldRequired)]
        [AssertThat(nameof(TransferDestinationNotCurrentTbService),
            ErrorMessage = ValidationMessages.TransferDestinationCannotBeCurrentTbService)]
        public string TbServiceCode { get; set; }

        public virtual TBService TbService { get; set; }

        [BindProperty]
        [Display(Name = "Case Manager")]
        [AssertThat(nameof(CaseManagerAllowedForTbService),
            ErrorMessage = ValidationMessages.CaseManagerMustBeAllowedForSelectedTbService)]
        public int? CaseManagerId { get; set; }

        public virtual User CaseManager { get; set; }

        [BindProperty]
        public TransferReason TransferReason { get; set; }
        
        [BindProperty]
        [Display(Name = "Transfer date")]
        [Required(ErrorMessage = ValidationMessages.RequiredEnter)]
        [ValidDateRange(ValidDates.EarliestBirthDate)]
        [AssertThat(nameof(TransferDateAfterNotificationStart), ErrorMessage = ValidationMessages.DateShouldBeLaterThanNotificationStart)]
        [AssertThat(nameof(TransferDateAfterLatestTransfer), ErrorMessage = ValidationMessages.DateShouldBeLaterThanLatestTransfer)]
        public DateTime TransferDate { get; set; }
        
        public DateTime? NotificationStartDate { get; set; }
        public DateTime? LatestTransferDate { get; set; }

        [BindProperty]
        [Display(Name = "Other description")]
        [MaxLength(200)]
        [RegularExpression(
            ValidationRegexes.CharacterValidationWithNumbersForwardSlashAndNewLine,
            ErrorMessage = ValidationMessages.StringWithNumbersAndForwardSlashFormat)]
        public string OtherReasonDescription { get; set; }

        [BindProperty]
        [Display(Name = "Optional note")]
        [MaxLength(200)]
        [RegularExpression(
            ValidationRegexes.CharacterValidationWithNumbersForwardSlashAndNewLine,
            ErrorMessage = ValidationMessages.StringWithNumbersAndForwardSlashFormat)]
        public string TransferRequestNote { get; set; }


        public bool CaseManagerAllowedForTbService
        {
            // ReSharper disable once UnusedMember.Global - used in the validation
            get
            {
                // If email not set, or TBService missing (ergo navigation properties not yet retrieved) pass validation
                if (CaseManagerId == null || TbServiceCode == null)
                {
                    return true;
                }

                if (CaseManager?.CaseManagerTbServices == null || CaseManager.CaseManagerTbServices.Count == 0)
                {
                    return false;
                }

                return CaseManager.CaseManagerTbServices.Any(c => c.TbServiceCode == TbServiceCode);
            }
        }

        public string NotificationTbServiceCode { get; set; }

        public bool TransferDestinationNotCurrentTbService => TbServiceCode != NotificationTbServiceCode;

        public bool TransferDateAfterNotificationStart => TransferDate > NotificationStartDate;
        public bool TransferDateAfterLatestTransfer => LatestTransferDate is null || TransferDate > LatestTransferDate;
    }
}
