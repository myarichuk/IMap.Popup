﻿using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace IMAP.Popup.Views
{
    public partial class NewMailBaloon : UserControl
    {
        private bool _isClosing;

        public static readonly DependencyProperty SubjectTextProperty =
        DependencyProperty.Register("SubjectText",
            typeof(string),
            typeof(NewMailBaloon),
            new FrameworkPropertyMetadata(""));

        public string SubjectText
        {
            get { return (string)GetValue(SubjectTextProperty); }
            set { SetValue(SubjectTextProperty, value); }
        }

        public static readonly DependencyProperty FromTextProperty =
            DependencyProperty.Register("FromText",
                typeof(string),
                typeof(NewMailBaloon),
                new FrameworkPropertyMetadata(""));

        public string FromText
        {
            get { return (string)GetValue(FromTextProperty); }
            set { SetValue(FromTextProperty, value); }
        }

        public SolidColorBrush HighlightBrush
        {
            get 
            {
                return HighlightRectangle.Fill as SolidColorBrush;
            }
            set 
            {
                HighlightRectangle.Fill = value; 
            }
        }        

        public NewMailBaloon()
        {
            InitializeComponent();
            HighlightRectangle.Fill = new SolidColorBrush(Colors.Transparent);
            TaskbarIcon.AddBalloonClosingHandler(this, OnBalloonClosing);
        }


        private void OnBalloonClosing(object sender, RoutedEventArgs e)
        {
            e.Handled = true; //suppresses the popup from being closed immediately
            _isClosing = true;
        }


        private void imgClose_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //the tray icon assigned this attached property to simplify access
            TaskbarIcon taskbarIcon = TaskbarIcon.GetParentTaskbarIcon(this);
            taskbarIcon.CloseBalloon();
        }

        private void grid_MouseEnter(object sender, MouseEventArgs e)
        {
            if (_isClosing) return;
                        
            //the tray icon assigned this attached property to simplify access
            TaskbarIcon taskbarIcon = TaskbarIcon.GetParentTaskbarIcon(this);
            taskbarIcon.ResetBalloonCloseTimer();
        }

        private void OnFadeOutCompleted(object sender, EventArgs e)
        {
            System.Windows.Controls.Primitives.Popup pp = (System.Windows.Controls.Primitives.Popup)Parent;
            pp.IsOpen = false;
        }
    }
}