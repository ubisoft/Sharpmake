#pragma once

#include <QtWidgets/QWidget>

class QPrivateWidgetChildren;

class QPrivateWidget : public QWidget
{
    Q_OBJECT

public:
    QPrivateWidget(QWidget *parent = 0);

protected:
    QPrivateWidgetChildren* children;
};
