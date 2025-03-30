using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Xenium;

class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine();

        // Default to showing the help command if launched with no arguments.
        if (args.Length == 0)
        {
            args =
            [
                "--help"
            ];
        }
        
        var rootCommand = new RootCommand("Generates the initialization scripts for a Garry's Mod addon/gamemode, using a module-based approach.")
        {
            TreatUnmatchedTokensAsErrors = true
        };
        
        var projectPath = new Option<DirectoryInfo>(
            name: "--path",
            description: "The path to the root directory of the project to execute commands for. Defaults to the current working directory.",
            getDefaultValue: () => new DirectoryInfo(Directory.GetCurrentDirectory())
        )
        {
            Arity = ArgumentArity.ExactlyOne
        };
        
        projectPath.AddValidator(result =>
        {
            var directory = result.GetValueForOption(projectPath);
            if (directory == null || !directory.Exists)
            {
                result.ErrorMessage = $"Could not find directory at path '{directory?.FullName ?? "NULL"}'";
            }
        });
        
        projectPath.AddAlias("-p");
        rootCommand.AddGlobalOption(projectPath);

        var verboseOutput = new Option<bool>(
            name: "--verbose",
            description: "Enables verbose output of the modules, files and folders being handled.",
            getDefaultValue: () => false
        )
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        
        verboseOutput.AddAlias("-v");
        rootCommand.AddGlobalOption(verboseOutput);

        var setupCommand = new Command("setup", "Sets up the Xenium configuration file and modules folder for a project.");
        var projectName = new Option<string>(
            name: "--name",
            description: "The name of the project. Used when generating file names."
        )
        {
            IsRequired = true,
            Arity = ArgumentArity.ExactlyOne
        };
        
        projectName.AddValidator((result) =>
        {
            var name = result.GetValueForOption(projectName);
            if (string.IsNullOrWhiteSpace(name))
            {
                result.ErrorMessage = "Name cannot be empty or whitespace";
                return;
            }
            
            if (!Utils.AlphanumericRegex.IsMatch(name))
            {
                result.ErrorMessage = "Name must be alphanumerical only.";
            }
        });
        
        projectName.AddAlias("-n");
        setupCommand.AddOption(projectName);
        
        var projectType = new Option<string>(
            name: "--type",
            description: "The type of project to generate statements for."
        )
        {
            IsRequired = true,
            Arity = ArgumentArity.ExactlyOne
        };
        
        projectType.AddAlias("-t");
        projectType.FromAmong("addon", "gamemode");
        
        setupCommand.AddOption(projectType);
        setupCommand.SetHandler(async (name, path, type, isVerbose) =>
        {
            Configuration.IsVerbose = isVerbose;
            await XeniumSetup.SetupProjectAsync(name, path, type);
        }, projectName, projectPath, projectType, verboseOutput);
        
        rootCommand.AddCommand(setupCommand);

        var generateCommand = new Command("generate", "Generates the initialization scripts for a project.");
        generateCommand.SetHandler(async (path, isVerbose) =>
        {
            Configuration.IsVerbose = isVerbose;
            await XeniumGenerate.GenerateScriptsAsync(path);
        }, projectPath, verboseOutput);
        
        rootCommand.AddCommand(generateCommand);

        var createModuleCommand = new Command("create-module", "Creates a new module with the given name for the desired project.");
        var moduleName = new Option<string>(
            name: "--name",
            description: "The name of the module to create."
        )
        {
            IsRequired = true,
            Arity = ArgumentArity.ExactlyOne
        };
        
        moduleName.AddAlias("-n");
        moduleName.AddValidator(result =>
        {
            var name = result.GetValueForOption(moduleName);
            if (string.IsNullOrWhiteSpace(name))
            {
                result.ErrorMessage = "Name cannot be empty or whitespace";
                return;
            }
            
            if (!Utils.AlphanumericRegex.IsMatch(name))
            {
                result.ErrorMessage = "Name must be alphanumerical only.";
            }
        });
        
        createModuleCommand.AddOption(moduleName);
        createModuleCommand.SetHandler(async (name, path, isVerbose) =>
        {
            Configuration.IsVerbose = isVerbose;
            await XeniumCreateModule.CreateModuleAsync(path, name);
        }, moduleName, projectPath, verboseOutput);
        
        rootCommand.AddCommand(createModuleCommand);
        
        // Create a command line builder to get rid of the default --version command.
        // We really don't need it, and it somewhat coincides with -v for verbose.
        var parser = new CommandLineBuilder(rootCommand)
            .UseHelp()
            .UseEnvironmentVariableDirective()
            .UseParseDirective()
            .UseSuggestDirective()
            .RegisterWithDotnetSuggest()
            .UseTypoCorrections()
            .UseParseErrorReporting()
            .UseExceptionHandler((exception, context) =>
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(exception.Message);
                Console.ResetColor();
                context.ExitCode = 1;
            })
            .CancelOnProcessTermination()
            .Build();
        
        return await parser.InvokeAsync(args);
    }
}