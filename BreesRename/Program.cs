using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BreesRename
{
    public class Program
    {
        private const string Title = "Brees Bulk Rename Utility 2015.";
        private static readonly char[] InvalidChars = Path.GetInvalidFileNameChars();
        private readonly StringBuilder errors = new StringBuilder();
        private readonly Regex tvShowRegex = new Regex(@"^[Ss](\d{2})[Ee](\d{2})(.*)");
        private bool debugMode;
        private string filter;
        private string folder;
        private bool properCase;
        private bool quietMode;
        private char[] replaceChars;
        private string replaceWith;
        private Regex truncateAtRegex;
        private Regex removeRegex;

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
                Console.WriteLine("    " + this.truncateAtRegex);
            }

            if (this.removeRegex != null)
            {
                Console.WriteLine("Remove regex matches mode is active");
                Console.WriteLine("    The following regex will be used to match and and matched text will be removed.");
                Console.WriteLine("    " + this.removeRegex);
            }

            if (this.properCase)
            {
                Console.WriteLine("Proper Case mode is active. This applies only to space and . seperated words.");
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

                case "/x":
                    this.removeRegex = new Regex(kvp[1], RegexOptions.Singleline);
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
                case "/p":
                    this.properCase = true;
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

        private string ProperCaseWordsInFileName(string file)
        {
            if (!this.properCase) return file;
            var fileName = Path.GetFileName(file);
            var folderName = Path.GetDirectoryName(file);
            var extension = Path.GetExtension(file);
            Debug.Assert(!string.IsNullOrWhiteSpace(folderName));
            Debug.Assert(!string.IsNullOrWhiteSpace(fileName));
            Debug.Assert(!string.IsNullOrWhiteSpace(extension));

            extension = extension.Replace(".", string.Empty);
            var split = fileName.Split('.', ' ');
            var textInfo = new CultureInfo("en-US", false).TextInfo;
            var builder = new StringBuilder();
            foreach (var word in split)
            {
                var properCaseWord = textInfo.ToTitleCase(word);
                var index = fileName.IndexOf(word, StringComparison.OrdinalIgnoreCase) + word.Length;
                if (extension != word)
                {
                    var matches = this.tvShowRegex.Match(properCaseWord);
                    if (matches.Success)
                    {
                        // Found a Tv show season and episode reference. Make sure S and E are upper case.
                        properCaseWord = string.Format("S{0}E{1}{2}", matches.Groups[1].Value, matches.Groups[2].Value,
                            matches.Groups[3].Value);
                    }

                    builder.Append(properCaseWord);
                    builder.Append(fileName.Substring(index, 1));
                }
            }

            var newName = string.Format("{0}.{1}", Path.Combine(folderName, builder.ToString().TrimEnd()), extension);
            RenameFile(file, newName);
            return newName;
        }

        private void RenameFile(string oldName, string newName)
        {
            var fileInfo = new FileInfo(oldName);
            if (!fileInfo.Exists) return; //Unexpected error, it should have been checked previously.

            if (File.Exists(newName) && string.Compare(oldName, newName, StringComparison.OrdinalIgnoreCase) != 0)
            {
                SetError("Unable to rename '{0}' proposed new name '{1}' already exists.", Path.GetFileName(oldName),
                    Path.GetFileName(newName));
                return;
            }

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

        /// <summary>
        ///     Renames the file by replace characters in the <see cref="replaceChars" /> array with the <see cref="replaceWith" />
        ///     string.
        /// </summary>
        private string ReplaceCharsInFileName(string fileName)
        {
            if (!FileMatches(fileName))
            {
                return fileName;
            }

            var newName = this.replaceChars.Aggregate(fileName,
                (current, c) => current.Replace(c.ToString(), this.replaceWith));
            if (this.replaceChars.Contains('.'))
            {
                var lastIndex = newName.LastIndexOf(this.replaceWith);
                newName = newName.Substring(0, lastIndex) + "." + newName.Substring(lastIndex + 1);
            }

            RenameFile(fileName, newName);
            return newName;
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
                string newFileName = file;
                if (this.truncateAtRegex != null)
                {
                    newFileName = TruncateFileNameAtRegex(newFileName);
                }

                if (this.removeRegex != null)
                {
                    newFileName = RemovedMatchedText(newFileName);
                }

                if (this.replaceChars != null)
                {
                    newFileName = ReplaceCharsInFileName(newFileName);
                }

                if (this.properCase)
                {
                    newFileName = ProperCaseWordsInFileName(newFileName);
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

        private void SetError(string s)
        {
            this.errors.AppendLine(s);
        }

        private void SetError(string s, params object[] args)
        {
            this.errors.AppendLine(string.Format(s, args));
        }

        /// <summary>
        ///     Truncates the file name at the matched regex.
        /// </summary>
        private string TruncateFileNameAtRegex(string fileName)
        {
            var match = this.truncateAtRegex.Match(fileName);
            if (match.Success)
            {
                var index = match.Index + match.Length;
                var newName = fileName.Substring(0, index) + Path.GetExtension(fileName);
                if (newName != fileName) RenameFile(fileName, newName);
                return newName;
            }

            return fileName;
        }

        /// <summary>
        ///     Removes any text matched by the regex.
        /// </summary>
        private string RemovedMatchedText(string fileName)
        {
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            var match = this.removeRegex.Match(fileNameWithoutExtension);
            if (match.Success)
            {
                var newName = this.removeRegex.Replace(fileNameWithoutExtension, string.Empty);
                newName += extension;
                if (newName != fileName) RenameFile(fileName, newName);
                return newName;
            }

            return fileName;
        }

        public static void Main(string[] args)
        {
            new Program().Run(args);
        }
    }
}