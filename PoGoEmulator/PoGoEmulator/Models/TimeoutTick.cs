﻿using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using PoGoEmulator.Enums;
using Timer = System.Timers.Timer;

namespace PoGoEmulator.Models
{
    /// <summary>
    /// request timeout checker 
    /// </summary>
    public class TimeoutTick
    {
        private Connection _conn;
        private CancellationToken _ct;
        private Timer _tmr;

        public TimeoutTick(CancellationToken ct)
        {
            _ct = ct;
            Stopwatch = new Stopwatch();
            _tmr = new Timer(1000);
            _tmr.Elapsed += Tmr_Elapsed;
        }

        /// <summary>
        /// </summary>
        /// <param name="ct">
        /// action cancelation token 
        /// </param>
        /// <param name="elapsedMethod">
        /// method which triggered with every tick 
        /// </param>
        /// <param name="conn">
        /// </param>
        /// <param name="startAfterCreate">
        /// auto starts the function 
        /// </param>
        public TimeoutTick(CancellationToken ct, Connection conn, bool startAfterCreate) : this(ct)
        {
            _conn = conn;
            if (startAfterCreate)
                this.Start();
        }

        public Stopwatch Stopwatch { get; set; }

        public void Start()
        {
            if (_tmr.Enabled) throw new Exception("timeouter already activated");
            Stopwatch.Start();
            Task.Run(() => _tmr.Start(), _ct);
        }

        public void Stop()
        {
            _tmr?.Stop();
            _tmr?.Dispose();
            Stopwatch?.Stop();
        }

        private void Tmr_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_ct.IsCancellationRequested)
            {
                Stop();
                return;
            }

            try
            {
                if (_conn.Client.Client.Poll(1, SelectMode.SelectRead) && _conn.Client.Client.Available == 0)//detect the custom aborting
                    _conn.Abort(RequestState.CanceledByUser, new Exception("canceled"));
                else if (Stopwatch.ElapsedMilliseconds > Global.Cfg.RequestTimeout.TotalMilliseconds)
                    _conn.Abort(RequestState.Timeout, new Exception("connectionTimeout"));
            }
            catch
            {
                // ignored
            }
        }
    }
}