#include "useless_static_lib2.h"

#pragma warning(push)
#pragma warning(disable: 4668)
#pragma warning(disable: 4710)
#pragma warning(disable: 4711)
#include <iostream>
#pragma warning(pop)

namespace StaticLib2
{
    void UselessMethod()
    {
        std::cout << "- Useless in fact!" << std::endl;
    }
}

