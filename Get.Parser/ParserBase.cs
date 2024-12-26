using Get.PLShared;

namespace Get.Parser;

abstract class ParserBase<TOut>
{
    public ParserBase()
    {
        ParserDFA = GenerateDFA();
    }
    readonly ILRParserDFA ParserDFA;
    protected abstract ILRParserDFA GenerateDFA();
    public TOut Parse(IEnumerable<ITerminalValue?> inputTerminals)
    {
        return LRParserRunner<TOut>.Parse(ParserDFA, inputTerminals);
    }
}
