using System;

namespace ntbs_service.Models.Validations
{
    public static class ValidationMessages
    {
        public const string ValidDate = "Please enter a valid date";
        public const string ValidYear = "Please enter a valid year";
        public static string DateValidityRange(string startDate)
        {
            return $"Must be between {startDate} and {DateTime.Now.ToShortDateString()}";
        }

        public static string ValidYearLaterThanBirthYear(int birthYear)
        {
            return $"Should be later than birth year ({birthYear})";
        }

        public const string StandardStringFormat = "Can only contain letters and ' - . ,";
        public const string NhsNumberFormat = "NHS Number can only contain digits 0-9";
        public const string NhsNumberLength = "NHS Number needs to be 10 digits long";
        public const string PreviousTBDiagnosisYear = "Please enter a valid year. Year of diagnosis cannot be after 2000";
        public const string RiskFactorSelection = "At least one field should be selected";
        public const string PositiveNumbersOnly = "Please enter a positive value";
        public const string ContactTracingContactsScreened = "Must be smaller or equal to the number identified";
        public const string ContactTracingContactsLatentTB = "Must be smaller or equal to than number screened minus those with latent TB";
        public const string ContactTracingContactsActiveTB = "Must be smaller or equal to than number of adults screened minus those with active TB";
        public const string ContactTracingContactsStartedTreatment = "Must be smaller or equal to number of adults with latent TB";
        public const string ContactTracingContactsFinishedTreatment = "Must be smaller or equal to number of adults the started treatment";
    }

    public static class ValidDates
    {
        public const string EarliestBirthDate = "01/01/1900";
        public const string EarliestClinicalDate = "01/01/2010";
        public const int EarliestYear = 1900;
    }

    public static class ValidationRegexes
    {
        public const string ValidCharactersForName = @"[a-zA-Z \-,.']+";
    }
}