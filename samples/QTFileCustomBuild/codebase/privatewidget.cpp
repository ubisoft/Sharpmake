#include "stdafx.h"
#include "privatewidget.h"
#include <cmath>

class QPrivateWidgetChildren
{
    Q_OBJECT
public:
    QPrivateWidgetChildren() {}
};

QPrivateWidget(QWidget *parent /* = 0 */) : QWidget(parent), chilren(nullptr)
{
}

#include "moc_privatewidget.cpp"
#include "privatewidget.moc"
