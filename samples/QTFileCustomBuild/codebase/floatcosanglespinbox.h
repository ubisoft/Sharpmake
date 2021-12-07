#pragma once

#include <QtWidgets/QDoubleSpinBox>

class FloatCosAngleSpinBox : public QDoubleSpinBox
{
    Q_OBJECT
public:
    FloatCosAngleSpinBox(float* valueRef, QWidget *parent = 0);

    void Bind(float* valueRef) {m_valueRef = valueRef;}
    void RefreshValue();

    protected slots:

        void OnValueChanged(double newVal) ;

protected:

    float* m_valueRef;
};
