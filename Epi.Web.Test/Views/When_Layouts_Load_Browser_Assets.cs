using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Epi.Web.MVC.Test.Views
{
    /// <summary>
    /// Static characterization of the browser assets each layout requests, in source order. These tests read
    /// the .cshtml/.js/.css files; they do not render Razor or run a browser, so they cannot show whether a
    /// script throws at load. Update the snapshots deliberately when a layout's asset stack changes.
    /// </summary>
    [TestFixture]
    public class When_Layouts_Load_Browser_Assets
    {
        private const string DesktopLayout = "~/Views/Shared/_Layout.cshtml";
        private const string MobileLayout = "~/Views/Shared/_Layout.Mobile.cshtml";
        private const string SurveyManagerLayout = "~/Views/Shared/_LayoutPage2.cshtml";
        private const string ReportLayout = "~/Views/Shared/_LayoutPage3.cshtml";

        private static readonly string[] Layouts = { DesktopLayout, MobileLayout, SurveyManagerLayout, ReportLayout };

        // Both CUSTOM_THEME branches appear because the server renders exactly one of them per request.
        private static readonly Dictionary<string, string[]> ExpectedLoadOrder = new Dictionary<string, string[]>
        {
            {
                DesktopLayout, new[]
                {
                    "script ~/Scripts/jquery-1.7.1.js",
                    "script ~/Scripts/jquery.validationEngine.js",
                    "script ~/Scripts/jquery.validationEngine-en.js",
                    "script ~/Scripts/jquery.numeric.js",
                    "script ~/Scripts/jquery.maskedinput-1.3.js",
                    "script ~/Scripts/jquery-ui-1.8.16.custom.min.js",
                    "script ~/Scripts/jquery.ui.mouse.js",
                    "script ~/Scripts/jquery.ui.dialog.js",
                    "script ~/Scripts/jquery.ui.position.js",
                    "script ~/Scripts/jquery.bgiframe-2.1.2.js",
                    "script ~/Scripts/jquery.ui.core.js",
                    "script ~/Scripts/jquery.ui.widget.js",
                    "script ~/Scripts/jquery.Epi.Survey.Components.js",
                    "script ~/Scripts/CheckCodeEngineV1.js",
                    "script ~/Scripts/jquery.PreventEnter.js",
                    "script ~/Scripts/idle-timer.js",
                    "script ~/Scripts/jquery-ui-sliderAccess.js",
                    "script ~/Scripts/jquery-ui-timepicker-addon.js",
                    "script ~/Scripts/jquery.idletimeout.js",
                    "script ~/Scripts/jquery.idletimer.js",
                    "script ~/Scripts/Print.js",
                    "style  ~/Content/css/awesomplete.css",
                    "script ~/Scripts/awesomplete.js",
                    "style  ~/Content/css/page.css",
                    "style  ~/Content/css/validationEngine.jquery.css",
                    "style  ~/Content/css/ui-lightness/jquery-ui-1.8.16.custom.css",
                    "style  ~/Content/css/jquery.ui.all.css",
                    "script https://www.google.com/recaptcha/api.js",
                    "style  ~/Content/css/epistyle.css",
                    "style  ~/Content/css/epistyle_controls.css",
                    "style  ~/Content/css/epistyle-blue.css",
                    "style  ~/Content/css/epistyle-blue_controls.css",
                    "script ~/Scripts/jquery.tools.min.js",
                }
            },
            {
                MobileLayout, new[]
                {
                    "style  ~/Content/jquery.mobile-1.1.1.css",
                    "style  ~/Content/jquery.mobile.structure-1.1.1.css",
                    "style  ~/Content/Site.Mobile.css",
                    "style  ~/Content/css/MobilePage.css",
                    "style  ~/Content/css/validationEngine.jquery.Mobile.css",
                    "style  ~/Content/css/TabletEpiStyle.css",
                    "style  ~/Content/css/awesomplete.css",
                    "script ~/Scripts/mobileautocomplete.js",
                    "script ~/Scripts/jquery-1.6.3.min.js",
                    "script ~/Scripts/jquery-1.7.1.js",
                    "script ~/Scripts/jquery.validationEngine.js",
                    "script ~/Scripts/jquery.validationEngine-en.js",
                    "script ~/Scripts/jquery.numeric.js",
                    "script ~/Scripts/jquery.maskedinput-1.3.js",
                    "script ~/Scripts/jquery.Epi.Survey.Components.js",
                    "script ~/Scripts/CheckCodeEngineV1.js",
                    "script ~/Scripts/jquery.PreventEnter.js",
                    "script ~/Scripts/jquery-ui-sliderAccess.js",
                    "script ~/Scripts/jquery.tools.min.js",
                    "script ~/Scripts/jquery.mobile-1.1.1.js",
                    "script ~/Scripts/jquery.mobile.datebox-1.0.1.js",
                    "style  ~/Content/css/jquery.mobile.datebox-1.0.1.css",
                    "style  ~/Content/css/jquery.mobile.simpledialog.min.css",
                    "script ~/Scripts/jquery.mobile.simpledialog.min.js",
                    "script ~/Scripts/jquery.mobile.simpledialog2.min.js",
                    "style  ~/Content/css/MobileEpiStyle.css",
                    "style  ~/Content/css/mobile-epistyle-blue.css",
                }
            },
            {
                SurveyManagerLayout, new[]
                {
                    "script ~/Scripts/jquery-1.7.1.js",
                    "script ~/Scripts/jquery.validationEngine.js",
                    "script ~/Scripts/jquery.validationEngine-en.js",
                    "script ~/Scripts/jquery.numeric.js",
                    "script ~/Scripts/jquery.maskedinput-1.3.js",
                    "script ~/Scripts/jquery-ui-1.8.16.custom.min.js",
                    "script ~/Scripts/jquery.ui.mouse.js",
                    "script ~/Scripts/jquery.ui.dialog.js",
                    "script ~/Scripts/jquery.ui.position.js",
                    "script ~/Scripts/jquery.bgiframe-2.1.2.js",
                    "script ~/Scripts/jquery.ui.core.js",
                    "script ~/Scripts/jquery.ui.widget.js",
                    "script ~/Scripts/jquery.Epi.Survey.Components.js",
                    "script ~/Scripts/CheckCodeEngine.js",
                    "script ~/Scripts/jquery.PreventEnter.js",
                    "script ~/Scripts/idle-timer.js",
                    "script ~/Scripts/jquery-ui-sliderAccess.js",
                    "script ~/Scripts/jquery-ui-timepicker-addon.js",
                    "script ~/Scripts/jquery.idletimeout.js",
                    "script ~/Scripts/jquery.idletimer.js",
                    "script ~/Scripts/Print.js",
                    "script ~/Scripts/awesomplete.js",
                    "style  ~/Content/css/validationEngine.jquery.css",
                    "style  ~/Content/css/ui-lightness/jquery-ui-1.8.16.custom.css",
                    "style  ~/Content/css/jquery.ui.all.css",
                    "style  https://fonts.googleapis.com/css?family=Material+Icons|Material+Icons+Outlined|Material+Icons+Two+Tone|Material+Icons+Round|Material+Icons+Sharp",
                    "style  ~/Content/css/epiwebsurvey_admin.css",
                    "style  ~/Content/css/bootstrap.min.css",
                    "script ~/Scripts/jquery.tools.min.js",
                    "script ~/Scripts/bootstrapV4.min.js",
                }
            },
            {
                // The local jQuery/jQuery UI stack in this layout is entirely inside a Razor comment.
                ReportLayout, new[]
                {
                    "script https://code.jquery.com/jquery-1.12.4.js",
                    "script https://code.jquery.com/ui/1.12.1/jquery-ui.js",
                    "style  ~/Content/css/bootstrap.min.css",
                    "script ~/Scripts/jquery.tools.min.js",
                    "script ~/Scripts/bootstrapV4.min.js",
                }
            },
        };

        // Header comment of a jQuery core build, e.g. "/*! jQuery v1.6.3" or "/*!\n * jQuery JavaScript Library v1.7.1".
        private static readonly Regex JQueryCoreHeader = new Regex(@"^\s*/\*!?[\s*]*jQuery (?:JavaScript Library )?v(?<version>\d+\.\d+\.\d+)");

        private static IList<AssetReference> AssetsOf(string layout)
        {
            return ViewAssetReferences.ScriptsAndStylesheets(WebProject.Read(layout));
        }

        [TestCaseSource(nameof(Layouts))]
        public void Then_Scripts_And_Stylesheets_Load_In_The_Characterized_Order(string layout)
        {
            Assert.That(AssetsOf(layout).Select(asset => asset.ToString()), Is.EqualTo(ExpectedLoadOrder[layout]));
        }

        [TestCaseSource(nameof(Layouts))]
        public void Then_Every_Local_Asset_And_Css_Import_Exists_On_Disk(string layout)
        {
            var local = AssetsOf(layout).Where(asset => asset.IsAppRelative).ToList();
            var paths = local.Select(asset => asset.Url)
                .Concat(local.Where(asset => asset.Kind == AssetKind.Stylesheet).SelectMany(asset => ViewAssetReferences.CssImports(asset.Url)))
                .ToList();

            Assert.That(paths, Is.Not.Empty);
            Assert.That(paths.Where(path => !WebProject.FileExists(path)), Is.Empty);
        }

        [TestCaseSource(nameof(Layouts))]
        public void Then_Every_Local_Asset_Is_Published_As_Project_Content(string layout)
        {
            ISet<string> content = WebProject.CsprojContentItems();

            var unpublished = AssetsOf(layout).Where(asset => asset.IsAppRelative && !content.Contains(asset.Url)).Select(asset => asset.Url);

            Assert.That(unpublished, Is.Empty);
        }

        [Test]
        public void Then_External_Assets_Are_Fetched_Without_Subresource_Integrity()
        {
            var external = Layouts.SelectMany(layout => AssetsOf(layout).Where(asset => !asset.IsAppRelative)).ToList();

            Assert.That(external.Select(asset => asset.Url), Is.EquivalentTo(new[]
            {
                "https://www.google.com/recaptcha/api.js",
                "https://fonts.googleapis.com/css?family=Material+Icons|Material+Icons+Outlined|Material+Icons+Two+Tone|Material+Icons+Round|Material+Icons+Sharp",
                "https://code.jquery.com/jquery-1.12.4.js",
                "https://code.jquery.com/ui/1.12.1/jquery-ui.js",
            }));
            Assert.That(external.Where(asset => asset.HasIntegrity), Is.Empty, "No layout currently pins an external asset with an integrity hash.");
        }

        [TestCase(DesktopLayout, new[] { "1.7.1" })]
        [TestCase(MobileLayout, new[] { "1.6.3", "1.7.1" })]
        [TestCase(SurveyManagerLayout, new[] { "1.7.1" })]
        [TestCase(ReportLayout, new string[0])]
        public void Then_Local_jQuery_Core_Versions_Match_The_Loaded_File_Headers(string layout, string[] expectedVersions)
        {
            var versions = AssetsOf(layout)
                .Where(asset => asset.IsAppRelative && asset.Kind == AssetKind.Script)
                .Select(asset => JQueryCoreHeader.Match(WebProject.Read(asset.Url)))
                .Where(match => match.Success)
                .Select(match => match.Groups["version"].Value);

            // The mobile layout loads two jQuery cores; the later 1.7.1 replaces the 1.6.3 global.
            Assert.That(versions, Is.EqualTo(expectedVersions));
        }

        [Test]
        public void Then_Survey_Manager_Loads_Bootstrap_4_Whose_jQuery_Guard_Rejects_The_Loaded_jQuery_1_7_1()
        {
            var scripts = AssetsOf(SurveyManagerLayout).Where(asset => asset.Kind == AssetKind.Script).Select(asset => asset.Url).ToList();
            string bootstrap = WebProject.Read("~/Scripts/bootstrapV4.min.js");

            Assert.That(scripts.IndexOf("~/Scripts/jquery-1.7.1.js"), Is.LessThan(scripts.IndexOf("~/Scripts/bootstrapV4.min.js")));
            Assert.That(bootstrap, Does.Contain("Bootstrap v4.2.1"));
            // Static evidence only: in a browser this guard throws, so Bootstrap's JavaScript plugins never register.
            Assert.That(bootstrap, Does.Contain("Bootstrap's JavaScript requires at least jQuery v1.9.1"));
        }

        [Test]
        public void Then_The_Only_Missing_App_Relative_Files_In_Views_Are_The_Known_Defects()
        {
            var missing = WebProject.AllViews()
                .SelectMany(view => ViewAssetReferences.AppRelativeFiles(WebProject.Read(view))
                    .Where(reference => !WebProject.FileExists(reference.Key))
                    .Select(reference => view + ":" + reference.Value + " " + reference.Key))
                .ToList();

            // Known defects, not fixed here. Both images render only when TECHNICAL_SUPPORT_IS_ENABLED is TRUE and are absent
            // from Git history; the mobile notice image path drops a '/' (the file is ~/Content/css/images/exclamationcircle.png)
            // and renders only when ModerateEnv is TRUE.
            Assert.That(missing, Is.EquivalentTo(new[]
            {
                "~/Views/Home/Index.Mobile.cshtml:222 ~/Contentcss//images/exclamationcircle.png",
                "~/Views/Shared/FooterPartial.cshtml:18 ~/Content/images/email1.png",
                "~/Views/Shared/FooterPartial.cshtml:21 ~/Content/images/phone.png",
            }));
        }
    }
}
