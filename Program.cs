using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using GlobExpressions;
using PgpCore;

namespace Dir.Enc
{
  public static class Program
  {
    private static readonly PGP Pgp = new PGP();

    private static string _privateKeyPath = "";
    private static string _publicKeyPath = "";

    [Verb("pgp", HelpText = "Create a PGP key")]
    internal class pgpOptions
    {
      [Option('p', "path", Required = false, Default = "")]
      public string KeyPath { get; set; }
    }

    [Verb("enc", HelpText = "Encrypt directories with matched patterns")]
    internal class encryptOptions
    {
      [Option('p', "path", Required = false, Default = "")]
      public string KeyPath { get; set; }
    }

    [Verb("dec", HelpText = "Decrypt directories with matched patterns")]
    internal class decryptOptions
    {
      [Option('p', "path", Required = false, Default = "")]
      public string KeyPath { get; set; }
    }

    private static void NotParsedFunc(IEnumerable<Error> arg)
    {
#if DEBUG
      throw new Exception(arg.First().Tag.ToString());
#endif
    }

    public static void Main(string[] args)
    {
      // No args passed
      if (args.Length == 0)
      {
        UpdateKeyPath();
        DecryptPatternMatchedDirectories();
      }
      else
      {
        Parser.Default.ParseArguments<pgpOptions, encryptOptions, decryptOptions>(args)
          .WithParsed<pgpOptions>(options =>
          {
            UpdateKeyPath(options.KeyPath, false);
            CreatePgpKey();
          })
          .WithParsed<encryptOptions>(options =>
          {
            UpdateKeyPath(options.KeyPath);
            EncryptPatternMatchedDirectories();
          })
          .WithParsed<decryptOptions>(options =>
          {
            UpdateKeyPath(options.KeyPath);
            DecryptPatternMatchedDirectories();
          })
          .WithNotParsed(NotParsedFunc);
      }
    }

    private static void CreatePgpKey()
    {
      if (File.Exists(_publicKeyPath) || File.Exists(_privateKeyPath))
      {
        Console.WriteLine($"Key files exist: [{_publicKeyPath}] and [{_privateKeyPath}]. Please move/remove them before key creation.");
        return;
      }

      Pgp.GenerateKey(_publicKeyPath, _privateKeyPath, "default", "");

      Console.WriteLine("PGP key successfully created.");
    }

    private static void EncryptPatternMatchedDirectories()
    {
      Console.WriteLine("Listing matched pattern directories...");
      Console.WriteLine("--------------------------------------");
      var matchedDirectories = FindPatternMatchedDirectories();
      foreach (var directory in matchedDirectories)
      {
        foreach (var file in GetDirectoryFileList(directory.Path))
        {
          var inputFilePath = Path.Combine(directory.Path, file);
          var outputFilePath = Path.Combine(directory.EncryptedPath, file);

          Console.WriteLine($"[Encrypting] [{inputFilePath}] -> [{outputFilePath}]");

          EncryptFileWithPgpKey(inputFilePath, outputFilePath);
        }
      }
    }

    private static void DecryptPatternMatchedDirectories()
    {
      Console.WriteLine("Listing matched pattern directories...");
      Console.WriteLine("---------------------------------------");
      var matchedDirectories = FindPatternMatchedDirectories();
      foreach (var directory in matchedDirectories)
      {
        foreach (var file in GetDirectoryFileList(directory.EncryptedPath))
        {
          var inputFilePath = Path.Combine(directory.EncryptedPath, file);
          var outputFilePath = Path.Combine(directory.Path, file);

          Console.WriteLine($"[Decrypting] [{inputFilePath}] -> [{outputFilePath}]");

          DecryptFileWithPgpKey(inputFilePath, outputFilePath);
        }
      }
    }

    private static IEnumerable<BiDirectory> FindPatternMatchedDirectories()
    {
      return Glob.Directories(Directory.GetCurrentDirectory(), "**/*.enc")
        .Select(path => new BiDirectory(path));
    }

    private static IEnumerable<string> GetDirectoryFileList(string path)
    {
      return Glob.Files(path, "**/*");
    }

    private static void EncryptFileWithPgpKey(string inputFilePath, string outputFilePath)
    {
      EnsurePathParentDirectoryExists(outputFilePath);

      Pgp.EncryptFile(inputFilePath, outputFilePath, _publicKeyPath, true, true);
    }

    private static void DecryptFileWithPgpKey(string inputFilePath, string outputFilePath)
    {
      EnsurePathParentDirectoryExists(outputFilePath);

      Pgp.DecryptFile(inputFilePath, outputFilePath, _privateKeyPath, "");
    }

    private static string GetDefaultKeyPath()
    {
      return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnet-dir-enc");
    }

    private static void EnsurePathParentDirectoryExists(string path)
    {
      if (!Directory.Exists(Path.GetDirectoryName(path)))
      {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
      }
    }

    private static void UpdateKeyPath(string keyPath = "", bool checkExistence = true)
    {
      if (keyPath.Length == 0)
      {
        keyPath = GetDefaultKeyPath();
      }

      _privateKeyPath = $"{keyPath}.private_key";
      _publicKeyPath = $"{keyPath}.public_key";

      if (checkExistence && (!File.Exists(_privateKeyPath) || !File.Exists(_publicKeyPath)))
      {
        throw new Exception($"Key files missing: [{_privateKeyPath}] and [{_publicKeyPath}].");
      }
    }
  }

  public class BiDirectory
  {
    public string Path { get; set; }
    public string EncryptedPath { get; set; }

    public BiDirectory(string encryptedPath)
    {
      this.Path = encryptedPath.Substring(0, encryptedPath.Length - 4);
      this.EncryptedPath = encryptedPath;
    }

    public override string ToString()
    {
      return this.Path;
    }
  }
}
