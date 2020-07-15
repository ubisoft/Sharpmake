#include "LibA.h"

int main(int /*argc*/, char* /*argv*/[])
{
    const auto t1 = fcnA(3) == 1;
    const auto t2 = fcnA(0) == -2;
    const auto t3 = fcnA(-8) == -10;

    return (t1 && t2 && t3) ? 0 : 1;
}
