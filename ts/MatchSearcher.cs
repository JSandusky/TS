using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ts
{
    // This searcher is automatic mode only
    public class MatchSearcher
    {
        public MatchSearcher(SearchParams args, SearchDir dir)
        {
            Console.WriteLine(string.Format("\r\nMatch search mode: {0}\r\n", args.Query));
            HashSet<string> hitRecord = new HashSet<string>();

            // setup the default regex, word boundary mode is the only case where this useful
            // TODO: I believe that's true but I'm not 100% certain, case inconsistencies perhaps?
            if (!args.RegexMode && args.SpecialRegex == null) // use a word bounded query
            {
                if (args.CaseSensitive)
                    args.SpecialRegex = new Regex(string.Format("(\\b\\w*{0}\\w*\\b)", args.Query));
                else
                    args.SpecialRegex = new Regex(string.Format("(?i)(\\b\\w*{0}\\w*\\b)", args.Query));
            }

            if (args.RegexMode || args.SpecialRegex != null)
            {
                Regex reg = args.SpecialRegex == null ? new Regex(args.Query) : args.SpecialRegex;
                var current = dir.GetNext();
                while (current != null)
                {
                    string[] lines = current.GetSearchText();
                    args.DataFileCount += 1;
                    for (int i = 0; i < lines.Length; ++i)
                    {
                        MatchCollection matches = reg.Matches(lines[i]);

                        if (matches.Count > 0)
                        {
                            current.hadHit_ = true;
                            foreach (Match match in matches)
                            {
                                if (args.UniqueOnly && !hitRecord.Contains(match.Value))
                                {
                                    OutputHit(match.Value, i, current.path_, args);
                                    hitRecord.Add(match.Value);
                                    args.DataHitCount += 1;
                                }
                                else if (!args.UniqueOnly)
                                {
                                    OutputHit(match.Value, i, current.path_, args);
                                    args.DataHitCount += 1;
                                }
                            }
                        }
                    }

                    current = dir.GetNext();
                }
            }
            else
            {
                var current = dir.GetNext();
                while (current != null)
                {
                    string[] lines = current.GetSearchText();
                    args.DataFileCount += 1;
                    for (int i = 0; i < lines.Length; ++i)
                    {
                        int hitIndex = -1;
                        string searchLine = current.GetLine(i, args.CaseSensitive);
                        string asWrittenLine = lines[i];
                        do
                        {
                            hitIndex = searchLine.IndexOf(args.Query, hitIndex + 1);
                            if (hitIndex > -1)
                            {
                                current.hadHit_ = true;
                                string foundString = asWrittenLine.Substring(hitIndex, args.Query.Length);
                                if (args.UniqueOnly && !hitRecord.Contains(foundString))
                                {
                                    OutputHit(foundString, i, current.path_, args);
                                    hitRecord.Add(foundString);
                                    args.DataHitCount += 1;
                                }
                                else if (!args.UniqueOnly)
                                {
                                    OutputHit(foundString, i, current.path_, args);
                                    args.DataHitCount += 1;
                                }
                            }
                        } while (hitIndex > -1);
                    }
                    current = dir.GetNext();
                }
            }

            ConsoleHelper.Fill();
            ConsoleHelper.WriteColor("complete", ConsoleColor.Green);
            args.WriteResults(dir);
        }

        void OutputHit(string text, int line, string fileName, SearchParams args)
        {
            if (args.ShowMatchFileName && args.ShowLineNumbers)
                Console.WriteLine(string.Format("{0,5}: {1,-40} {2}", line, text, fileName));
            else if (args.ShowMatchFileName)
                Console.WriteLine(string.Format("{0,-40} {1}", text, fileName));
            else if (args.ShowLineNumbers)
                Console.WriteLine(string.Format("{0,5}: {1}", line, text));
            else
                Console.WriteLine(text);
        }
    }
}
