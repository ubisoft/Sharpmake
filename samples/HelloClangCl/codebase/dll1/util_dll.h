#pragma once

#pragma warning(push)
#pragma warning(disable: 4710)
#pragma warning(disable: 4711)
#include <vector>
#pragma warning(pop)

#if defined(UTIL_DLL_EXPORT) && defined(_MSC_VER)
#   define UTIL_DLL __declspec(dllexport)
#elif defined(UTIL_DLL_IMPORT) && defined(_MSC_VER)
#   define UTIL_DLL __declspec(dllimport)
#else
#   define UTIL_DLL
#endif

struct UtilDll1
{
    UTIL_DLL int ComputeSum(const std::vector<int>& someInts);
};

