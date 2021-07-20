#include "stdafx.h"
#include "privatewidget.h"
#include <cmath>

class QPrivateWidgetChildren: public QWidget
{
    Q_OBJECT
public:
    QPrivateWidgetChildren() {}
};

QPrivateWidget::QPrivateWidget(QWidget *parent /* = 0 */)
    : QWidget(parent)
    , children(nullptr)
{
}

#include "moc_privatewidget.cpp"
#include "privatewidget.moc"
