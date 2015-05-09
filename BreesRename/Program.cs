using System;
using System.IO;
using System.Linq;

namespace BreesRename
{
    public class Program
    {
        private static readonly char[] InvalidChars = Path.GetInvalidFileNameChars();
        private bool debugMode;
        private string filter;
        private string folder;
        private char[] replaceChars;
        private string replaceWith;

        /// <summary>
        ///     Builds the new name of the file. This assumes the filename has already been checked and does contain characters
        ///     that need to be replaced.
        /// </summary>
        private string BuildNewFileName(string name)
        {
            var newName = this.replaceChars.Aggregate(name,
                (current, c) => current.Replace(c.ToString(), this.replaceWith));
            if (this.replaceChars.Contains('.'))
            {
                var lastIndex = newName.LastIndexOf(this.replaceWith);
                newName = newName.Substring(0, lastIndex) + "." + newName.Substring(lastIndex + 1);
            }

            return newName;
        }

        /// <summary>
        ///     Checks to see if the file matches any characters that are to be replaced.
        /// </summary>
        private bool FileMatches(string file)
        {
            var renameablePartOfFileName = Path.GetFileNameWithoutExtension(file);
            if (string.IsNullOrWhiteSpace(renameablePartOfFileName)) return false;
            return this.replaceChars.Any(renameablePartOfFileName.Contains);
        }

        /// <summary>
        ///     Outputs the arguments passed to this application.
        /// </summary>
        private void OutputArguments()
        {
            if (this.debugMode) Console.WriteLine("Debug mode active - no files will be modified");

            if (this.replaceChars != null)
            {
                Console.WriteLine("Replace character mode active");
                Console.Write("    All the following characters will be replace: ");
                foreach (var replaceChar in this.replaceChars)
                {
                    Console.Write(replaceChar + " ");
                }
                Console.WriteLine();
                Console.WriteLine("    They will be replaced with: " +
                                  (this.replaceWith.Length == 0 ? "<Empty>" : this.replaceWith));
                Console.WriteLine();
            }
        }

        /// <summary>
        ///     Parses the arguments provided with the commandline.
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

            if (string.IsNullOrWhiteSpace(this.filter)) this.filter = "*.*";
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
                    this.replaceChars = kvp[1].ToCharArray();
                    this.replaceWith = string.Empty;
                    break;
                case "/rw":
                    this.replaceWith = kvp[1];
                    if (InvalidChars.Any(c => this.replaceWith.Contains(c)))
                    {
                        ThrowError(string.Format("'{0}' is an invalid character and cannot be part of a filename.",
                            this.replaceWith));
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
                    this.debugMode = true;
                    break;
            }
        }

        private bool ParseTargetFolder(string[] args)
        {
            this.folder = args[0];
            if (!Directory.Exists(this.folder))
            {
                var index = this.folder.LastIndexOf('\\');
                if (index > 0)
                {
                    this.filter = this.folder.Substring(index + 1);
                    this.folder = this.folder.Replace(this.filter, string.Empty);
                    if (!Directory.Exists(this.folder))
                    {
                        ThrowError("Folder does not exist: " + this.folder);
                        return false;
                    }
                }
                else
                {
                    ThrowError("Folder does not exist: " + this.folder);
                    return false;
                }
            }

            if (File.Exists(this.folder))
            {
                ThrowError("The folder specified seems to be a file. It should be a folder. " + this.folder);
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Renames the file by replace characters in the <see cref="replaceChars" /> array with the <see cref="replaceWith" />
        ///     string.
        /// </summary>
        private void ReplaceCharsInFileName()
        {
            foreach (var file in Directory.GetFiles(this.folder, this.filter))
            {
                if (!FileMatches(file))
                {
                    continue;
                }

                var fileInfo = new FileInfo(file);
                var newName = BuildNewFileName(fileInfo.Name);
                if (this.debugMode)
                {
                    Console.WriteLine("{0} --> {1}", file, Path.Combine(this.folder, newName));
                }
                else
                {
                    Console.WriteLine("{0} --> {1}", file, newName);
                    fileInfo.MoveTo(Path.Combine(this.folder, newName));
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

            if (this.debugMode)
            {
                OutputArguments();
            }

            if (this.replaceChars != null)
            {
                ReplaceCharsInFileName();
            }

            Console.WriteLine("Finished.");
            if (this.debugMode)
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