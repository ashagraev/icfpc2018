using System;

namespace TraceReaderTool
{
    using System.IO;

    using Solution;

    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine("Usage: TRACE_FILE");
                Environment.Exit(1);
            }
            var bytes = File.ReadAllBytes(args[0]);
            var commands = TraceReader.Read(bytes);
            foreach (var cmd in commands)
            {
                Console.WriteLine(cmd);
            }
        }
    }
}