namespace Get.RegexMachine;
public interface ITextSeekable : ISeekable<char>
{
    int LineNo { get; }
    int CharNo { get; }
}