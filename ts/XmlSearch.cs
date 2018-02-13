using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.XPath;

namespace ts
{
    /// <summary>
    /// Scans xml files (extension irrelevant) for an XPath query
    /// </summary>
    public class XmlSearch
    {
        public XmlSearch(SearchParams args, SearchDir dir)
        {
            Console.WriteLine(string.Format("\r\nXML search mode: {0}\r\n", args.Query));

            int consoleWidth = Console.BufferWidth;
            int textLen = args.Query.Length;

            var current = dir.GetNext();
            while (current != null)
            {
                try
                {
                    /// this is just to ensure that it's Xml, total bunk
                    System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
                    doc.Load(current.path_);
                } catch
                {
                    current.Done();
                    current = dir.GetNext();
                    continue;
                }

                if (args.Verbose)
                    System.Console.WriteLine(current.path_);

                args.DataFileCount += 1;

                var lines = current.GetSearchText();
                int thisFileHits = 0;

                using (FileStream xmlStream = new FileStream(current.path_, FileMode.Open))
                {
                    xmlStream.Position = 0;
                    XPathDocument pathDocument = new XPathDocument(xmlStream);
                    foreach (XPathNavigator element in pathDocument.CreateNavigator().Select(args.Query))
                    {
                        args.DataHitCount += 1; // we have a hit
                        current.hadHit_ = true;
                        thisFileHits += 1;

                        IXmlLineInfo lineInfo = element as IXmlLineInfo;
                        int highlightLine = lineInfo.LineNumber;
                        Point drawPos = null;
                        int vOffset = 0;
                        int hOffset = 0;

                        if (!args.FileNamesOnly && !args.CountFileNames)
                        {
                            ConsoleHelper.WriteWholeLine(string.Format("#{3,-3} Line: {0,5} Col: {1,4} -> {2}", highlightLine + 1, lineInfo.LinePosition, current.path_, thisFileHits));
                            int startTop = Console.CursorTop;
                        PRINT_TEXT:
                            Console.CursorTop = startTop;
                            Console.CursorLeft = 0;
                            drawPos = FastConsole.WriteResult(lines, highlightLine-1, args.DrawExtra, -1, 0, vOffset, hOffset, args.ShowLineNumbers, drawPos, null, null);
                            Console.CursorTop += args.LineCount + 1;
                            Console.CursorLeft = 0;

                            if (!args.Auto)
                            {
                                int resCode = ConsoleHelper.ProcessInput(current.path_, lines.Length, highlightLine, args.LineCount, args.DrawExtra, ref hOffset, ref vOffset);
                                if (resCode == ConsoleHelper.INPUT_REPRINT)
                                    goto PRINT_TEXT;
                                else if (resCode == ConsoleHelper.INPUT_SKIP)
                                    goto SKIP_TARGET;
                                else if (resCode == ConsoleHelper.INPUT_SKIPFOLDER)
                                {
                                    dir.SkipCurrentDirectory();
                                    goto SKIP_TARGET;
                                }
                                else if (resCode == ConsoleHelper.INPUT_AUTO)
                                    args.Auto = true;
                                else if (resCode == ConsoleHelper.INPUT_QUIT)
                                {
                                    ConsoleHelper.Fill();
                                    ConsoleHelper.WriteColor("canceled", ConsoleColor.Red);
                                    args.WriteResults(dir);
                                    return;
                                }
                            }
                        }
                    }
                }

                if (thisFileHits > 0 && (args.CountFileNames || args.FileNamesOnly))
                {
                    if (!args.CountFileNames)
                        Console.WriteLine("    " + current.path_);
                    else
                        Console.WriteLine("    {0} -> {1}", thisFileHits, current.path_);
                }

            SKIP_TARGET:
                current.Done();
                current = dir.GetNext();
            }

            ConsoleHelper.Fill();
            ConsoleHelper.WriteColor("complete", ConsoleColor.Green);
            args.WriteResults(dir);
        }
    }
}
