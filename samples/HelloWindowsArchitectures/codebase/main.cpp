#include <iostream>

int main(int, char**)
{
    std::cout << "I was built for "

#if defined(_M_ARM64)
        "ARM64"
#elif defined(_M_X64)
        "x64"
#else
        "an unknown architecture"
#endif

        << std::endl;

    return 0;
}
