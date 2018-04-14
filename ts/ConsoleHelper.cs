using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ts
{
    public class Point
    {
        public int x = -1;
        public int y = -1;
        public Point() { }
        public Point(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }


    public static class ConsoleHelper
    {
        /// <summary>
        /// Sets the background color if different, it's
        /// actually pretty slow to make pointless changes it
        /// </summary>
        /// <param name="color">new color to use</param>
        /// <returns>true if the color was changed</returns>
        public static bool SetBG(ConsoleColor color)
        {
            if (Console.BackgroundColor != color)
            {
                Console.BackgroundColor = color;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Sets the foreground color if not presently the same, it's
        /// actually pretty slow to change color
        /// </summary>
        /// <param name="color">new color to use</param>
        /// <returns>true if the color was changed</returns>
        public static bool SetFG(ConsoleColor color)
        {
            if (Console.ForegroundColor != color)
            {
                Console.ForegroundColor = color;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Resets all colors to gray on black
        /// </summary>
        public static void ResetColors()
        {
            if (Console.BackgroundColor != ConsoleColor.Black)
                Console.BackgroundColor = ConsoleColor.Black;
            if (Console.ForegroundColor != ConsoleColor.Gray)
                Console.ForegroundColor = ConsoleColor.Gray;
        }

        /// <summary>
        /// Writes trivial text with an arbitrary color, cleans up after itself
        /// </summary>
        /// <param name="text">text to write</param>
        /// <param name="color">color to write it as</param>
        public static void WriteColor(string text, ConsoleColor color)
        {
            SetFG(color);
            Console.WriteLine(text);
            ResetColors();
        }

        /// <summary>
        /// Fills an entire line with spaces
        /// </summary>
        public static void Fill()
        {
            for (int i = 0; i < Console.BufferWidth; ++i)
                Console.Write(' ');
        }

        /// <summary>
        /// Gets the current cursor position as a point object
        /// </summary>
        /// <returns>cursor location as point</returns>
        public static Point GetCursor()
        {
            return new Point(Console.CursorLeft, Console.CursorTop);
        }

        /// <summary>
        /// Sets the position of the cursor
        /// </summary>
        /// <param name="p">Point to write at</param>
        public static void SetWritePoint(Point p)
        {
            Console.CursorLeft = p.x;
            Console.CursorTop = p.y;
        }

        /// <summary>
        /// Calculates a safe horizontal offset (basically no negatives)
        /// </summary>
        /// <param name="adjustAmount">amount to adjust the offset by</param>
        /// <param name="currentOffset">current horizontal offset</param>
        /// <param name="charCount">number of characters in the current line</param>
        /// <returns>the new horizontal offset</returns>
        public static int HorizontalOffset(int adjustAmount, int currentOffset, int charCount)
        {
            currentOffset += adjustAmount;
            if (currentOffset < 0)
                currentOffset = 0;
            return currentOffset;
        }

        /// <summary>
        /// Calculates safe vertical offset into the source text
        /// </summary>
        /// <param name="adjustAmount">amount to move the offset by</param>
        /// <param name="currentOffset">current offset</param>
        /// <param name="centroid">focused line</param>
        /// <param name="lineCount">number of lines to be displayed</param>
        /// <param name="extraDrawSpace">extra draw space around the centroid line, calculated from lineCount</param>
        /// <returns>the new vertical offset</returns>
        public static int VerticalOffset(int adjustAmount, int currentOffset, int centroid, int lineCount, int extraDrawSpace)
        {
            currentOffset += adjustAmount;
            int calcValue = centroid + currentOffset - extraDrawSpace + 1;
            while (calcValue <= 0)
            {
                currentOffset += 1;
                calcValue = centroid + currentOffset - extraDrawSpace + 1;
            }

            calcValue = centroid + currentOffset + extraDrawSpace + 1;
            while (calcValue > lineCount+2)
            {
                currentOffset -= 1;
                calcValue = centroid + currentOffset + extraDrawSpace + 1;
            }
            return currentOffset;
        }

        /// <summary>
        /// Writes text that uses / to indicate that the next character should be yellow.
        /// TODO: deprecate in favor of 'color coded'
        /// </summary>
        /// <param name="text">Text to print</param>
        public static void WriteBold(string text)
        {
            bool nextBold = false;
            for (int i = 0; i < text.Length; ++i)
            {
                if (text[i] == '/')
                    nextBold = true;
                else
                {
                    if (nextBold)
                    {
                        SetFG(ConsoleColor.Yellow);
                        Console.Write(text[i]);
                        ResetColors();
                        nextBold = false;
                    }
                    else
                        Console.Write(text[i]);
                }
            }
        }

        /// <summary>
        /// Indexing of color escape codes
        /// </summary>
        static ConsoleColor[] Colors =
        {
            ConsoleColor.Gray,      //0
            ConsoleColor.White,     //1
            ConsoleColor.Yellow,    //2
            ConsoleColor.Green,     //3
            ConsoleColor.Magenta,   //4
            ConsoleColor.Red,       //5
            ConsoleColor.DarkRed,   //6
            ConsoleColor.Blue,      //7
            ConsoleColor.Cyan       //8
        };

        /// <summary>
        /// Writes color escaped coded output
        /// </summary>
        /// <param name="text">text to write</param>
        /// <param name="noEnd">if true then spaces will be written to fully fill the buffer width</param>
        /// <returns>number of characters printed</returns>
        public static int WriteCoded(string text, bool noEnd = false)
        {
            int ct = 0;
            for (int i = 0; i < text.Length; ++i)
            {
                if (text[i] == '/')
                {
                    if (char.IsDigit(text[i + 1]))
                    {
                        ++i;
                        SetFG(Colors[(int)char.GetNumericValue(text[i])]);
                        continue;
                    }
                    else if (text[i + 1] == '/')
                    {
                        ++i;
                        Console.Write('/');
                        continue;
                    }
                }
                ++ct;
                Console.Write(text[i]);
            }
            ResetColors();
            if (!noEnd)
                Console.WriteLine();
            return ct;
        }

        /// <summary>
        /// Writes two string into a column form.
        /// </summary>
        /// <param name="columnA">text for the first column</param>
        /// <param name="columnB">text for the second column</param>
        /// <param name="colAWidth">width of the first column, second gets everything</param>
        /// <param name="indent">Padding to apply to the first column</param>
        public static void WriteTwoColumnCoded(string columnA, string columnB, int colAWidth, int indent = 0)
        {
            string leftSide = columnA.PadLeft(indent);
            int dim = WriteCoded(columnA, true);
            Console.CursorLeft = colAWidth;
            WriteCoded(columnB);
        }

        /// <summary>
        /// Writes an error as color coded output in a line for each entry.
        /// </summary>
        /// <param name="a">list of lines to print</param>
        public static void Write(string[] a)
        {
            for (int i = 0; i < a.Length; ++i)
            {
                if (string.IsNullOrEmpty(a[i]))
                    Console.WriteLine();
                else
                    WriteCoded(a[i]);
            }
        }

        /// <summary>
        /// Writes two arrays of text into the console in two columns.
        /// </summary>
        /// <param name="a">list of strings to print in the left column</param>
        /// <param name="b">list of strings to print in the right column</param>
        /// <param name="colAWidth">Width of the left column, right column gets everything left</param>
        public static void WriteColumns(string[] a, string[] b, int colAWidth)
        {
            for (int i = 0; i < a.Length || i < b.Length; ++i)
            {
                if (i < a.Length && i < b.Length)
                    WriteTwoColumnCoded(a[i], b[i], colAWidth);
                else if (i < a.Length)
                {
                    WriteCoded(a[i]);
                }
                else if (i < b.Length)
                {
                    Console.CursorLeft = colAWidth;
                    WriteCoded(b[i]);
                }
            }
        }

        /// <summary>
        /// Outputs a wall of text into the buffer
        /// </summary>
        /// <param name="lines">lines of text in the file</param>
        /// <param name="baseLine">current 'focus' line</param>
        /// <param name="extraLines">number of lines around the focus to display</param>
        /// <param name="startHighlight">column index to start highlight</param>
        /// <param name="hightlightLength">length to end the column index highlight</param>
        /// <param name="vOffset">vertical offset in the buffer</param>
        /// <param name="hOffset">horizontal offset in the buffer</param>
        /// <param name="drawLineNumbers">show line number header (max 99,999)</param>
        /// <param name="bufferPos">position in the console to draw the window, should be a null object initially</param>
        /// <param name="searchText">Text to match for highlight, case insensitive</param>
        /// <param name="regex">Regex expression to use for highlighting matches</param>
        /// <returns>The point where the buffer was drawn</returns>
        [Obsolete("ConsoleHelper.WriteResult is too slow, use FastConsole.WriteResult instead. Input buffering causes a descent into zero control.", true)]
        public static Point WriteResult(string[] lines, int baseLine, int extraLines, int startHighlight, int hightlightLength, int vOffset, int hOffset, bool drawLineNumbers, Point bufferPos, string searchText = null, Regex regex = null)
        {
            Point cursorPos = GetCursor();
            Point pos = GetCursor();
            if (bufferPos != null)
            {
                pos = bufferPos;
                SetWritePoint(pos);
            }

            int lineStart = baseLine - extraLines + vOffset;
            int lineEnd = baseLine + extraLines + vOffset;
            //if (lineEnd == lineStart) // make sure we at least can output 1 line
            //    lineEnd = lineStart + 1;

            ConsoleColor defaultBackground = ConsoleColor.DarkBlue;
            ConsoleColor defaultText = ConsoleColor.Gray;
            ConsoleColor lineHighlightBackground = ConsoleColor.Blue;
            ConsoleColor resultBackground = ConsoleColor.Cyan;
            ConsoleColor resultText = ConsoleColor.Black;
            ConsoleColor extraHitText = ConsoleColor.Green;

            SetBG(defaultBackground);
            SetFG(defaultText);
            for (int j = lineStart; j <= lineEnd; ++j)
            {
                if (j < 0)
                {
                    lineEnd++;
                    continue;
                }
                else if (j >= lines.Length)
                {
                    SetBG(defaultBackground);
                    if (j == lines.Length)
                        Console.Write("===EOF===");
                    for (int c = j == lines.Length ? 9 : 0; c < Console.BufferWidth; ++c)
                        Console.Write(' ');
                    continue;
                }

                ConsoleColor chosenBackgroundColor = j == baseLine ? lineHighlightBackground : defaultBackground;
                SetBG(chosenBackgroundColor);
                int LINEHEADER_WIDTH = 7;
                if (drawLineNumbers)
                {
                    string lineStr = (j + 1).ToString();
                    if (j == lines.Length)
                        lineStr = "=EOF=";
                    // the horror!
                    while (lineStr.Length < 5)
                        lineStr = " " + lineStr;

                    lineStr += ": ";
                    if (j == baseLine)
                        SetFG(ConsoleColor.Yellow);
                    else
                        SetFG(defaultText);
                    Console.Write(lineStr);
                }

                MatchCollection regexMatches = null;
                if (regex != null)
                    regexMatches = regex.Matches(lines[j]);

                for (int c = hOffset; c < lines[j].Length; ++c)
                {
                    List<int> hits = new List<int>();
                    if (searchText != null)
                    {
                        int hit = lines[j].ToLowerInvariant().IndexOf(searchText);
                        while (hit != -1)
                        {
                            hits.Add(hit);
                            hit = lines[j].ToLowerInvariant().IndexOf(searchText, hit + 1);
                        }
                    }

                    if (c >= startHighlight && c < (startHighlight + hightlightLength) && baseLine == j)
                    {
                        SetBG(resultBackground);
                        SetFG(resultText);
                    }
                    else
                    {
                        bool overriden = false;
                        for (int i = 0; i < hits.Count; ++i)
                        {
                            if (c >= hits[i] && c < hits[i] + hightlightLength)
                            {
                                overriden = true;
                                SetBG(chosenBackgroundColor);
                                SetFG(extraHitText);
                            }
                        }
                        if (regexMatches != null)
                        {
                            foreach (Match match in regexMatches)
                            {
                                if (c >= match.Index && c < match.Index + match.Length)
                                {
                                    overriden = true;
                                    SetBG(chosenBackgroundColor);
                                    SetFG(extraHitText);
                                }
                            }
                        }
                        if (!overriden)
                        {
                            SetBG(chosenBackgroundColor);
                            SetFG(defaultText);
                        }
                    }
                    Console.Write(lines[j][c]);
                }

                SetBG(chosenBackgroundColor);
                SetFG(defaultText);
                for (int c = Math.Max(lines[j].Length - hOffset, 0); c < Console.BufferWidth - (drawLineNumbers ? LINEHEADER_WIDTH : 0); ++c)
                    Console.Write(' ');

                //Debug.Assert(Console.CursorLeft == 0);
            }
            ResetColors();

            return pos;
        }

        /// <summary>
        /// Writes in 'bold' mode the given text, then flood the rest of the line with spaces to make sure the buffer is wiped
        /// </summary>
        /// <param name="text">Text to write</param>
        public static void WriteWholeLine(string text)
        {
            WriteBold(text);
            for (int i = text.Length; i < Console.BufferWidth; ++i)
                Console.Write(' ');
        }

        public static readonly int INPUT_REPRINT = 1;   // reprint current result
        public static readonly int INPUT_QUIT = -1;     // stop all searching
        public static readonly int INPUT_CONTINUE = 0;  // next result
        public static readonly int INPUT_SKIP = 2;      // skip this file
        public static readonly int INPUT_AUTO = 3;      // activate automatic mode, no input stopping
        public static readonly int INPUT_SKIPFOLDER = 4;// skip the current folder

        /// <summary>
        /// Takes care of input for the search handlers.
        /// </summary>
        /// <param name="filePath">path to the current file</param>
        /// <param name="totalLines">number of lines in the file</param>
        /// <param name="lineNumber">current search result line</param>
        /// <param name="displayLines">number of lines shown</param>
        /// <param name="displayPad">extra lines displayed</param>
        /// <param name="hOffset">horizontal scroll offset</param>
        /// <param name="vOffset">vertical scroll offset</param>
        /// <returns>resulting input code, see values above</returns>
        public static int ProcessInput(string filePath, int totalLines, int lineNumber, int displayLines, int displayPad, ref int hOffset, ref int vOffset)
        {
            ConsoleHelper.WriteBold("Continue? /Yes /No /Skip /Folder-skip /Auto /Edit /Open-folder /A/r/r/o/w/s: scroll");
            ConsoleKeyInfo key = Console.ReadKey();
            int multiplier = key.Modifiers.HasFlag(ConsoleModifiers.Control) ? Math.Max(displayLines, 5) : 1;

            // block so long as we don't get a valid key
            // !!! because ALT+TAB accidents are brutal
            while (!AllowedInputKeys.Contains(key.Key))
            {
                // erase the print char
                Console.CursorLeft -= 1;
                Console.Write(' ');
                key = Console.ReadKey();
                multiplier = key.Modifiers.HasFlag(ConsoleModifiers.Control) ? Math.Max(displayLines, 5) : 1;
            }

            Console.Write("\r");
            if (key.Key == ConsoleKey.E)
            {
                string editor = Environment.GetEnvironmentVariable("TS_EDIT_TEXT");
                if (!string.IsNullOrEmpty(editor))
                {
                    System.Diagnostics.ProcessStartInfo pStart = new System.Diagnostics.ProcessStartInfo(editor, System.IO.Path.GetFullPath(filePath));

                // !!! WARNING !!! program specific hacks

                    // append line number for notepad++ so it launches focused on the exact line
                    if (editor.ToLower().Contains("notepad++"))
                        pStart.Arguments = string.Format("\"{0}\" -n{1}", pStart.Arguments, lineNumber);

                    System.Diagnostics.Process.Start(pStart);
                    return INPUT_REPRINT;
                }
            }
            else if (key.Key == ConsoleKey.O)
            {
                string folder = System.IO.Path.GetDirectoryName(filePath);
                System.Diagnostics.Process.Start("explorer.exe", folder);
                return INPUT_REPRINT;
            }

            if (key.Key == ConsoleKey.Escape || key.Key == ConsoleKey.N)
            {
                return INPUT_QUIT;
            }
            else if (key.Key == ConsoleKey.UpArrow)
            {
                vOffset = ConsoleHelper.VerticalOffset(-1 * multiplier, vOffset, lineNumber, totalLines, displayPad);
                return INPUT_REPRINT;
            }
            else if (key.Key == ConsoleKey.DownArrow)
            {
                vOffset = ConsoleHelper.VerticalOffset(1 * multiplier, vOffset, lineNumber, totalLines, displayPad);
                return INPUT_REPRINT;
            }
            else if (key.Key == ConsoleKey.LeftArrow)
            {
                hOffset = ConsoleHelper.HorizontalOffset(-1 * multiplier, hOffset, 0);
                return INPUT_REPRINT;
            }
            else if (key.Key == ConsoleKey.RightArrow)
            {
                hOffset = ConsoleHelper.HorizontalOffset(1 * multiplier, hOffset, 0);
                return INPUT_REPRINT;
            }
            else if (key.Key == ConsoleKey.PageUp)
            {
                vOffset = ConsoleHelper.VerticalOffset(-displayLines, vOffset, lineNumber, totalLines, displayPad);
                return INPUT_REPRINT;
            }
            else if (key.Key == ConsoleKey.PageDown)
            {
                vOffset = ConsoleHelper.VerticalOffset(displayLines, vOffset, lineNumber, totalLines, displayPad);
                return INPUT_REPRINT;
            }
            else if (key.Key == ConsoleKey.S)
            {
                return INPUT_SKIP;
            }
            else if (key.Key == ConsoleKey.F)
                return INPUT_SKIPFOLDER;
            else if (key.Key == ConsoleKey.A)
            {
                return INPUT_AUTO;
            }

            return INPUT_CONTINUE;
        }

        /// <summary>
        /// List of allowed keys, keys not in this array will cause input capture to repeat.
        /// This prevents accidental errors from ALT+TAB chaos.
        /// </summary>
        static ConsoleKey[] AllowedInputKeys =
        {
            // scroll vertical
            ConsoleKey.UpArrow, ConsoleKey.DownArrow, ConsoleKey.PageUp, ConsoleKey.PageDown,
            // scroll horizontal
            ConsoleKey.LeftArrow, ConsoleKey.RightArrow, 
            // continue, these just work since they're in the list thus there's no code checking these codes
            ConsoleKey.Enter, ConsoleKey.Spacebar, ConsoleKey.Y,
            // quit
            ConsoleKey.N, ConsoleKey.Escape,
            ConsoleKey.E, // edit file
            ConsoleKey.O, // open folder
            ConsoleKey.S, // skip file
            ConsoleKey.F, // skip folder
            ConsoleKey.A, // go automatic
            ConsoleKey.C, // Copy
        };
    }
}
