﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EFAuditer;
using ExpressiveAnnotations.Attributes;
using ntbs_service.Models.Validations;

namespace ntbs_service.Models.Entities
{
    public abstract partial class SocialContextBase : ModelBase, IHasRootEntityForAuditing
    {
        public int NotificationId { get; set; }
        // We are not including a navigation property to Notification, otherwise it gets validated
        // on every TryValidateModel

        [MaxLength(150)]
        [RegularExpression(
            ValidationRegexes.CharacterValidationWithNumbersForwardSlashAndNewLine,
            ErrorMessage = ValidationMessages.StringWithNumbersAndForwardSlashFormat)]
        [Display(Name = "Address")]
        public string Address { get; set; }

        [RegularExpression(ValidationRegexes.PostcodeValidation, ErrorMessage = ValidationMessages.NotValid)]
        [Display(Name = "Postcode")]
        public string Postcode { get; set; }

        [AssertThat(nameof(DateFromAfterDob), ErrorMessage = ValidationMessages.DateShouldBeLaterThanDob)]
        [ValidDateRange(ValidDates.EarliestAllowedDate)]
        [Display(Name = "From")]
        public DateTime? DateFrom { get; set; }

        [AssertThat(nameof(DateToAfterDob), ErrorMessage = ValidationMessages.DateShouldBeLaterThanDob)]
        [AssertThat(@"DateFrom == null || DateTo >= DateFrom", ErrorMessage = ValidationMessages.VenueDateToShouldBeLaterThanDateFrom)]
        [ValidDateRange(ValidDates.EarliestAllowedDate)]
        [Display(Name = "To")]
        public DateTime? DateTo { get; set; }

        [MaxLength(250)]
        [ContainsNoTabs]
        [RegularExpression(
            ValidationRegexes.CharacterValidationAsciiSpaceTozAndEmDashAndNewline,
            ErrorMessage = ValidationMessages.InvalidCharacter)]
        [Display(Name = "Comments")]
        public string Details { get; set; }

        /// <summary>
        /// Used for validation purposes only, requires consumer to populate it.
        /// </summary>
        [NotMapped]
        public DateTime? Dob { get; set; }

        public bool DateFromAfterDob => Dob == null || DateFrom >= Dob;
        public bool DateToAfterDob => Dob == null || DateTo >= Dob;

        public abstract int Id { set; }

        string IHasRootEntityForAuditing.RootEntityType => RootEntities.Notification;
        string IHasRootEntityForAuditing.RootId => NotificationId.ToString();
    }
}
