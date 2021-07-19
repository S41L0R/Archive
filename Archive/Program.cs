using System;

namespace Archive
{
    class Program
    {
        static void Main(string[] startupArgs)
        {
            Core.Archive archive = null;
            if (startupArgs.Length == 1)
            {
                archive = new Core.Archive(startupArgs[0]);
            }
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(@"
|Avaliable Commands:                                                                                                  |
|    openArch [filepath]: Opens or creates an archive file. Same as calling the program with filepath as an arg       |                                           
|    addDir [filepath] [alias] [compression level: none|fast|good]: Adds a dir to the archive with the supplied alias |
|    exDir [alias] [filepath]: Extracts a dir from the archive via the supplied alias                                 |
|    verify: Checks the archive file against the hash.                                                                |
|    flush: Makes sure all changes to the file are saved. They should be anyway.                                      |
");
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
            Utils.CommandLineManager commandLineManager = new Utils.CommandLineManager();
            while (true)
            {
                string[] args = commandLineManager.Prompt("Option:");
                switch (args[0].ToLower())
                {
                    case "openarch":
                        archive = new Core.Archive(args[1]);
                        break;
                    case "adddir":
                        if (archive == null)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("You don't have an archive selected! Hint: Use openArch.");
                            Console.ForegroundColor = ConsoleColor.White;
                            break;
                        }
                        if (args.Length != 4)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Not enough arguments.");
                            Console.WriteLine("addDir [filepath] [alias]: Adds a dir to the archive with the supplied alias");
                            Console.ForegroundColor = ConsoleColor.White;
                            break;
                        }
                        switch (args[3].ToLower())
                        {
                            case "fast":
                                archive.AddFolder(args[1], args[2], true, System.IO.Compression.CompressionLevel.Fastest);
                                break;
                            case "good":
                                archive.AddFolder(args[1], args[2], true, System.IO.Compression.CompressionLevel.Optimal);
                                break;
                            case "none":
                                archive.AddFolder(args[1], args[2], false, System.IO.Compression.CompressionLevel.Optimal);
                                break;
                            default:
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Specified compression level not an option");
                                Console.WriteLine("addDir [filepath] [alias] [compression level: none|fast|good] : Adds a dir to the archive with the supplied alias");
                                Console.ForegroundColor = ConsoleColor.White;
                                break;
                        }
                        break;
                    case "exdir":
                        if (archive == null)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("You don't have an archive selected! Hint: Use openArch.");
                            Console.ForegroundColor = ConsoleColor.White;
                            break;
                        }
                        if (args.Length != 3)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Not enough arguments.");
                            Console.WriteLine("exDir [alias] [filepath]: Extracts a dir from the archive via the supplied alias");
                            Console.ForegroundColor = ConsoleColor.White;
                            break;
                        }
                        archive.ExtractFolder(args[1], args[2]);
                        break;
                    case "verify":
                        if (archive == null)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("You don't have an archive selected! Hint: Use openArch.");
                            Console.ForegroundColor = ConsoleColor.White;
                            break;
                        }
                        archive.VerifyHash();
                        break;
                    case "flush":
                        if (archive == null)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("You don't have an archive selected! Hint: Use openArch.");
                            Console.ForegroundColor = ConsoleColor.White;
                            break;
                        }
                        archive.Flush();
                        break;
                    default:
                        Console.WriteLine($"{args[0]} is not a valid option.");
                        break;
                }
            }
        }
    }
}
