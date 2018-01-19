using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ts
{
    /// <summary>
    /// Based on:
    ///     https://stackoverflow.com/questions/2754518/how-can-i-write-fast-colored-output-to-console
    /// </summary>
    public static class FastConsole
    {
        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern SafeFileHandle CreateFile(
        string fileName,
        [MarshalAs(UnmanagedType.U4)] uint fileAccess,
        [MarshalAs(UnmanagedType.U4)] uint fileShare,
        IntPtr securityAttributes,
        [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
        [MarshalAs(UnmanagedType.U4)] int flags,
        IntPtr template);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteConsoleOutput(
          SafeFileHandle hConsoleOutput,
          CharInfo[] lpBuffer,
          Coord dwBufferSize,
          Coord dwBufferCoord,
          ref SmallRect lpWriteRegion);

        [StructLayout(LayoutKind.Sequential)]
        public struct Coord
        {
            public short X;
            public short Y;

            public Coord(short X, short Y)
            {
                this.X = X;
                this.Y = Y;
            }
        };

        [StructLayout(LayoutKind.Explicit)]
        public struct CharUnion
        {
            [FieldOffset(0)]
            public char UnicodeChar;
            [FieldOffset(0)]
            public byte AsciiChar;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct CharInfo
        {
            [FieldOffset(0)]
            public CharUnion Char;
            [FieldOffset(2)]
            public short Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SmallRect
        {
            public short Left;
            public short Top;
            public short Right;
            public short Bottom;
        }

        /// Color of extra highlighted results, Bright green
        static short EXTRA_RESULT_TEXT = 0x0002 | 0x0008;
        /// Color of main result text, black used on Cyan background for search result
        static short RESULT_TEXT = 0x0000;
        /// All other text, Standard gray
        static short STANDARD_TEXT = 0x0002 | 0x001 | 0x0004;
        /// Line number for the highlighted line, Yellow ??? is this even worth doing?
        static short FOCUSED_LINE_NUM = 0x0004 | 0x0002 | 0x0008;
        /// === EOF === marker
        static short EOF_TEXT = 0x004 | 0x008;

        /// Viewport background color, dark blue
        static short VIEWPORT_BACKGROUND = 0x0010;
        /// Highlighted line background color, light blue
        static short LINE_HIGHLIGHT_BACKGROUND = 0x0010 | 0x0080;
        /// Main result text background, cyan
        static short RESULT_BACKGROUND = 0x0010 | 0x0080 | 0x0020;

        /// <summary>
        /// Outputs result text into the viewport defined by the buffer
        /// </summary>
        /// <param name="buffer">Console buffer character data array (1d indexing)</param>
        /// <param name="str">String to print</param>
        /// <param name="color">Color to use for character attributes</param>
        /// <param name="fill">Whether to output empty space to fill the buffer horizontally</param>
        /// <param name="bufWidth">width of the buffer</param>
        /// <param name="x">horizontal X offset to start writing into the buffer</param>
        /// <param name="y">vertical Y offset for the current row of the buffer</param>
        /// <returns>unused junk</returns>
        public static int Write(this CharInfo[] buffer, string str, int color, bool fill, int bufWidth, int x, int y, char fillCharacter = ' ')
        {
            // assumes beginning
            for (int i = 0; i < bufWidth; ++i)
            {
                if (i < str.Length)
                {
                    buffer[bufWidth * y + (x + i)].Attributes = (short)color;
                    buffer[bufWidth * y + (x + i)].Char.AsciiChar = (byte)str[i];
                }
                else if (fill)
                {
                    buffer[bufWidth * y + (x + i)].Attributes = (short)color;
                    buffer[bufWidth * y + (x + i)].Char.AsciiChar = (byte)fillCharacter;
                }
                else
                    return str.Length + x;
            }
            return bufWidth;
        }

        /// <summary>
        /// Outputs a search result viewport windows, with line highlight, extra results coloring,
        /// optional line numbers, and vertical/horizontal scrolling.
        /// Uses PInvoke based buffer blitting to render more quickly than Console's WriteLine and Write methods can.
        /// This is necessary for scrolling text fast enough that inputs aren't excessively buffered, resulting in loss
        /// of control of the buffer's pan and scroll.
        /// </summary>
        /// <param name="lines">file lines array</param>
        /// <param name="baseLine">line of the `main` result be displayed</param>
        /// <param name="extraLines">Number of extra lines each side of the baseLine to display</param>
        /// <param name="startHighlight">starting index of the main result</param>
        /// <param name="hightlightLength">length of the main result</param>
        /// <param name="vOffset">vertical scrolling offset</param>
        /// <param name="hOffset">horizontal scrolling offset</param>
        /// <param name="drawLineNumbers">whether to draw line numbers or not</param>
        /// <param name="bufferPos">position at which to blit the buffer</param>
        /// <param name="searchText">optional search text for highlighting extra results (case insensitive)</param>
        /// <param name="regex">Optional regex for highlighting extra results</param>
        /// <returns>the point that the buffer was written into</returns>
        public static Point WriteResult(string[] lines, int baseLine, int extraLines, int startHighlight, int hightlightLength, int vOffset, int hOffset, bool drawLineNumbers, Point bufferPos, string searchText = null, Regex regex = null)
        {
            int bufferWidth = Console.BufferWidth;

            Point pos = ConsoleHelper.GetCursor();
            if (bufferPos != null)
                pos = bufferPos;

            int lineStart = baseLine - extraLines + vOffset;
            int lineEnd = baseLine + extraLines + vOffset;

            SafeFileHandle h = CreateFile("CONOUT$", 0x40000000, 2, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero);
            if (!h.IsInvalid)
            {

                int buffHeight = (extraLines*2 + 1);
                CharInfo[] buf = new CharInfo[bufferWidth * buffHeight];
                SmallRect rect = new SmallRect() { Left = (short)pos.x, Top = (short)pos.y, Right = (short)(bufferWidth + pos.x), Bottom = (short)(pos.y + buffHeight) };

                int yPos = 0;
                for (int j = lineStart; j <= lineEnd; ++j)
                {
                    if (j < 0)
                    {
                        lineEnd++;
                        continue;
                    }
                    else if (j >= lines.Length)
                    {
                        if (j == lines.Length && yPos < buffHeight)
                        {
                            buf.Write(">>>>>> EOF ", VIEWPORT_BACKGROUND | EOF_TEXT, true, bufferWidth, 0, yPos, '<');
                        }
                        else if (yPos < buffHeight)
                            buf.Write(" ", VIEWPORT_BACKGROUND | STANDARD_TEXT, true, bufferWidth, 0, yPos);
                        yPos += 1;
                        continue;
                    }

                    int baseBG = (j == baseLine) ? LINE_HIGHLIGHT_BACKGROUND : VIEWPORT_BACKGROUND;
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
                        buf.Write(lineStr, baseBG | (j == baseLine ? FOCUSED_LINE_NUM : STANDARD_TEXT), false, bufferWidth, 0, yPos);
                    }

                    int headerWidth = (drawLineNumbers ? LINEHEADER_WIDTH : 0);

                    MatchCollection regexMatches = null;
                    if (regex != null)
                        regexMatches = regex.Matches(lines[j]);

                    for (int c = hOffset; c < lines[j].Length; ++c)
                    {
                        List<int> hits = new List<int>();
                        if (searchText != null)
                        {
                            try { // screw checking it
                                int hit = lines[j].ToLowerInvariant().IndexOf(searchText);
                                while (hit != -1)
                                {
                                    hits.Add(hit);
                                        hit = lines[j].ToLowerInvariant().IndexOf(searchText, hit + 1);
                                }
                            } catch { }
                        }

                        int conColor = baseBG | STANDARD_TEXT;
                        if (c >= startHighlight && c < (startHighlight + hightlightLength) && baseLine == j)
                        {
                            conColor = RESULT_BACKGROUND | RESULT_TEXT;
                        }
                        else
                        {
                            bool overriden = false;
                            for (int i = 0; i < hits.Count; ++i)
                            {
                                if (c >= hits[i] && c < hits[i] + hightlightLength)
                                {
                                    overriden = true;
                                    conColor = baseBG | EXTRA_RESULT_TEXT;
                                }
                            }
                            if (regexMatches != null)
                            {
                                foreach (Match match in regexMatches)
                                {
                                    if (c >= match.Index && c < match.Index + match.Length)
                                    {
                                        overriden = true;
                                        conColor = baseBG | EXTRA_RESULT_TEXT;
                                    }
                                }
                            }
                            if (!overriden)
                            {
                                conColor = baseBG | STANDARD_TEXT;
                            }
                        }
                        buf.Write(lines[j][c].ToString(), conColor, false, bufferWidth, Math.Min(Math.Max(c - hOffset + headerWidth, 0), bufferWidth-1), yPos);
                    }

                    int tailColor = baseBG | STANDARD_TEXT;
                    for (int c = Math.Max(lines[j].Length - hOffset + headerWidth, headerWidth); c < Console.BufferWidth; ++c)
                        buf.Write(" ", tailColor, false, bufferWidth, c, yPos);
                    yPos += 1;
                }

                while (yPos < buffHeight)
                {
                    buf.Write(" ", VIEWPORT_BACKGROUND, true, bufferWidth, 0, yPos);
                    ++yPos;
                }

                bool b = WriteConsoleOutput(h, buf,
                      new Coord() { X = (short)bufferWidth, Y = (short)buffHeight },
                      new Coord() { X = 0, Y = 0 },
                      ref rect);
            }
            return pos;
        }
    }
}
