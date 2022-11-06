#include <string>
#include <iostream>
#include <external.h>
#include "src/static_prelinked_lib_consumer.h"

int main(int, char**)
{
    std::cout << "Hello XCode World, from " CREATION_DATE "!" << std::endl;

    PrintBuildString("Exe_Prelinked");

    static_prelinked_lib_consumer::Test();
    
    return 0;
}
