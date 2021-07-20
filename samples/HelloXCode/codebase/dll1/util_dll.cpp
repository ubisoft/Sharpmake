#include "precomp.h"
#include "util_dll.h"

#include "src/util_static_lib1.h"
#include <iostream>
#include <external.h>

int UtilDll1::ComputeSum(const std::vector<int>& someInts)
{
    int acc = 0;
    for (int item : someInts)
        acc += item;

    PrintBuildString("Dll1");

    acc += static_lib1_utils::GetRandomPosition();
    
    return acc;
}

