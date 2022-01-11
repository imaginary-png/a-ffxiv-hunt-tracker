﻿using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using untitled_ffxiv_hunt_tracker;
using untitled_ffxiv_hunt_tracker.Entities;
using untitled_ffxiv_hunt_tracker.ViewModels;

namespace ufht_UI.UserControls
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class MainMapControl : UserControl
    {
        private readonly Session _session;

        private ObservableCollection<Mob> _nearbyMobs;

        private double _playerIconX;
        private double _playerIconY;
        private double _playerIconRotation;

        private double _ARankIconX;
        private double _ARankIconY;

        private double _BRankIconX;
        private double _BRankIconY;

        private double _SRankIconX;
        private double _SRankIconY;

        private double _SSRankIconX;
        private double _SSRankIconY;

        private readonly BitmapImage _mobIconA;
        private readonly BitmapImage _mobIconB;
        private readonly BitmapImage _mobIconS;
        private readonly BitmapImage _mobIconSS;

        private Mob A1;
        private Mob A2;


        public MainMapControl(Session session)
        {
            _session = session;
            InitializeComponent();

            _nearbyMobs = _session.CurrentNearbyMobs;

            _mobIconA = new BitmapImage((new Uri(
                $"{AppDomain.CurrentDomain.BaseDirectory}./Images/Icons/Mob Icons/A.png",
                UriKind.Absolute)));

            _mobIconB = new BitmapImage((new Uri(
                $"{AppDomain.CurrentDomain.BaseDirectory}./Images/Icons/Mob Icons/B.png",
                UriKind.Absolute)));

            _mobIconS = new BitmapImage((new Uri(
                $"{AppDomain.CurrentDomain.BaseDirectory}./Images/Icons/Mob Icons/S.png",
                UriKind.Absolute)));

            _mobIconSS = new BitmapImage((new Uri(
                $"{AppDomain.CurrentDomain.BaseDirectory}./Images/Icons/Mob Icons/SS.png",
                UriKind.Absolute)));

            _session.CurrentPlayer.CoordsChanged += PlayerIconMove;
            _session.CurrentNearbyMobs.CollectionChanged += AddNearbyMobIcon;
        }



        private void PlayerIconMove(object o, Coords coords)
        {
            _ = Task.Run(() =>
            {

                _playerIconX = UpdatePositionOnMapImage(coords.X);
                _playerIconY = UpdatePositionOnMapImage(coords.Y);
                //map (1,1) to (41.9,41.9), adjust for player coord,
                //separate map image size into 41 chunks,
                //each in-game whole coordinate = map image pixel / 41

                //Trace.WriteLine($"X = {coords.X}, y = {coords.Y}");
                //Trace.WriteLine($"X = {_playerIconX}, y = {_playerIconY}");

                Application.Current.Resources["_playerIconX"] = _playerIconX;
                Application.Current.Resources["_playerIconY"] = _playerIconY;

                //Radius detection circle stuff
                Application.Current.Resources["_playerRadiusX"] = _playerIconX - (PlayerRadius.ActualWidth / 2) + (PlayerIcon.ActualWidth / 2);
                Application.Current.Resources["_playerRadiusY"] = _playerIconY - (PlayerRadius.ActualHeight / 2) + (PlayerIcon.ActualHeight / 2);

                Application.Current.Resources["_playerRadiusWidth"] = (MapImage.ActualHeight / 41) * 4; //roughly 2 squares --actual detection seems a little larger.
                Application.Current.Resources["_playerRadiusHeight"] = (MapImage.ActualHeight / 41) * 4;
                //End Radius Circle stuff


                //put this into own method, add event for player.heading? or good enough.
                _playerIconRotation = _session.CurrentPlayer.Heading * (180 / Math.PI);
                _playerIconRotation = Math.Abs(_playerIconRotation - 180);

                Application.Current.Resources["_playerIconRotation"] = _playerIconRotation;

                //update tooltip info
                var _playerToolTipInfo = ((Player)o).ToString();
                Application.Current.Resources["_playerToolTipInfo"] = _playerToolTipInfo;
            });
        }

        private void AddNearbyMobIcon(object o, NotifyCollectionChangedEventArgs e)
        {
            int ACount = 0;
            int BCount = 0;
            int SCount = 0;
            int SSCount = 0;


            #region idk could use something like this instead to dynamically add as many as needed, but need to workout how to remove/update em... list?

            /*
                        Dispatcher.Invoke(() =>
                        {
                            var image = new Image();
                            image.Source = _mobIconA; //etc
                            image.HorizontalAlignment = HorizontalAlignment.Center;
                            image.VerticalAlignment = VerticalAlignment.Center;
                            image.Height = 64;
                            image.Width = 64;

                            PlayerIconCanvas.Children.Add(image);
                            Canvas.SetTop(image, UpdatePositionOnMapImage(m.Coordinates.X));
                            Canvas.SetLeft(image, UpdatePositionOnMapImage(m.Coordinates.Y));
                        });
                    }*/

            #endregion

            foreach (var m in (ObservableCollection<Mob>) o)
            {
                if (m.Rank == "A")
                {
                    ACount++;
                }
            }

            if (ACount <= 1)
            {
                A1 = null;
                A2 = null;
                this.Dispatcher.Invoke(() =>
                    {
                        ARank.Source = null;
                        ARank2.Source = null;
                    }
                );
            }

            ACount = 0;

            //can prob simplify/refactor this...
                foreach (var m in (ObservableCollection<Mob>)o)
            {
                if (m.Rank == "A")
                {
                    ACount++;
                    A1 ??= m;
                    var x = m.Coordinates.X;
                    var y = m.Coordinates.Y;

                    {

                        this.Dispatcher.Invoke(() =>
                        {
                            if (ARank.Source == null || ARank2.Source == null)
                            {
                                ARank.Source = _mobIconA;
                                
                                if (m.Name != A1.Name)
                                {
                                    ARank2.Source = _mobIconSS;
                                    A2 = m;
                                }
                            }
                        });
                        //Gajasura
                        /* _ = Task.Run(() =>
                         {
                             _ARankIconX = UpdatePositionOnMapImageForMobs(A1.Coordinates.X);
                             _ARankIconY = UpdatePositionOnMapImageForMobs(A1.Coordinates.Y);



                                 Application.Current.Resources["_ARankIconX"] = _ARankIconX;
                                 Application.Current.Resources["_ARankIconY"] = _ARankIconY;

                             if (A2 != null)
                             {
                                 Application.Current.Resources["_ARank2IconX"] = UpdatePositionOnMapImageForMobs(A2.Coordinates.Y);
                                 Application.Current.Resources["_ARank2IconY"] = UpdatePositionOnMapImageForMobs(A2.Coordinates.X);
                             }

                         });*/
                        m.CoordsChanged += UpdateNearbyMobIcon;
                    }
                }
                else if (m.Rank == "B")
                {
                    BCount++;

                    this.Dispatcher.Invoke(() =>
                    {
                        BRank.Source = _mobIconB;
                    });

                 /*   _BRankIconX = UpdatePositionOnMapImageForMobs(m.Coordinates.X);
                    _BRankIconY = UpdatePositionOnMapImageForMobs(m.Coordinates.Y);

                    Application.Current.Resources["_BRankIconX"] = _BRankIconX;
                    Application.Current.Resources["_BRankIconY"] = _BRankIconY;*/
                    m.CoordsChanged += UpdateNearbyMobIcon;

                }
                else if (m.Rank == "S")
                {
                    SCount++;

                    this.Dispatcher.Invoke(() => { SRank.Source = _mobIconS; });

                    Trace.WriteLine("S added");

                   /* _SRankIconX = UpdatePositionOnMapImageForMobs(m.Coordinates.X);
                    _SRankIconY = UpdatePositionOnMapImageForMobs(m.Coordinates.Y);

                    Application.Current.Resources["_SRankIconX"] = _SRankIconX;
                    Application.Current.Resources["_SRankIconY"] = _SRankIconY;*/
                    m.CoordsChanged += UpdateNearbyMobIcon;
                }
                else if (m.Rank == "SS")
                {
                    SSCount++;

                    this.Dispatcher.Invoke(() => { SSRank.Source = _mobIconSS; });

                    Trace.WriteLine("SS added");

                  /*  _SSRankIconX = UpdatePositionOnMapImageForMobs(m.Coordinates.X);
                    _SSRankIconY = UpdatePositionOnMapImageForMobs(m.Coordinates.Y);

                    Application.Current.Resources["_SSRankIconX"] = _SSRankIconX;
                    Application.Current.Resources["_SSRankIconY"] = _SSRankIconY;*/
                    m.CoordsChanged += UpdateNearbyMobIcon;
                }
            }

            if (ACount == 0)
            {
                Dispatcher.Invoke(() => { 
                    ARank.Source = null;
                    ARank2.Source = null;
                    A1 = null;
                    A2 = null;
                });
            }
            if (BCount == 0)
            {
                Dispatcher.Invoke(() => { BRank.Source = null; });
            }
            if (SCount == 0)
            {
                Dispatcher.Invoke(() => { SRank.Source = null; });
            }
            if (SSCount == 0)
            {
                Dispatcher.Invoke(() => { SSRank.Source = null; });
            }
        }

        private void UpdateNearbyMobIcon(object o, Coords coords)
        {
            if (o is Mob mob)
            {
                var rank = mob.Rank;

                var iconX = UpdatePositionOnMapImageForMobs(mob.Coordinates.X);
                var iconY = UpdatePositionOnMapImageForMobs(mob.Coordinates.Y);

                var toolTipInfo = mob.ToString();

                if (rank == "A")
                {
                    if (mob.Name == A1.Name)
                    {
                        Application.Current.Resources["_ARankIconX"] = iconX;
                        Application.Current.Resources["_ARankIconY"] = iconY;
                        Application.Current.Resources["_nearbyA"] = toolTipInfo;


                        if (mob.HPPercent == 0)
                        {
                            Dispatcher.Invoke(() => ARank.Source = null);
                            A1 = null;
                        }
                    }
                    else
                    {
                        Application.Current.Resources["_ARank2IconX"] = iconX;
                        Application.Current.Resources["_ARank2IconY"] = iconY;
                        Application.Current.Resources["_nearbyA2"] = toolTipInfo;


                        if (mob.HPPercent == 0)
                        {
                            Dispatcher.Invoke(() => ARank2.Source = null);
                            A2 = null;
                        }
                    }

                    Trace.WriteLine($"A1: {A1?.ToString()}");
                    Trace.WriteLine($"A2: {A2?.ToString()}");
                    Trace.WriteLine($"-----------------------");
                }
                else if (rank == "B")
                {
                    Application.Current.Resources["_BRankIconX"] = iconX;
                    Application.Current.Resources["_BRankIconY"] = iconY;

                    Application.Current.Resources["_nearbyB"] = toolTipInfo;
                    if (mob.HPPercent == 0)
                    {
                        Dispatcher.Invoke(() => BRank.Source = null);
                    }
                }
                if (rank == "S")
                {
                    Application.Current.Resources["_SRankIconX"] = iconX;
                    Application.Current.Resources["_SRankIconY"] = iconY;
                    Application.Current.Resources["_nearbyS"] = toolTipInfo;
                    if (mob.HPPercent == 0)
                    {
                        Dispatcher.Invoke(() => SRank.Source = null);
                    }
                }
                if (rank == "SS")
                {
                    Application.Current.Resources["_SSRankIconX"] = iconX;
                    Application.Current.Resources["_SSRankIconY"] = iconY;
                    Application.Current.Resources["_nearbySS"] = toolTipInfo;
                    if (mob.HPPercent == 0)
                    {
                        Dispatcher.Invoke(() => SSRank.Source = null);
                    }
                }
            }
        }


        //helpers

        private double UpdatePositionOnMapImage(double coordinateValue)
        {
            //image height and width should be the same
            //Trace.WriteLine($"{((coordinateValue - 1) * (MapImage.ActualHeight / 41) - PlayerIcon.ActualHeight / 2)}");
            return ((coordinateValue - 1) * (MapImage.ActualHeight / 41) - PlayerIcon.ActualHeight / 2);

        }

        //idk if i decide to make icons resizable...
        private double UpdatePositionOnMapImageForMobs(double coordinateValue)
        {
            var mobIconHeight = 64; //make this settable somewhere....? and ARank, BRank, etc, height and width equal this...
            //image height and width should be the same
            return ((coordinateValue - 1) * (MapImage.ActualHeight / 41) - mobIconHeight / 2);
        }



        #region Mouse Over ToolTips Using PopUp

        private double MouseOverPositionOnMapImage(double coordinateValue)
        {
            var mapBlock = MapImage.ActualHeight / 41;

            return (coordinateValue / mapBlock) + 1; //plus 1 because ffxiv maps start at (1, 1)
        }

        private void MapImage_OnMouseMove(object sender, MouseEventArgs e)
        {

            var X = MouseOverPositionOnMapImage(e.GetPosition(MapImage).X);
            var Y = MouseOverPositionOnMapImage(e.GetPosition(MapImage).Y);

            Application.Current.Resources["_mapMouseOver"] = $"{X,0:0.0}, {Y,0:0.0}";

            MapTT.IsOpen = true;
            MapTT.HorizontalOffset = e.GetPosition(MapImage).X + 30;
            MapTT.VerticalOffset = e.GetPosition(MapImage).Y - 40;
        }

        private void MapImage_OnMouseLeave(object sender, MouseEventArgs e)
        {
            //this doesn't work 100% :(
            if (!MapTT.IsMouseOver && !MapImage.IsMouseOver)
            {
                MapTT.IsOpen = false;
            }
        }

        private void PlayerIcon_OnMouseMove(object sender, MouseEventArgs e)
        {
            PlayerTT.IsOpen = true;
            PlayerTT.HorizontalOffset = e.GetPosition(MapImage).X + 80;
            PlayerTT.VerticalOffset = e.GetPosition(MapImage).Y - 40;

        }

        private void PlayerIcon_OnMouseLeave(object sender, MouseEventArgs e)
        {
            PlayerTT.IsOpen = false;
        }

        private void ARank_OnMouseMove(object sender, MouseEventArgs e)
        {
            ARankTT.IsOpen = true;
            ARankTT.HorizontalOffset = e.GetPosition(MapImage).X + 80;
            ARankTT.VerticalOffset = e.GetPosition(MapImage).Y - 40;
        }

        private void ARank_OnMouseLeave(object sender, MouseEventArgs e)
        {
            ARankTT.IsOpen = false;
        }

        private void ARank2_OnMouseMove(object sender, MouseEventArgs e)
        {
            ARank2TT.IsOpen = true;
            ARank2TT.HorizontalOffset = e.GetPosition(MapImage).X + 80;
            ARank2TT.VerticalOffset = e.GetPosition(MapImage).Y - 40;
        }

        private void ARank2_OnMouseLeave(object sender, MouseEventArgs e)
        {
            ARank2TT.IsOpen = false;
        }

        private void BRank_OnMouseMove(object sender, MouseEventArgs e)
        {
            BRankTT.IsOpen = true;
            BRankTT.HorizontalOffset = e.GetPosition(MapImage).X + 80;
            BRankTT.VerticalOffset = e.GetPosition(MapImage).Y - 40;
        }

        private void BRank_OnMouseLeave(object sender, MouseEventArgs e)
        {
            BRankTT.IsOpen = false;
        }
        private void SRank_OnMouseMove(object sender, MouseEventArgs e)
        {
            SRankTT.IsOpen = true;
            SRankTT.HorizontalOffset = e.GetPosition(MapImage).X + 80;
            SRankTT.VerticalOffset = e.GetPosition(MapImage).Y - 40;
        }

        private void SRank_OnMouseLeave(object sender, MouseEventArgs e)
        {
            SRankTT.IsOpen = false;
        }

        private void SSRank_OnMouseMove(object sender, MouseEventArgs e)
        {
            SSRankTT.IsOpen = true;
            SSRankTT.HorizontalOffset = e.GetPosition(MapImage).X + 80;
            SSRankTT.VerticalOffset = e.GetPosition(MapImage).Y - 40;
        }

        private void SSRank_OnMouseLeave(object sender, MouseEventArgs e)
        {
            SSRankTT.IsOpen = false;
        }

        #endregion
    }
}
