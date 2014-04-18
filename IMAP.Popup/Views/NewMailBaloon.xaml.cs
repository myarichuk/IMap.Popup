/*
  Parts of code in this file were adapted from sample of Wpf NotifyIcon, awesome project by Philipp Sumi (http://www.hardcodet.net)
 */
using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace IMAP.Popup.Views
{
    public partial class NewMailBaloon : UserControl
    {
        private bool _isClosing;

        public event Action BaloonClosing;
        public event Action<uint> OpenFullMailView;

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

        public uint EmailUid { get; set; }

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
            if (BaloonClosing != null)
                BaloonClosing();
        }


        private void imgClose_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        public void Close()
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

        private void OnMouseClick(object sender, MouseButtonEventArgs e)
        {
            var openFullMailViewEventHandler = OpenFullMailView;
            if (openFullMailViewEventHandler != null)
                openFullMailViewEventHandler(EmailUid);
        }

     
    }
}
