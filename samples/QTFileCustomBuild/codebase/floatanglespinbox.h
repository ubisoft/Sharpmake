#ifndef __FLOATANGLESPINBOX_H__
#define __FLOATANGLESPINBOX_H__

#include <QtGui/QDoubleSpinBox>

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

#endif //__FLOATANGLESPINBOX_H__
