using System;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm;
using System.Collections.ObjectModel;
using TradersToolbox.Data;
using TradersToolbox.Core;
using System.Windows.Data;
using System.ComponentModel;
using DevExpress.Data.Filtering;
using System.Collections.Generic;
using System.Windows;

namespace TradersToolbox.ViewModels
{
    [POCOViewModel]
    public class LogViewModel : PanelWorkspaceViewModel
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        //public virtual ObservableCollection<LogMessage> LogsCollection { get; set; }
        public ICollectionView LogsCollection { get; set; }


        public virtual LogMessage FocusedMessage { get; set; }


        [BindableProperty(OnPropertyChangedMethodName = "UpdateFilter")]
        public virtual bool IsTrace { get; set; } //= true;
        [BindableProperty(OnPropertyChangedMethodName = "UpdateFilter")]
        public virtual bool IsDebug { get; set; } = true;
        [BindableProperty(OnPropertyChangedMethodName = "UpdateFilter")]
        public virtual bool IsInfo { get; set; } = true;
        [BindableProperty(OnPropertyChangedMethodName = "UpdateFilter")]
        public virtual bool IsWarn { get; set; } = true;
        [BindableProperty(OnPropertyChangedMethodName = "UpdateFilter")]
        public virtual bool IsError { get; set; } = true;
        [BindableProperty(OnPropertyChangedMethodName = "UpdateFilter")]
        public virtual bool IsAll { get; set; } = true;

        public virtual CriteriaOperator FilterCriteria { get; set; }

        public virtual bool IsAutoScroll { get; set; }

        protected override string WorkspaceName => "RightHost";

        public LogViewModel()
        {
            DisplayName = "Logger";

            Logger.logBuffer.CollectionChanged += LogBuffer_CollectionChanged;

            //LogsCollection = new ObservableCollection<LogMessage>();
            LogsCollection = CollectionViewSource.GetDefaultView(Logger.logBuffer);
            BindingOperations.EnableCollectionSynchronization(Logger.logBuffer, Logger.locker);
        }

        protected void UpdateFilter()
        {
            CriteriaOperator criteria = new BinaryOperator(nameof(LogMessage.LogLvl), LogMessage.LogLevel.Fatal);

            if (IsTrace)
                criteria |= new BinaryOperator(nameof(LogMessage.LogLvl), LogMessage.LogLevel.Trace);
            if (IsDebug)
                criteria |= new BinaryOperator(nameof(LogMessage.LogLvl), LogMessage.LogLevel.Debug);
            if (IsInfo)
                criteria |= new BinaryOperator(nameof(LogMessage.LogLvl), LogMessage.LogLevel.Info);
            if (IsWarn)
                criteria |= new BinaryOperator(nameof(LogMessage.LogLvl), LogMessage.LogLevel.Warning);
            if (IsError)
                criteria |= new BinaryOperator(nameof(LogMessage.LogLvl), LogMessage.LogLevel.Error);

            FilterCriteria = criteria;
        }

        private void LogBuffer_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                if (IsAutoScroll)
                    Application.Current?.Dispatcher.InvokeAsync(() =>
                    {
                        FocusedMessage = e.NewItems[0] as LogMessage;
                    });
            }

            /*if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                Application.Current?.Dispatcher.InvokeAsync(() =>
                {
                    foreach (var m in e.NewItems)
                        LogsCollection.Add(m as LogMessage);
                    if (IsAutoScroll)
                        FocusedMessage = e.NewItems[0] as LogMessage;
                });
            }
            else if(e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                Application.Current?.Dispatcher.InvokeAsync(() =>
                {
                    foreach (var m in e.OldItems)
                        LogsCollection.Remove(m as LogMessage);
                });
            }
            else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
            {
                Application.Current?.Dispatcher.InvokeAsync(() =>
                {
                    LogsCollection.Clear();
                });
            }*/
        }

        public void Clear()
        {
            lock (Logger.locker)
            {
                Logger.logBuffer.Clear();
            }
        }
    }
}