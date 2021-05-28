﻿using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using ntbs_ui_tests.Helpers;
using ntbs_ui_tests.Hooks;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using TechTalk.SpecFlow;

namespace ntbs_ui_tests.Steps
{
    [Binding]
    public class ActSteps
    {
        private readonly IWebDriver Browser;
        private readonly TestConfig Settings;
        private readonly TestContext TestContext;

        public ActSteps(IWebDriver driver, TestConfig settings, TestContext testContext)
        {
            Browser = driver;
            Settings = settings;
            TestContext = testContext;
        }

        #region Navigation and action

        [When(@"I navigate to the url of the current notification")]
        public void WhenINavigateToCurrentNotificationUrl()
        {
            Browser.Navigate().GoToUrl($"{Settings.EnvironmentConfig.RootUri}/Notifications/{TestContext.AddedNotificationIds.Single()}");
        }
        
        [When(@"I click '(.*)' on the navigation bar")]
        public void ClickOnTheNavigationBar(string label)
        {
            var pageLink = HtmlElementHelper.FindElementByXpath(Browser, $"//*[@id='navigation-side-menu']/li[contains(.,'{label}')]/a");
            pageLink.Click();
        }

        [When(@"I log out")]
        public void WhenILogOut()
        {
            Browser.Navigate().GoToUrl($"{Settings.EnvironmentConfig.RootUri}/Logout");
        }

        [When(@"I click on the (.*) link")]
        public void WhenIClickOnTheLink(string linkLabel)
        {
            HtmlElementHelper.FindElementByXpath(Browser, $"//a[contains(.,'{linkLabel}')]").Click();
        }

        [When(@"I go to edit the '(.*)' section")]
        public void WhenIGoToEditTheSection(string section)
        {
            var sectionId =HtmlElementHelper.GetSectionIdFromSection(section);
            var link = Browser.FindElement(By.Id($"{sectionId}-title"))
                .FindElement(By.LinkText("Edit"));
            link.Click();
        }

        [When(@"I take action on the alert with title (.*)")]
        public void WhenITakeActionOnAlert(string title)
        {
            var alert = HtmlElementHelper.FindElementsByXpath(Browser, "//*[contains(@id, 'alert-')]").Single(a => a.Text.Contains(title));
            alert.FindElement(By.LinkText("Take action")).Click();
        }

        [When(@"I expand manage notification section")]
        public void WhenIExpandNotificationSection()
        {
            var button = Browser
                .FindElement(By.Id("manage-notification"))
                .FindElement(By.TagName("summary"));
            button.Click();
        }

        [When(@"I expand the '(.*)' section")]
        public void WhenIExpandSection(string sectionId)
        {
            var element = HtmlElementHelper.FindElementById(Browser, sectionId).FindElement(By.TagName("summary"));
            element.Click();
        }

        #endregion

        #region Input

        [When(@"I uncheck '(.*)'")]
        public void WhenIDeselectCheckbox(string elementId)
        {
            var element = HtmlElementHelper.FindElementById(Browser, elementId);
            if (element.Selected)
            {
                element.Click();
            }
        }

        [When(@"I check '(.*)'")]
        [When(@"I select radio value '(.*)'")]
        public void WhenISelectRadioOrCheckbox(string elementId)
        {
            var element = HtmlElementHelper.FindElementById(Browser, elementId);
            if (!element.Selected)
            {
                element.Click();
            }
        }

        [When(@"I click on the '(.*)' button")]
        public void WhenIClickOn(string elementId)
        {
            var button = HtmlElementHelper.FindElementById(Browser, elementId);
            button.Click();
        }

        [When(@"I select (.*) from input list '(.*)'")]
        public void WhenISelectFromInputList(string value, string inputListId)
        {
            var inputList = HtmlElementHelper.FindElementById(Browser, inputListId);
            inputList.Click();
            inputList.SendKeys(Keys.Control + "a");
            inputList.SendKeys(Keys.Delete);
            inputList.SendKeys(value);
            HtmlElementHelper.FindElementById(Browser, inputListId+"__option--0").Click();
            inputList.SendKeys("\t");
        }

        [When(@"I enter (.*) into '(.*)'")]
        public void WhenIEnterValueIntoFieldWithId(string value, string elementId)
        {
            var element = HtmlElementHelper.FindElementById(Browser, elementId);
            element.Click();
            element.SendKeys(Keys.Control + "a");
            element.SendKeys(Keys.Delete);
            element.SendKeys(value + "\t");
            if (!Settings.IsHeadless)
            {
                Thread.Sleep(1000);
            }
        }

        [When(@"I enter '(.*)' into date fields with id '(.*)'")]
        public void WhenIEnterDateIntoFieldWithId(string dateString, string elementId)
        {
            var date = DateTime.Parse(dateString, new CultureInfo("en-GB").DateTimeFormat);
            WhenIEnterValueIntoFieldWithId(date.Day.ToString(), elementId + "_Day");
            WhenIEnterValueIntoFieldWithId(date.Month.ToString(), elementId + "_Month");
            WhenIEnterValueIntoFieldWithId(date.Year.ToString(), elementId + "_Year");
            if (!Settings.IsHeadless)
            {
                Thread.Sleep(1000);
            }
        }
        
        [When(@"I make selection (.*) from (.*) section for '(.*)'")]
        public void WhenISelectValueFromGroupForFieldWithId(string value, string group, string elementId)
        {
            var selection = HtmlElementHelper.FindElementByXpath(Browser, $"//select[@id='{elementId}']/optgroup[@label='{group}']/option[@value='{value}']");
            selection.Click();
            if (!Settings.IsHeadless)
            {
                Thread.Sleep(1000);
            }
        }

        [When(@"I select (.*) for '(.*)'")]
        public void WhenISelectTextFromDropdown(string text, string selectId)
        {
            new SelectElement(HtmlElementHelper.FindElementById(Browser, selectId)).SelectByText(text);
        }

        #endregion

        [When(@"I wait")]
        public void WhenIWait()
        {
            Thread.Sleep(3000);
        }
    }
}
