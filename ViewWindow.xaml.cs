﻿using RetroSpy.Readers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace RetroSpy
{
    public partial class ViewWindow : Window, INotifyPropertyChanged
    {
        public void CalculateMagic()
        {
            _magicHeight = Height - _originalHeight;
            _magicWidth = Width - _originalWidth;
        }

        public double GetMagicHeight()
        {
            return _magicHeight;
        }

        public double GetMagicWidth()
        {
            return _magicWidth;
        }

        public double GetHeight()
        {
            return Height - _magicHeight;
        }

        public double GetWidth()
        {
            return Width - _magicWidth;
        }

        public double GetRatio()
        {
            return _originalWidth / _originalHeight;
        }

        private void AdjustImage(Skin.ElementConfig config, Image image, double xRatio, double yRatio)
        {
            uint newX = config.X = (uint)Math.Round(config.OriginalX * xRatio);
            uint newY = config.Y = (uint)Math.Round(config.OriginalY * yRatio);

            int newWidth = (int)Math.Round(config.Width * xRatio);
            int newHeight = (int)Math.Round(config.Height * yRatio);

            image.Margin = new Thickness(newX, newY, 0, 0);
            image.Width = newWidth;
            image.Height = newHeight;
        }

        private void AdjustGrid(Skin.ElementConfig config, Grid grid, double xRatio, double yRatio)
        {
            uint newX = config.X = (uint)Math.Round(config.OriginalX * xRatio);
            uint newY = config.Y = (uint)Math.Round(config.OriginalY * yRatio);

            uint newWidth = config.Width = (uint)Math.Round(config.OriginalWidth * xRatio);
            uint newHeight = config.Height = (uint)Math.Round(config.OriginalHeight * yRatio);

            ((Image)grid.Children[0]).Width = newWidth;
            ((Image)grid.Children[0]).Height = newHeight;

            grid.Margin = new Thickness(newX, newY, 0, 0);
            grid.Width = newWidth;
            grid.Height = newHeight;
        }

        public void AdjustControllerElements()
        {
            ControllerGrid.Width = ((Image)ControllerGrid.Children[0]).Width = GetWidth();
            ControllerGrid.Height = ((Image)ControllerGrid.Children[0]).Height = GetHeight();
            double xRatio = GetWidth() / _originalWidth;
            double yRatio = GetHeight() / _originalHeight;

            foreach (Tuple<Skin.Detail, Image> detail in _detailsWithImages)
            {
                AdjustImage(detail.Item1.Config, detail.Item2, xRatio, yRatio);
            }

            foreach (Tuple<Skin.AnalogTrigger, Grid> trigger in _triggersWithGridImages)
            {
                AdjustGrid(trigger.Item1.Config, trigger.Item2, xRatio, yRatio);
            }

            foreach (Tuple<Skin.Button, Image> button in _buttonsWithImages)
            {
                AdjustImage(button.Item1.Config, button.Item2, xRatio, yRatio);
            }

            foreach (Tuple<Skin.RangeButton, Image> button in _rangeButtonsWithImages)
            {
                AdjustImage(button.Item1.Config, button.Item2, xRatio, yRatio);
            }

            foreach (Tuple<Skin.AnalogStick, Image> stick in _sticksWithImages)
            {
                AdjustImage(stick.Item1.Config, stick.Item2, xRatio, yRatio);
                stick.Item1.XRange = (uint)(stick.Item1.OriginalXRange * xRatio);
                stick.Item1.YRange = (uint)(stick.Item1.OriginalYRange * yRatio);
            }

            foreach (Tuple<Skin.TouchPad, Image> touchpad in _touchPadWithImages)
            {
                AdjustImage(touchpad.Item1.Config, touchpad.Item2, xRatio, yRatio);
                touchpad.Item1.XRange = (uint)(touchpad.Item1.OriginalXRange * xRatio);
                touchpad.Item1.YRange = (uint)(touchpad.Item1.OriginalYRange * yRatio);
            }
        }

        private class NativeMethods
        {
            [DllImport("user32.dll")]
            public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

            [DllImport("user32.dll")]
            public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        }

        private const int GWL_STYLE = -16;
        private const int WS_MAXIMIZEBOX = 0x10000;

        private void Window_SourceInitialized(object sender, EventArgs ea)
        {
            IntPtr hwnd = new WindowInteropHelper((Window)sender).Handle;
            int value = NativeMethods.GetWindowLong(hwnd, GWL_STYLE);
            NativeMethods.SetWindowLong(hwnd, GWL_STYLE, value & ~WS_MAXIMIZEBOX);

            WindowAspectRatio.Register((ViewWindow)sender);
        }

        private double _originalHeight;
        private double _originalWidth;
        private double _magicWidth;
        private double _magicHeight;
        private Skin _skin;
        private IControllerReader _reader;
        private Keybindings _keybindings;
        private BlinkReductionFilter _blinkFilter = new BlinkReductionFilter();
        private List<Tuple<Skin.Detail, Image>> _detailsWithImages = new List<Tuple<Skin.Detail, Image>>();
        private List<Tuple<Skin.Button, Image>> _buttonsWithImages = new List<Tuple<Skin.Button, Image>>();
        private List<Tuple<Skin.TouchPad, Image>> _touchPadWithImages = new List<Tuple<Skin.TouchPad, Image>>();
        private List<Tuple<Skin.RangeButton, Image>> _rangeButtonsWithImages = new List<Tuple<Skin.RangeButton, Image>>();
        private List<Tuple<Skin.AnalogStick, Image>> _sticksWithImages = new List<Tuple<Skin.AnalogStick, Image>>();

        // The triggers images are embedded inside of a Grid element so that we can properly mask leftwards and upwards
        // without the image aligning to the top left of its element.
        private List<Tuple<Skin.AnalogTrigger, Grid>> _triggersWithGridImages = new List<Tuple<Skin.AnalogTrigger, Grid>>();

        /// Expose the enabled status of the low-pass filter for data binding.
        public bool ButtonBlinkReductionEnabled
        {
            get => _blinkFilter.ButtonEnabled;
            set
            {
                _blinkFilter.ButtonEnabled = value;
                OnPropertyChanged("ButtonBlinkReductionEnabled");
            }
        }

        public bool MassBlinkReductionEnabled
        {
            get => _blinkFilter.MassEnabled;
            set
            {
                _blinkFilter.MassEnabled = value;
                OnPropertyChanged("MassBlinkReductionEnabled");
            }
        }

        public bool AnalogBlinkReductionEnabled
        {
            get => _blinkFilter.AnalogEnabled;
            set
            {
                _blinkFilter.AnalogEnabled = value;
                OnPropertyChanged("AnalogBlinkReductionEnabled");
            }
        }

        public bool AllBlinkReductionEnabled
        {
            get => ButtonBlinkReductionEnabled && AnalogBlinkReductionEnabled && MassBlinkReductionEnabled;
            set
            {
                ButtonBlinkReductionEnabled = AnalogBlinkReductionEnabled = MassBlinkReductionEnabled = value;
                OnPropertyChanged("AllBlinkReductionEnabled");
            }
        }

        public ViewWindow(Skin skin, Skin.Background skinBackground, IControllerReader reader, bool staticViewerWindowName)
        {
            InitializeComponent();
            DataContext = this;

            _skin = skin;
            _reader = reader;

            if (staticViewerWindowName)
            {
                Title = "RetroSpy Viewer";
            }
            else
            {
                Title = skin.Name;
            }

            ControllerGrid.Width = Width = _originalWidth = skinBackground.Width;
            ControllerGrid.Height = Height = _originalHeight = skinBackground.Height;
            SolidColorBrush brush = new SolidColorBrush(skinBackground.Color);
            ControllerGrid.Background = brush;

            if (skinBackground.Image != null)
            {
                Image img = new Image
                {
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Source = skinBackground.Image,
                    Stretch = Stretch.Fill,
                    Margin = new Thickness(0, 0, 0, 0),
                    Width = skinBackground.Image.PixelWidth,
                    Height = skinBackground.Image.PixelHeight
                };

                ControllerGrid.Children.Add(img);
            }

            foreach (Skin.Detail detail in _skin.Details)
            {
                if (BgIsActive(skinBackground.Name, detail.Config.TargetBackgrounds, detail.Config.IgnoreBackgrounds))
                {
                    detail.Config.X = detail.Config.OriginalX;
                    detail.Config.Y = detail.Config.OriginalY;
                    Image image = GetImageForElement(detail.Config);
                    _detailsWithImages.Add(new Tuple<Skin.Detail, Image>(detail, image));
                    ControllerGrid.Children.Add(image);
                }
            }

            foreach (Skin.AnalogTrigger trigger in _skin.AnalogTriggers)
            {
                if (BgIsActive(skinBackground.Name, trigger.Config.TargetBackgrounds, trigger.Config.IgnoreBackgrounds))
                {
                    trigger.Config.X = trigger.Config.OriginalX;
                    trigger.Config.Y = trigger.Config.OriginalY;
                    Grid grid = GetGridForAnalogTrigger(trigger);
                    _triggersWithGridImages.Add(new Tuple<Skin.AnalogTrigger, Grid>(trigger, grid));
                    ControllerGrid.Children.Add(grid);
                }
            }

            foreach (Skin.Button button in _skin.Buttons)
            {
                if (BgIsActive(skinBackground.Name, button.Config.TargetBackgrounds, button.Config.IgnoreBackgrounds))
                {
                    button.Config.X = button.Config.OriginalX;
                    button.Config.Y = button.Config.OriginalY;
                    Image image = GetImageForElement(button.Config);
                    _buttonsWithImages.Add(new Tuple<Skin.Button, Image>(button, image));
                    image.Visibility = Visibility.Hidden;
                    ControllerGrid.Children.Add(image);
                }
            }

            foreach (Skin.RangeButton button in _skin.RangeButtons)
            {
                if (BgIsActive(skinBackground.Name, button.Config.TargetBackgrounds, button.Config.IgnoreBackgrounds))
                {
                    button.Config.X = button.Config.OriginalX;
                    button.Config.Y = button.Config.OriginalY;
                    Image image = GetImageForElement(button.Config);
                    _rangeButtonsWithImages.Add(new Tuple<Skin.RangeButton, Image>(button, image));
                    image.Visibility = Visibility.Hidden;
                    ControllerGrid.Children.Add(image);
                }
            }

            foreach (Skin.AnalogStick stick in _skin.AnalogSticks)
            {
                if (BgIsActive(skinBackground.Name, stick.Config.TargetBackgrounds, stick.Config.IgnoreBackgrounds))
                {
                    stick.Config.X = stick.Config.OriginalX;
                    stick.Config.Y = stick.Config.OriginalY;
                    stick.XRange = stick.OriginalXRange;
                    stick.YRange = stick.OriginalYRange;
                    Image image = GetImageForElement(stick.Config);
                    _sticksWithImages.Add(new Tuple<Skin.AnalogStick, Image>(stick, image));
                    if (stick.VisibilityName != "")
                    {
                        image.Visibility = Visibility.Hidden;
                    }

                    ControllerGrid.Children.Add(image);
                }
            }

            foreach (Skin.TouchPad touchpad in _skin.TouchPads)
            {
                if (BgIsActive(skinBackground.Name, touchpad.Config.TargetBackgrounds, touchpad.Config.IgnoreBackgrounds))
                {
                    touchpad.Config.X = touchpad.Config.OriginalX;
                    touchpad.Config.Y = touchpad.Config.OriginalY;
                    touchpad.XRange = touchpad.OriginalXRange;
                    touchpad.YRange = touchpad.OriginalYRange;
                    Image image = GetImageForElement(touchpad.Config);
                    _touchPadWithImages.Add(new Tuple<Skin.TouchPad, Image>(touchpad, image));
                    image.Visibility = Visibility.Hidden;
                    ControllerGrid.Children.Add(image);
                }
            }

            _reader.ControllerStateChanged += Reader_ControllerStateChanged;
            _reader.ControllerDisconnected += Reader_ControllerDisconnected;

            try
            {
                _keybindings = new Keybindings(Keybindings.XML_FILE_PATH, _reader);
            }
            catch (ConfigParseException)
            {
                MessageBox.Show("Error parsing keybindings.xml. Not binding any keys to gamepad inputs");
            }

            MassBlinkReductionEnabled = Properties.Settings.Default.MassFilter;
            AnalogBlinkReductionEnabled = Properties.Settings.Default.AnalogFilter;
            ButtonBlinkReductionEnabled = Properties.Settings.Default.ButtonFilter;
            Topmost = Properties.Settings.Default.TopMost;
        }

        private static bool BgIsActive(string bgName, List<string> eligableBgs, List<string> ignoreBgs)
        {
            if (ignoreBgs.Contains(bgName))
            {
                return false;
            }
            return eligableBgs.Count == 0 || eligableBgs.Contains(bgName);
        }

        private static Image GetImageForElement(Skin.ElementConfig config)
        {
            Image img = new Image
            {
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left,
                Source = config.Image,
                Stretch = Stretch.Fill,
                Margin = new Thickness(config.X, config.Y, 0, 0),
                Width = config.Width,
                Height = config.Height
            };
            return img;
        }

        private static Grid GetGridForAnalogTrigger(Skin.AnalogTrigger trigger)
        {
            Image img = new Image
            {
                VerticalAlignment = VerticalAlignment.Top,

                HorizontalAlignment =
                  trigger.Direction == Skin.AnalogTrigger.DirectionValue.Left
                ? HorizontalAlignment.Right
                : HorizontalAlignment.Left
            };

            img.VerticalAlignment =
                  trigger.Direction == Skin.AnalogTrigger.DirectionValue.Up
                ? VerticalAlignment.Bottom
                : VerticalAlignment.Top;

            img.Source = trigger.Config.Image;
            img.Stretch = Stretch.Fill;
            img.Margin = new Thickness(0, 0, 0, 0);
            img.Width = trigger.Config.Width;
            img.Height = trigger.Config.Height;

            Grid grid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(trigger.Config.X, trigger.Config.Y, 0, 0),
                Width = trigger.Config.Width,
                Height = trigger.Config.Height
            };

            grid.Children.Add(img);

            return grid;
        }

        private void AlwaysOnTop_Click(object sender, RoutedEventArgs e)
        {
            Topmost = !Topmost;
            Properties.Settings.Default.TopMost = Topmost;
        }

        private void AllBlinkReductionEnabled_Click(object sender, RoutedEventArgs e)
        {
            if (ButtonBlinkReductionEnabled && AnalogBlinkReductionEnabled && MassBlinkReductionEnabled)
            {
                AllBlinkReductionEnabled = false;
            }
            else
            {
                AllBlinkReductionEnabled = true;
            }
            Properties.Settings.Default.ButtonFilter = ButtonBlinkReductionEnabled;
            Properties.Settings.Default.AnalogFilter = AnalogBlinkReductionEnabled;
            Properties.Settings.Default.MassFilter = MassBlinkReductionEnabled;
        }

        private void ButtonBlinkReductionEnabled_Click(object sender, RoutedEventArgs e)
        {
            ButtonBlinkReductionEnabled = !ButtonBlinkReductionEnabled;
            Properties.Settings.Default.ButtonFilter = ButtonBlinkReductionEnabled;
        }

        private void AnalogBlinkReductionEnabled_Click(object sender, RoutedEventArgs e)
        {
            AnalogBlinkReductionEnabled = !AnalogBlinkReductionEnabled;
            Properties.Settings.Default.AnalogFilter = AnalogBlinkReductionEnabled;
        }

        private void MassBlinkReductionEnabled_Click(object sender, RoutedEventArgs e)
        {
            MassBlinkReductionEnabled = !MassBlinkReductionEnabled;
            Properties.Settings.Default.MassFilter = MassBlinkReductionEnabled;
        }

        private void Reader_ControllerDisconnected(object sender, EventArgs e)
        {
            if (Dispatcher.CheckAccess())
            {
                Close();
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    Close();
                });
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.Save();
            if (_keybindings != null)
            {
                _keybindings.Finish();
            }
            _reader.Finish();
        }

        private void Reader_ControllerStateChanged(object reader, ControllerState e)
        {
            e = _blinkFilter.Process(e);

            // This assumes you can't press left/right and up/down at the same time.  The code gets more complicated otherwise.
            Dictionary<string, bool> compassDirectionStates = new Dictionary<string, bool>();
            if (e.Buttons.ContainsKey("up") && e.Buttons.ContainsKey("left") && e.Buttons.ContainsKey("right") && e.Buttons.ContainsKey("down"))
            {
                string[] compassDirections = { "north", "northeast", "east", "southeast", "south", "southwest", "west", "northwest" };

                bool[] compassDirectionStatesTemp = new bool[8];
                compassDirectionStatesTemp[0] = e.Buttons["up"];
                compassDirectionStatesTemp[2] = e.Buttons["right"];
                compassDirectionStatesTemp[4] = e.Buttons["down"];
                compassDirectionStatesTemp[6] = e.Buttons["left"];

                if (compassDirectionStatesTemp[0] && compassDirectionStatesTemp[2])
                {
                    compassDirectionStatesTemp[1] = true;
                    compassDirectionStatesTemp[0] = compassDirectionStatesTemp[2] = false;
                }
                else if (compassDirectionStatesTemp[2] && compassDirectionStatesTemp[4])
                {
                    compassDirectionStatesTemp[3] = true;
                    compassDirectionStatesTemp[2] = compassDirectionStatesTemp[4] = false;
                }
                else if (compassDirectionStatesTemp[4] && compassDirectionStatesTemp[6])
                {
                    compassDirectionStatesTemp[5] = true;
                    compassDirectionStatesTemp[4] = compassDirectionStatesTemp[6] = false;
                }
                else if (compassDirectionStatesTemp[6] && compassDirectionStatesTemp[0])
                {
                    compassDirectionStatesTemp[7] = true;
                    compassDirectionStatesTemp[6] = compassDirectionStatesTemp[0] = false;
                }

                for (int i = 0; i < compassDirections.Length; ++i)
                {
                    compassDirectionStates[compassDirections[i]] = compassDirectionStatesTemp[i];
                }
            }

            foreach (Tuple<Skin.Button, Image> button in _buttonsWithImages)
            {
                if (e.Buttons.ContainsKey(button.Item1.Name))
                {
                    if (button.Item2.Dispatcher.CheckAccess())
                    {
                        button.Item2.Visibility = e.Buttons[button.Item1.Name] ? Visibility.Visible : Visibility.Hidden;
                    }
                    else
                    {
                        button.Item2.Dispatcher.Invoke(() =>
                        {
                            button.Item2.Visibility = e.Buttons[button.Item1.Name] ? Visibility.Visible : Visibility.Hidden;
                        });
                    }
                }
                else if (compassDirectionStates.ContainsKey(button.Item1.Name))
                {
                    if (button.Item2.Dispatcher.CheckAccess())
                    {
                        button.Item2.Visibility = compassDirectionStates[button.Item1.Name] ? Visibility.Visible : Visibility.Hidden;
                    }
                    else
                    {
                        button.Item2.Dispatcher.Invoke(() =>
                        {
                            button.Item2.Visibility = compassDirectionStates[button.Item1.Name] ? Visibility.Visible : Visibility.Hidden;
                        });
                    }
                }
            }

            foreach (Tuple<Skin.RangeButton, Image> button in _rangeButtonsWithImages)
            {
                if (!e.Analogs.ContainsKey(button.Item1.Name))
                {
                    continue;
                }

                float value = e.Analogs[button.Item1.Name];
                bool visible = button.Item1.From <= value && value <= button.Item1.To;
                button.Item2.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
            }

            foreach (Tuple<Skin.AnalogStick, Image> stick in _sticksWithImages)
            {
                Skin.AnalogStick skin = stick.Item1;
                Image image = stick.Item2;

                float xrange = (skin.XReverse ? -1 : 1) * skin.XRange;
                float yrange = (skin.YReverse ? 1 : -1) * skin.YRange;

                float x = e.Analogs.ContainsKey(skin.XName)
                      ? skin.Config.X + xrange * e.Analogs[skin.XName]
                      : skin.Config.X;

                float y = e.Analogs.ContainsKey(skin.YName)
                      ? skin.Config.Y + yrange * e.Analogs[skin.YName]
                      : skin.Config.Y;

                Visibility visibility;
                if (skin.VisibilityName != "")
                {
                    visibility = (e.Buttons.ContainsKey(skin.VisibilityName) && e.Buttons[skin.VisibilityName]) ? Visibility.Visible : Visibility.Hidden;
                }
                else
                {
                    visibility = Visibility.Visible;
                }

                if (image.Dispatcher.CheckAccess())
                {
                    image.Margin = new Thickness(x, y, 0, 0);
                    image.Visibility = visibility;
                }
                else
                {
                    image.Dispatcher.Invoke(() =>
                    {
                        image.Margin = new Thickness(x, y, 0, 0);
                        image.Visibility = visibility;
                    });
                }
            }

            foreach (Tuple<Skin.TouchPad, Image> touchpad in _touchPadWithImages)
            {
                Skin.TouchPad skin = touchpad.Item1;

                if (e.Analogs.ContainsKey(skin.XName) && e.Analogs.ContainsKey(skin.YName))
                {
                    // Show
                    double x = (e.Analogs[skin.XName] * skin.XRange) + skin.Config.X - (touchpad.Item2.Width / 2);
                    double y = (e.Analogs[skin.YName] * skin.YRange) + skin.Config.Y - (touchpad.Item2.Height / 2);

                    if (touchpad.Item2.Dispatcher.CheckAccess())
                    {
                        touchpad.Item2.Margin = new Thickness(x, y, 0, 0);
                        touchpad.Item2.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        touchpad.Item2.Dispatcher.Invoke(() =>
                        {
                            touchpad.Item2.Margin = new Thickness(x, y, 0, 0);
                            touchpad.Item2.Visibility = Visibility.Visible;
                        });
                    }
                }
                else
                {
                    if (touchpad.Item2.Dispatcher.CheckAccess())
                    {
                        touchpad.Item2.Visibility = Visibility.Hidden;
                    }
                    else
                    {
                        touchpad.Item2.Dispatcher.Invoke(() =>
                        {
                            touchpad.Item2.Visibility = Visibility.Hidden;
                        });
                    }
                }
            }

            foreach (Tuple<Skin.AnalogTrigger, Grid> trigger in _triggersWithGridImages)
            {
                Skin.AnalogTrigger skin = trigger.Item1;
                Grid grid = trigger.Item2;

                if (!e.Analogs.ContainsKey(skin.Name))
                {
                    continue;
                }

                float val = e.Analogs[skin.Name];
                if (skin.UseNegative)
                {
                    val *= -1;
                }

                if (skin.IsReversed)
                {
                    val = 1 - val;
                }

                if (val < 0)
                {
                    val = 0;
                }

                switch (skin.Direction)
                {
                    case Skin.AnalogTrigger.DirectionValue.Right:
                        if (grid.Dispatcher.CheckAccess())
                        {
                            grid.Width = skin.Config.Width * val;
                        }
                        else
                        {
                            grid.Dispatcher.Invoke(() =>
                            {
                                grid.Width = skin.Config.Width * val;
                            });
                        }
                        break;

                    case Skin.AnalogTrigger.DirectionValue.Left:
                        float width = skin.Config.Width * val;
                        float offx = skin.Config.Width - width;
                        if (grid.Dispatcher.CheckAccess())
                        {
                            grid.Margin = new Thickness(skin.Config.X + offx, skin.Config.Y, 0, 0);
                            grid.Width = width;
                        }
                        else
                        {
                            grid.Dispatcher.Invoke(() =>
                            {
                                grid.Margin = new Thickness(skin.Config.X + offx, skin.Config.Y, 0, 0);
                                grid.Width = width;
                            });
                        }
                        break;

                    case Skin.AnalogTrigger.DirectionValue.Down:
                        if (grid.Dispatcher.CheckAccess())
                        {
                            grid.Height = skin.Config.Height * val;
                        }
                        else
                        {
                            grid.Dispatcher.Invoke(() =>
                            {
                                grid.Height = skin.Config.Height * val;
                            });
                        }
                        break;

                    case Skin.AnalogTrigger.DirectionValue.Up:
                        float height = skin.Config.Height * val;
                        float offy = skin.Config.Height - height;
                        if (grid.Dispatcher.CheckAccess())
                        {
                            grid.Margin = new Thickness(skin.Config.X, skin.Config.Y + offy, 0, 0);
                            grid.Height = height;
                        }
                        else
                        {
                            grid.Dispatcher.Invoke(() =>
                            {
                                grid.Margin = new Thickness(skin.Config.X, skin.Config.Y + offy, 0, 0);
                                grid.Height = height;
                            });
                        }
                        break;

                    case Skin.AnalogTrigger.DirectionValue.Fade:
                        if (grid.Dispatcher.CheckAccess())
                        {
                            grid.Height = skin.Config.Height;
                            grid.Width = skin.Config.Width;
                            grid.Opacity = val;
                        }
                        else
                        {
                            grid.Dispatcher.Invoke(() =>
                            {
                                grid.Height = skin.Config.Height;
                                grid.Width = skin.Config.Width;
                                grid.Opacity = val;
                            });
                        }
                        break;
                }
            }
        }

        // INotifyPropertyChanged interface implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    internal class WindowAspectRatio
    {
        private readonly double _ratio;
        private ViewWindow _window;
        private bool _calculatedMagic;

        private WindowAspectRatio(ViewWindow window)
        {
            _calculatedMagic = false;
            _window = window;
            _ratio = window.GetRatio();
            ((HwndSource)HwndSource.FromVisual(window)).AddHook(DragHook);
        }

        public static void Register(ViewWindow window)
        {
            new WindowAspectRatio(window);
        }

        internal enum WM
        {
            WINDOWPOSCHANGING = 0x0046,
        }

        [Flags()]
        public enum SWP
        {
            NoMove = 0x2,
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct WINDOWPOS
        {
            public IntPtr hwnd;
            public IntPtr hwndInsertAfter;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public int flags;
        }

        private IntPtr DragHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handeled)
        {
            if (!_calculatedMagic)
            {
                _window.CalculateMagic();
                _calculatedMagic = true;
            }

            if ((WM)msg == WM.WINDOWPOSCHANGING)
            {
                WINDOWPOS position = (WINDOWPOS)Marshal.PtrToStructure(lParam, typeof(WINDOWPOS));

                if ((position.flags & (int)SWP.NoMove) != 0 ||
                    HwndSource.FromHwnd(hwnd).RootVisual == null)
                {
                    return IntPtr.Zero;
                }

                double magicWidth = _window.GetMagicWidth();
                double magicHeight = _window.GetMagicHeight();

                position.cx = (int)(magicWidth + ((position.cy - magicHeight) * _ratio));

                Marshal.StructureToPtr(position, lParam, true);
                handeled = true;

                _window.AdjustControllerElements();
            }

            return IntPtr.Zero;
        }
    }
}