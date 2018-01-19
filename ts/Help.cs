using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ts
{
    public static class Help
    {
        public static void PrintHelp(Init setupData)
        {
            Console.WriteLine();
            ConsoleHelper.WriteCoded("/2Text Search /0♦ /6Copyright 2007-2018 ♦ JSandusky");
            Console.WriteLine();
            ConsoleHelper.WriteCoded("    usage: /2optional /5required");
            //Console.WriteLine();
            ConsoleHelper.WriteCoded("/3ts /5<target_list> /2/[flags] /5<search_query>");
            Console.WriteLine();
            ConsoleHelper.WriteCoded("/3ts /5AFolder AnotherFolder /2/c /str /5/\"My Search Text\"");
            Console.WriteLine("    Two folders wrapping search text in quotes");
            ConsoleHelper.WriteCoded("/3ts /5. /2/s /o cpp h /5GUID_VALUE");
            Console.WriteLine("    Recurse from current directory, only checking *.cpp and *.h files");
            ConsoleHelper.WriteCoded("/3ts /5SubFolder /2/r /5([A-Z])\\w+");
            Console.WriteLine("    Regex search on named folder");
            ConsoleHelper.WriteCoded("/3ts /5SubFolder /2/x /5////rotation");
            Console.WriteLine("    XPath query");
            ConsoleHelper.WriteCoded("/3ts /2//config");
            ConsoleHelper.WriteWholeLine("    Show config help information");
            ConsoleHelper.WriteCoded("/3ts /2//viewdoc");
            ConsoleHelper.WriteWholeLine("    Show editor setup and search viewport action help");
            Console.WriteLine();

            List<string> switches = new List<string>();

            switches.Add("Switches:");
            //switches.Add("");
            switches.Add("    /3/a /0= Automatic mode, doesn't wait for input");
            switches.Add("    /3/b/4[mode] /0= binary data search, reinterprets query as value");
            switches.Add("        /3/b /0= anything");
            switches.Add("        /3/bf /0= 32-bit floating point");
            switches.Add("        /3/bu16 /bs16 /0= ushort, short");
            switches.Add("        /3/bu32 /bs32 /0= uint, int");
            switches.Add("    /3/c /0= Case-sensitive");
            switches.Add("        default = case-insensitive");
            switches.Add("    /3/hit<num>/0 = set maximum number of results to output");
            switches.Add("    /3/l /0= show line numbers header (max 99,999)");
            switches.Add("    /3/m/4[u,f,uf] /0= output matches only");
            switches.Add("        /1/mu/0 = unique only, /1/mf/0 = with filename");
            switches.Add("        /1/muf/0 = unique + filename");
            switches.Add("    /3/r /0= query text is regex");
            switches.Add("    /3/s /0= resursive scan subdirectories");
            switches.Add("    /3/str /0= wrap search text in double-quote \"");
            switches.Add("    /3/t /0= only filenames, no results display");
            switches.Add("    /3/T /0= only filenames with count of hits");
            switches.Add("    /3/x /0= XML xPath query mode");
            switches.Add("        query text is interpreted as an xPath expression");
            switches.Add("    /3/o /4[list] /0= only listed file extensions (without .)");
            switches.Add("        /3/o/0 cpp hpp cc h");
            switches.Add("    /3/e /4[list] /0= exclude listed file extensions (without .)");
            switches.Add("        /3/e/0 obj pdb sln");
            switches.Add("    /3/<num>/0 = results display size (default)");
            switches.Add("        //20, default: 5");
            switches.Add("    /3/<num>[b,k,m]/0 = max filesize to search, default: 20mb");

            List<string> codeHelpers = new List<string>();
            codeHelpers.Add("Code Helpers (honors /c flag):");
            //codeHelpers.Add("");
            codeHelpers.Add("    /3/#/0 = line starts with #");
            codeHelpers.Add("        macros #defs and markdown headers");
            codeHelpers.Add("    /3/instr/0 = contained within double quotes");
            codeHelpers.Add("        /1\"my Search stuff\"/0 will /3pass/0 with a query of /1\"Search\"");
            codeHelpers.Add("    /3/infunc/0 = contained within parenthesis");
            codeHelpers.Add("        /1(aVal, mySearch)/0 will /3pass");
            codeHelpers.Add("        /1\"aVal, mySearch\"/0 will /5fail");
            codeHelpers.Add("    /3/temp/0 = contained within < >");
            codeHelpers.Add("        for finding template parameters");
            codeHelpers.Add("    /3/var/0 = must be in a word to the right of /1./0 or /1->");
            codeHelpers.Add("        /1`->mySearch = 5` /3passes");
            codeHelpers.Add("        /1`.mySearch = 5;` /3passes");
            codeHelpers.Add("        /1`mySearch = 10;` /5fails");
            codeHelpers.Add("    /3/ptr/0 = must be in a word to the right of /1->");
            codeHelpers.Add("        /1`->mySearch = 5` /3passes");
            codeHelpers.Add("        /1`.mySearch = 5;` /5fails");
            codeHelpers.Add("    /3/set/0 = must be to the left of an /1=");
            codeHelpers.Add("        /1`mySearch = 5` /3passes");
            codeHelpers.Add("        /1`mySearch;` /5fails");
            codeHelpers.Add("    /3/unset/0 = must be to the left of ; without an = before it");
            codeHelpers.Add("        /1`int mySearch = 5;` /5fails");
            codeHelpers.Add("        /1`int mySearch;` /3passes");
            codeHelpers.Add("    /3/new/0 = must be to the right of /1`new`");
            codeHelpers.Add("    /3/delete/0 = must be to the right of /1`delete`");

            if (Console.BufferWidth < 124)
            {
                // narrow print, buffer is stupid small
                ConsoleHelper.Write(switches.ToArray());
                Console.WriteLine();
                ConsoleHelper.Write(setupData.PrintCommands());
                Console.WriteLine();
                ConsoleHelper.Write(Init.PrintColorInfo());
            }
            else
            {
                //two-column print
                ConsoleHelper.WriteColumns(switches.ToArray(), codeHelpers.ToArray(), 64);
                Console.WriteLine();
                ConsoleHelper.WriteColumns(setupData.PrintCommands(), Init.PrintColorInfo(), 64);
            }
            Console.WriteLine();
        }

        public static void PrintExtraHelp()
        {
            Console.WriteLine();
            ConsoleHelper.WriteCoded("/2TS Additional Usage Information");
            Console.WriteLine();
            ConsoleHelper.WriteCoded("  /1Editor Launch");
            Console.WriteLine();
            ConsoleHelper.WriteCoded("    Using /1edit mode/0 requires an environemnt variable /4TS_EDIT_TEXT/0 set to the text editor path.");
            ConsoleHelper.WriteCoded("    Launched as /1TS_EDIT_TEXT <current-file>");
            ConsoleHelper.WriteCoded("    Notepad++ will open to the correct line #, add hacks to `ConsoleHelper.ProcessInput` for more");
            Console.WriteLine();
            ConsoleHelper.WriteCoded("/6------------------------------------------------------------------------------------");
            Console.WriteLine();
            ConsoleHelper.WriteCoded("  /1Scrolling");
            Console.WriteLine();
            ConsoleHelper.WriteCoded("    Use the /1up/0 // /1down/0 arrows to vertically scroll text.");
            Console.WriteLine();
            ConsoleHelper.WriteCoded("    Use the /1left/0 // /1right/0 arrows to horizontally scroll text.");
            Console.WriteLine();
            ConsoleHelper.WriteCoded("    Hold /1CTRL/0 to scroll in larger chunks (based on displayed line size).");
            Console.WriteLine();
            ConsoleHelper.WriteCoded("    The /1page-up/0 and /1page-down/0 keys will also scroll vertically in larger increments.");
        }

        public static void PrintCfgHelp()
        {
            List<string> msg = new List<string>();
            msg.Add("");
            msg.Add("/2TS Configuration File");
            msg.Add("");
            msg.Add("/6------------------------------------------------------------------------------------");
            msg.Add("");
            msg.Add("Place a text file called `/4ts.cfg/0` alongside the `/3ts.exe/0` executable.");
            msg.Add("");
            msg.Add("Add custom switches using the syntax /4//SWITCH = REGULAR_EXPRESSION");
            msg.Add("");
            msg.Add("Comments can be written using # //// or REM. Comments /1directly above/0 a custom");
            msg.Add("switch will have their text included in the `/3ts/0` and `/3ts /help/0` outputs.");
            msg.Add("The regular expression will also be presented in the worst case.");
            msg.Add("");
            msg.Add("/1    # Contained in an email address");
            msg.Add("/1    /email = [insert_nasty_regex_here]");
            msg.Add("");
            msg.Add("Regular expressions containing {0} will be processed with String.Format to place");
            msg.Add("the query parameter in that location.");
            msg.Add("Regexes that contain {0} still requery the tail query parameter.");
            msg.Add("Regexes that do not contain {0} do not require the tail parameter.");
            msg.Add("");
            msg.Add("/1    ts . /s /myParameterlessSwitch");
            msg.Add("");
            msg.Add("If a custom switch's right-hand side starts with a // then it will be interpeted as ");
            msg.Add("a collection of flag switches to use");
            msg.Add("");
            msg.Add("/1    /code = /l /o cpp h hpp c cc cs js cxx hxx");
            msg.Add("");
            msg.Add("/6------------------------------------------------------------------------------------");
            msg.Add("");
            msg.Add("You can also include default switches adding /4default = <SWITCHES>/0 to the cfg");
            msg.Add("");
            msg.Add("This isn't recommended outside of defaulting as /3//c/0 or /3//l/0 as they'll be");
            msg.Add("included with every search, which can be confusing as to why searches aren't right.");
            ConsoleHelper.Write(msg.ToArray());
        }
    }
}
