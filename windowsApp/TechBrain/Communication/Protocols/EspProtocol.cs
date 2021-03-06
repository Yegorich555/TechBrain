﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using TechBrain.Communication.Drivers;
using TechBrain.Entities;

namespace TechBrain.Communication.Protocols
{
    public class EspProtocol : Protocol
    {
        public EspProtocol(IDriver driver) : base(driver)
        {
        }

        public override bool Ping()
        {
            try
            {
                using (var client = Driver.OpenClient())
                {
                    client.Write("esp_ping()\n");
                    client.WaitResponse("OK: ping()");
                    return true;
                }
            }
            catch (TimeoutException ex)
            {
                Trace.WriteLine(ex);
                return false;
            }
        }

        public override void SetOut(int number, int value)
        {
            if (number < 0 || number > 10)
                throw new ArgumentOutOfRangeException("number", $"Number == {number} must be 0..10");

            if (value < 0 || value > 100)
                throw new ArgumentOutOfRangeException("value", $"Value == {value} must be 0..100");

            using (var client = Driver.OpenClient())
            {
                var num = number == 0 ? "all" : number.ToString();
                var cmd = $"out{num}({value})";
                client.Write($"esp_{cmd}\n");
                client.WaitResponse($"OK: {cmd}");
            }
        }

        public override void SetTime(DateTime dt)
        {
            throw new NotSupportedException();
        }

        public override void Sleep(TimeSpan time)
        {
            if (time.TotalSeconds < 10 || time.TotalMinutes > 70)
                throw new ArgumentOutOfRangeException("time", "time must be 10sec...70min");

            using (var client = Driver.OpenClient())
            {
                var cmd = $"sleep({Convert.ToInt16(time.TotalSeconds)})";
                client.Write($"esp_{cmd}\n");
            }
            //if (Ping())
            //    throw new InvalidOperationException("Sleep failed. Device respondes Ping after sleep command");
        }

        public override void UpdateSensors(IList<Sensor> sensors)
        {
            throw new NotImplementedException();
        }
    }
}
