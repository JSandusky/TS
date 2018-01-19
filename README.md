# TS

A windows console text search program similar to grep, but better. Scrolling search results and enough modes to forget about.

![program help printout](default_help.png)

![regular results](vanilla.png)

TS is feature packed with programming specific switches such as `/var`, `/ptr`, `/instr` and `/infunc` as well as the capability to use a config file to add custom switches for commonly used regular expressions or combinations of flags.

    # standard code search custom flag
    /code = /l /o cpp hpp c h cxx hxx cs
    
TS also includes rudimentary binary file searching for reverse engineering or identifying offsets into blobs.
    
![binary mode](binary_mode.png)

# TODO - Missing GREP features

Add `/m` and and `/hit<num>` options:

- `/m` only show matching text, like grep -o
    - This is useful when building interop tables, such as grabbing everything that's `VAR_` to get `VAR_FLOAT, VAR_INT, etc`
    - `/mu` *match unique*
- `/hit<num>` only show `<num>` matches.