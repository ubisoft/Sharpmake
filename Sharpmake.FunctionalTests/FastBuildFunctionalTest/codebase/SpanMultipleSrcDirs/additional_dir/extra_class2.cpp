#include "extra_class2.h"
#include <cstdio>

void ExtraClass2::PrintMyContent() const
{
    printf("%s value = %d\n", __func__, myValue);
}
