#include "useless_static_lib2.h"
#include "util_static_lib2.h"
#include <iostream>

namespace StaticLib2
{
    void UselessMethod()
    {
        Util2::Log("- Useless in fact!");
    }
}

