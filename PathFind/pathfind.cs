using System;
using System.Collections.Generic;

/* Version history
 *
 * 1.0.0 (2/24/2006) - Initial version
 * 2.0.0 (5/15/2007) (SOURCE LOST!! as of 7/2011) - Added support for * and ? wildcards
 * 2.0.1 (7/17/2011) - Re-implemented wildcard support (since v2.0 source was lost)
 *    - Fixed a bug where trailing ";" on the path would output a spurious error message
 *    - Writes out count of folders where matches were found, and count of matching filenames found
 *    - Better output messages (at a cost of a much more messy Main method, could very much use cleanup)
 *
 * Possible future enhancements
 * - Support /c to copy first path found to the clipboard?
 */


namespace pathfind
{
	class pathfind
	{
		private enum Results
		{
			Success = 0,
			NotFound = 1,
			Error  = 2
		}

		/// <summary>
		/// Searches for instances of a specified file name in the path (as defined by the PATH
		/// environment variable).
		/// </summary>
		/// <param name="args">The command line input. </param>
		/// <returns>An integer value from the Results enumeration. </returns> 
		[STAThread]
		static int Main(string[] args)
		{
			try
			{
				Console.Out.WriteLine();
                Console.Out.WriteLine("PathFind v2.0.1 by Jon Schneider");

				if ((args.Length < 1) || args[0]=="-?" || args[0]=="/?")
				{
                    Console.Out.WriteLine();
                    Console.Out.WriteLine("Searches the current path for instances of a file with the specified name,");
                    Console.Out.WriteLine("based on the value of the PATH environment variable.");
                    Console.Out.WriteLine();
                    Console.Out.WriteLine("If the specified filename does not include an extension, then the search will");
                    Console.Out.WriteLine("include all extensions from the PATHEXT environment variable (which typically");
                    Console.Out.WriteLine("includes executable extensions such as .exe and .bat).");
                    Console.Out.WriteLine();
                    Console.Out.WriteLine("Additionally, the * and ? wildcards are supported.");
                    Console.Out.WriteLine();
                    Console.Out.WriteLine("Usage:");  
					Console.Out.WriteLine();
					Console.Out.WriteLine("  pathfind [filename]");
					Console.Out.WriteLine();
					Console.Out.WriteLine("To view the value of the PATH environment variable, use:");
					Console.Out.WriteLine();
					Console.Out.WriteLine("  path");
					return (int)Results.Success;
				}

				//Get the filename from the first command-line argument.  Note that a file name with spaces
				//enclosed in double quotes is automatically handled for us.
				string filename = args[0];

				//Validate the filename for length and restricted characters
				if (!ValidateFilename(filename))
				{
					return (int)Results.Error;
				}

				//Get the path environment variable
				string path = Environment.GetEnvironmentVariable("path");
				if (path == null)
				{
					Console.Out.WriteLine("The PATH environment variable is undefined.");
					return (int)Results.Error;
				}

				//Parse apart the semicolon-separated directory names from the path environment variable.
				char[] pathSeparatorCharacter = new char[] {';'};
				List<string> pathDirectories = new List<string>(path.Split(pathSeparatorCharacter));

				//Search the current directory as well (if we haven't already, that is, if it isn't in the PATH),
                //to warn the user if the filename also appears in the current directory (which means that if 
                //they subsequently run the file from the command prompt, the first path copy won't run if the 
                //current directory isn't the first path directory).
				string currentDirectory = System.IO.Directory.GetCurrentDirectory();
                bool searchCurrentDirectory = false;
                bool matchFoundInCurrentDirectory = false;
                List<string> currentDirectoryDummyList = new List<string>(1);
                if (!DoesListOfFoldersContainFolder(pathDirectories, currentDirectory))
                {
                    currentDirectoryDummyList.Add(currentDirectory);
                    searchCurrentDirectory = true;
                }

                List<string> foldersFound = new List<string>();
                List<string> filenamesFound = new List<string>();

                //If no "." was specified in the filename, search the given filename plus all possible PATHEXT 
                //extensions.  (So, argument "test" searches "test.exe", "test.bat", ... whereas argument "test."
                //searches just "test." with no extension.)
                bool resultsFound = false;
                char[] skipPathExtSearchIfSpecified = new char[3] {'.', '*', '?'};
                if (filename.IndexOfAny(skipPathExtSearchIfSpecified) == -1)
                {
                    Console.WriteLine();
                    Console.WriteLine("(No extension specified; using PATHEXT environment variable extensions.)");

                    string pathext = Environment.GetEnvironmentVariable("pathext");
                    if (!String.IsNullOrEmpty(pathext))
                    {
                        string[] pathExtensions = pathext.Split(pathSeparatorCharacter);
                        foreach (string pathExtension in pathExtensions)
                        {
                            string filenameWithExtension = filename + pathExtension.ToLower();
                            Dictionary<string, List<string>> filenameMatchLocations = PerformSearch(filenameWithExtension, pathDirectories);
                            AddValuesToList(filenamesFound, new List<string>(filenameMatchLocations.Keys));
                            AddValuesToList(foldersFound, new List<List<string>>(filenameMatchLocations.Values));

                            OutputMainSearchResults(filenameMatchLocations);
                            resultsFound |= (filenameMatchLocations.Keys.Count > 0);

                            if (searchCurrentDirectory)
                            {
                                Dictionary<string, List<string>> currentDirectoryMatchLocations = PerformSearch(filenameWithExtension, currentDirectoryDummyList);
                                matchFoundInCurrentDirectory |= (currentDirectoryMatchLocations.Keys.Count > 0);
                            }
                        }
                    }
                }
                else  //No PATHEXT (although the specified filename may include * or ? wildcards)
                {
                    Dictionary<string, List<string>> filenameMatchLocations = PerformSearch(filename, pathDirectories);
                    OutputMainSearchResults(filenameMatchLocations);
                    resultsFound = (filenameMatchLocations.Keys.Count > 0);
                    AddValuesToList(filenamesFound, new List<string>(filenameMatchLocations.Keys));
                    AddValuesToList(foldersFound, new List<List<string>>(filenameMatchLocations.Values));

                    if (searchCurrentDirectory)
                    {
                        Dictionary<string, List<string>> currentDirectoryMatchLocations = PerformSearch(filename, currentDirectoryDummyList);
                        matchFoundInCurrentDirectory = (currentDirectoryMatchLocations.Keys.Count > 0);
                    }
                }

                if (!resultsFound)
                {
                    Console.Out.WriteLine();
                    Console.Out.WriteLine("No matching file was found on the PATH.");

                    if (matchFoundInCurrentDirectory)
                    {
                        Console.WriteLine();
                        Console.Out.WriteLine("Note: A matching file *is* present in the current directory. (The current");
                        Console.Out.WriteLine("directory is not on the PATH).");
                        return (int)Results.Success;
                    }

                    return (int)Results.NotFound;
                }

                if (foldersFound.Count >= 2 || filenamesFound.Count >= 2)
                {
                    Console.WriteLine();
                    Console.WriteLine(filenamesFound.Count + " total matching filename(s) found in " + foldersFound.Count + " PATH folder(s).");
                }

                if (matchFoundInCurrentDirectory)
                {
                    Console.WriteLine();
                    Console.Out.WriteLine("Caution: A matching file is also present in the current directory. (The current");
                    Console.Out.WriteLine("directory is not on the PATH).");
                }

				return (int)Results.Success;
			}
			catch (Exception ex)
			{
				Console.Out.WriteLine("An unexpected exception occurred:");
				Console.Out.WriteLine(ex.GetType().ToString() + ": " + ex.Message);
				return (int)Results.Error;
			}
		}

		/// <summary>
		/// Writes out the specified search results to the console.
		/// </summary>
		private static void OutputMainSearchResults(Dictionary<string, List<string>> filenameMatchLocations)
		{
            //Write out the results: Loop through the map of results and for each filename found, 
            //write the list of directories where it was found.
            foreach (string fileMatch in filenameMatchLocations.Keys)
            {
                //Add a blank line between filenames (and also at the beginning of the output)
                Console.WriteLine();

                Console.WriteLine(fileMatch + " is present in:");
                foreach (string pathMatch in filenameMatchLocations[fileMatch])
                {
                    Console.WriteLine("  " + pathMatch);
                }
            }
		}


        /// <summary>
        /// Searches for the specified filename in the specified set of directories, and returns the results.
        /// </summary>
        /// <param name="filename">The file to search for. Wildcards * and ? ARE supported. </param>
        /// <param name="pathDirectories">The directories to search in. </param>
        /// <returns>The number of results found. </returns>
        private static Dictionary<string, List<string>> PerformSearch(string filename, List<string> pathDirectories)
        {
            //Set up the container for the results.  Note: Even though this method takes only a single "filename"
            //parameter, we may end up finding results because the filename argument may contain wildcards.
            //Therefore we set up this container as a multi-map of filenames to (multiple) file paths.
            Dictionary<String, List<String>> filenameMatchLocations = new Dictionary<String, List<String>>();

            foreach (string directory in pathDirectories)
            {
                //Skip empty PATH entries (which might be caused by multiple consecutive ";" characters,
                //or by a trailing ";" character)
                if (directory.Trim().Length < 1)
                {
                    continue;
                }

                string fullySpecifiedPath = CreateFullySpecifiedPathName(directory, filename);

                string[] fullySpecifiedPathMatches = null;
                try
                {
                    //The Directory.GetFiles API takes care of wildcard matching (* and ?) for us.
                    fullySpecifiedPathMatches = System.IO.Directory.GetFiles(directory, filename);
                }
                catch (System.IO.DirectoryNotFoundException)
                {
                    //There might be a directory on the PATH that doesn't actually exist. Ignore this and continue.
                    continue;
                }

                foreach (string fullySpecifiedPathMatch in fullySpecifiedPathMatches)
                {
                    int lastBackslashIndex = fullySpecifiedPathMatch.LastIndexOf(@"\");
                    string fileMatch = fullySpecifiedPathMatch.Substring(lastBackslashIndex + 1);
                    string pathMatch = fullySpecifiedPathMatch.Substring(0, lastBackslashIndex + 1);

                    if (!filenameMatchLocations.ContainsKey(fileMatch))
                    {
                        filenameMatchLocations.Add(fileMatch, new List<string>());
                    }
                    filenameMatchLocations[fileMatch].Add(pathMatch);
                }
            }
            return filenameMatchLocations;
        }


		/// <summary>
		/// Given a fully specified directory name and file name, returns the fully specified 
		/// name of the file as though it were present in that directory.
		/// </summary>
		/// <param name="directory">The directory name.  The trailing backslash character is optional. </param>
		/// <param name="filename">The file name. </param>
		/// <returns>The fully specified file name. </returns>
		private static string CreateFullySpecifiedPathName(string directory, string filename)
		{
			string fullySpecifiedPath = directory;

			//Add the trailing backslash on the directory if it isn't already present.
			if (!directory.EndsWith(@"\"))
			{
				fullySpecifiedPath += @"\";
			}

			fullySpecifiedPath += filename;
			return fullySpecifiedPath;
		}

		/// <summary>
		/// Performs simple validation on the specified filename for length and invalid characters.
		/// Writes out an error message and returns false if the filename is too long, if the filename 
		/// contains invalid characters, or if the filename contains wildcard characters; returns 
		/// true otherwise.
		/// </summary>
		/// <param name="filename">The filename to validate. </param>
		/// <returns>True if validation was successful, false otherwise. </returns>
		private static bool ValidateFilename(string filename)
		{
			if (filename.Length > 255)
			{
                Console.Out.WriteLine();
				Console.Out.WriteLine("The specified file name is too long.  Please enter a file name that is 255");
				Console.Out.WriteLine("characters or less in length.");
				return false;
			}

			char[] restrictedChars = new char[] {'\\', '/', ':', '<', '>', '|'}; 
			if (filename.IndexOfAny(restrictedChars) != -1)
			{
                Console.Out.WriteLine();
				Console.Out.WriteLine(@"Please enter a filename that does not include these characters:  \ / : < > |");
				return false;
			}

			return true;
		}

        /// <summary>
        /// Returns true if the specified "folder" (a fully-specified path) is in the specified list of folders;
        /// false otherwise.  Accounts for trailing backslash either being present or not. Search is case-insensitive.
        /// </summary>
        private static bool DoesListOfFoldersContainFolder(List<string> folders, string folderToFind)
        {
            string folderToFindLowercase = folderToFind.ToLower();
            foreach (string folder in folders)
            {
                string folderLowercase = folder.ToLower();
                if (folderLowercase == folderToFindLowercase || folderLowercase + @"\" == folderToFindLowercase || folderLowercase == folderToFindLowercase + @"\")
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// To the specified list, adds any values from valuedToAdd not already in the list.
        /// </summary>
        private static void AddValuesToList(List<string> list, List<string> valuesToAdd)
        {
            foreach (string value in valuesToAdd)
            {
                if (!list.Contains(value))
                {
                    list.Add(value);
                }
            }
        }


        /// <summary>
        /// To the specified list, adds any values from valuedToAdd not already in the list.
        /// </summary>
        private static void AddValuesToList(List<string> list, List<List<string>> valuesToAdd)
        {
            foreach (List<string> listToAdd in valuesToAdd)
            {
                AddValuesToList(list, listToAdd);
            }
        }

	}
}

