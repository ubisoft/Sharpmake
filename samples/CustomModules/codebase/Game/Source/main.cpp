#include <cstdio>
#include "Engine.h"
#include "Gameplay.h"
#include "Core.h"

#ifndef WITH_CORE
#error "WITH_CORE is a public define of the Core module and must reach projects depending on Engine"
#endif

#ifdef CORE_BUILD
#error "CORE_BUILD is private to the project embedding the Core module"
#endif

int main()
{
    std::printf("%s, %s, %s\n", EngineName(), GameplayName(), CoreName());
    return 0;
}
