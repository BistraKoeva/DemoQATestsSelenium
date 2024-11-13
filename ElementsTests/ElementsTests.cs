using OpenQA.Selenium;
using OpenQA.Selenium.BiDi.Modules.BrowsingContext;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace ElementsTests
{
    public class ElementsTests
    {
        private IWebDriver driver;
        private static readonly string BaseUrl = "https://demoqa.com/elements";
        private Actions actions;
        private WebDriverWait wait;
        private string downloadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "downloads");

        [SetUp]
        public void Setup()
        {
            var chromeOptions = new ChromeOptions();
            // These are settings for configuring Chrome browser for the download test.
            chromeOptions.AddUserProfilePreference("download.default_directory", downloadDirectory);
            chromeOptions.AddUserProfilePreference("download.prompt_for_download", false); 
            chromeOptions.AddUserProfilePreference("download.directory_upgrade", true); 
            chromeOptions.AddUserProfilePreference("safebrowsing.enabled", true);

            driver = new ChromeDriver(chromeOptions);
            driver.Manage().Window.Maximize();

            actions = new Actions(driver);
            wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));

            driver.Navigate().GoToUrl(BaseUrl);

            // There is an I-frame for Google ads in the application under test that has to be loaded before running the tests. 
            try
            {
                wait.Until(ExpectedConditions.FrameToBeAvailableAndSwitchToIt(By.XPath("//iframe[contains(@id,'google_ads_iframe')]")));
                driver.SwitchTo().DefaultContent();
            }
            catch (WebDriverTimeoutException)
            {
                Console.WriteLine("The google ads iframe did not load in 5 seconds.");
            }
        }

        [TearDown]
        public void TearDown()
        {
            driver.Quit();
            driver.Dispose();
        }

        private void OpenMenuElement(string elementId)
        {
            //All sections in the page have items with ids item-0, item-1, etc.
            //So the driver will find several elements by such an id and we want to click on the one which is currently visible as its section is expanded.
            var elementsOptions = driver.FindElements(By.Id(elementId));

            foreach (var option in elementsOptions)
            {
                if (option.Displayed)
                {
                    ScrollToFractionOfPage(6);
                    option.Click();
                    break;
                }
            }
        }

        // Scrolling to element on the page sometimes leaves the element behind a Google ads i-frame. This is why scrolling the whole page to certain fraction is needed.
        private void ScrollToFractionOfPage(int pageFraction)
        {
            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
            js.ExecuteScript($"window.scrollTo(0, document.body.scrollHeight / {pageFraction});");
        }


        [Test, Category("TextBoxTests")]
        public void TextBoxTest_ValidInput()
        {
            OpenMenuElement("item-0");

            var submitBtn = driver.FindElement(By.XPath("//button[text()='Submit']"));

            driver.FindElement(By.Id("userName")).SendKeys("Bistra Koeva");
            driver.FindElement(By.Id("userEmail")).SendKeys("bistra@test.bg");
            driver.FindElement(By.Id("currentAddress")).SendKeys("Sofia");
            driver.FindElement(By.Id("permanentAddress")).SendKeys("Plovdiv");

            ScrollToFractionOfPage(1);

            submitBtn.Click();

            var resultFields = driver.FindElements(By.XPath("//div[@class='border col-md-12 col-sm-12']//p"));
            
            Assert.That(resultFields.Count, Is.EqualTo(4), "The paragraphs count in the result field should be 4.");
            Assert.That(resultFields[0].Text, Is.EqualTo("Name:Bistra Koeva"), "The name is not displayed correctly.");
            Assert.That(resultFields[1].Text, Is.EqualTo("Email:bistra@test.bg"), "The email is not displayed correctly.");
            Assert.That(resultFields[2].Text, Is.EqualTo("Current Address :Sofia"), "The current address is not displayed correctly.");
            Assert.That(resultFields[3].Text, Is.EqualTo("Permananet Address :Plovdiv"), "The permanent address is not displayed correctly.");
        }

        [Test, Category("TextBoxTests")]
        public void TextBoxTest_InvalidEmailInput()
        {
            OpenMenuElement("item-0");

            var submitBtn = driver.FindElement(By.XPath("//button[text()='Submit']"));

            driver.FindElement(By.Id("userName")).SendKeys("Bistra Koeva");
            driver.FindElement(By.Id("userEmail")).SendKeys("invalidEmail");
            driver.FindElement(By.Id("currentAddress")).SendKeys("Sofia");
            driver.FindElement(By.Id("permanentAddress")).SendKeys("Plovdiv");

            ScrollToFractionOfPage(1);

            submitBtn.Click();

            // Waiting for the userEmailInput to change its border-color to red.
            wait.Until(driver =>
            {
                IWebElement userEmailInput = driver.FindElement(By.Id("userEmail"));
                string borderColor = userEmailInput.GetCssValue("border-color");
                return borderColor == "rgb(255, 0, 0)";
            });

            var userEmailInput = driver.FindElement(By.Id("userEmail"));

            Assert.That(userEmailInput.GetCssValue("border-color"), Is.EqualTo("rgb(255, 0, 0)"), "The border color should be red.");
        }

        [Test, Category("CheckboxTests")]
        public void CheckboxTest_ExpandAll()
        {
            OpenMenuElement("item-1");

            IWebElement expandAllButton = driver.FindElement(By.XPath("//button[@aria-label='Expand all']"));
            expandAllButton.Click();

            var allOptionsSpans = driver.FindElements(By.XPath("//span[@class='rct-text']"));
            Assert.That(allOptionsSpans.Count, Is.EqualTo(17), "All 17 option should be expanded.");
        }

        [Test, Category("CheckboxTests")]
        public void CheckboxTest_ColapseAll()
        {
            OpenMenuElement("item-1");

            IWebElement expandAllButton = driver.FindElement(By.XPath("//button[@aria-label='Expand all']"));
            expandAllButton.Click();

            IWebElement collapseAllButton = driver.FindElement(By.XPath("//button[@aria-label='Collapse all']"));
            collapseAllButton.Click();

            var allOptionsSpans = driver.FindElements(By.XPath("//span[@class='rct-text']"));
            Assert.That(allOptionsSpans.Count, Is.EqualTo(1), "Only option 'Home' should be displayed.");
            Assert.That(allOptionsSpans[0].Text, Is.EqualTo("Home"), "Only option 'Home' should be displayed.");
        }

        [Test, Category("CheckboxTests")]
        public void CheckboxTest_SelectAllOptions()
        {
            OpenMenuElement("item-1");

            IWebElement homeCheckbox = wait.Until(ExpectedConditions.ElementExists(By.XPath("//label[@for='tree-node-home']//span[@class='rct-checkbox']")));
            homeCheckbox.Click();

            IWebElement expandAllButton = driver.FindElement(By.XPath("//button[@aria-label='Expand all']"));
            expandAllButton.Click();

            var allOptionsIcons = driver.FindElements(By.XPath("//li[@class='rct-node rct-node-parent rct-node-expanded']//span[@class='rct-checkbox']//*[local-name()='svg']"));

            for (int i = 0; i < allOptionsIcons.Count; i++)
            {
                string currentIconCalss = allOptionsIcons[i].GetAttribute("class");
                Assert.IsTrue(currentIconCalss.Contains("rct-icon-check"), "All opitons shoud be checked.");
            }
        }

        [Test, Category("CheckboxTests")]
        public void CheckboxTest_SelectOneOption()
        {
            OpenMenuElement("item-1");

            IWebElement expandAllButton = driver.FindElement(By.XPath("//button[@aria-label='Expand all']"));
            expandAllButton.Click();

            IWebElement reactCheckbox = wait.Until(ExpectedConditions.ElementExists(By.XPath("//label[@for='tree-node-react']//span[@class='rct-checkbox']")));

            ScrollToFractionOfPage(3);

            reactCheckbox.Click();

            // The react checkbox should have the checked icon and its ancestors should have the half-checked icon.
            var ReactCheckboxAndItsAncestors = driver.FindElements(By.XPath("//label[@for='tree-node-react']/ancestor::li/span//span[@class='rct-checkbox']/*[local-name()='svg']"));

            for (int i = 0; i < ReactCheckboxAndItsAncestors.Count - 1; i++)
            {
                string iconClass = ReactCheckboxAndItsAncestors[i].GetAttribute("class");
                Assert.IsTrue(iconClass.Contains("rct-icon-half-check"), "The half-checked icon should be displayed in the ancestors' checkbox.");
            }

            string classOfCheckedIconOfReactCheckbox = ReactCheckboxAndItsAncestors.Last().GetAttribute("class");
            Assert.IsTrue(classOfCheckedIconOfReactCheckbox.Contains("rct-icon-check"), "The checked icon should be displayed in the react checkbox.");

            IWebElement resultField = driver.FindElement(By.Id("result"));
            Assert.That(resultField.Text, Is.EqualTo($"You have selected :{Environment.NewLine}react"), "The react option should be selected."); 
        }

        [Test, Category("CheckboxTests")]
        public void CheckboxTest_DeselectOneOption()
        {
            OpenMenuElement("item-1");

            IWebElement homeCheckbox = wait.Until(ExpectedConditions.ElementExists(By.XPath("//label[@for='tree-node-home']//span[@class='rct-checkbox']")));
            homeCheckbox.Click();

            IWebElement expandAllButton = driver.FindElement(By.XPath("//button[@aria-label='Expand all']"));
            expandAllButton.Click();

            IWebElement reactCheckbox = wait.Until(ExpectedConditions.ElementExists(By.XPath("//label[@for='tree-node-react']//span[@class='rct-checkbox']")));
            ScrollToFractionOfPage(3);
            reactCheckbox.Click();

            // The react checkbox should have the uncheck icon and its ancestors should have the half-checked icon.
            var ancestorElementsAndUncheckedElementIcons = driver.FindElements(By.XPath("//label[@for='tree-node-react']/ancestor::li/span//span[@class='rct-checkbox']/*[local-name()='svg']"));

            for (int i = 0; i < ancestorElementsAndUncheckedElementIcons.Count - 1; i++)
            {
                string iconClass = ancestorElementsAndUncheckedElementIcons[i].GetAttribute("class");
                Assert.IsTrue(iconClass.Contains("rct-icon-half-check"), "The half-checked icon should be displayed in the ancestors' checkbox.");
            }

            string classOfUncheckedIconOfReactCheckbox = ancestorElementsAndUncheckedElementIcons.Last().GetAttribute("class");
            Assert.IsTrue(classOfUncheckedIconOfReactCheckbox.Contains("rct-icon-uncheck"), "The unchecked icon should be displayed in the react checkbox.");

            IWebElement resultField = driver.FindElement(By.Id("result"));
            Assert.That(resultField.Text, Does.Not.Contain("react"), "The react option should not be selected.");

        }

        [Test, Category("RadioButtonTests")]
        public void RadioButton_SelectOptionYes()
        {
            OpenMenuElement("item-2");

            // The input for option Yes is under the label, so the label should be clicked. 
            IWebElement labelYes = driver.FindElement(By.XPath("//label[@for='yesRadio']"));
            labelYes.Click();

            IWebElement successParagraph = driver.FindElement(By.ClassName("mt-3"));

            Assert.That(successParagraph.Text, Is.EqualTo("You have selected Yes"), "Yes radio button should be selected.");
        }

        [Test, Category("RadioButtonTests")]
        public void RadioButton_SelectOptionImpressive()
        {
            OpenMenuElement("item-2");

            // The input for option Impressive is under the label, so the label should be clicked.
            IWebElement labelImpressive = driver.FindElement(By.XPath("//label[@for='impressiveRadio']"));
            labelImpressive.Click();

            IWebElement successParagraph = driver.FindElement(By.ClassName("mt-3"));

            Assert.That(successParagraph.Text, Is.EqualTo("You have selected Impressive"), "Impressive radio button should be selected.");
        }

        [Test, Category("RadioButtonTests")]
        public void RadioButton_SelectOneOptionThenChangeSelectedOption()
        {
            OpenMenuElement("item-2");

            // The inputs for all options are under the respective labels, so the labels should be clicked.
            // The input elements have the property selected that we need to check.
            IWebElement optionYes = driver.FindElement(By.XPath("//input[@id='yesRadio']"));
            IWebElement labelYes = driver.FindElement(By.XPath("//label[@for='yesRadio']"));
            IWebElement optionImpressive = driver.FindElement(By.XPath("//input[@id='impressiveRadio']"));
            IWebElement labelImpressive = driver.FindElement(By.XPath("//label[@for='impressiveRadio']"));

            labelYes.Click();
            Assert.IsTrue(optionYes.Selected, "Option Yes should be selected.");
            Assert.IsFalse(optionImpressive.Selected, "Option Impressive should not be selected.");

            IWebElement successParagraph = driver.FindElement(By.ClassName("mt-3"));
            Assert.That(successParagraph.Text, Is.EqualTo("You have selected Yes"), "The success paragraph text is not correct.");

            labelImpressive.Click();
            Assert.IsFalse(optionYes.Selected, "Option Yes should not be selected.");
            Assert.IsTrue(optionImpressive.Selected, "Option Impressive should be selected.");

            successParagraph = driver.FindElement(By.ClassName("mt-3"));
            Assert.That(successParagraph.Text, Is.EqualTo("You have selected Impressive"), "The success paragraph text is not correct.");
        }

        [Test, Category("RadioButtonTests")]
        public void RadioButton_OptionNoShouldBeDisabled()
        {
            OpenMenuElement("item-2");

            IWebElement optionNo = driver.FindElement(By.XPath("//input[@id='noRadio']"));
            Assert.IsFalse(optionNo.Enabled, "Option No should be disabled.");
            
        }

        [Test, Category("WebTableTests")]
        public void WebTable_VerifyTableHeader()
        {
            OpenMenuElement("item-3");

            var headerRow = driver.FindElements(By.XPath("//div[@role='row']"))[0];
            var gridCells = headerRow.FindElements(By.CssSelector(".rt-resizable-header-content"));

            Assert.That(gridCells[0].Text, Is.EqualTo("First Name"), "First name column header does not match.");
            Assert.That(gridCells[1].Text, Is.EqualTo("Last Name"), "Last name column header does not match.");
            Assert.That(gridCells[2].Text, Is.EqualTo("Age"), "Age column header does not match.");
            Assert.That(gridCells[3].Text, Is.EqualTo("Email"), "Email column header does not match.");
            Assert.That(gridCells[4].Text, Is.EqualTo("Salary"), "Salary column header does not match.");
            Assert.That(gridCells[5].Text, Is.EqualTo("Department"), "Department column heared does not match.");
            Assert.That(gridCells[6].Text, Is.EqualTo("Action"), "Department column header does not match.");
        }

        [Test, Category("WebTableTests")]
        public void WebTable_CreateNewRecord()
        {
            OpenMenuElement("item-3");

            driver.FindElement(By.Id("addNewRecordButton")).Click();

            driver.FindElement(By.Id("firstName")).SendKeys("Bistra");
            driver.FindElement(By.Id("lastName")).SendKeys("Koeva");
            driver.FindElement(By.Id("userEmail")).SendKeys("bistra@test.bg");
            driver.FindElement(By.Id("age")).SendKeys("40");
            driver.FindElement(By.Id("salary")).SendKeys("1000");
            driver.FindElement(By.Id("department")).SendKeys("Legal");
            driver.FindElement(By.Id("submit")).Click();

            var NewRecordRow = driver.FindElements(By.XPath("//div[@role='row']"))[4];
            var gridCells = NewRecordRow.FindElements(By.XPath(".//div[@role='gridcell']"));

            Assert.That(gridCells[0].Text, Is.EqualTo("Bistra"), "First name does not match.");
            Assert.That(gridCells[1].Text, Is.EqualTo("Koeva"), "Last name does not match.");
            Assert.That(gridCells[2].Text, Is.EqualTo("40"), "Age does not match.");
            Assert.That(gridCells[3].Text, Is.EqualTo("bistra@test.bg"), "Email does not match.");  
            Assert.That(gridCells[4].Text, Is.EqualTo("1000"), "Salary does not match.");
            Assert.That(gridCells[5].Text, Is.EqualTo("Legal"), "Department does not match.");
        }

        [Test, Category("WebTableTests")]
        public void WebTable_EditExistingRecord()
        {
            OpenMenuElement("item-3");
            ScrollToFractionOfPage(4);
            driver.FindElement(By.Id("edit-record-3")).Click();

            driver.FindElement(By.Id("firstName")).Clear();
            driver.FindElement(By.Id("firstName")).SendKeys("Bistra");

            driver.FindElement(By.Id("lastName")).Clear();
            driver.FindElement(By.Id("lastName")).SendKeys("Koeva");

            driver.FindElement(By.Id("userEmail")).Clear();
            driver.FindElement(By.Id("userEmail")).SendKeys("bistra@test.bg");

            driver.FindElement(By.Id("age")).Clear();
            driver.FindElement(By.Id("age")).SendKeys("40");

            driver.FindElement(By.Id("salary")).Clear();
            driver.FindElement(By.Id("salary")).SendKeys("1000");

            driver.FindElement(By.Id("department")).Clear();
            driver.FindElement(By.Id("department")).SendKeys("QA Automation");

            driver.FindElement(By.Id("submit")).Click();

            var NewRecordRow = driver.FindElements(By.XPath("//div[@role='row']"))[3];
            var gridCells = NewRecordRow.FindElements(By.XPath(".//div[@role='gridcell']"));

            Assert.That(gridCells[0].Text, Is.EqualTo("Bistra"), "First name does not match.");
            Assert.That(gridCells[1].Text, Is.EqualTo("Koeva"), "Last name does not match.");
            Assert.That(gridCells[2].Text, Is.EqualTo("40"), "Age does not match.");
            Assert.That(gridCells[3].Text, Is.EqualTo("bistra@test.bg"), "Email does not match.");
            Assert.That(gridCells[4].Text, Is.EqualTo("1000"), "Salary does not match.");
            Assert.That(gridCells[5].Text, Is.EqualTo("QA Automation"), "Department does not match.");
        }

        [Test, Category("WebTableTests")]
        public void WebTable_DeleteAllRecords()
        {
            OpenMenuElement("item-3");
            driver.FindElement(By.Id("delete-record-1")).Click();
            driver.FindElement(By.Id("delete-record-2")).Click();
            driver.FindElement(By.Id("delete-record-3")).Click();

            var AllTableRows = driver.FindElements(By.XPath("//div[@role='row']"));

            for (int i = 0; i < AllTableRows.Count; i++)
            {
                var currentRow = AllTableRows[i];
                var gridCells = currentRow.FindElements(By.XPath(".//div[@role='gridcell']"));

                for (int j = 0; j < gridCells.Count; j++)
                {
                    Assert.That(gridCells[j].Text, Is.EqualTo(" "), "The cell is not empty.");
                }
            }
        }


        [Test, Category("WebTableTests")]
        public void WebTable_SortByFirstName()
        {
            OpenMenuElement("item-3");
            driver.FindElement(By.XPath("//div[text()='First Name']")).Click();

            var AllTableRows = driver.FindElements(By.XPath("//div[@role='row']"));

            var FirstName1 = AllTableRows[1].FindElements(By.XPath(".//div[@role='gridcell']"))[0];
            Assert.That(FirstName1.Text, Is.EqualTo("Alden"), "The records in the table should be sorted by first name.");
            var FirstName2 = AllTableRows[2].FindElements(By.XPath(".//div[@role='gridcell']"))[0];
            Assert.That(FirstName2.Text, Is.EqualTo("Cierra"), "The records in the table should be sorted by first name.");
            var FirstName3 = AllTableRows[3].FindElements(By.XPath(".//div[@role='gridcell']"))[0];
            Assert.That(FirstName3.Text, Is.EqualTo("Kierra"), "The records in the table should be sorted by first name.");
        }

        [Test, Category("WebTableTests")]
        public void WebTable_Pagination()
        {
            OpenMenuElement("item-3");

            IWebElement itemsPerPageDropdown = driver.FindElement(By.XPath("//select[@aria-label='rows per page']"));
            SelectElement select = new SelectElement(itemsPerPageDropdown);
            select.SelectByText("5 rows");

            for (int i = 0; i < 3; i++)
            {
                driver.FindElement(By.Id("addNewRecordButton")).Click();

                driver.FindElement(By.Id("firstName")).SendKeys($"Bistra{i}");
                driver.FindElement(By.Id("lastName")).SendKeys($"Koeva{i}");
                driver.FindElement(By.Id("userEmail")).SendKeys($"bistra{i}@test.bg");
                driver.FindElement(By.Id("age")).SendKeys($"4{i}");
                driver.FindElement(By.Id("salary")).SendKeys($"100{i}");
                driver.FindElement(By.Id("department")).SendKeys($"Legal{i}");
                driver.FindElement(By.Id("submit")).Click();
            }

            IWebElement totalPagesSpan = driver.FindElement(By.ClassName("-totalPages"));
            Assert.That(totalPagesSpan.Text, Is.EqualTo("2"), "There should be two pages.");

            var AllTableRows = driver.FindElements(By.XPath("//div[@role='row']"));
            // The table header is a separate row with the same identifier. 
            Assert.That(AllTableRows.Count, Is.EqualTo(6), "There should be 5 records per page.");

            IWebElement nextButton = driver.FindElement(By.XPath("//button[text()='Next']"));
            IWebElement previousButton = driver.FindElement(By.XPath("//button[text()='Previous']"));

            Assert.That(previousButton.Enabled, Is.False, "The previous button should be disabled.");
            Assert.That(nextButton.Enabled, Is.True, "The next button should be enabled.");

            IWebElement jumpToPageInput = driver.FindElement(By.XPath("//input[@value='1']"));
            Assert.IsNotNull(jumpToPageInput, "The jumpToPageInput should show that page 1 is displayed.");

            nextButton.Click();

            Assert.That(previousButton.Enabled, Is.True, "The previous button should be enabled.");
            Assert.That(nextButton.Enabled, Is.False, "The next button should be disabled.");
            jumpToPageInput = driver.FindElement(By.XPath("//input[@value='2']"));
            Assert.IsNotNull(jumpToPageInput, "The jumpToPageInput should show that page 2 is displayed.");

            AllTableRows = driver.FindElements(By.XPath("//div[@role='row']"));
            var LastRecordRow = AllTableRows[1];
            var gridCells = LastRecordRow.FindElements(By.XPath(".//div[@role='gridcell']"));

            Assert.That(gridCells[0].Text, Is.EqualTo("Bistra2"), "First name does not match.");
            Assert.That(gridCells[1].Text, Is.EqualTo("Koeva2"), "Last name does not match.");
            Assert.That(gridCells[2].Text, Is.EqualTo("42"), "Age does not match.");
            Assert.That(gridCells[3].Text, Is.EqualTo("bistra2@test.bg"), "Email does not match.");
            Assert.That(gridCells[4].Text, Is.EqualTo("1002"), "Salary does not match.");
            Assert.That(gridCells[5].Text, Is.EqualTo("Legal2"), "Department does not match.");
        }

        [Test, Category("ButtonsTests")]
        public void Buttons_PerformDoubleClick()
        {
            OpenMenuElement("item-4");

            IWebElement doubleClickButton = driver.FindElement(By.Id("doubleClickBtn"));
            actions.ScrollToElement(doubleClickButton).Perform();
            actions.DoubleClick(doubleClickButton).Perform();

            IWebElement resultParagraph = driver.FindElement(By.Id("doubleClickMessage"));
            
            Assert.That(resultParagraph.Text, Is.EqualTo("You have done a double click"), "The double click message is not there.");
        }

        [Test, Category("ButtonsTests")]
        public void Buttons_PerformRightClick()
        {
            OpenMenuElement("item-4");

            IWebElement rightClickButton = driver.FindElement(By.Id("rightClickBtn"));
            actions.ScrollToElement(rightClickButton).Perform();
            actions.ContextClick(rightClickButton).Perform();

            IWebElement resultParagraph = driver.FindElement(By.Id("rightClickMessage"));

            Assert.That(resultParagraph.Text, Is.EqualTo("You have done a right click"), "The right click message is not there.");
        }

        [Test, Category("ButtonsTests")]
        public void Buttons_PerformDynamicClick()
        {
            OpenMenuElement("item-4");

            ScrollToFractionOfPage(3);

            // The id of the button is dynamic e.g. differs every time.
            driver.FindElement(By.XPath("//button[text()='Click Me']")).Click();

            IWebElement resultParagraph = driver.FindElement(By.Id("dynamicClickMessage"));

            Assert.That(resultParagraph.Text, Is.EqualTo("You have done a dynamic click"), "The dynamic click message is not there.");
        }

        [Test, Category("LinksTests")]
        public void SimpleLinkThatOpensNewTab()
        {
            OpenMenuElement("item-5");

            driver.FindElement(By.Id("simpleLink")).Click();

            IList<string> windowHandles = new List<string>(driver.WindowHandles);
            Assert.That(windowHandles.Count, Is.EqualTo(2), "A new tab should have opened.");
            driver.SwitchTo().Window(windowHandles[1]);

            String expectedUrl = "https://demoqa.com/";
            Assert.That(driver.Url, Is.EqualTo(expectedUrl), "The new tab URL should be as expected.");
        }

        [Test, Category("LinksTests")]
        public void DynamicLinkThatOpensNewTab()
        {
            OpenMenuElement("item-5");

            driver.FindElement(By.Id("dynamicLink")).Click();

            IList<string> windowHandles = new List<string>(driver.WindowHandles);
            Assert.That(windowHandles.Count, Is.EqualTo(2), "A new tab should have opened.");
            driver.SwitchTo().Window(windowHandles[1]);

            String expectedUrl = "https://demoqa.com/";
            Assert.That(driver.Url, Is.EqualTo(expectedUrl), "The new tab URL should be as expected.");
        }

        [Test, Category("BrokenLinksAndImagesTests")]
        public void ValidImage()
        {
            OpenMenuElement("item-6");

            // There are two identical img elements on the page. We want to check the second one.
            IWebElement validImg = driver.FindElements(By.XPath("//img[@src='/images/Toolsqa.jpg']"))[1];

            Assert.That(validImg.GetAttribute("naturalWidth"), Is.Not.EqualTo("0"), "The valid image width should be greater than 0.");
        }

        [Test, Category("BrokenLinksAndImagesTests")]
        public void BrokenImage()
        {
            OpenMenuElement("item-6");

            IWebElement brokenImg = driver.FindElement(By.XPath("//img[contains(@src,'Toolsqa_1')]"));
 
            Assert.That(brokenImg.GetAttribute("naturalWidth"), Is.EqualTo("0"), "This should be a sample of a broken image.");
        }

        [Test, Category("BrokenLinksAndImagesTests")]
        public void ValidLink()
        {
            OpenMenuElement("item-6");

            ScrollToFractionOfPage(3);
            IWebElement validLink = driver.FindElement(By.LinkText("Click Here for Valid Link"));
            validLink.Click();

            String expectedUrl = "https://demoqa.com/";
            Assert.That(driver.Url, Is.EqualTo(expectedUrl), "The URL should be as expected.");
        }

        [Test, Category("BrokenLinksAndImagesTests")]
        public async Task BrokenLink()
        {
            OpenMenuElement("item-6");

            HttpClient client = new();

            IWebElement brokenLink = driver.FindElement(By.LinkText("Click Here for Broken Link"));
            string linkURL = brokenLink.GetAttribute("href");
            HttpResponseMessage response = await client.GetAsync(linkURL);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError), "The response status code should be 500 Internal Server Error.");
        }

        [Test, Category("UploadAndDownloadTests")]
        public void DownloadFile()
        {
            OpenMenuElement("item-7");

            IWebElement downloadButton = driver.FindElement(By.Id("downloadButton"));
            actions.ScrollToElement(downloadButton);
            downloadButton.Click();

            Thread.Sleep(500); 
            
            string downloadedFilePath = Path.Combine(downloadDirectory, "sampleFile.jpeg");
            Assert.IsTrue(File.Exists(downloadedFilePath), "The file should be downloaded successfully.");

            if(File.Exists(downloadedFilePath)) 
            { 
                File.Delete(downloadedFilePath); 
            }
        }

        [Test, Category("UploadAndDownloadTests")]
        public void UploadFile()
        {
            OpenMenuElement("item-7");

            IWebElement uploadInputElement = driver.FindElement(By.Id("uploadFile"));
            actions.ScrollToElement(uploadInputElement);
            string uploadFilePath = Path.Combine(downloadDirectory, "Gaspacho.docx");
            uploadInputElement.SendKeys(uploadFilePath);

            //uploadInputElement = driver.FindElement(By.Id("uploadFile"));
            //Assert.That(uploadInputElement.Text, Is.EqualTo(""));

            IWebElement uploadedFilePathPar = driver.FindElement(By.Id("uploadedFilePath"));
            Assert.That(uploadedFilePathPar.Text, Is.EqualTo(@"C:\fakepath\Gaspacho.docx"), "The uploaded file path should be as expected.");
        }

        [Test, Category ("DynamicPropertiesTests")]
        public void DynamicButtons()
        {
            OpenMenuElement("item-8");

            wait.Timeout = TimeSpan.FromSeconds(6);

            IWebElement ButtonEnabledWithDelay = wait.Until(ExpectedConditions.ElementToBeClickable(By.Id("enableAfter")));
            Assert.IsTrue(ButtonEnabledWithDelay.Enabled, "The button should be enabled after 5 seconds.");

            IWebElement ButtonDisplayedWithDelay = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("visibleAfter")));
            Assert.IsTrue(ButtonDisplayedWithDelay.Displayed, "The button should be visible after 5 seconds.");

            wait.Until(driver =>
            {
                IWebElement colorChangeElement = driver.FindElement(By.Id("colorChange"));
                string textColor = colorChangeElement.GetCssValue("color");
                return textColor == "rgba(220, 53, 69, 1)";
            });

            var colorChangeElement = driver.FindElement(By.Id("colorChange"));
            Assert.That(colorChangeElement.GetCssValue("color"), Is.EqualTo("rgba(220, 53, 69, 1)"), "The color should be red.");

        }
    }
}