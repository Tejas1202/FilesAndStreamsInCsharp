using static System.Console;
using System.IO;
using System;

namespace DataProcessor
{
    internal class FileProcessor
    {
        private static readonly string _backupDirectoryName = "backup";
        private static readonly string _inProgressDirectoryName = "processing";
        private static readonly string _completedDirectoryName = "complete";

        public string InputFilePath { get; }

        public FileProcessor(string filePath)
        {
            InputFilePath = filePath;
        }

        public void Process()
        {
            WriteLine($"Begin process of {InputFilePath}");

            //Check if file exists
            if (!File.Exists(InputFilePath))
            {
                WriteLine($"Error: file {InputFilePath} does not exist.");
                return;
            }

            //If we wanted to go two levels above, then do Parent.Parent
            string rootDirectoryPath = new DirectoryInfo(InputFilePath).Parent.FullName;
            WriteLine($"Root data path is {rootDirectoryPath}");

            //Making a backup of the file that we want to process
            string inputFileDirectoryPath = Path.GetDirectoryName(InputFilePath);
            string backupDirectoryPath = Path.Combine(rootDirectoryPath, _backupDirectoryName);

            if (!Directory.Exists(backupDirectoryPath))
            {
                WriteLine($"Creating {backupDirectoryPath}");
                Directory.CreateDirectory(backupDirectoryPath);
            }

            //Copy file to backup dir
            string inputFileName = Path.GetFileName(InputFilePath);
            string backupFilePath = Path.Combine(backupDirectoryPath, inputFileName);
            WriteLine($"Copying {InputFilePath} to {backupFilePath}");
            //If we don't specify override true in overload, then it throws exception if file already exists
            File.Copy(InputFilePath, backupFilePath, true);

            //Move to in progress dir
            Directory.CreateDirectory(Path.Combine(rootDirectoryPath, _inProgressDirectoryName));
            string inProgressFilePath = Path.Combine(rootDirectoryPath, _inProgressDirectoryName, inputFileName);
            //As File.Move method doesn't have an override option, so it will throw exception if file already exists, hence handling here
            if (File.Exists(inProgressFilePath))
            {
                WriteLine($"ERROR: a file with the name {inProgressFilePath} is already processed.");
                return;
            }
            WriteLine($"Moving {InputFilePath} to {inProgressFilePath}");
            File.Move(InputFilePath, inProgressFilePath);

            //Determine type of file
            string extension = Path.GetExtension(inProgressFilePath);
            switch(extension)
            {
                case ".txt":
                    ProcessTextFile(inProgressFilePath);
                    break;
                default:
                    WriteLine($"{extension} is an unsupported file type.");
                    break;
            }

            //Moving the in progress file to completed file
            string completedDirectoryPath = Path.Combine(rootDirectoryPath, _completedDirectoryName);
            Directory.CreateDirectory(completedDirectoryPath);
            WriteLine($"Moving {inProgressFilePath} to {completedDirectoryPath}");
            var completedFileName =
                $"{Path.GetFileNameWithoutExtension(InputFilePath)}-{Guid.NewGuid()}{extension}";
            //completedFileName = Path.ChangeExtension(completedFileName, ".complete"); //If we want to change the extension
            var completedFilePath = Path.Combine(completedDirectoryPath, completedFileName);
            File.Move(inProgressFilePath, completedFilePath);

            //Deleting directory
            string inProgressDirectoryPath = Path.GetDirectoryName(inProgressFilePath);
            Directory.Delete(inProgressDirectoryPath, true); //If true not specified, then method throws exception if dir isn't empty
        }

        private void ProcessTextFile(string inProgressFilePath)
        {
            WriteLine($"Processing text file {inProgressFilePath}");
            //Read in and Process
        }
    }
}