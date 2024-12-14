using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Get.RegexMachine;

public interface IBacktrackableEnumerator<T> : IEnumerator<T>
{
    int CurrentPosition { get; }
    void Reverse(int count);
}
