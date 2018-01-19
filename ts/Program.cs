﻿using System;
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
        // return start index and length
        public Regex SpecialRegex { get; set; } = null;

        public static int KB = 1024;
        public static int MB = 1024 * 1024;

        public List<string> OnlyExtensions { get; private set; } = new List<string>();
        public List<string> ExcludedExtensions { get; private set; } = new List<string>();

        public BinaryMode BinaryMode { get; set; } = BinaryMode.None;
        public int MaxBytes { get; set; } = (1024 * 1024) * 20; //20mb limit
        public string Query { get; set; }
        public bool CaseSensitive { get; set; } = false;
        public bool RegexMode { get; set; } = false;
        public bool WholeRegex { get; set; } = true;
        public bool XmlMode { get; set; } = false;
        public bool FileNamesOnly { get; set; } = false;
        public bool CountFileNames { get; set; } = false;
        public bool Recurse { get; set; } = false;
        public int LineCount { get; set; } = 5;
        public bool Auto { get; set; } = false;
        public bool ShowLineNumbers { get; set; } = false;

        public bool wrapInQuote = false;
        public string quickMode = null;

        public int DataFileCount { get; set; } = 0;
        public int DataHitCount { get; set; } = 0;

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
                "(?i)\\s*(\\#).*(\\b\\w*{0}\\b)" },
            { "/instr", // result contained in " "
                "(?i)(\\\".*)(\\b\\w*{0}\\b).*(\\\")" },
            { "/infunc", // result contained in ( )
                "(?i)(\\(.*)(\\b\\w*{0}\\b).*(\\))" },
            { "/temp", // result contained in < >
                "(?i)(<.*)(\\b\\w*{0}\\b).*(>)" },
            { "/var", // result left of . or ->
                "(?i)((->)|(\\.))(\\b\\w*{0}\\b)" },
            { "/ptr", // result left of ->
                "(?i)(->)(\\b\\w*{0}\\b)" },
            { "/set", // result left of =
                "(?i)(\\b\\w*{0}\\b)\\s(=)" },
            { "/unset", // result left of ; and not right of =
                "(?i)([^=]\\s*)(\\b\\w*{0}\\b)\\s*(;)" },
            { "/new", // new bob
                "(?i)(new)\\s*(\\b{0}\\b)"
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
                searchArgs.Query = searchArgs.quickMode;

            if (searchArgs.quickMode != null)
            {
                if (searchArgs.quickMode.Contains("{0}"))
                    searchArgs.SpecialRegex = new Regex(string.Format(searchArgs.quickMode, searchArgs.Query));
                else
                    searchArgs.SpecialRegex = new Regex(searchArgs.quickMode);
                searchArgs.WholeRegex = true;
            }

            if (searchArgs.wrapInQuote)
                searchArgs.Query = string.Format("\"{0}\"", searchArgs.Query);

            SearchDir searchRoot = new SearchDir();

            for (int i = 0; i < searchDirs.Count; ++i)
            {
                if (System.IO.Directory.Exists(searchDirs[i]))
                    searchRoot.subDirs_.Add(new SearchDir(searchDirs[i], searchArgs));
            }

            if (searchArgs.BinaryMode != BinaryMode.None)
                new BinarySearch(searchArgs, searchRoot);
            else if (searchArgs.RegexMode || searchArgs.quickMode != null) // Regex search must be before `TellSearch` because it has a `TellMode`
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

                if (args[i].Equals("/t"))
                    searchArgs.FileNamesOnly = true;
                if (args[i].Equals("/T"))
                    searchArgs.CountFileNames = true;
                if (lowerCaseArg.Equals("/s"))
                    searchArgs.Recurse = true;
                if (args[i].Equals("/R"))
                    searchArgs.WholeRegex = searchArgs.RegexMode = true;
                if (args[i].Equals("/r"))
                    searchArgs.RegexMode = true;
                if (lowerCaseArg.Equals("/x"))
                    searchArgs.XmlMode = true;

                if (lowerCaseArg.Equals("/a"))
                    searchArgs.Auto = true;
                if (lowerCaseArg.Equals("/l"))
                    searchArgs.ShowLineNumbers = true;
                if (lowerCaseArg.Equals("/c"))
                    searchArgs.CaseSensitive = true;
                if (lowerCaseArg.Equals("/str"))
                    searchArgs.wrapInQuote = true;

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

                if (SpecialSearches.ContainsKey(lowerCaseArg))
                    searchArgs.quickMode = SpecialSearches[lowerCaseArg];
                if (setupData.CustomCommands.ContainsKey(lowerCaseArg))
                {
                    var customCmd = setupData.CustomCommands[lowerCaseArg];
                    if (!customCmd.IsSwitches)
                        searchArgs.quickMode = customCmd.Regex;
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
