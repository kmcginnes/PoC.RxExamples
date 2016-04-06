using System.Diagnostics.CodeAnalysis;

namespace PoC.RxExamples
{
    // Pulled from GitHub decompiled
    [SuppressMessage("Microsoft.Design", "CA1027:MarkEnumsWithFlags")]
    public enum Importance
    {
        Undefined = 0,
        Critical = 1,
        High = 2,
        Normal = 4,
        Low = 5
    }
}