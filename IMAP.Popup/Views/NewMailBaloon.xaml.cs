/*
  Parts of code in this file were adapted from sample of Wpf NotifyIcon, awesome project by Philipp Sumi (http://www.hardcodet.net)
 */
using Hardcodet.Wpf.TaskbarNotification;
using IMAP.Popup.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Linq;
using System.Windows.Threading;
using Humanizer;
using System.Windows.Media.Effects;
using System.Threading;
using System.Threading.Tasks;

namespace IMAP.Popup.Views
{
    public partial class NewMailBaloon : UserControl
    {
        private bool _isClosing;
        private Timer _closeTimer;
        private ManualResetEventSlim _shouldResetCloseTimerSignal;
        private readonly long _popupCloseDelay;

        private readonly PersistanceModel _persistanceModel;

        public event Action<NewMailBaloon> BaloonClosing;
        public event Action<uint> OpenFullMailView;
        public event Action<long> RemindMeLaterSelected;

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

        public NewMailBaloon(PersistanceModel persistanceModel)
        {
            InitializeComponent();
            
            _shouldResetCloseTimerSignal = new ManualResetEventSlim();
            _persistanceModel = persistanceModel;
            _popupCloseDelay = _persistanceModel.LoadConfiguration().PopupDelay;
            HighlightRectangle.Fill = new SolidColorBrush(Colors.Transparent);
            TaskbarIcon.AddBalloonClosingHandler(this, OnBalloonClosing);
            AddRemindMeTimingsIfNeeded();
        }

        private void AddRemindMeTimingsIfNeeded()
        {
            var configuration = _persistanceModel.LoadConfiguration();

            RemindMePanel.Dispatcher.Invoke(() =>
            {
                if (configuration.RemindMeTimespans != null && configuration.RemindMeTimespans.Any(x => x > 0))
                {
                    foreach (var remindDelay in configuration.RemindMeTimespans.Where(x => x > 0))
                    {                        
                        var remindDelayInMinutes = remindDelay;
                        var remindDelayTextBlock = new TextBlock
                        {
                            Text = "+" + TimeSpan.FromMinutes(remindDelayInMinutes)
                                                 .Humanize(precision: 3),
                            FontWeight = FontWeights.Bold,
                            Foreground = new SolidColorBrush(Colors.Black),
                            Margin = new Thickness(5),
                            Cursor = Cursors.Hand,
                        };


                        remindDelayTextBlock.Tag = remindDelayInMinutes;

                        remindDelayTextBlock.Visibility = System.Windows.Visibility.Visible;
                        RemindMePanel.Children.Add(remindDelayTextBlock);
                    }

                    foreach(var textBlock in RemindMePanel.Children.OfType<TextBlock>())
                    {
                        textBlock.MouseDown += (sender, args) =>
                        {
                            var remindMeSelected = RemindMeLaterSelected;
                            var remindMeIntervalInMinutes = (long)textBlock.Tag;
                            if (remindMeSelected != null)
                                remindMeSelected(remindMeIntervalInMinutes);
                            SelectedRemindMeIntervalInMinutes = remindMeIntervalInMinutes;
                            var txtBlock = sender as TextBlock;
                            if (((SolidColorBrush)txtBlock.Foreground).Color == Colors.Black)
                            {
                                foreach (var txt in RemindMePanel.Children.OfType<TextBlock>()
                                                                          .Where(x => !ReferenceEquals(x,txtBlock)))
                                    txt.Foreground = new SolidColorBrush(Colors.Black);

                                txtBlock.Foreground = new SolidColorBrush(Colors.Red);
                            }
                            else
                                txtBlock.Foreground = new SolidColorBrush(Colors.Black);
                        };

                        textBlock.MouseEnter += (sender, args) =>
                        {
                            var txtBlock = sender as TextBlock;
                            txtBlock.FontWeight = FontWeights.Heavy;
                            txtBlock.Effect = new DropShadowEffect
                            {
                                Color = Colors.Gray
                            };

                            foreach (var txt in RemindMePanel.Children.OfType<TextBlock>()
                                                                      .Where(x => !ReferenceEquals(x, txtBlock)))
                            {
                                txt.FontWeight = FontWeights.Bold;
                                txt.Effect = null;
                            }
                        };
                        textBlock.MouseLeave += (sender, args) =>
                        {
                            var txtBlock = sender as TextBlock;
                            txtBlock.FontWeight = FontWeights.Bold;
                            txtBlock.Effect = null;
                        };

                    }

                    RemindMePanel.Visibility = System.Windows.Visibility.Visible;
                    RemindMePanel.InvalidateArrange();
                    RemindMePanel.InvalidateVisual();
                }
                else
                    RemindMePanel.Visibility = System.Windows.Visibility.Hidden;
            }, DispatcherPriority.DataBind);
        }

        public bool IsFollowupSelected { get; private set; }

        public long? SelectedRemindMeIntervalInMinutes { get; private set; }

        public string FollowupIconSource
        {
            get
            {
                return IsFollowupSelected ? 
                    "/Images/followup-red-icon.png" :
                    "/Images/followup-black-icon.png";
            }
        }

        private void OnBalloonClosing(object sender, RoutedEventArgs e)
        {
            e.Handled = true; //suppresses the popup from being closed immediately
            _isClosing = true;
            if (BaloonClosing != null)
                BaloonClosing(this);
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

        private void grid_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_closeTimer != null)
            {
                _closeTimer.Dispose();
                _closeTimer = null;
            }

            _shouldResetCloseTimerSignal.Reset();

            _closeTimer = new Timer(state =>
            {
                if (!_shouldResetCloseTimerSignal.IsSet)
                    Dispatcher.Invoke(() => Close());
            }, null, _popupCloseDelay, Timeout.Infinite);
        }

        private void grid_MouseEnter(object sender, MouseEventArgs e)
        {
            if (_isClosing) return;
                        
            //the tray icon assigned this attached property to simplify access
            TaskbarIcon taskbarIcon = TaskbarIcon.GetParentTaskbarIcon(this);
            _shouldResetCloseTimerSignal.Set();
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

        private void FollowupMouseClick(object sender, MouseButtonEventArgs e)
        {
            IsFollowupSelected = !IsFollowupSelected;            
            FollowupIcon.Source = new BitmapImage(new Uri("pack://application:,,,/IMAP.Popup;component" + FollowupIconSource));            
        }

     
    }
}
