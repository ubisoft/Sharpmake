#include "LibC.h"

int main(int /*argc*/, char* /*argv*/[])
{
    const auto t1 = (0 == 0);
    const auto t2 = (1 == 1);

    return (t1 && t2) ? 0 : 1;
}
