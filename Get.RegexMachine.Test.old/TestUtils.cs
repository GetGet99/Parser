using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Get.RegexMachine.Test;

static class TestUtils
{
    public static ListSeekable<char> Iter(string str) => new([.. str]);
}
class AssertionHelper<T>(RegexCompiler<T>.DFAState dfa, ISeekable<char> iter)
    where T : class
{
    public void AssertNext(T? expectMatched, string matchedText)
    {
        var output = RegexRunner<T>.Next(dfa, iter);
        var (val, matched) = Assert.NotNull(output);
        Assert.Equal(val, expectMatched);
        Assert.Equal(matchedText, matched);
    }
    public void AssertNoMore(T? expectMatched = null)
        => AssertNext(expectMatched, "");
}
