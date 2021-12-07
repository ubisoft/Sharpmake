#include "util.h"
#include "extra_class1.h"
#include "extra_class2.h"
#include "floating_class.h"

extern void extra_file_method();
extern void floating_file_method();

int main(int, char**)
{
    Util::StaticUtilityMethod();

    // call things from the additional_dir
    {
        ExtraClass1 e1(4);
        e1.PrintMyContent();

        ExtraClass2* e2 = new ExtraClass2(6);
        e2->PrintMyContent();
        delete e2;
    }

    // call things from the dir_individual_files
    {
        FloatingClass f(2);
        f.PrintMyContent();
        floating_file_method();
    }

    return 0;
}
