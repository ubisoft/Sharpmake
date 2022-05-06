#include "static_prelinked_lib_consumer.h"
#include "src/static_prelinked_lib_consumed.h"
#include <iostream>
#include <external.h>

namespace static_prelinked_lib_consumer
{
    void Test()
    {
        PrintBuildString("StaticPrelinkedLibConsumer");
        
        //  Call the other static lib that is prelinked into this one.
        //  This call will fail if the static_prelinked_lib_consumed is not pre-linked correctly
        
        std::cout << "Value from prelinked library : " << static_prelinked_lib_consumed::PRELINKED_TEST_EXPORTED_VALUE << std::endl;
        std::cout << "Value from prelinked library (method) : " << static_prelinked_lib_consumed::TestPrelinkedMethod() << std::endl;
    }
}

