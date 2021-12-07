#include "stdafx.h"
#include "floatcosanglespinbox.h"
#include <cmath>

FloatCosAngleSpinBox::FloatCosAngleSpinBox(float* valueRef,QWidget *parent)
    : QDoubleSpinBox(parent), m_valueRef(valueRef)
{
    connect(
        this, qOverload<double>(&QDoubleSpinBox::valueChanged),
        this, &FloatCosAngleSpinBox::OnValueChanged
    );
}

// Updates the UI field (in degrees) from the reference value (a cosine).
void FloatCosAngleSpinBox::RefreshValue()
{
    if (m_valueRef !=nullptr)
        setValue(180.0f / 3.14159265358979f * std::acos(*m_valueRef));
}

// Updates the reference value (a cosine) from the UI field (in degrees).
void FloatCosAngleSpinBox::OnValueChanged(double newVal) 
{
    if (m_valueRef) 
        *m_valueRef = (float)std::cos(3.14159265358979f / 180.0f * (float)newVal);
}

#include "moc_floatcosanglespinbox.cpp"
