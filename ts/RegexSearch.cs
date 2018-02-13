using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace ts
{
    public class RegexSearch
    {
        public RegexSearch(SearchParams args, SearchDir dir)
        {
            Regex reg = args.SpecialRegex == null ? new Regex(args.Query) : args.SpecialRegex;
            Console.WriteLine(string.Format("\r\nRegular expression search mode: {0}\r\n", args.Query));

            int consoleWidth = Console.BufferWidth;
            var current = dir.GetNext();
            while (current != null)
            {
                // Print scanned file, but not if in tell mode
                if (args.Verbose && !args.FileNamesOnly)
                    System.Console.WriteLine(current.path_);

                string[] lines = current.GetSearchText();
                args.DataFileCount += 1;
                int thisFileHits = 0;

                for (int i = 0; i < lines.Length; ++i)
                {
                    MatchCollection matches = reg.Matches(lines[i]);

                    // do standard search?
                    if (!args.CountFileNames && !args.FileNamesOnly)
                    {
                        current.hadHit_ = true;
                        foreach (Match match in matches)
                        {
                            int hitIndex = match.Index;
                            int textLen = match.Length;
                            if (hitIndex > -1)
                            {
                                thisFileHits += 1;
                                args.DataHitCount += 1;
                                Point drawPoint = null;
                                int vOffset = 0;
                                int hOffset = 0;

                                ConsoleHelper.WriteWholeLine(string.Format("#{3,-3} Line: {0,5} Col: {1,4} -> {2}", i + 1, hitIndex, current.path_, thisFileHits));
                                int startTop = Console.CursorTop;
                            PRINT_TEXT:
                                Console.CursorTop = startTop;
                                Console.CursorLeft = 0;
                                drawPoint = FastConsole.WriteResult(lines, i, args.DrawExtra, hitIndex, textLen, vOffset, hOffset, args.ShowLineNumbers, drawPoint, null, reg);
                                Console.CursorTop += args.LineCount + 1;
                                Console.CursorLeft = 0;

                                if (!args.Auto)
                                {
                                    int res = ConsoleHelper.ProcessInput(current.path_, lines.Length, i, args.LineCount, args.DrawExtra, ref hOffset, ref vOffset);
                                    if (res == ConsoleHelper.INPUT_REPRINT)
                                        goto PRINT_TEXT;
                                    else if (res == ConsoleHelper.INPUT_SKIP)
                                        goto SKIP_TARGET;
                                    else if (res == ConsoleHelper.INPUT_SKIPFOLDER)
                                    {
                                        dir.SkipCurrentDirectory();
                                        goto SKIP_TARGET;
                                    }
                                    else if (res == ConsoleHelper.INPUT_AUTO)
                                        args.Auto = true;
                                    else if (res == ConsoleHelper.INPUT_QUIT)
                                    {
                                        ConsoleHelper.Fill();
                                        ConsoleHelper.WriteColor("canceled", ConsoleColor.Red);
                                        args.WriteResults(dir);
                                        return;
                                    }
                                }

                                if (args.CheckHits())
                                    goto SEARCH_END;
                            }
                        }
                    }
                    else
                    {
                        // `tell` mode
                        current.hadHit_ = true;
                        thisFileHits += matches.Count;
                    }
                }

                if (thisFileHits > 0 && (args.CountFileNames || args.FileNamesOnly))
                {
                    args.DataHitCount += thisFileHits;
                    if (!args.CountFileNames)
                        Console.WriteLine("    " + current.path_);
                    else
                        Console.WriteLine("    {0} -> {1}", thisFileHits, current.path_);
                }

            SKIP_TARGET:
                current.Done();
                current = dir.GetNext();
            }

        SEARCH_END:
            ConsoleHelper.Fill();
            ConsoleHelper.WriteColor("complete", ConsoleColor.Green);
            args.WriteResults(dir);
        }
    }
}
