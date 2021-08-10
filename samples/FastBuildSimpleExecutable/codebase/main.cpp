#include <iostream>

int main(int, char**)
{
    std::cout << "I was built in "

#if _DEBUG
        "Debug"
#endif

#if NDEBUG
        "Release"
#endif

    << std::endl;


    return 0;
}
