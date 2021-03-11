#include "src/pch.h"

#if defined(_DEBUG) && _DEBUG
    #error Unexpected Debug define (_DEBUG) was found
#endif

#if !NDEBUG
    #error Expected Release define (NDEBUG) was not found
#endif

#pragma message("Release is built!")
