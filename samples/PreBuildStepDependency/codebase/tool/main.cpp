#include <stdio.h>
int main(const int argc, char* argvs[])
{
    printf("[PREBUILD]: Calling prebuilt tool.\n");
    printf("[PREBUILD]: ");
    for (int i = 0; i < argc; ++i)
    {
        printf("%s ", argvs[i]);
    }
    printf("\n");
    return 0;
}
