using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ts
{
    public enum BinaryMode
    {
        None,
        Float,
        Double,
        Int16,
        Int32,
        UInt16,
        UInt32,
        Any
    }

    public class SearchParams
    {
        public static int KB = 1024;
        public static int MB = 1024 * 1024;

    // Exceptional case regex, a result of formatted a source regex with the query parameter
        public Regex SpecialRegex { get; set; } = null;

    // Standard settings
        public int MaxBytes { get; set; } = MB * 20; //default is 20mb limit
        public List<string> Queries { get; private set; } = new List<string>();
        public string Query { get; set; }
        public bool CaseSensitive { get; set; } = false;

    // File extension filtering
        public List<string> OnlyExtensions { get; private set; } = new List<string>();
        public List<string> ExcludedExtensions { get; private set; } = new List<string>();

    // /b* binary searching mode setting
        public BinaryMode BinaryMode { get; set; } = BinaryMode.None;
    // Regular expressions
        public bool RegexMode { get; set; } = false;
    // /x /X - xml xPath query
        public bool XmlMode { get; set; } = false;
    // /t /T - `tell mode`
        public bool FileNamesOnly { get; set; } = false;
        public bool NotMode { get; set; } = false;
        public bool CountFileNames { get; set; } = false;
    // `match` mode
        public bool MatchOnly { get; set; } = false;
        public bool UniqueOnly { get; set; } = false;
        public bool ShowMatchFileName { get; set; } = false;
    // Less important general settings
        public bool Recurse { get; set; } = false; // Recurse subdirectories, applies to all searches
        public int LineCount { get; set; } = 5; // Number of lines of text to display in the scrollable viewport, applies to Text/Regex/xPath searches
        public bool Auto { get; set; } = false; // don't wait for user input
        public bool ShowLineNumbers { get; set; } = false; // print line numbers, applies to Text/Regex/xPath/Match searches
        public bool Verbose { get; set; } = false;

    // Odd hackish settings
        public bool WrapInQuote { get; set; } = false; // wrap query text in "" as that's a PITA to do in console parameters
        public string QuickMode { get; set; } = null;  // Special regex string from the exceptions table
        public int HitLimit { get; set; } = -1; // negative 1 for unlimited

    // Value tracking
        public int DataFileCount { get; set; } = 0; // Number of files touched
        public int DataHitCount { get; set; } = 0;  // Number of results matching

        /// <summary>
        /// Check if we've hit our maximum allotted hits.
        /// </summary>
        /// <returns>True if it's been reached</returns>
        public bool CheckHits()
        {
            return HitLimit > 0 && DataHitCount >= HitLimit;
        }

        /// <summary>
        /// Dumps tail information about files and hit counts/
        /// </summary>
        public void WriteResults(SearchDir dirData)
        {
            Console.WriteLine("Found {0} times in {1} files", DataHitCount, dirData.CountHits());
            Console.WriteLine(string.Format("Scanned {0} files", DataFileCount));
        }

        /// <summary>
        /// Half space to draw in extra around the current line.
        /// </summary>
        public int DrawExtra {
            get
            {
                if (LineCount == 1)
                    return 0;
                return (LineCount % 2 == 0 ? LineCount : LineCount) / 2;
            }
        }
    }

    class Program
    {
        #region Special Searches
        static Dictionary<string, string> SpecialSearches = new Dictionary<string, string>
        {
            { "/#", // line starts with #
                "\\s*(\\#).*(\\b\\w*{0}\\b)" },
            { "/instr", // result contained in " "
                "(\\\".*)(\\b\\w*{0}\\b).*(\\\")" },
            { "/infunc", // result contained in ( )
                "(\\(.*)(\\b\\w*{0}\\b).*(\\))" },
            { "/temp", // result contained in < >
                "(<.*)(\\b\\w*{0}\\b).*(>)" },
            { "/var", // result left of . or ->
                "((->)|(\\.))(\\b\\w*{0}\\b)" },
            { "/ptr", // result left of ->
                "(->)(\\b\\w*{0}\\b)" },
            { "/set", // result left of =
                "(\\b\\w*{0}\\b)\\s(=)" },
            { "/unset", // result left of ; and not right of =
                "([^=]\\s*)(\\b\\w*{0}\\b)\\s*(;)" },
            { "/new", // new bob
                "(new)\\s*(\\b{0}\\b)"
            },
            { "/delete", // delete bob
                "(?i)(delete)\\s*(\\b{0}\\b)"}
        };
        #endregion

        /// <summary>
        /// Convoluted main loop
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            // Clean up color mutilation as a result of CTRL+C, assumes standard console colors are used
            Console.CancelKeyPress += (o, e) =>
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.BackgroundColor = ConsoleColor.Black;
            };

            Init setupData = new Init();

            SearchParams searchArgs = new SearchParams();

            if (args == null || args.Length < 2)
            {
                if (args.Length == 1)
                {
                    if (args[0].ToLowerInvariant().Equals("/config") || args[0].ToLowerInvariant().Equals("/cfg"))
                    {
                        Help.PrintCfgHelp();
                        return;
                    }
                    else if (args[0].ToLowerInvariant().Equals("/viewdoc"))
                    {
                        Help.PrintExtraHelp();
                        return;
                    }
                    else if (args[0].ToLowerInvariant().Equals("/fix"))
                    {
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.BackgroundColor = ConsoleColor.Black;
                        return;
                    }
                }
                Help.PrintHelp(setupData);
                return;
            }

        // INPUT PROCESSING
            List<string> searchDirs = new List<string>();
            searchDirs.Add(args[0]); // add the mandatory first search directory

            bool needsQueryString = setupData.NeedsSearchParam(args);
            ProcessParams(searchArgs, setupData, searchDirs, args, 1, needsQueryString ? args.Length - 1 : args.Length);
            // Check for and apply custom switch switches
            setupData.ApplyExtraSwitches(searchArgs, searchDirs, args, needsQueryString ? args.Length - 1 : args.Length);
            // Handle default switches, these are dangerous
            ProcessParams(searchArgs, setupData, searchDirs, setupData.DefaultSwitches.ToArray(), 0, setupData.DefaultSwitches.Count);

            searchArgs.LineCount = Math.Max(1, searchArgs.LineCount);
            if (needsQueryString)
            {
                searchArgs.Query = args[args.Length - 1];
                searchArgs.Query = searchArgs.Query.Replace('`', '"');
            }

            // !!! WARNING !!!
            // lower-case text for case-insensitive --- DON'T DO IT WHEN REGEX THOUGH!!!
            if (!searchArgs.CaseSensitive && !searchArgs.RegexMode && needsQueryString)
                searchArgs.Query = searchArgs.Query.ToLowerInvariant();
            else if (!needsQueryString)
                searchArgs.Query = searchArgs.QuickMode;

            if (searchArgs.QuickMode != null)
            {
                // append case insensitivity as needed
                if (!searchArgs.CaseSensitive)
                    searchArgs.QuickMode = "(?i)" + searchArgs.QuickMode;

                if (searchArgs.QuickMode.Contains("{0}"))
                    searchArgs.SpecialRegex = new Regex(string.Format(searchArgs.QuickMode, searchArgs.Query));
                else
                    searchArgs.SpecialRegex = new Regex(searchArgs.QuickMode);
            }

            // wrap text in quotes if desired
            if (searchArgs.WrapInQuote && needsQueryString && searchArgs.SpecialRegex == null)
                searchArgs.Query = string.Format("\"{0}\"", searchArgs.Query);

            // fill search targets
            SearchDir searchRoot = new SearchDir(); // fake root
            for (int i = 0; i < searchDirs.Count; ++i)
            {
                if (System.IO.Directory.Exists(searchDirs[i]))
                    searchRoot.subDirs_.Add(new SearchDir(searchDirs[i], searchArgs));
                else if (System.IO.File.Exists(searchDirs[i])) // add an explicit file
                    searchRoot.files_.Add(new SearchFile(searchDirs[i]));
            }

            // Launch the appropriate search class instance
            if (searchArgs.MatchOnly)
                new MatchSearcher(searchArgs, searchRoot);
            else if (searchArgs.BinaryMode != BinaryMode.None)
                new BinarySearch(searchArgs, searchRoot);
            else if (searchArgs.RegexMode || searchArgs.QuickMode != null) // Regex search must be before `TellSearch` because it has a `TellMode`
                new RegexSearch(searchArgs, searchRoot);
            else if (searchArgs.XmlMode) // Xml search must be before `TellSearch` because it has a `TellMode`
                new XmlSearch(searchArgs, searchRoot);
            else if (searchArgs.CountFileNames || searchArgs.FileNamesOnly)
                new TellSearch(searchArgs, searchRoot);
            else
                new TextSearch(searchArgs, searchRoot);
        }

        internal static void ProcessParams(SearchParams searchArgs, Init setupData, List<string> searchDirs, string[] args, int startIdx = 0, int count = int.MaxValue)
        {
            bool hitSwitch = false;
            bool inOnlyBlock = false;
            bool inExcludeBlock = false;
            for (int i = startIdx; i < count; ++i)
            {
                string lowerCaseArg = args[i].ToLowerInvariant(); // simplifies checks that are case irrelevant /x /X
                if (args[i].StartsWith("/"))
                {
                    hitSwitch = true;
                    inOnlyBlock = false;
                    inExcludeBlock = false;
                }

                // scanner control flags
                if (lowerCaseArg.Equals("/o"))
                    inOnlyBlock = true;
                if (lowerCaseArg.Equals("/e"))
                    inExcludeBlock = true;

                // match mode
                if (lowerCaseArg.Equals("/m"))
                    searchArgs.MatchOnly = true;
                if (lowerCaseArg.Equals("/mu"))
                    searchArgs.MatchOnly = searchArgs.UniqueOnly = true;
                if (lowerCaseArg.Equals("/mf"))
                    searchArgs.MatchOnly = searchArgs.ShowMatchFileName = true;
                if (lowerCaseArg.Equals("/muf"))
                    searchArgs.MatchOnly = searchArgs.UniqueOnly = searchArgs.ShowMatchFileName = true;

                if (args[i].Equals("/t"))
                    searchArgs.FileNamesOnly = true;
                if (args[i].Equals("/T"))
                    searchArgs.CountFileNames = true;
                if (lowerCaseArg.Equals("/s"))
                    searchArgs.Recurse = true;
                if (lowerCaseArg.Equals("/r"))
                    searchArgs.RegexMode = true;
                if (lowerCaseArg.Equals("/x"))
                    searchArgs.XmlMode = true;
                if (lowerCaseArg.Equals("/not"))
                    searchArgs.NotMode = searchArgs.FileNamesOnly = true;
                if (lowerCaseArg.Equals("/a"))
                    searchArgs.Auto = true;
                if (lowerCaseArg.Equals("/l"))
                    searchArgs.ShowLineNumbers = true;
                if (lowerCaseArg.Equals("/c"))
                    searchArgs.CaseSensitive = true;
                if (lowerCaseArg.Equals("/str"))
                    searchArgs.WrapInQuote = true;
                if (lowerCaseArg.Equals("/v"))
                    searchArgs.Verbose = true;

                // binary search switches
                if (lowerCaseArg.StartsWith("/b"))
                {
                    if (lowerCaseArg.EndsWith("u32"))
                        searchArgs.BinaryMode = BinaryMode.UInt32;
                    else if (lowerCaseArg.EndsWith("u16"))
                        searchArgs.BinaryMode = BinaryMode.UInt16;
                    else if (lowerCaseArg.EndsWith("s32"))
                        searchArgs.BinaryMode = BinaryMode.Int32;
                    else if (lowerCaseArg.EndsWith("s16"))
                        searchArgs.BinaryMode = BinaryMode.Int16;
                    else if (lowerCaseArg.EndsWith("f"))
                        searchArgs.BinaryMode = BinaryMode.Float;
                    else if (lowerCaseArg.EndsWith("d"))
                        searchArgs.BinaryMode = BinaryMode.Double;
                    else
                        searchArgs.BinaryMode = BinaryMode.Any;
                }

                // Hit limits
                if (lowerCaseArg.StartsWith("/hit"))
                {
                    string subStr = lowerCaseArg.Substring("/hit".Length);
                    try {
                        searchArgs.HitLimit = int.Parse(subStr);
                    } catch { }
                }

                if (SpecialSearches.ContainsKey(lowerCaseArg))
                    searchArgs.QuickMode = SpecialSearches[lowerCaseArg];
                if (setupData.CustomCommands.ContainsKey(lowerCaseArg))
                {
                    var customCmd = setupData.CustomCommands[lowerCaseArg];
                    if (!customCmd.IsSwitches)
                        searchArgs.QuickMode = customCmd.Regex;
                }

                // deal with extensions and multiple search paths
                if (!args[i].StartsWith("/"))
                {
                    if (inOnlyBlock) // list of extensions for 'only' mode
                        searchArgs.OnlyExtensions.Add(args[i]);
                    else if (inExcludeBlock) // list of extensions for 'exclusion' mode
                        searchArgs.ExcludedExtensions.Add(args[i]);
                    else if (!hitSwitch) // additional search paths
                        searchDirs.Add(args[i]);
                }
                else // check for line or filesize rules
                {
                    string subbed = args[i].Substring(1);
                    if (char.IsDigit(subbed[0]))
                    {
                        // argument is a number, parse value
                        int value = 0;
                        for (int c = 0; c < subbed.Length; ++c)
                        {
                            if (char.IsDigit(subbed[c]))
                            {
                                value *= 10;
                                value += (int)char.GetNumericValue(subbed[c]);
                            }
                        }

                        // check if it's a file-size or a line count
                        if (args[i].EndsWith("b") || args[i].EndsWith("B")) // bytes
                            searchArgs.MaxBytes = value;
                        else if (args[i].EndsWith("k") || args[i].EndsWith("K")) // kilobytes
                            searchArgs.MaxBytes = value * SearchParams.MB;
                        else if (args[i].EndsWith("m") || args[i].EndsWith("M")) // megabyes
                            searchArgs.MaxBytes = value * SearchParams.MB;
                        else // it's a line count
                            searchArgs.LineCount = value;
                    }
                }
            }
        }
    }
}
