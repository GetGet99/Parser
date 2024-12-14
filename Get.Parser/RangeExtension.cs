namespace Get.Parser;

static class RangeExtension
{
    public static IEnumerator<int> GetEnumerator(this Range r)
    {
        if (r.Start.IsFromEnd) throw new ArgumentException();
        if (r.End.IsFromEnd) throw new ArgumentException();
        for (int i = r.Start.Value; i < r.End.Value; i++)
            yield return i;
    }
}
