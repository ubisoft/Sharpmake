#include <iostream>
#include <nlohmann/json.hpp>
using json = nlohmann::json;


int main(int, char**)
{
    std::cout << "I was built in "

#if _DEBUG
        "Debug"
#endif

#if NDEBUG
        "Release"
#endif

#if _WIN64
        " x64"
#else
        " x86"
#endif

        << std::endl;


    json j = {
        {"happy", true},
        {"pi", 3.141},
    };

    for (auto& element : j) {
        std::cout << element << '\n';
    }

    return 0;
}
