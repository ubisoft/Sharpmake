#pragma once
#include <vector>

#if defined(UTIL_DLL_EXPORT) && HAVE_VISIBILITY
#   define UTIL_DLL __attribute__((__visibility("default")))
#elif defined(UTIL_DLL_EXPORT) && defined(_MSC_VER)
#   define UTIL_DLL __declspec(dllexport)
#elif defined(UTIL_DLL_IMPORT) && defined(_MSC_VER)
#define UTIL_DLL __declspec(dllimport)
#else
#define UTIL_DLL
#endif

struct UtilDll1
{
    UTIL_DLL int ComputeMagicNumber(const std::vector<int>& someInts);
};

