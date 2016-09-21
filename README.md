LBi.Cli.Arguments
=================

This is a command line argument parser that attempts to mimic the syntax and capabilities of PowerShell.

Example
-------
### 1. Define some parameter sets.

```csharp
public abstract class ExecuteCommandBase
{
    [Parameter(HelpMessage = "Optional parameter dictionary")]
    [DefaultValue("@{}")]
    public IDictionary<string, object> Parameters { get; set; }

    [Parameter(HelpMessage = "If set, no action is taken.")]
    public Switch WhatIf { get; set; }

    public abstract void Execute();
}

[ParameterSet("Name", HelpMessage = "Executes command given a name.")]
public class ExecuteCommandUsingName : ExecuteCommandBase
{
    [Parameter(HelpMessage = "Name"), Required]
    public string Name { get; set; }

    public override void Execute()
    {
        if (this.WhatIf.IsPresent)
            Console.WriteLine("Would have executed using name: {0}", this.Name);
        else
            Console.WriteLine("Executing using name: {0}", this.Name);
    }
}

[ParameterSet("Path", HelpMessage = "Executes command given a path.")]
public class ExecuteCommandUsingPath : ExecuteCommandBase
{
    [Parameter(HelpMessage = "The path"), Required]
    public string Path { get; set; }

    public override void Execute()
    {
        if (this.WhatIf.IsPresent)
            Console.WriteLine("Would have executed using path: {0}", this.Path);
        else
            Console.WriteLine("Executing using path: {0}", this.Path);
    }
}

// Also supports command style interfaces (e.g. 'app.exe Test -Duration 10')
[ParameterSet("WithCommand", Command = "Test", HelpMessage = "Executes test with duration.")]
public class ParameterSetWithCommand : ExecuteCommandBase
{
    [Parameter(HelpMessage = "Test duration")]
    public int? Duration { get; set; }
}
```

### 2a. Simple usage

```csharp
// set up argument parser
ArgumentParser<ExecuteCommandBase> argParser = new ArgumentParser<ExecuteCommandBase>(typeof(ExecuteCommandUsingName), typeof(ExecuteCommandUsingPath));
ExecuteCommandBase paramSet;
if (argParser.TryParse(CommandLine.Arguments, out paramSet))
{
    paramSet.Execute();
}
```

### 2b. Advanced usage

```csharp
// create parameter set collection from types
ParameterSetCollection sets = ParameterSetCollection.FromTypes(typeof(ExecuteCommandUsingName), typeof(ExecuteCommandUsingPath));

// parse the command line arguments
Parser parser = new Parser();
NodeSequence nodes = parser.Parse(CommandLine.Arguments);

// resolve parameter set against the parsed node set
ResolveResult result = sets.Resolve(new ParameterSetBinder(),
                                    DefaultActivator.Instance, // entry point for DI, see ITypeActivator or DelegateActivator
                                    new IntransigentTypeConverter(),
                                    CultureInfo.InvariantCulture,
                                    nodes);
if (result.IsMatch)
{
    paramSet = (ExecuteCommandBase)result.BestMatch.Object;
    paramSet.Execute();
}
else
{
    ErrorWriter errorWriter = new ErrorWriter();
    errorWriter.Write(new ConsoleWriter(Console.Error), result.BestMatch);

    HelpWriter helpWriter = new HelpWriter();
    helpWriter.Write(new ConsoleWriter(Console.Out), sets, HelpLevel.Parameters);
}

```
