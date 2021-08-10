#pragma once

#include <QtWidgets/QDoubleSpinBox>

class FloatAngleSpinBox : public QDoubleSpinBox
{
    Q_OBJECT
public:
    FloatAngleSpinBox(float* valueRef, QWidget *parent = 0);

    void Bind(float* valueRef) {m_valueRef = valueRef;};
    void RefreshValue();

    protected slots:

        void OnValueChanged(double newVal) ;

protected:

    float* m_valueRef;
};
