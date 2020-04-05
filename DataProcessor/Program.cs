using static System.Console;
using System.IO;

namespace DataProcessor
{
    class Program
    {
        //Usage: (Paths should be surrounded by "" if there's any space in between viz. Computer Science here)
        //1. --file "E:\Computer Science\C#\TempFilesUsedForExercises\journal.txt" (or basically your path here)
        //2. --dir "E:\Computer Science\C#\TempFilesUsedForExercises" TEXT (add multiple text files in this dir)
        static void Main(string[] args)
        {
            WriteLine("Passing command line options");

            var command = args[0];

            if (command == "--file")
            {
                var filePath = args[1];
                WriteLine($"Single file {filePath} selected");
                ProcessSingleFile(filePath);
            }
            else if (command == "--dir")
            {
                var directoryPath = args[1];
                var fileType = args[2];
                WriteLine($"Directory {directoryPath} selected for {fileType} files");
                ProcessDirectory(directoryPath, fileType);
            }
            else
            {
                WriteLine("Invalid command line arguments");
            }

            WriteLine("Press enter to quit");
            ReadLine();
        }

        private static void ProcessSingleFile(string filePath)
        {
            var fileProcessor = new FileProcessor(filePath);
            fileProcessor.Process();
        }

        private static void ProcessDirectory(string directoryPath, string fileType)
        {
            //var allFiles = Directory.GetFiles(directoryPath);

            switch(fileType)
            {
                case "TEXT":
                    string[] textFiles = Directory.GetFiles(directoryPath, "*.txt");
                    foreach(var textFilePath in textFiles)
                    {
                        var fileProcessor = new FileProcessor(textFilePath);
                        fileProcessor.Process();
                    }
                    break;
                default:
                    WriteLine($"ERROR: {fileType} is not supported");
                    return;
            }
        }
    }
}
