﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using Sakuno.ING.Composition;
using Sakuno.ING.Localization;
using Sakuno.ING.Services;
using Sakuno.ING.Settings;
using Sakuno.ING.Shell.Layout;

namespace Sakuno.ING.Shell.Desktop
{
    [Export(typeof(IShell), LazyCreate = false)]
    internal class DesktopShell : FlexibleShell
    {
        private readonly LayoutSetting layoutSetting;
        private readonly ILocalizationService localization;
        private readonly ITextStreamProvider provider;
        private readonly string localeName;
        private readonly FontFamily userFont;
        private readonly List<Window> layoutWindows = new List<Window>();
        private LayoutRoot layout;

        public DesktopShell(LayoutSetting layoutSetting, ILocalizationService localization, LocaleSetting locale, ITextStreamProvider provider, Compositor compositor)
            : base(localization, compositor)
        {
            this.layoutSetting = layoutSetting;
            this.localization = localization;
            this.provider = provider;
            localeName = locale.Language.Value;
            var userFontName = locale.UserLanguageFont.Value;
            if (!string.IsNullOrEmpty(userFontName))
                userFont = new FontFamily(userFontName);
        }

        public override void Run()
        {
            Window window;
            try
            {
                layout = (LayoutRoot)XamlReader.Parse(layoutSetting.XamlString.Value);
                window = new MainWindow { MainContent = layout.MainWindow.LoadContent() };
            }
            catch
            {
                layout = (LayoutRoot)Application.LoadComponent(new Uri("/Sakuno.ING.Shell.Desktop;component/Layout/Default.xaml", UriKind.Relative));
                window = new MainWindow { MainContent = layout.MainWindow.LoadContent() };
            }

            InitWindow(window);

            var app = new Application
            {
                ShutdownMode = ShutdownMode.OnMainWindowClose
            };

            var style = new Style
            {
                TargetType = typeof(ViewPresenter),
            };
            style.Setters.Add(new Setter(ViewPresenter.ViewSourceProperty, Views));
            app.Resources[typeof(ViewPresenter)] = style;
            app.Resources[ViewSwitcher.SwitchActionKey] = new Action<string>(viewId =>
            {
                var windows = Application.Current.Windows;
                for (int i = 0; i < windows.Count; i++)
                {
                    var w = windows[i];
                    if (viewId.Equals(w.Tag))
                    {
                        w.Activate();
                        return;
                    }
                }

                if (viewId == "Settings")
                {
                    var w = new SettingsWindow { DataContext = CreateSettingViews() };
                    InitWindow(w);
                    w.Show();
                    w.Activate();
                }

                var view = layout[viewId];
                if (view != null)
                {
                    var w = new Window
                    {
                        Tag = viewId,
                        Title = localization.GetLocalized("ViewTitle", viewId) ?? viewId,
                        Content = view.LoadContent(),
                    };
                    InitWindow(w);
                    w.Show();
                    w.Activate();
                }
            });

            app.Startup += (s, e) => provider.Enabled = true;
            app.Run(window);
        }

        private void InitWindow(Window window)
        {
            if (!string.IsNullOrEmpty(localeName))
                window.Language = XmlLanguage.GetLanguage(localeName);
            if (userFont != null)
                window.FontFamily = userFont;
        }
    }
}
