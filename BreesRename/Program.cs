using System;
using System.IO;
using System.Linq;

namespace BreesRename
{
    public class Program
    {
        private bool debugMode;
        private string folder;
        private char[] replaceChars;
        private string replaceWith;
        private static char[] invalidChars = Path.GetInvalidFileNameChars();

        /// <summary>
        /// Builds the new name of the file. This assumes the filename has already been checked and does contain characters that need to be replaced.
        /// </summary>
        private string BuildNewFileName(string name)
        {
            var newName = replaceChars.Aggregate(name, (current, c) => current.Replace(c.ToString(), replaceWith));
            if (replaceChars.Contains('.'))
            {
                var lastIndex = newName.LastIndexOf(replaceWith);
                newName = newName.Substring(0, lastIndex) + "." + newName.Substring(lastIndex + 1);
            }

            return newName;
        }

        /// <summary>
        /// Checks to see if the file matches any characters that are to be replaced.
        /// </summary>
        private bool FileMatches(string file)
        {
            var renameablePartOfFileName = Path.GetFileNameWithoutExtension(file);
            if (string.IsNullOrWhiteSpace(renameablePartOfFileName)) return false;
            return replaceChars.Any(renameablePartOfFileName.Contains);
        }

        /// <summary>
        /// Outputs the arguments passed to this application.
        /// </summary>
        private void OutputArguments()
        {
            Console.WriteLine("Debug mode active - no files will be modified");
            if (replaceChars != null)
            {
                Console.WriteLine("Replace character mode active");
                Console.Write("    All the following characters will be replace: ");
                foreach (var replaceChar in replaceChars)
                {
                    Console.Write(replaceChar + " ");
                }
                Console.WriteLine();
                Console.WriteLine("    They will be replaced with: " +
                                  (replaceWith.Length == 0 ? "<Empty>" : replaceWith));
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Parses the arguments provided with the commandline.
        /// </summary>
        private bool ParseArguments(string[] args)
        {
            if (args.Length == 0)
            {
                ThrowError("Not enough arguments passed.");
                return false;
            }

            if (!ParseTargetFolder(args)) return false;

            var index = -1;
            var switches = args.Skip(1).ToArray();
            foreach (var arg in switches)
            {
                index++;
                var kvp = arg.Split(':');
                if (kvp.Length == 1)
                {
                    ParseSinglePartArgument(arg);
                }
                else
                {
                    if (!ParseMultiPartArgument(kvp, arg)) return false;
                }
            }

            return true;
        }

        private bool ParseMultiPartArgument(string[] kvp, string arg)
        {
            if (kvp.Length != 2)
            {
                ThrowError("Incorrect parameter: " + arg);
                return false;
            }

            switch (kvp[0])
            {
                case "/r":
                    replaceChars = kvp[1].ToCharArray();
                    replaceWith = string.Empty;
                    break;
                case "/rw":
                    replaceWith = kvp[1];
                    if (invalidChars.Any(c => replaceWith.Contains(c)))
                    {
                        ThrowError(string.Format("'{0}' is an invalid character and cannot be part of a filename.", replaceWith));
                        return false;
                    }

                    break;
            }
            return true;
        }

        private void ParseSinglePartArgument(string arg)
        {
            switch (arg)
            {
                case "/d":
                    debugMode = true;
                    break;
            }
        }

        private bool ParseTargetFolder(string[] args)
        {
            folder = args[0];
            if (!Directory.Exists(folder))
            {
                ThrowError("Folder does not exist.");
                return false;
            }

            if (File.Exists(folder))
            {
                ThrowError("The folder specified seems to be a file. It should be a folder.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Renames the file by replace characters in the <see cref="replaceChars"/> array with the <see cref="replaceWith"/> string.
        /// </summary>
        private void ReplaceCharsInFileName()
        {
            foreach (var file in Directory.GetFiles(folder))
            {
                if (!FileMatches(file))
                {
                    continue;
                }

                var fileInfo = new FileInfo(file);
                var newName = BuildNewFileName(fileInfo.Name);
                if (debugMode)
                {
                    Console.WriteLine("{0} --> {1}", file, Path.Combine(folder, newName));
                }
                else
                {
                    Console.WriteLine("{0} --> {1}", file, newName);
                    fileInfo.MoveTo(Path.Combine(folder, newName));
                }
            }
        }

        private void Run(string[] args)
        {
            Console.WriteLine("Brees Bulk Rename Utility 2015.");
            if (!ParseArguments(args))
            {
                return;
            }

            if (debugMode)
            {
                OutputArguments();
            }

            if (replaceChars != null)
            {
                ReplaceCharsInFileName();
            }

            Console.WriteLine("Finished.");
            if (debugMode)
            {
                Console.WriteLine("DEBUG MODE IS ACTIVE - NO FILE WAS MODIFIED.");
            }
        }

        private void ThrowError(string s)
        {
            Console.WriteLine(s);
        }

        public static void Main(string[] args)
        {
            new Program().Run(args);
        }
    }
}