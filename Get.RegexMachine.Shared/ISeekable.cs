using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Get.RegexMachine;

public interface ISeekable<T>
{
    int CurrentPosition { get; }
    T Current { get; }
    bool MoveNext();
    void Reverse(int chars);
}