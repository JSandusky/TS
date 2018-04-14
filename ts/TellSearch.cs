using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ts
{
    public class TellSearch
    {
        public TellSearch(SearchParams args, SearchDir dir)
        {
            if (args.CountFileNames)
                Console.WriteLine(string.Format("\r\nCount search mode: \"{0}\"\r\n", args.Query));
            else if (args.NotMode)
                Console.WriteLine(string.Format("\r\nNot Found search mode: \"{0}\"\r\n", args.Query));
            else
                Console.WriteLine(string.Format("\r\nTell search mode: \"{0}\"\r\n", args.Query));

            List<KeyValuePair<int, string>> countHits = new List<KeyValuePair<int, string>>();

            var current = dir.GetNext();
            int searchCt = 0;
            while (current != null)
            {
                ++searchCt;
                int count = 0;
                string[] text = current.GetSearchText();

                for (int i = 0; i < text.Length; ++i)
                {
                    if (args.CaseSensitive)
                        count += text[i].Split(new string[] { args.Query }, StringSplitOptions.None).Length - 1;
                    else
                        count += text[i].ToLowerInvariant().Split(new string[] { args.Query }, StringSplitOptions.None).Length - 1;
                }

                args.DataFileCount += 1;
                args.DataHitCount += count;
                current.hadHit_ = count > 0 ? true : false;

                if (count > 0 && args.CountFileNames && !args.NotMode)
                {
                    countHits.Add(new KeyValuePair<int, string>(count, current.path_));
                }
                else if (count > 0 && !args.NotMode)
                    Console.WriteLine("  " + current.path_);
                else if (count == 0 && args.NotMode)
                    Console.WriteLine("  " + current.path_);

                if (args.CheckHits())
                    goto SEARCH_END;

                current.Done();
                current = dir.GetNext();
            }

            if (args.CountFileNames)
            {
                countHits.Sort((a, b) =>
                {
                    if (a.Key > b.Key)
                        return -1;
                    else if (b.Key > a.Key)
                        return 1;
                    return 0;
                });
                foreach (var hit in countHits)
                    Console.WriteLine(string.Format("  {0,6} -> {1}", hit.Key, hit.Value));
            }

        SEARCH_END:
            ConsoleHelper.Fill();
            ConsoleHelper.WriteColor("complete", ConsoleColor.Green);
            args.WriteResults(dir);
        }
    }
}
