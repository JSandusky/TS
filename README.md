# TS

A windows console text search program similar to grep, but better. Scrolling search results and enough modes to forget about. Drop it in C:/Windows or put it on your PATH and just fire away a `ts` in the commandline for help.

![program help printout](default_help.png)

![regular results](vanilla.png)

TS is feature packed with programming specific switches such as `/var`, `/ptr`, `/instr` and `/infunc` as well as the capability to use a config file to add custom switches for commonly used regular expressions. These regexes will be System.String.Format'ed if they contain a {0} sequence to take the entered query parameter. If {0} is not contained in the regex then the regex is considered to be parameterless and can be used without entering query text (such as grabbing all email addresses from files in a directory, with `/mu` to only take the unique email addresses that's pretty useful).

    # Nasty email ripping regex, doesn't require parameters
    /email = ([\da-zA-Z_-]+)@([\da-zA-Z_-]+).([\da-zA-Z_-]{2,6})?
    
    # use as `ts . /s /mu /email` to grab all emails

Custom switches can also just be a collection of switches:

    # standard code search switches
    /code = /l /o cpp hpp c h cxx hxx cs
    
TS also includes rudimentary binary file searching for reverse engineering or identifying offsets into blobs.
    
![binary mode](binary_mode.png)

# TODO

- ~~Add `/m` only show matching text, like grep -o~~
    - This is useful when building interop tables, such as grabbing everything that's `VAR_` to get `VAR_FLOAT, VAR_INT, etc`
    - `/mu` *match unique*
- ~~Add `/hit<num>` only show `<num>` matches.~~
- ~~Add support for explicitly named files instead of directories `ts MyFileName.bin /bu16 3338`~~
- Add json-query support
- Add RIFF container searching
    - Header and structure blocks only really
- Zip archive searching
    - Headers and records
