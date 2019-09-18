using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using CommandLine;

namespace Linux.Cert {
  public static class Program {
    [Verb ("default", HelpText = "")]
    internal class defaultOptions {
      [Option ('d', "default", Required = false)]
      public string DefaultString { get; set; }
    }

    private static void NotParsedFunc (IEnumerable<Error> arg) {
      Console.WriteLine("Seed Project");
    }

    public static void Main (string[] args) {
      CommandLine.Parser.Default.ParseArguments<defaultOptions> (args)
        .WithParsed<defaultOptions> (options => {})
        .WithNotParsed (NotParsedFunc);
    }
  }
}
