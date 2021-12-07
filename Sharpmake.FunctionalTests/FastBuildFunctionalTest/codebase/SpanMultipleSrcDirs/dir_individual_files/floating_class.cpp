#include "floating_class.h"
#include <cstdio>

void FloatingClass::PrintMyContent() const
{
    printf("%s value = %d\n", __func__, myValue);
}
