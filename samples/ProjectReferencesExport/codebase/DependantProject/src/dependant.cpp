#include <string>
#include "dependant.h"
#include "foobar.h"

std::string get_string()
{
    return "Hello " + get_name() + "!";
}
