namespace LBi.Cli.Arguments.Parsing.Ast
{
    public class SwitchParameter : ParameterName
    {
        public SwitchParameter(ISourceInfo sourceInfo, string name, AstNode value)
            : base(NodeType.Switch, sourceInfo, name)
        {
            this.Value = value;
        }

        public AstNode Value { get; protected set; }

        public override object Visit(IAstVisitor visitor)
        {
            return visitor.Visit(this);
        }
    }
}