using System.ComponentModel.DataAnnotations;
using LBi.Cli.Arguments.Output;

namespace LBi.Cli.Arguments
{
    // TODO put all strings in a resource file
    [ParameterSet("Help", HelpMessage = "Show help")]
    public class HelpCommand
    {
        [Parameter(HelpMessage = "Show help"), Required]
        public Switch Help { get; set; }

        [Parameter(HelpMessage = "Show all help")]
        public Switch Full { get; set; }

        [Parameter(HelpMessage = "Show detailed help")]
        public Switch Detailed { get; set; }

        [Parameter(HelpMessage = "Show parameter information")]
        public Switch Parameters { get; set; }

        [Parameter(HelpMessage = "Show examples")]
        public Switch Examples { get; set; }

        public HelpLevel ToHelpLevel()
        {
            HelpLevel ret = HelpLevel.Syntax;

            if (this.Full)
                ret = HelpLevel.Full;

            if (this.Examples)
                ret |= HelpLevel.Examples;

            if (this.Detailed)
                ret |= HelpLevel.Detailed;

            if (this.Parameters)
                ret |= HelpLevel.Parameters;

            return ret;
        }
    }
}