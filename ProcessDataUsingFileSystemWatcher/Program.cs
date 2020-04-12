using static System.Console;
using System.IO;
using System.Collections.Concurrent;
using System.Threading;
using System.Runtime.Caching;
using System;

namespace ProcessDataUsingFileSystemWatcher
{
    //Usage:
    // Passing just the dir to watch in cli: "E:\Computer Science\C#\TempFilesUsedForExercises\filesystemwatcher" 1
    // Passing 1/2/3 as enum to run application in three different conditions
    // run the console app, and then try to put any file inside it (e.g. journal.txt) to see the events fired
    // Similarly rename/modify/delete the file to see different events being fired
    class Program
    {
        private static int _applicationRunningMethod;

        //As it is a limitation of FileSystemWatcher that it fires duplicate event sometimes, hence we're using ConcurrentDictionary to avoid this
        private static ConcurrentDictionary<string, string> FilesToProcess =
            new ConcurrentDictionary<string, string>();

        //Getting the default instance of MemoryCache
        private static MemoryCache FilesToProcessUsingCache = MemoryCache.Default;
        
        static void Main(string[] args)
        {
            WriteLine("Passing command line options");

            var directoryToWatch = args[0];
            _applicationRunningMethod = int.Parse(args[1]);

            if (!Directory.Exists(directoryToWatch))
            {
                WriteLine($"ERROR: {directoryToWatch} does not exist");
            }
            else
            {
                WriteLine($"Watching directory {directoryToWatch} for changes");

                ProcessExisitingFiles(directoryToWatch);

                //As FileSystemWatcher class implements IDisposable, hence creating inside using statement
                using (var inputFileWatcher = new FileSystemWatcher(directoryToWatch))
                {
                    if (_applicationRunningMethod == (int)ApplicationRunningMethod.UsingConcurrentDictionary)
                    {
                        using (var timer = new Timer(ProcessFiles, null, 0, 1000)) //Timer calling ProcessFiles() every 1000ms
                        {
                            CommonInstructionsToExecute(inputFileWatcher);
                        }
                    }
                    else
                    {
                        CommonInstructionsToExecute(inputFileWatcher);
                    }
                }
            }
        }

        public static void CommonInstructionsToExecute(FileSystemWatcher inputFileWatcher)
        {
            inputFileWatcher.IncludeSubdirectories = false;
            inputFileWatcher.InternalBufferSize = 32768; //32 KB
            inputFileWatcher.Filter = "*.*";
            inputFileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;

            //Creating event handlers for the file system events that we're interested in
            inputFileWatcher.Created += FileCreated;
            inputFileWatcher.Changed += FileChanged;
            inputFileWatcher.Deleted += FileDeleted;
            inputFileWatcher.Renamed += FileRenamed;
            inputFileWatcher.Error += WatchError;

            //Have to set this default false property to true to actually raise the above events
            inputFileWatcher.EnableRaisingEvents = true;

            WriteLine("Press enter to quit");
            ReadLine();
        }

        //Creating event handler methods
        public static void FileCreated(object sender, FileSystemEventArgs e)
        {
            WriteLine($"File created: {e.Name} - type: {e.ChangeType}");

            if (_applicationRunningMethod == (int)ApplicationRunningMethod.Normally)
            {
                var fileProcessor = new FileProcessor(e.FullPath);
                fileProcessor.Process();
            }
            else if(_applicationRunningMethod == (int)ApplicationRunningMethod.UsingConcurrentDictionary)
            {
                //Hence instead of calling the Process method directly like above, we're using TryAdd method of concurrent dictionary
                //and it will only add the event if there's not one already present
                FilesToProcess.TryAdd(e.FullPath, e.FullPath);
            }
            else
            {
                AddToCache(e.FullPath);
            }
        }

        public static void FileChanged(object sender, FileSystemEventArgs e)
        {
            WriteLine($"File changed: {e.Name} - type: {e.ChangeType}");

            if (_applicationRunningMethod == (int)ApplicationRunningMethod.Normally)
            {
                var fileProcessor = new FileProcessor(e.FullPath);
                fileProcessor.Process();
            }
            else if (_applicationRunningMethod == (int)ApplicationRunningMethod.UsingConcurrentDictionary)
            {
                //Hence instead of calling the Process method directly like above, we're using TryAdd method of concurrent dictionary
                //and it will only add the event if there's not one already present
                FilesToProcess.TryAdd(e.FullPath, e.FullPath);
            }
            else
            {
                AddToCache(e.FullPath);
            }
        }

        //All the below event handler methods can be implemented same as above
        public static void FileDeleted(object sender, FileSystemEventArgs e)
        {
            WriteLine($"File deleted: {e.Name} - type: {e.ChangeType}");
        }

        public static void FileRenamed(object sender, RenamedEventArgs e)
        {
            WriteLine($"File renamed: {e.OldName} to {e.Name} - type: {e.ChangeType}");
        }

        public static void WatchError(object sender, ErrorEventArgs e)
        {
            WriteLine($"ERROR: file system watching may no longer be active: {e.GetException()}");
        }

        //All the methods mentioned in Enum processes the file changes when the app is running
        // but if we want to process a file which is already present before running this app, then we need to do this
        private static void ProcessExisitingFiles(string inputDirectory)
        {
            WriteLine($"Checking {inputDirectory} for existing files");

            foreach (var filePath in Directory.EnumerateFiles(inputDirectory))
            {
                WriteLine($" - Found {filePath}");
                AddToCache(filePath);
            }
        }

        #region Using ConcurrentDictionary
        //ProcessFiles method called every 1 sec
        //Duplicate events are generated but are not getting executed in this case
        //However it may happen that the events gets duplicated before this function is even called (that in less than 1000ms)
        //Hence we can use MemoryCache as a workaround
        public static void ProcessFiles(object stateInfo)
        {
            foreach(var fileName in FilesToProcess.Keys)
            {
                // as we don't need the out param, hence out _
                if (FilesToProcess.TryRemove(fileName, out _))
                {
                    var fileProcessor = new FileProcessor(fileName);
                    fileProcessor.Process();
                }
            }
        }
        #endregion

        #region For memory caching
        private static void AddToCache(string fullPath)
        {
            //Initializing a new item in the memory cache
            var item = new CacheItem(fullPath, fullPath);

            var policy = new CacheItemPolicy
            {
                RemovedCallback = ProcessFile, //what happens after an item is removed from the cache
                SlidingExpiration = TimeSpan.FromSeconds(2) //item will be removed from the cache after 2 secs if not accessed
            };

            //Adding to the cache (Add method won't take duplicate key, but if passed, it updates SlidingExpiration property
            FilesToProcessUsingCache.Add(item, policy);
        }

        //Called when a cache item expires and item not accessed till two seconds
        //Note: This method also can raise exceptions if there are too many operations and multiple two operations are changing
        //one file from diffrent threads
        private static void ProcessFile(CacheEntryRemovedArguments args)
        {
            WriteLine($"Cache item removed: {args.CacheItem.Key} becayse {args.RemovedReason}");

            if (args.RemovedReason == CacheEntryRemovedReason.Expired)
            {
                var fileProcessor = new FileProcessor(args.CacheItem.Key);
                fileProcessor.Process();
            }
            else
            {
                WriteLine($"Warning: {args.CacheItem.Key} was removed unexpectedly and may not");
            }
        }
        #endregion
    }
}
