#include "stdafx.h"



#if defined(WIN32) || defined(WIN64)
int _tmain(int , _TCHAR* [])
{
	return 0;
}
#endif

#ifdef _XBOX
void SquareRootTest();

void __cdecl main()
{
    SquareRootTest();
}
#endif

#ifdef SN_TARGET_PS3
int main(int argc, char* argv[])
{
    return ((int)argv + (int)argc) * 0;
}
#endif

#ifdef linux
#include <iostream>
int main()
{
    std::cout << "ConfigureOrder" << std::endl;
    return 0;
}
#endif // linux

#ifdef SN_TARGET_PS3_SPU
void cellSpursJobMain2(CellSpursJobContext2* stInfo, CellSpursJob256 *job)
{
}
#endif



#ifdef _XBOX

#include <xnamath.h>

#include <vectorintrinsics.h>

XMVECTOR RealSquareRootTest(XMVECTOR value)
{
    return value;
}

void SquareRootTest()
{

    //float error = 0.0f;

    //for ( float floatValue = 1.0f; floatValue < 100.0f; floatValue += 0.5f )
    //{
    //    float floatResult = 1.0f / sqrtf(floatValue);

    //    XMVECTOR value = XMVectorReplicate(floatValue);

    //    XMVECTOR result0 = __vrsqrtefp(value); // precission 1/4096 = 0,000244140625

    //    XMVECTOR result1 = XMVectorReciprocalSqrt(value);


    //    error += floatResult;

    //}




}

#endif