#include "src/pch.h"

#if !_DEBUG
    #error Expected Debug define (_DEBUG) was not found
#endif

#if NDEBUG
    #error Unexpected Release define (NDEBUG) was found
#endif

#warning Debug is built!
