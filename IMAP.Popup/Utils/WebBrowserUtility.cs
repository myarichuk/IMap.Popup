﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;

namespace IMAP.Popup.Utils
{
    //this solution is taken from a StackOverflow question
    //http://stackoverflow.com/questions/7728707/webbrowser-in-wpf-using-mvvm-pattern
    public static class WebBrowserUtility
    {
        public static readonly DependencyProperty BindableSourceProperty =
                               DependencyProperty.RegisterAttached("BindableSource", typeof(string),
                               typeof(WebBrowserUtility), new UIPropertyMetadata(null,
                               BindableSourcePropertyChanged));

        public static string GetBindableSource(DependencyObject obj)
        {
            return (string)obj.GetValue(BindableSourceProperty);
        }

        public static void SetBindableSource(DependencyObject obj, string value)
        {
            obj.SetValue(BindableSourceProperty, value);
        }

        public static void BindableSourcePropertyChanged(DependencyObject o,
                                                         DependencyPropertyChangedEventArgs e)
        {
            var webBrowser = (WebBrowser)o;
            webBrowser.NavigateToString((string)e.NewValue);
        }
    }
}