#include "LibB.h"
#include "LibC.h"

int main(int /*argc*/, char* /*argv*/[])
{
    fcnC(fcnB());
    return 0;
}
