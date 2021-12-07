#pragma once

class ExtraClass1
{
public:
    ExtraClass1(int aValue)
        : myValue(aValue) {}

    void PrintMyContent() const;

private:
    int myValue;
};
