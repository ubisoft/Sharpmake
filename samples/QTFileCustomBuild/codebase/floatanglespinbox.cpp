#include "stdafx.h"
#include "floatanglespinbox.h"

FloatAngleSpinBox::FloatAngleSpinBox(float* valueRef,QWidget *parent)
    : QDoubleSpinBox(parent), m_valueRef(valueRef)
{
    connect(
        this, qOverload<double>(&QDoubleSpinBox::valueChanged),
        this, &FloatAngleSpinBox::OnValueChanged
    );
}

void FloatAngleSpinBox::RefreshValue()
{
    if (m_valueRef !=nullptr)
        setValue(180.0f / 3.14159265358979f * *m_valueRef);
}

void FloatAngleSpinBox::OnValueChanged(double newVal) 
{
    if (m_valueRef) 
        *m_valueRef = 3.14159265358979f / 180.0f * (float)newVal;
}

