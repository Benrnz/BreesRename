using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BreesRename
{
    public class Program
    {
        private static readonly char[] InvalidChars = Path.GetInvalidFileNameChars();
        private const string Title = "Brees Bulk Rename Utility 2015.";
        private StringBuilder errors = new StringBuilder();
        private bool debugMode;
        private string filter;
        private string folder;
        private char[] replaceChars;
        private string replaceWith;
        private Regex truncateAtRegex;
        private bool quietMode;

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
            if (this.quietMode) return;
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

            if (this.truncateAtRegex != null)
            {
                Console.WriteLine("Truncate mode is active");
                Console.WriteLine("    The following regex will be used to match and truncate after the match.");
                Console.WriteLine("    " + this.truncateAtRegex.ToString());
            }
        }

        /// <summary>
        ///     Parses the arguments provided with the commandline.
        /// </summary>
        private bool ParseArguments(string[] args)
        {
            if (args.Length == 0)
            {
                SetError("Not enough arguments passed.");
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
                SetError("Incorrect parameter: " + arg);
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
                        SetError(string.Format("'{0}' is an invalid character and cannot be part of a filename.",
                            this.replaceWith));
                        return false;
                    }

                    break;
                case "/t":
                    this.truncateAtRegex = new Regex(kvp[1], RegexOptions.Singleline);
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
                case "/q":
                    this.quietMode = true;
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
                        SetError("Folder does not exist: " + this.folder);
                        return false;
                    }
                }
                else
                {
                    SetError("Folder does not exist: " + this.folder);
                    return false;
                }
            }

            if (File.Exists(this.folder))
            {
                SetError("The folder specified seems to be a file. It should be a folder. " + this.folder);
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Renames the file by replace characters in the <see cref="replaceChars" /> array with the <see cref="replaceWith" />
        ///     string.
        /// </summary>
        private void ReplaceCharsInFileName(string fileName)
        {
            if (!FileMatches(fileName))
            {
                return;
            }

            var newName = this.replaceChars.Aggregate(fileName, (current, c) => current.Replace(c.ToString(), this.replaceWith));
            if (this.replaceChars.Contains('.'))
            {
                var lastIndex = newName.LastIndexOf(this.replaceWith);
                newName = newName.Substring(0, lastIndex) + "." + newName.Substring(lastIndex + 1);
            }

            RenameFile(fileName, newName);
        }

        private void RenameFile(string oldName, string newName)
        {
            var fileInfo = new FileInfo(oldName);
            if (!fileInfo.Exists) return;
            if (this.debugMode)
            {
                Console.WriteLine("{0} --> {1}", fileInfo.FullName, Path.Combine(this.folder, newName));
            }
            else
            {
                Console.WriteLine("{0} --> {1}", fileInfo.FullName, newName);
                fileInfo.MoveTo(Path.Combine(this.folder, newName));
            }
        }

        private void Run(string[] args)
        {
            var parseSuccess = ParseArguments(args);
            if (!parseSuccess)
            {
                Console.WriteLine(Title);
                Console.WriteLine(this.errors);
                return;
            }

            OutputArguments();

            foreach (var file in Directory.GetFiles(this.folder, this.filter))
            {
                if (this.truncateAtRegex != null)
                {
                    TruncateFileNameAtRegex(file);
                }

                if (this.replaceChars != null)
                {
                    ReplaceCharsInFileName(file);
                }
            }

            if (this.errors.Length > 0)
            {
                Console.WriteLine(this.errors);
            }

            if (!this.quietMode)
            {
                Console.WriteLine("Finished.");
                if (this.debugMode)
                {
                    Console.WriteLine("DEBUG MODE IS ACTIVE - NO FILE WAS MODIFIED.");
                }
            }
        }

        /// <summary>
        /// Truncates the file name at the matched regex.
        /// </summary>
        private void TruncateFileNameAtRegex(string fileName)
        {
            var match = this.truncateAtRegex.Match(fileName);
            if (match.Success)
            {
                var index = match.Index + match.Length;
                var newName = fileName.Substring(0, index) + Path.GetExtension(fileName);
                if (newName != fileName) RenameFile(fileName, newName);
            }
        }

        private void SetError(string s)
        {
            this.errors.AppendLine(s);
        }

        public static void Main(string[] args)
        {
            new Program().Run(args);
        }
    }
}