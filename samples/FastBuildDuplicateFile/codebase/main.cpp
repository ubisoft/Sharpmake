#include <iostream>

#include "test1/test.h"
#include "test2/test.h"

int main(int, char**)
{
    std::cout << "I have two files named test.h. test1 makes " << test1() << ". test2 makes " << test2() << "." << std::endl;
    return 0;
}
