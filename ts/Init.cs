using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ts
{
    public class CustomSwitch
    {
        /// <summary>
        /// Comment text from the cfg file
        /// </summary>
        public string Comment { get; set; }
        /// <summary>
        /// Poorly named, could be a regex string or a set of switches
        /// </summary>
        public string Regex { get; set; }

        /// <summary>
        /// Returns true if this regex doesn't use a query parameter
        /// </summary>
        public bool HasParameter { get { return IsSwitches || Regex.Contains("{0}"); } }

        /// <summary>
        /// Returns true if this custom switch is a collection of switches
        /// </summary>
        public bool IsSwitches { get { return Regex.StartsWith("/"); } }

        /// <summary>
        /// Turns the 'regex' field into an array of switch commands for processing.
        /// </summary>
        public string[] GetAsSwitches()
        {
            return Regex.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }

    public class Init
    {
        public List<string> DefaultSwitches { get; private set; } = new List<string>();
        public Dictionary<string, CustomSwitch> CustomCommands { get; private set; } = new Dictionary<string, CustomSwitch>();

        public Init()
        {
            string dir = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            string cfgPath = System.IO.Path.Combine(dir, "ts.cfg");
            if (System.IO.File.Exists(cfgPath))
            {
                string[] lines = System.IO.File.ReadAllLines(cfgPath);
                string nextComment = null;
                for (int i = 0; i < lines.Length; ++i)
                {
                    // grab any default switches
                    if (lines[i].ToLowerInvariant().StartsWith("default"))
                    {
                        string[] sides = lines[i].Split('=');
                        string[] switches = sides[1].Split(new char[]{ ' '}, StringSplitOptions.RemoveEmptyEntries);
                        DefaultSwitches.AddRange(switches);
                        nextComment = null;
                        continue;
                    }

                    // comments, multiple options because habits die hard
                    if (lines[i].ToLowerInvariant().StartsWith("rem"))
                    {
                        nextComment = lines[i].Substring(3).Trim();
                        continue;
                    }
                    if (lines[i].StartsWith("#"))
                    {
                        nextComment = lines[i].Substring(1).Trim();
                        continue;
                    }
                    if (lines[i].StartsWith("//"))
                    {
                        nextComment = lines[i].Substring(2).Trim();
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(lines[i].Trim()))
                    {
                        nextComment = null;
                        continue;
                    }

                    int eqIdx = lines[i].IndexOf('=');
                    if (eqIdx > 0 )
                    {
                        // we've got something
                        string switchStr = lines[i].Substring(0, eqIdx - 1).Trim().ToLowerInvariant();
                        if (!switchStr.StartsWith("/")) // might forget the switch string
                            switchStr = "/" + switchStr;
                        string regexStr = lines[i].Substring(eqIdx + 1).Trim();
                        CustomCommands[switchStr] = new CustomSwitch { Comment = nextComment, Regex = regexStr };
                        nextComment = null;
                    }
                }
            }
        }

        public bool NeedsSearchParam(string[] args)
        {
            for (int i = 0; i < args.Length; ++i)
            {
                if (CustomCommands.ContainsKey(args[i]))
                {
                    var cmd = CustomCommands[args[i]];
                    if (!cmd.HasParameter)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public void ApplyExtraSwitches(SearchParams setupData, List<string> dirs, string[] args, int endPoint)
        {
            for (int i = 0; i < args.Length; ++i)
            {
                if (CustomCommands.ContainsKey(args[i]))
                {
                    var cmd = CustomCommands[args[i]];
                    if (cmd.IsSwitches)
                    {
                        var switches = cmd.GetAsSwitches();
                        Program.ProcessParams(setupData, this, dirs, switches, 0, switches.Length);
                    }
                }
            }
        }

        public string[] PrintCommands()
        {
            List<string> cmds = new List<string>();
            if (CustomCommands.Count > 0)
            {
                cmds.Add("Custom Switches (ts.cfg):");
                cmds.Add("");
                foreach (var sw in CustomCommands)
                {
                    string comment = sw.Value.Comment;
                    if (comment == null)
                        cmds.Add(string.Format("    /3{0}/0 = {1}", sw.Key, sw.Value.Regex));
                    else
                    {
                        cmds.Add(string.Format("    /3{0}/0 = {1}", sw.Key, sw.Value.Comment));
                        cmds.Add("        " + sw.Value.Regex);
                    }
                }
            }
            else
            {
                cmds.Add("Add custom quick switches to `ts.cfg`");
                cmds.Add("    # comment of My Regex on /mySwitch");
                cmds.Add("    /mySwitch = ^(\\b\\w*myRegex\\b)");
                cmds.Add("");
                cmds.Add("    # Comment coloring");
                cmds.Add("    #/3//3 green test/5//5 red text");
            }
            return cmds.ToArray();
        }

        public static string[] PrintColorInfo()
        {
            List<string> cmds = new List<string>();
            cmds.Add("Custom Switch Comment Coloring Codes:");
            cmds.Add("");
            cmds.Add("    /0//0 Gray     /1//1 White   /2//2 Yellow");
            cmds.Add("    /3//3 Green    /4//4 Magenta /5//5 Red");
            cmds.Add("    /6//6 Dark Red /7//7 Blue    /8//8 Cyan");
            return cmds.ToArray();
        }
    }
}
