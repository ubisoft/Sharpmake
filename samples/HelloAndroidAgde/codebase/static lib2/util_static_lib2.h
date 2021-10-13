#pragma once

class Util2
{
public:
    Util2();
    ~Util2();

public:
    void DoSomethingUseful() const;
    static void Log(const char* s);
private:
    void DoSomethingInternal(const char* anArgument) const;
};

