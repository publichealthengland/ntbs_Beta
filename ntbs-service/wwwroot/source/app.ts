// The line below ensures that the `module` variable below is resolved to the correct declaration by TS
// - otherwise it clashes with the definition created for Node which doesn't include the `hot` property
///<reference types="webpack-env" />

// Root styles import - other global styles are imported from this sass file
import "../css/site.scss"
// @ts-ignore
import config from "./config/config-APP_TARGET";
import Vue from "vue";
import {initAll as govUkJsInitAll} from "govuk-frontend";
//import * as Sentry from '@sentry/browser';
//import * as SentryIntegrations from '@sentry/integrations';
// @ts-ignore
import Details from '../../node_modules/nhsuk-frontend/packages/components/details/details';
// @ts-ignore
import ErrorSummary from '../../node_modules/nhsuk-frontend/packages/components/error-summary/error-summary';
import '../../node_modules/nhsuk-frontend/packages/polyfills';
import VueAccessibleModal from 'vue-accessible-modal'
import cssVars from 'css-vars-ponyfill';
// Components
import ValidateInput from "./Components/ValidateInput";
import ValidateDate from "./Components/ValidateDate";
import DateComparison from "./Components/DateComparison";
import YearComparison from "./Components/YearComparison";
import ValidateContactTracing from "./Components/ValidateContactTracing";
import ValidateImmunosuppression from "./Components/ValidateImmunosuppression";
import ValidateTravelOrVisit from "./Components/ValidateTravelOrVisit";
import ValidateMultiple from "./Components/ValidateMultiple";
import ValidateRequiredCheckboxes from "./Components/ValidateRequiredCheckboxes";
import ValidatePostcode from "./Components/ValidatePostcode";
import ConditionalSelectWrapper from "./Components/ConditionalSelectWrapper";
import AutocompleteSelect from "./Components/AutocompleteSelect";
import NhsNumberDuplicateWarning from "./Components/NhsNumberDuplicateWarning";
import FilteredDropdown from "./Components/FilteredDropdown";
import CascadingDropdown from "./Components/CascadingDropdown";
import TbServiceFilteredAlerts from "./Components/TbServiceFilteredAlerts";
import ValidateRelatedNotification from "./Components/ValidateRelatedNotification";
import NotificationInfo from "./Components/NotificationInfo";
import HideSectionIfNotTrue from "./Components/HideSectionIfNotTrue";
import FetchSpecimenPotentialMatch from "./Components/FetchSpecimenPotentialMatch";
import FilteredHomepageKpiDetails from "./Components/FilteredHomepageKpiDetails";
import PrintButton from "./Components/PrintButton";
import FormLeaveChecker from "./Components/FormLeaveChecker";
import ConfirmComponent from "./Components/ConfirmComponent";
import InactivityChecker from "./Components/InactivityChecker";
import InactivityLeaveComponent from "./Components/InactivityLeaveComponent";
import DateInput from "./Components/DateInput";
import NotificationWarning from "./Components/NotificationWarning";
import NavigationWithSubmenu from "./Components/NavigationWithSubmenu";
import BackLinkRetainingHistory from "./Components/BackLinkRetainingHistory";
import SingleSubmitForm from "./Components/SingleSubmitForm";
// For compatibility with IE11. ArrayFromPolyfill required by vue-accessible-modal.
require("es6-promise").polyfill();
require("./Polyfills/ArrayFromPolyfill");

Vue.use(VueAccessibleModal, { transition: "fade" });
// register Vue components
Vue.component("date-input", DateInput);
Vue.component("validate-input", ValidateInput);
Vue.component("validate-date", ValidateDate);
Vue.component("date-comparison", DateComparison);
Vue.component("validate-contact-tracing", ValidateContactTracing);
Vue.component("validate-immunosuppression", ValidateImmunosuppression);
Vue.component("validate-travel-or-visit", ValidateTravelOrVisit);
Vue.component("year-comparison", YearComparison);
Vue.component("validate-multiple", ValidateMultiple);
Vue.component("validate-required-checkboxes", ValidateRequiredCheckboxes);
Vue.component("validate-postcode", ValidatePostcode);
Vue.component("conditional-select-wrapper", ConditionalSelectWrapper);
Vue.component("autocomplete-select", AutocompleteSelect);
Vue.component("nhs-number-duplicate-warning", NhsNumberDuplicateWarning);
Vue.component("filtered-dropdown", FilteredDropdown);
Vue.component("cascading-dropdown", CascadingDropdown);
Vue.component("tb-service-filtered-alerts", TbServiceFilteredAlerts);
Vue.component("validate-related-notification", ValidateRelatedNotification);
Vue.component("notification-info", NotificationInfo);
Vue.component("notification-warning", NotificationWarning);
Vue.component("hide-section-if-not-true", HideSectionIfNotTrue);
Vue.component("fetch-specimen-potential-match", FetchSpecimenPotentialMatch);
Vue.component("filtered-homepage-kpi", FilteredHomepageKpiDetails);
Vue.component("print-button", PrintButton);
Vue.component("form-leave-checker", FormLeaveChecker);
Vue.component("confirm-component", ConfirmComponent);
Vue.component("inactivity-checker", InactivityChecker);
Vue.component("inactivity-component", InactivityLeaveComponent);
Vue.component("navigation-with-submenu", NavigationWithSubmenu);
Vue.component("back-link-retaining-history", BackLinkRetainingHistory);
Vue.component("single-submit-form", SingleSubmitForm);

// Vue needs to be the first thing to load!
// Otherwise, it replaces the templates of its components with fresh content, potentially overwriting changes from other scripts!
new Vue({
    el: "#app",
});

cssVars();
govUkJsInitAll();

// Initialize NHS components
document.addEventListener('DOMContentLoaded', () => {
    Details();
    ErrorSummary();
});
