#pragma once

class ExtraClass2
{
public:
    ExtraClass2(int aValue)
        : myValue(aValue) {}

    void PrintMyContent() const;

private:
    int myValue;
};
