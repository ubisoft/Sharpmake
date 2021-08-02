#if !defined(_LIBRARY_APIEXPORT_HPP)
#define _LIBRARY_APIEXPORT_HPP

// dllexport boilerplate
#if defined(LIBRARY_DLL)
#   if defined(_MSC_VER)
#       if defined(LIBRARY_COMPILE)
#           define LIBRARY_API __declspec(dllexport)
#       else
#           define LIBRARY_API __declspec(dllimport)
#       endif
#   elif defined(__GNUC__) || defined(__clang__)
#       if defined(LIBRARY_COMPILE)
#           define LIBRARY_API __attribute__ ((visibility ("default")))
#       endif
#   endif
#endif

#if !defined(LIBRARY_API)
#   define LIBRARY_API 
#endif

#endif // _LIBRARY_APIEXPORT_HPP
