using Get.PLShared;

namespace Get.Parser;

public interface ISpanSetter
{
    Position Start { set; }
    Position End { set; }
}
