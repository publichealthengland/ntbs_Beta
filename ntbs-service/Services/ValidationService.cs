﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using ntbs_service.Helpers;
using ntbs_service.Models;
using ntbs_service.Models.Entities;
using ntbs_service.Models.Validations;

namespace ntbs_service.Services
{
    public class ValidationService
    {
        private readonly PageModel _pageModel;

        private ModelStateDictionary ModelState => _pageModel.ModelState;

        public ValidationService(PageModel pageModel)
        {
            _pageModel = pageModel;
        }

        public ContentResult GetPropertyValidationResult<T>(string key, object value, bool shouldValidateFull) where T : ModelBase
        {
            var model = (T)Activator.CreateInstance(typeof(T));
            model.ShouldValidateFull = shouldValidateFull;
            return GetPropertyValidationResult(model, key, value);
        }

        public ContentResult GetPropertyValidationResult(object model, string key, object value)
        {
            try
            {
                SetProperty(model, key, value);
            }
            catch (Exception)
            {
                var propertyDisplayName = model.GetMemberDisplayName(key);
                ModelState.AddModelError(key, ValidationMessages.ValueInvalid(propertyDisplayName));
            }

            return GetValidationResult(model, key);
        }

        public ContentResult GetMultiplePropertiesValidationResult<T>(
            IEnumerable<(string, object)> propertyValueTuples,
            bool shouldValidateFull = false,
            bool? isLegacy = false)
            where T : ModelBase
        {
            var model = (T)Activator.CreateInstance(typeof(T));
            model.ShouldValidateFull = shouldValidateFull;
            model.IsLegacy = isLegacy;

            var keys = new List<string>();
            foreach (var (key, value) in propertyValueTuples)
            {
                keys.Add(key);

                try
                {
                    SetProperty(model, key, value);
                }
                catch (Exception)
                {
                    var propertyDisplayName = model.GetMemberDisplayName(key) ?? key;
                    ModelState.AddModelError(key, ValidationMessages.ValueInvalid(propertyDisplayName));
                }
            }
            return GetValidationResult(model, keys);
        }

        /// <exception cref="Exception">Throws a range of exceptions corresponding to invalid conversions to the property type</exception>
        private static void SetProperty(object model, string key, object value)
        {
            if (value == null)
            {
                return;
            }

            var property = model.GetType().GetProperty(key);
            var converter = TypeDescriptor.GetConverter(property.PropertyType);

            try
            {
                value = converter.ConvertFrom(value);
            }
            catch (NotSupportedException)
            {
                /*
                     Ignore conversion issues when attempting to convert to a non-supported type e.g. List.
                */
            }

            property.SetValue(model, value);
        }

        public ContentResult GetDateValidationResult<T>(string key, string day, string month, string year,
            bool isLegacy) where T : ModelBase
        {
            var model = (T)Activator.CreateInstance(typeof(T));
            model.IsLegacy = isLegacy;
            return GetDateValidationResult(model, key, day, month, year);
        }

        public ContentResult GetDateValidationResult<T>(T model, string key, string day, string month, string year)
        {
            var formattedDate = new FormattedDate { Day = day, Month = month, Year = year };
            var modelType = model.GetType();
            if (formattedDate.TryConvertToDateTime(out var convertedDate))
            {
                modelType.GetProperty(key).SetValue(model, convertedDate);
                return GetValidationResult(model, key);
            }
            else
            {
                var propertyDisplayName = modelType.GetMemberDisplayName(key) ?? key;
                return _pageModel.Content(ValidationMessages.InvalidDate(propertyDisplayName));
            }
        }

        public ContentResult GetValidationResult(object model, string key)
        {
            ModelState.Clear();
            _pageModel.TryValidateModel(model);

            var modelStateByKey = ModelState[key];

            if (modelStateByKey?.ValidationState == ModelValidationState.Invalid)
            {
                return _pageModel.Content(modelStateByKey.Errors[0].ErrorMessage);
            }
            return ValidContent();
        }

        private ContentResult GetValidationResult(object model, IEnumerable<string> keys)
        {
            if (!_pageModel.TryValidateModel(model))
            {
                var errorMessageMap = new Dictionary<int, string>();
                var errorIndex = 0;

                foreach (var key in keys)
                {
                    var modelStateByKey = ModelState[key];
                    if (modelStateByKey?.ValidationState == ModelValidationState.Invalid)
                    {
                        errorMessageMap.Add(errorIndex, modelStateByKey.Errors[0].ErrorMessage);
                    }
                    errorIndex++;
                }
                if (errorMessageMap.Count > 0)
                {
                    return _pageModel.Content(JsonConvert.SerializeObject(errorMessageMap), "application/json");
                }
            }

            return ValidContent();
        }

        public ContentResult ValidContent()
        {
            return _pageModel.Content("");
        }

        public bool IsValid(string key)
        {
            if (ModelState[key] == null)
            {
                return true;
            }
            return ModelState[key].Errors.Count == 0;
        }

        /// <param name="model"> The model on which date gets set </param>
        /// <param name="modelKey"> Prefix for model state errors </param>
        /// <param name="key"> Date key for model state errors </param>
        /// <param name="formattedDate"> The FormattedDate to covert and set </param>
        public void TrySetFormattedDate(object model, string modelKey, string key, FormattedDate formattedDate)
        {
            var modelType = model.GetType();
            if (formattedDate.TryConvertToDateTime(out var convertedDob))
            {
                modelType.GetProperty(key)?.SetValue(model, convertedDob);
            }
            else
            {
                var propertyDisplayName = modelType.GetMemberDisplayName(key);
                ModelState.AddModelError($"{modelKey}.{key}", ValidationMessages.InvalidDate(propertyDisplayName));
            }
        }

        public ContentResult GetFullModelValidationResult(object model)
        {
            if (_pageModel.TryValidateModel(model, model.GetType().Name))
            {
                return ValidContent();
            }
            else
            {
                var keyErrorDictionary = new Dictionary<string, string>();
                var modelState = _pageModel.ViewData.ModelState;
                foreach (var modelStateKey in modelState.Keys)
                {
                    var modelStateVal = modelState[modelStateKey];
                    foreach (var error in modelStateVal.Errors)
                    {
                        // Only add the first error per key to the dictionary.
                        keyErrorDictionary.TryAdd(modelStateKey, error.ErrorMessage);
                    }
                }
                return _pageModel.Content(JsonConvert.SerializeObject(keyErrorDictionary));
            }
        }

        public ContentResult GetYearComparisonValidationResult(int yearToValidate, int yearToCompare, string propertyName)
        {
            if (!IsValidYear(yearToValidate))
            {
                return _pageModel.Content(ValidationMessages.InvalidYear(propertyName));
            }

            if (yearToValidate < yearToCompare)
            {
                return _pageModel.Content(ValidationMessages.ValidYearLaterThanBirthYear(propertyName, yearToCompare));
            }
            else
            {
                return ValidContent();
            }
        }

        public void ValidateProperty(object model, string modelKey, object modelProperty, string propertyKey)
        {
            var validationContext = new ValidationContext(model);
            var validationResults = new List<ValidationResult>();
            validationContext.MemberName = propertyKey;

            Validator.TryValidateProperty(modelProperty, validationContext, validationResults);

            if (validationResults.Any())
            {
                var errorKey = $"{modelKey}.{propertyKey}";
                ModelState.AddModelError(errorKey, validationResults[0].ErrorMessage);
            }
        }

        public void ValidateYearComparison(object model, string key, int yearToValidate, int? yearToCompare)
        {
            var modelType = model.GetType();
            var modelTypeName = modelType.Name;

            if (!IsValidYear(yearToValidate))
            {
                var propertyDisplayName = modelType.GetMemberDisplayName(key);
                ModelState.AddModelError($"{modelTypeName}.{key}", ValidationMessages.InvalidYear(propertyDisplayName));
                return;
            }

            if (yearToCompare != null && yearToValidate < (int)yearToCompare)
            {
                var propertyDisplayName = modelType.GetMemberDisplayName(key);
                ModelState.AddModelError($"{modelTypeName}.{key}", ValidationMessages.ValidYearLaterThanBirthYear(propertyDisplayName, (int)yearToCompare));
            }
        }

        private bool IsValidYear(int year)
        {
            return year >= ValidDates.EarliestYear && year <= DateTime.Now.Year;
        }
    }
}
