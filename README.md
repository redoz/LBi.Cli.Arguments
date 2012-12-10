LBi.Cli.Arguments
=================

This is a command line argument parser that attempts to mimic the syntax and capabilities of PowerShell.

Example
-------
### 1. Define some parameter sets.

```csharp
public abstract class ExecuteCommandBase
{
    [Parameter(HelpMessage = "Action to take"), Required]
    public string Action { get; set; }
}

[ParameterSet("Name", HelpMessage = "Executes command given a name.")]
public class ExecuteCommandUsingName : ExecuteCommandBase
{
    [Parameter(HelpMessage = "Name"), Required]
    public string Name { get; set; }
}

[ParameterSet("Path", HelpMessage = "Executes command given a path.")]
public class ExecuteCommandUsingPath : ExecuteCommandBase
{
    [Parameter(HelpMessage = "The path."), Required]
    public string Path { get; set; }
}
```

### 2a. Simple usage

```csharp
// set up argument parser
ArgumentParser<ExecuteCommandBase> argParser = new ArgumentParser<ExecuteCommandBase>(typeof(ExecuteCommandUsingName), typeof(ExecuteCommandUsingPath));
ExecuteCommandBase paramSet;
if (argParser.TryParse(args, out paramSet))
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
NodeSequence nodes = parser.Parse(string.Join(" ", args));

// resolve parameter set against the parsed node set
ResolveResult result = sets.Resolve(nodes);
if (result.IsMatch)
{
    paramSet = (ExecuteCommandBase)result.BestMatch.Object;
    paramSet.Execute();
}
else
{
    ErrorWriter errorWriter = new ErrorWriter(Console.Error);
    errorWriter.Write(result.BestMatch);

    HelpWriter helpWriter = new HelpWriter(Console.Out);
    helpWriter.Write(sets, HelpLevel.Full);
}

```
