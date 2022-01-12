﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
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

        private readonly BitmapImage _mobIconA;
        private readonly BitmapImage _mobIconB;
        private readonly BitmapImage _mobIconS;
        private readonly BitmapImage _mobIconSS;

        private readonly List<Image> _mobIconList;
        private readonly List<Popup> _toolTipList;

        private Mob A1;
        private Mob A2;


        public MainMapControl(Session session)
        {
            _mobIconList = new List<Image>();
            _toolTipList = new List<Popup>();

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
            List<String> namesList = new List<string>();

            Dispatcher.Invoke(() =>
            {
                //adding mobs
                foreach (var m in (ObservableCollection<Mob>)o)
                {
                    var cleanedName = RemoveSpecialCharacters(m.Name);

                    namesList.Add(cleanedName);
                    Image mobIcon;

                    //if the mob icon already exists, update tooltip and skip
                    if (_mobIconList.FirstOrDefault(i => i.Name == cleanedName) != null)
                    {
                        UpdateToolTip(m);
                        continue;
                    }
                    mobIcon = m.Rank switch
                    {
                        "A" => new Image
                        { Source = _mobIconA, Name = cleanedName, Height = 64, Width = 64, },
                        "S" => new Image
                        { Source = _mobIconS, Name = cleanedName, Height = 64, Width = 64 },
                        "SS" => new Image
                        { Source = _mobIconSS, Name = cleanedName, Height = 64, Width = 64 },
                        "B" => new Image
                        { Source = _mobIconB, Name = cleanedName, Height = 64, Width = 64 }
                    };

                    mobIcon.MouseMove += Mob_OnMouseMove;
                    mobIcon.MouseLeave += Mob_OnMouseLeave;

                    var tt = CreateToolTip(m);

                    _mobIconList.Add(mobIcon);
                    _toolTipList.Add(tt);


                    PlayerIconCanvas.Children.Add(mobIcon);
                    PlayerIconCanvas.Children.Add(tt);

                    m.CoordsChanged += UpdateNearbyMobIcon;
                }

                var toRemove = new List<Image>();
                var toRemoveTT = new List<Popup>();

                //remove old mobs that aren't nearby anymore
                foreach (var img in _mobIconList)
                {

                    if (!namesList.Contains(img.Name))
                    {
                        var tt = _toolTipList.FirstOrDefault(t => t.Name == $"{img.Name}TT");

                        toRemove.Add(img);
                        toRemoveTT.Add(tt);
                    }
                }

                foreach (var imgToRemove in toRemove)
                {
                    _mobIconList.Remove(imgToRemove);
                    PlayerIconCanvas.Children.Remove(imgToRemove);
                }

                foreach (var popup in toRemoveTT)
                {
                    _toolTipList.Remove(popup);
                    PlayerIconCanvas.Children.Remove(popup);
                }
            });
        }

        private void UpdateNearbyMobIcon(object o, Coords coords)
        {
            if (o is Mob mob)
            {
                var rank = mob.Rank;

                var iconX = UpdatePositionOnMapImageForMobs(mob.Coordinates.X);
                var iconY = UpdatePositionOnMapImageForMobs(mob.Coordinates.Y);

                var toolTipInfo = mob.ToString();

                Dispatcher.Invoke(() =>
                {
                    var mobIconToUpdate = _mobIconList.FirstOrDefault(i => i.Name == RemoveSpecialCharacters(mob.Name));

                    if (mobIconToUpdate == null)
                    {
                        return;
                    }

                    Canvas.SetLeft(mobIconToUpdate, iconX);
                    Canvas.SetTop(mobIconToUpdate, iconY);
                });
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


        private void Mob_OnMouseMove(object sender, MouseEventArgs e)
        {
            var img = sender as Image;

            var tt = _toolTipList.FirstOrDefault(t => t.Name == $"{img.Name}TT");

            tt.IsOpen = true;
            tt.HorizontalOffset = e.GetPosition(MapImage).X + 80;
            tt.VerticalOffset = e.GetPosition(MapImage).Y - 40;

        }

        private void Mob_OnMouseLeave(object sender, MouseEventArgs e)
        {
            var img = sender as Image;

            var tt = _toolTipList.FirstOrDefault(t => t.Name == $"{img.Name}TT");
            if (tt == null)
            {
                return;
            }
            tt.IsOpen = false;
        }
        #endregion



        //helpers
        private string RemoveSpecialCharacters(string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
            {
                if (c is >= '0' and <= '9' or >= 'A' and <= 'Z' or >= 'a' and <= 'z')
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private Popup CreateToolTip(Mob m)
        {
            var pp = new Popup
            {
                Placement = PlacementMode.Relative,
                Name = $"{RemoveSpecialCharacters(m.Name)}TT",
                IsHitTestVisible = false

            };

            var border = new Border
            {
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(5, 2, 5, 2),
                Background = Brushes.Azure
            };

            border.Child = new TextBlock
            {
                Name = $"{RemoveSpecialCharacters(m.Name)}TTText",
                Text = m.ToString()
            };

            pp.Child = border;

            return pp;
        }

        private void UpdateToolTip(Mob m)
        {
            var tt = _toolTipList.FirstOrDefault(t => t.Name == $"{m.Name}TT");

            if (tt == null)
            {
                Trace.WriteLine("TOOLTIP NULL");
                return;

            }

            var border = tt.Child as Border;
            var textBlock = border.Child as TextBlock;
            textBlock.Text = m.ToString();
            Trace.WriteLine("UPDATED");

        }


    }
}
