#if defined(WIN32) || defined(WIN64)
#include <stdio.h>
#include <tchar.h>
#endif

#ifdef _XBOX
#include <xtl.h>
#include <xboxmath.h>
#endif

#ifdef SN_TARGET_PS3_SPU
#include <cell/spurs/job_chain.h>
#endif
