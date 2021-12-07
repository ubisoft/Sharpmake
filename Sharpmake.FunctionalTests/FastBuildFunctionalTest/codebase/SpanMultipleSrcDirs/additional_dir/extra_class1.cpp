#include "extra_class1.h"
#include <cstdio>

void ExtraClass1::PrintMyContent() const
{
    printf("%s value = %d\n", __func__, myValue);
}
