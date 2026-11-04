#include "Gameplay.h"
#include "Core.h"

#ifndef WITH_CORE
#error "WITH_CORE is a public define of the Core module and must reach projects depending on Engine"
#endif

const char* GameplayName()
{
    return CoreName();
}
