﻿using System;
using System.Globalization;
using System.Net.Mime;
using System.Runtime.Intrinsics.Arm;
using System.Speech.Synthesis;
using System.Threading;
using System.Threading.Tasks;
using untitled_ffxiv_hunt_tracker.Factories;
using untitled_ffxiv_hunt_tracker.Memory;
using untitled_ffxiv_hunt_tracker.ViewModels;

namespace untitled_ffxiv_hunt_tracker
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var session = new Session();

            while (true)
            {
                session.GetUser();
                session.SearchNearbyMobs();
                Thread.Sleep(1000);
            }
        }
    }
}
