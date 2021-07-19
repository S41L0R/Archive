using System;
using System.Collections.Generic;
using System.Text;

namespace Archive.Utils
{
    class CommandLineManager
    {
        public string[] Prompt(string promptText)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(promptText);
            Console.ForegroundColor = ConsoleColor.White;

            string line = Console.ReadLine();

            // We need to scan for quotation marks & spaces
            bool dQuote = false;
            bool quote = false;
            StringBuilder argBuilder = new StringBuilder();
            string[] args = new string[0];
            int index = 0;
            foreach (char c in line)
            {
                if (c.Equals('"'))
                {
                    dQuote = !dQuote;

                    if (index == line.Length - 1)
                    {
                        Array.Resize(ref args, args.Length + 1);
                        args[args.GetUpperBound(0)] = argBuilder.ToString();
                    }
                }
                else if (c.Equals("'"))
                {
                    quote = !quote;

                    if (index == line.Length - 1)
                    {
                        Array.Resize(ref args, args.Length + 1);
                        args[args.GetUpperBound(0)] = argBuilder.ToString();
                    }
                }
                else
                {
                    if (Char.IsWhiteSpace(c))
                    {
                        if (!dQuote && !quote)
                        {
                            Array.Resize(ref args, args.Length + 1);
                            args[args.GetUpperBound(0)] = argBuilder.ToString();

                            argBuilder.Clear();
                        }
                        else
                        {
                            argBuilder.Append(c);
                        }
                    }
                    else
                    {
                        argBuilder.Append(c);

                        if (index == line.Length - 1)
                        {
                            Array.Resize(ref args, args.Length + 1);
                            args[args.GetUpperBound(0)] = argBuilder.ToString();
                        }
                    }
                }
                index = index + 1;
            }
            return (args);
        }
    }
}
