LBi.Cli.Arguments
=================

This is a command line argument parser that attempts to mimic the syntax and capabilities of PowerShell.

Example
-------
### 1. Define some parameter sets.

```
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

### 2. Define some parameter sets.

```
ParameterSetCollection sets = ParameterSetCollection.FromTypes(typeof(ExecuteCommandUsingName), typeof(ExecuteCommandUsingPath));
Parser parser = new Parser();
ArgumentCollection args = parser.Parse("-Action Execute -Name 50");
ResolveResult result = sets.Resolve(args);
if (result.IsMatch)
{
    ParameterSetResult matchingSet = result.Match;
    ExecuteCommandUsingName cmdBase = matchingSet.Object as ExecuteCommandUsingName;
    // ...
}

