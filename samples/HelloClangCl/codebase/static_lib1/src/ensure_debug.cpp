#include "src/pch.h"

#if !_DEBUG
    #error Expected Debug define (_DEBUG) was not found
#endif

#if defined(NDEBUG) && NDEBUG
    #error Unexpected Release define (NDEBUG) was found
#endif

#pragma message("Debug is built!")
