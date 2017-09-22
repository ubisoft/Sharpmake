#include <iostream>

#include <hello.h>

int main()
{
    std::cout << "Entering main !" << std::endl;
    libstuff::say_hello();
    return 0;
}

