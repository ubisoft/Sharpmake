#ifndef PRIVATE_WIDGET_H__
#define PRIVATE_WIDGET_H__

#include <QWidget>

class QPrivateWidgetChildren;

class QPrivateWidget : public QWidget
{
    Q_OBJECT

public:
    QPrivateWidget(QWidget *parent = 0);

protected:
    QPrivateWidgetChildren* children;
};

#endif //PRIVATE_WIDGET_H__