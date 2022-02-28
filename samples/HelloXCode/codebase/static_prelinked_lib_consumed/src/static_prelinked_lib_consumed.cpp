#include "static_prelinked_lib_consumed.h"
#include <ios>
#include <external.h>

namespace static_prelinked_lib_consumed
{
    const char* PRELINKED_TEST_EXPORTED_VALUE = "HELLO_THIS_IS_PRELINKED_VALUE_FROM_STATIC_LIBRARY";
    static const char* PRELINKED_TEST_EXPORTED_VALUE2 = "HELLO_THIS_IS_PRELINKED_METHOD_RETURN_VALUE";

    const char* TestPrelinkedMethod()
    {
        return PRELINKED_TEST_EXPORTED_VALUE2;
    }
}
