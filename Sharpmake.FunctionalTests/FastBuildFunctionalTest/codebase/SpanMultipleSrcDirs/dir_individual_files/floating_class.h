#pragma once

class FloatingClass
{
public:
    FloatingClass(int aValue)
        : myValue(aValue) {}

    void PrintMyContent() const;

private:
    int myValue;
};
