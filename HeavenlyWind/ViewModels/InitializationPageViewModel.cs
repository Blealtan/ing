﻿using Sakuno.KanColle.Amatsukaze.Game.Proxy;
using Sakuno.KanColle.Amatsukaze.Models;
using Sakuno.SystemInterop;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Sakuno.KanColle.Amatsukaze.ViewModels
{
    class InitializationPageViewModel : ModelBase
    {
        const int LoopbackAddress = 16777343;

        MainWindowViewModel r_Owner;

        InitializationStep r_Step;
        public InitializationStep Step
        {
            get { return r_Step; }
            private set
            {
                if (r_Step != value)
                {
                    r_Step = value;
                    OnPropertyChanged(nameof(Step));
                }
            }
        }

        public bool IsPortAvailable { get; private set; } = true;
        public bool IsConnectionCycle { get; private set; }
        public bool IsUpstreamProxyAvailable { get; private set; }

        public ProcessInfo ProcessThatOccupyingPort { get; private set; }

        public InitializationPageViewModel(MainWindowViewModel rpOwner)
        {
            r_Owner = rpOwner;
        }

        public async void CheckProxyPort()
        {
            Step = InitializationStep.Initializing;

            await Task.Run((Action)CheckProxyPortCore);

            if (IsPortAvailable && !IsConnectionCycle)
            {
                Start();
                return;
            }

            Step = InitializationStep.Error;
        }

        void Start()
        {
            KanColleProxy.Start();
            r_Owner.Page = r_Owner.GameInformation;

            Step = InitializationStep.None;
        }

        unsafe void CheckProxyPortCore()
        {
            var rPort = Preference.Instance.Network.Port.Default;
            if (Preference.Instance.Network.PortCustomization)
                rPort = Preference.Instance.Network.Port.Value;
            var rUpstreamProxy = Preference.Instance.Network.UpstreamProxy;

            var rIsPortAvailable = true;
            var rIsUpstreamProxyAvailable = false;

            if (rUpstreamProxy.Enabled && (rUpstreamProxy.Host == "127.0.0.1" || rUpstreamProxy.Host =="localhost"))
                IsConnectionCycle = rPort == rUpstreamProxy.Port.Value;
            else
                rIsUpstreamProxyAvailable = true;

            var rBufferSize = 0;
            NativeMethods.IPHelperApi.GetExtendedTcpTable(IntPtr.Zero, ref rBufferSize, true, NativeConstants.AF.AF_INET, NativeConstants.TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_LISTENER);

            var rBuffer = stackalloc byte[rBufferSize];
            NativeMethods.IPHelperApi.GetExtendedTcpTable((IntPtr)rBuffer, ref rBufferSize, true, NativeConstants.AF.AF_INET, NativeConstants.TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_LISTENER);

            var rTable = (NativeStructs.MIB_TCPTABLE*)rBuffer;
            var rRow = &rTable->table;

            for (var i = 0; i < rTable->dwNumEntries; i++, rRow++)
            {
                if (rRow->dwLocalAddr != LoopbackAddress)
                    continue;

                if (rRow->LocalPort == rPort)
                {
                    rIsPortAvailable = false;

                    try
                    {
                        ProcessThatOccupyingPort = new ProcessInfo(Process.GetProcessById(rRow->dwOwningPid));
                    }
                    catch
                    {
                        ProcessThatOccupyingPort = null;
                    }
                }

                if (!rIsUpstreamProxyAvailable && rRow->LocalPort == rUpstreamProxy.Port)
                    rIsUpstreamProxyAvailable = true;
            }

            if (rIsPortAvailable)
                ProcessThatOccupyingPort = null;

            IsPortAvailable = rIsPortAvailable;
            IsUpstreamProxyAvailable = IsConnectionCycle || rIsUpstreamProxyAvailable;

            OnPropertyChanged(string.Empty);
        }
    }
}
