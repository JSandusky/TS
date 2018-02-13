using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ts
{
    public class BinarySearch
    {
        object TranslateArg(SearchParams args)
        {
            switch (args.BinaryMode)
            {
            case BinaryMode.Double:
                return double.Parse(args.Query);
            case BinaryMode.Float:
                return float.Parse(args.Query);
            case BinaryMode.Int16:
                return Int16.Parse(args.Query);
            case BinaryMode.Int32:
                return Int32.Parse(args.Query);
            case BinaryMode.UInt16:
                return UInt16.Parse(args.Query);
            case BinaryMode.UInt32:
                return UInt32.Parse(args.Query);
            case BinaryMode.Any:
                return null;
            }
            throw new Exception("Bad binary value");
        }

        public BinarySearch(SearchParams args, SearchDir dir)
        {
            object value = TranslateArg(args);
            string strValue = value != null ? value.ToString() : args.Query;
            Console.WriteLine(string.Format("\r\nBinary search mode: {0}\r\n", strValue));

            var current = dir.GetNext();
            bool pausePeriodically = true;
            while (current != null)
            {
                if (args.Verbose)
                    Console.WriteLine(current.path_);

                Console.WriteLine();
                Console.WriteLine("    " + current.path_);
                Console.WriteLine();
                Console.WriteLine("------------------------------------------------------------------------------------------------------------------");
                Console.WriteLine("   UInt32   |    Int32    |    Float32     |      UInt16       |       Int16       |      Bytes      |  Text[4]");
                Console.WriteLine("------------------------------------------------------------------------------------------------------------------");

                using (FileStream stream = new FileStream(current.path_, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (BinaryReader rdr = new BinaryReader(stream))
                    {
                        int ct = 0;
                        long startOffset = stream.Position;


                        byte[] data = rdr.ReadBytes(4);
                        while (data != null && data.Length > 0)
                        {
                            string fValue = "";
                            string uiValue = "";
                            string ushort1 = "";
                            string ushort2 = "";
                            string iValue = "";
                            string sshort1 = "";
                            string sshort2 = "";
                            List<object> compObjects = new List<object>();

                            if (data.Length == 4)
                            {
                                compObjects.Add(System.BitConverter.ToSingle(data, 0));
                                compObjects.Add(System.BitConverter.ToUInt32(data, 0));
                                compObjects.Add(System.BitConverter.ToInt32(data, 0));
                                compObjects.Add(System.BitConverter.ToUInt16(data, 0));
                                compObjects.Add(System.BitConverter.ToUInt16(data, 2));
                                compObjects.Add(System.BitConverter.ToInt16(data, 0));
                                compObjects.Add(System.BitConverter.ToInt16(data, 2));

                                fValue = System.BitConverter.ToSingle(data, 0).ToString();
                                uiValue = System.BitConverter.ToUInt32(data, 0).ToString();
                                iValue = System.BitConverter.ToInt32(data, 0).ToString();
                                ushort1 = System.BitConverter.ToUInt16(data, 0).ToString();
                                ushort2 = System.BitConverter.ToUInt16(data, 2).ToString();
                                sshort1 = System.BitConverter.ToInt16(data, 0).ToString();
                                sshort2 = System.BitConverter.ToInt16(data, 2).ToString();
                            }
                            if (data.Length >= 2 && data.Length < 4)
                            {
                                compObjects.Add(System.BitConverter.ToUInt16(data, 0));
                                compObjects.Add(System.BitConverter.ToInt16(data, 0));
                                ushort1 = System.BitConverter.ToUInt16(data, 0).ToString();
                                sshort1 = System.BitConverter.ToInt16(data, 0).ToString();
                            }

                            byte[] bytes = new byte[4];
                            for (int i = 0; i < 4 && i < data.Length; ++i)
                            {
                                bytes[i] = data[i];
                                compObjects.Add(data[i]);
                            }

                            string[] charData = new string[4] { " ", " ", " ", " " };
                            string charString = "";
                            for (int i = 0; i < 4 && i < data.Length; ++i)
                            {
                                charData[i] = "" + (char)data[i];
                                compObjects.Add(charData[i]);
                                if (charData[i][0] == '\0')
                                    charData[i] = "\\0";
                                else if (charData[i][0] == '\r')
                                    charData[i] = "\\r";
                                else if (charData[i][0] == '\n')
                                    charData[i] = "\\n";
                                else if (charData[i][0] == '\t')
                                    charData[i] = "\\t";
                                charString += charData[i];
                            }

                            bool hasHit = false;
                            for (int i = 0; i < compObjects.Count; ++i)
                            {
                                if (value == null)
                                    hasHit |= compObjects[i].ToString().Equals(strValue);
                                else if (compObjects[i].Equals(value))
                                    hasHit = true;
                            }
                            if (value == null) // do a string containment check as well
                                hasHit |= strValue.ToLowerInvariant().Contains(charString.ToLowerInvariant());

                            Console.BackgroundColor = hasHit ? ConsoleColor.Blue : ConsoleColor.Black;

                            Console.Write(string.Format(
                                "{0,11} | {9,11} | {1,14} | {2,8} {3,8} | {10,8} {11,8} | {4,3} {5,3} {6,3} {7,3} {8}", uiValue, fValue, ushort1, ushort2, bytes[0], bytes[1], bytes[2], bytes[3],
                                string.Format("| {0,2} {1,2} {2,2} {3,2}", charData[0], charData[1], charData[2], charData[3]), iValue, sshort1, sshort2));
                            Console.BackgroundColor = ConsoleColor.Black;
                            Console.WriteLine();
                            Console.ResetColor();
                            ct += 1;
                            if (ct % 4 == 0)
                            {
                                Console.ForegroundColor = ConsoleColor.DarkYellow;
                                Console.WriteLine(string.Format("offset {0,-9} -------------------------------------------------------------------------------------------------", ct * 4 /*advance is in 4 byte icnrements*/));
                                Console.ForegroundColor = ConsoleColor.Gray;
                            }

                            if (ct % (4 * 32) == 0 && !args.Auto && pausePeriodically) // stop every 128 bytes
                            {
                                ConsoleHelper.WriteBold("Auto-paused! Continue? /Yes /No /Skip /Don't autopause");
                                var keyInfo = Console.ReadKey();
                                Console.Write("\r");
                                if (keyInfo.Key == ConsoleKey.N || keyInfo.Key == ConsoleKey.Escape)
                                    return;
                                else if (keyInfo.Key == ConsoleKey.S)
                                    goto SKIP_TARGET;
                                else if (keyInfo.Key == ConsoleKey.D)
                                {
                                    pausePeriodically = false;
                                }
                            }

                            if (hasHit && !args.Auto)
                            {
                                Console.BackgroundColor = ConsoleColor.Black;
                                Console.WriteLine();
                                ConsoleHelper.WriteBold("Continue? /Yes /No /Auto /Skip /Folder-skip");

                            INPUT_BLOCK:
                                ConsoleKeyInfo key = Console.ReadKey();
                                Console.Write("\r");
                                if (key.Key == ConsoleKey.Escape || key.Key == ConsoleKey.N)
                                {
                                    ConsoleHelper.Fill();
                                    ConsoleHelper.WriteColor("canceled", ConsoleColor.Red);
                                    args.WriteResults(dir);
                                    return;
                                }
                                else if (key.Key == ConsoleKey.A)
                                {
                                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                                    args.Auto = true;
                                }
                                else if (key.Key == ConsoleKey.S)
                                {
                                    goto SKIP_TARGET;
                                }
                                else if (key.Key == ConsoleKey.F)
                                {
                                    dir.SkipCurrentDirectory();
                                    goto SKIP_TARGET;
                                }
                                else if (key.Key == ConsoleKey.Y || key.Key == ConsoleKey.Enter)
                                {
                                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                                }
                                else
                                    goto INPUT_BLOCK;
                            }

                            data = rdr.ReadBytes(4);
                        }
                    }
                }

            SKIP_TARGET:
                current = dir.GetNext();
            }

            ConsoleHelper.Fill();
            ConsoleHelper.WriteColor("complete", ConsoleColor.Green);
            args.WriteResults(dir);
        }
    }
}
