#define _SILENCE_TR1_NAMESPACE_DEPRECATION_WARNING
#include <gtest/gtest.h>

int main(int, char**)
{
    // just reference some code that comes from gtest
	return testing::kMaxStackTraceDepth ? 0 : 1;
}
