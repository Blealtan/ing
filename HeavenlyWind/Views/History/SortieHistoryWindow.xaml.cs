﻿using Sakuno.KanColle.Amatsukaze.ViewModels.History;

namespace Sakuno.KanColle.Amatsukaze.Views.History
{
    /// <summary>
    /// SortieHistoryWindow.xaml の相互作用ロジック
    /// </summary>
    partial class SortieHistoryWindow
    {
        SortieHistoryViewModel r_ViewModel;

        public SortieHistoryWindow()
        {
            InitializeComponent();

            DataContext = r_ViewModel = new SortieHistoryViewModel();

            Loaded += (s, e) => r_ViewModel.LoadRecords();
        }
    }
}
