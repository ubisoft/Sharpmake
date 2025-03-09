#include "src/pch.h"

#if _DEBUG
    #error Unexpected Debug define (_DEBUG) was found
#endif

#if !NDEBUG
    #error Expected Release define (NDEBUG) was not found
#endif

#warning Release is built!
