Homepage: http://www.jonschneider.com/utilities.html

Pathfind is a command-line utility which returns the locations in which a specified file is present on the current path (based on the value of the PATH environment variable). It's useful to find out the location from which a particular command-line application or file is run. If the filename to find is specified with no extension (for example, "iisreset"), then the file extensions from the PATHEXT environment variable (typically including .com, .exe., .bat, .cmd) are automatically searched (in addition to searching for the file with no extension).

New in version 2.0 is support for * and ? wildcard characters, which function the same way that they do in other cmd.exe utilities.

Notes: The functionality of this utility is similar to that of the "which" command for Unix/Linux. Also, a similar utility for Windows, where.exe, is included with newer Windows verions (including Windows 7); the functionality of PathFind is similar to that of where.exe, but PathFind's output is formatted more nicely, and only PathFind supports PATHEXT file extension searching.
