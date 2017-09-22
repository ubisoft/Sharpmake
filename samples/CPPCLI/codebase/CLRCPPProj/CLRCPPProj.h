// CLRCPPProj.h

#pragma once
#include "..\theEmptyCPPProject\TestClass.h"
using namespace System;

namespace CLRCPPProj {

	public ref class Class1
	{
	public:
		void Test()
		{
			Console::WriteLine("It worked!");
			Console::WriteLine(OtherCSharpProj::ClassCSharp::Oh);
			TestClass test;
		}
	};
}
