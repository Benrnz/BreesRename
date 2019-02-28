# BreesRename
A command-line utility to rename many files at once using replacements and patterns.

## Syntax
BreesRename.exe "TargetFolder[\wildcardFilter]" [/r:xxx /rw:xxx] [/t:xxx] [/x:xxx] [/d] [/q]

| Argument        | Description                          | Example         |
|-----------------|--------------------------------------|-----------------|
| TargetFolder    | A full or relative path to a target folder. An error will be thrown if this points to a file or a folder that does not exist. | C:\Downloads|
| wildcardFilter  | An extension of the previous specified folder to include a wildcard filter. | *.txt |
| /r:             | Will replace the listed characters. Also requires the /rw switch directly after. | /r:_ /rw:- Will replace _ with -  
| | |/r:.- /rw:" " Will replace . and - with a space. |
| /t:             | Finds a match of the provided regex and truncates the filename after the match. But will keep the extension.| /t:S\d\dE\d\d Will find an occurance of the Regex and truncate the file directly after it. MyFile.S01E02.BlahBlah.mp4 becomes MyFile.S01E02.mp4 |
| /d              | Debug mode - will display a lot of information and not actually rename any files. | /d |
| /q              | Quiet mode - will not echo any text apart from the file rename from --> to lines. | /q |
| /x:             | Finds a match for the given regex and removes the matched text.| /x:\d\d will remove the first 2 digits.


