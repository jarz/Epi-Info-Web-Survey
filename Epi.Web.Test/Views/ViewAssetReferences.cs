using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Epi.Web.MVC.Test.Views
{
    /// <summary>
    /// Reads the Epi.Web source tree (views, scripts, stylesheets, csproj) rather than the build output,
    /// so these static checks work wherever the test assembly is built.
    /// </summary>
    internal static class WebProject
    {
        public static readonly string Root = Locate();

        private static string Locate([CallerFilePath] string sourceFile = "")
        {
            string root = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourceFile), "..", "..", "Epi.Web"));
            if (!Directory.Exists(Path.Combine(root, "Views")))
            {
                throw new DirectoryNotFoundException("Epi.Web source tree not found relative to the compiled test source: " + root);
            }

            return root;
        }

        public static string MapPath(string appRelativePath)
        {
            if (!appRelativePath.StartsWith("~/", StringComparison.Ordinal))
            {
                throw new ArgumentException("Expected an app-relative path: " + appRelativePath);
            }

            return Path.Combine(Root, appRelativePath.Substring(2).Replace('/', Path.DirectorySeparatorChar));
        }

        public static bool FileExists(string appRelativePath)
        {
            return File.Exists(MapPath(appRelativePath));
        }

        public static string Read(string appRelativePath)
        {
            return File.ReadAllText(MapPath(appRelativePath));
        }

        public static IEnumerable<string> AllViews()
        {
            return Directory.GetFiles(Path.Combine(Root, "Views"), "*.cshtml", SearchOption.AllDirectories)
                .Select(path => "~/" + path.Substring(Root.Length + 1).Replace(Path.DirectorySeparatorChar, '/'))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);
        }

        public static ISet<string> CsprojContentItems()
        {
            string csproj = File.ReadAllText(Path.Combine(Root, "Epi.Web.csproj"));
            return new HashSet<string>(
                Regex.Matches(csproj, "<Content Include=\"(?<path>[^\"]+)\"").Cast<Match>()
                    .Select(match => "~/" + match.Groups["path"].Value.Replace('\\', '/')),
                StringComparer.OrdinalIgnoreCase);
        }
    }

    internal enum AssetKind
    {
        Script,
        Stylesheet
    }

    internal sealed class AssetReference
    {
        public AssetReference(AssetKind kind, string url, int line, bool hasIntegrity)
        {
            Kind = kind;
            Url = url;
            Line = line;
            HasIntegrity = hasIntegrity;
        }

        public AssetKind Kind { get; private set; }
        public string Url { get; private set; }
        public int Line { get; private set; }
        public bool HasIntegrity { get; private set; }
        public bool IsAppRelative { get { return Url.StartsWith("~/", StringComparison.Ordinal); } }

        public override string ToString()
        {
            return (Kind == AssetKind.Script ? "script " : "style  ") + Url;
        }
    }

    /// <summary>
    /// Static extraction of what a Razor view asks the browser to load. Razor and HTML comments are removed
    /// (the browser never fetches them); every remaining tag is listed in source order, including both branches
    /// of server-side conditionals such as the CUSTOM_THEME switch. This does not execute Razor.
    /// </summary>
    internal static class ViewAssetReferences
    {
        private static readonly Regex RazorComment = new Regex(@"@\*.*?\*@", RegexOptions.Singleline);
        private static readonly Regex HtmlComment = new Regex(@"<!--.*?-->", RegexOptions.Singleline);
        private static readonly Regex Tag = new Regex(@"<(?<tag>script|link)\b(?<attributes>[^>]*)>", RegexOptions.IgnoreCase);
        private static readonly Regex Source = new Regex(
            @"\b(?:src|href)\s*=\s*""(?:@Url\.Content\(\s*""(?<url>[^""]+)""\s*\)|(?<url>[^""@]+))""", RegexOptions.IgnoreCase);
        private static readonly Regex Stylesheet = new Regex(@"\brel\s*=\s*""stylesheet""", RegexOptions.IgnoreCase);
        private static readonly Regex Integrity = new Regex(@"\bintegrity\s*=", RegexOptions.IgnoreCase);
        private static readonly Regex AppRelativeFile = new Regex(@"Url\.Content\(\s*""(?<url>~/[^""]+\.[A-Za-z0-9]+)""\s*\)");
        private static readonly Regex CssImport = new Regex(@"@import\s+(?:url\()?\s*[""']?(?<url>[^""'\)\s;]+)");

        public static string StripComments(string source)
        {
            MatchEvaluator blank = match => Regex.Replace(match.Value, @"[^\n]", " ");
            return HtmlComment.Replace(RazorComment.Replace(source, blank), blank);
        }

        public static IList<AssetReference> ScriptsAndStylesheets(string viewSource)
        {
            string source = StripComments(viewSource);
            var references = new List<AssetReference>();
            foreach (Match tag in Tag.Matches(source))
            {
                string attributes = tag.Groups["attributes"].Value;
                Match url = Source.Match(attributes);
                if (!url.Success)
                {
                    continue;
                }

                bool isScript = tag.Groups["tag"].Value.Equals("script", StringComparison.OrdinalIgnoreCase);
                if (!isScript && !Stylesheet.IsMatch(attributes))
                {
                    continue;
                }

                references.Add(new AssetReference(
                    isScript ? AssetKind.Script : AssetKind.Stylesheet,
                    url.Groups["url"].Value,
                    LineOf(source, tag.Index),
                    Integrity.IsMatch(attributes)));
            }

            return references;
        }

        /// <summary>Every literal <c>Url.Content("~/...ext")</c> in uncommented markup (scripts, stylesheets, images).</summary>
        public static IEnumerable<KeyValuePair<string, int>> AppRelativeFiles(string viewSource)
        {
            string source = StripComments(viewSource);
            return AppRelativeFile.Matches(source).Cast<Match>()
                .Select(match => new KeyValuePair<string, int>(match.Groups["url"].Value, LineOf(source, match.Index)));
        }

        /// <summary>Follows <c>@import</c> chains from an app-relative stylesheet; returns each imported path.</summary>
        public static IEnumerable<string> CssImports(string appRelativeStylesheet)
        {
            if (!WebProject.FileExists(appRelativeStylesheet))
            {
                yield break;
            }

            string directory = appRelativeStylesheet.Substring(0, appRelativeStylesheet.LastIndexOf('/') + 1);
            foreach (Match import in CssImport.Matches(WebProject.Read(appRelativeStylesheet)))
            {
                string imported = directory + import.Groups["url"].Value;
                yield return imported;
                foreach (string nested in CssImports(imported))
                {
                    yield return nested;
                }
            }
        }

        private static int LineOf(string source, int index)
        {
            int line = 1;
            for (int i = 0; i < index; i++)
            {
                if (source[i] == '\n')
                {
                    line++;
                }
            }

            return line;
        }
    }
}
