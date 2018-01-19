using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ts
{
    public class TextSearch
    {
        public TextSearch(SearchParams args, SearchDir dir)
        {
            Console.WriteLine(string.Format("\r\nText search mode: {0}\r\n", args.Query));

            int consoleWidth = Console.BufferWidth;
            int textLen = args.Query.Length;

            var current = dir.GetNext();
            while (current != null)
            {
                string[] lines = current.GetSearchText();
                args.DataFileCount += 1;

                int hitCount = 0;
                for (int i = 0; i < lines.Length; ++i)
                {
                    int hitIndex = -1;
                    do
                    {
                        hitIndex = current.GetLine(i, args.CaseSensitive).IndexOf(args.Query, hitIndex + 1);
                        if (hitIndex > -1)
                        {
                            ++hitCount;
                            args.DataHitCount += 1;
                            current.hadHit_ = true;
                            Point drawPoint = null;
                            int vOffset = 0;
                            int hOffset = 0;

                            ConsoleHelper.WriteWholeLine(string.Format("#{3,-3} Line: {0,5} Col: {1,4} -> {2}", i + 1, hitIndex, current.path_, hitCount));
                            int startTop = Console.CursorTop;
                        PRINT_TEXT:
                            Console.CursorTop = startTop;
                            Console.CursorLeft = 0;
                            drawPoint = FastConsole.WriteResult(lines, i, args.DrawExtra, hitIndex, textLen, vOffset, hOffset, args.ShowLineNumbers, drawPoint, args.Query);
                            Console.CursorTop += args.LineCount+1;
                            Console.CursorLeft = 0;

                            if (!args.Auto)
                            {
                                int res = ConsoleHelper.ProcessInput(current.path_, lines.Length, i, args.LineCount, args.DrawExtra, ref hOffset, ref vOffset);
                                if (res == ConsoleHelper.INPUT_REPRINT)
                                    goto PRINT_TEXT;
                                else if (res == ConsoleHelper.INPUT_SKIP)
                                    goto SKIP_TARGET;
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
                    } while (hitIndex > -1);
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
