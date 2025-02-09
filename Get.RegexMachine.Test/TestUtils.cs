using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

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
        Assert.IsTrue(output.HasValue);
        var (val, matched) = output.Value;
        Assert.AreEqual(val, expectMatched);
        Assert.AreEqual(matchedText, matched);
    }
    public void AssertNoMore(T? expectMatched = null)
    {
        if (expectMatched is not null)
            AssertNext(expectMatched, ""); 
        var output = RegexRunner<T>.Next(dfa, iter);
        Assert.IsFalse(output.HasValue);
    }
}
