﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

namespace RetroSpy
{
    public class Skin
    {
        public class ElementConfig
        {
            public BitmapImage Image;
            public uint X, Y, OriginalX, OriginalY, Width, Height, OriginalWidth, OriginalHeight;
            public List<string> TargetBackgrounds { get; set; }
            public List<string> IgnoreBackgrounds { get; set; }
        }

        public class Background
        {
            public string Name { get; set; }
            public BitmapImage Image { get; set; }
            public Color Color { get; set; }
            public uint Width, Height;
        }

        public class Detail
        {
            public string Name { get; set; }
            public ElementConfig Config;
        }

        public class Button
        {
            public ElementConfig Config;
            public string Name;
        }

        public class RangeButton
        {
            public ElementConfig Config;
            public string Name;
            public float From, To;
        }

        public class AnalogStick
        {
            public ElementConfig Config;
            public string XName, YName, VisibilityName;
            public uint XRange, YRange;
            public uint OriginalXRange, OriginalYRange;
            public bool XReverse, YReverse;
        }

        public class AnalogTrigger
        {
            public enum DirectionValue { Up, Down, Left, Right, Fade }

            public ElementConfig Config;
            public string Name;
            public DirectionValue Direction;
            public bool IsReversed;
            public bool UseNegative;
        }

        public class TouchPad
        {
            public ElementConfig Config;
            public string XName, YName;
            public uint XRange, YRange;
            public uint OriginalXRange, OriginalYRange;
        }

        public string Name { get; private set; }
        public string Author { get; private set; }
        public InputSource Type { get; private set; }

        private List<Background> _backgrounds = new List<Background>();
        public IReadOnlyList<Background> Backgrounds => _backgrounds;

        private List<Detail> _details = new List<Detail>();
        public IReadOnlyList<Detail> Details => _details;

        private List<Button> _buttons = new List<Button>();
        public IReadOnlyList<Button> Buttons => _buttons;

        private List<RangeButton> _rangeButtons = new List<RangeButton>();
        public IReadOnlyList<RangeButton> RangeButtons => _rangeButtons;

        private List<AnalogStick> _analogSticks = new List<AnalogStick>();
        public IReadOnlyList<AnalogStick> AnalogSticks => _analogSticks;

        private List<AnalogTrigger> _analogTriggers = new List<AnalogTrigger>();
        public IReadOnlyList<AnalogTrigger> AnalogTriggers => _analogTriggers;

        private List<TouchPad> _touchPads = new List<TouchPad>();
        public IReadOnlyList<TouchPad> TouchPads => _touchPads;

        // ----------------------------------------------------------------------------------------------------------------

        private Skin()
        { }

        private Skin(string folder, List<Skin> generatedSkins)
        {
            string skinPath = Path.Combine(Environment.CurrentDirectory, folder);

            if (!File.Exists(Path.Combine(skinPath, "skin.xml")))
            {
                //throw new ConfigParseException ("Could not find skin.xml for skin at '"+folder+"'.");
                return;
            }
            XDocument doc = XDocument.Load(Path.Combine(skinPath, "skin.xml"));

            Name = ReadStringAttr(doc.Root, "name");
            Author = ReadStringAttr(doc.Root, "author");

            string typeStr = ReadStringAttr(doc.Root, ("type"));
            string[] typesVec = typeStr.Split(';');

            List<InputSource> types = new List<InputSource>();

            foreach (string type in typesVec)
            {
                InputSource TempType = InputSource.ALL.First(x => x.TypeTag == type);

                if (TempType == null)
                {
                    throw new ConfigParseException("Illegal value specified for skin attribute 'type'.");
                }
                types.Add(TempType);
            }

            int i = 0;
            foreach (InputSource inputSource in types)
            {
                Skin TempSkin = null;
                if (i == 0)
                {
                    TempSkin = this;
                    i++;
                }
                else
                {
                    TempSkin = new Skin();
                }

                TempSkin.LoadSkin(Name, Author, inputSource, doc, skinPath);
                generatedSkins.Add(TempSkin);
            }
        }

        public void LoadSkin(string name, string author, InputSource type, XDocument doc, string skinPath)
        {
            Name = name;
            Author = author;
            Type = type;

            IEnumerable<XElement> bgElems = doc.Root.Elements("background");

            if (bgElems.Count() < 1)
            {
                throw new ConfigParseException("Skin must contain at least one background.");
            }

            foreach (XElement elem in bgElems)
            {
                string imgPath = ReadStringAttr(elem, "image", false);
                BitmapImage image = null;
                uint width = 0;
                uint height = 0;
                if (!string.IsNullOrEmpty(imgPath))
                {
                    image = LoadImage(skinPath, imgPath);
                    width = (uint)image.PixelWidth;
                    IEnumerable<XAttribute> widthAttr = elem.Attributes("width");
                    if (widthAttr.Count() > 0)
                    {
                        width = uint.Parse(widthAttr.First().Value);
                    }

                    height = (uint)image.PixelHeight;
                    IEnumerable<XAttribute> heightAttr = elem.Attributes("height");
                    if (heightAttr.Count() > 0)
                    {
                        height = uint.Parse(heightAttr.First().Value);
                    }
                }
                else
                {
                    IEnumerable<XAttribute> widthAttr = elem.Attributes("width");
                    if (widthAttr.Count() > 0)
                    {
                        width = uint.Parse(widthAttr.First().Value);
                    }

                    IEnumerable<XAttribute> heightAttr = elem.Attributes("height");
                    if (heightAttr.Count() > 0)
                    {
                        height = uint.Parse(heightAttr.First().Value);
                    }

                    if (width == 0 || height == 0)
                    {
                        throw new ConfigParseException("Element 'background' should either define 'image' with optionally 'width' and 'height' or both 'width' and 'height'.");
                    }
                }
                _backgrounds.Add(new Background
                {
                    Name = ReadStringAttr(elem, "name"),
                    Image = image,
                    Color = ReadColorAttr(elem, "color", false),
                    Width = width,
                    Height = height
                });
            }

            foreach (XElement elem in doc.Root.Elements("detail"))
            {
                _details.Add(new Detail
                {
                    Config = ParseStandardConfig(skinPath, elem),
                    Name = ReadStringAttr(elem, "name"),
                });
            }

            foreach (XElement elem in doc.Root.Elements("button"))
            {
                _buttons.Add(new Button
                {
                    Config = ParseStandardConfig(skinPath, elem),
                    Name = ReadStringAttr(elem, "name")
                });
            }

            foreach (XElement elem in doc.Root.Elements("rangebutton"))
            {
                float from = ReadFloatConfig(elem, "from");
                float to = ReadFloatConfig(elem, "to");

                if (from > to)
                {
                    throw new ConfigParseException("Rangebutton 'from' field cannot be greater than 'to' field.");
                }

                _rangeButtons.Add(new RangeButton
                {
                    Config = ParseStandardConfig(skinPath, elem),
                    Name = ReadStringAttr(elem, "name"),
                    From = from,
                    To = to
                });
            }

            foreach (XElement elem in doc.Root.Elements("stick"))
            {
                _analogSticks.Add(new AnalogStick
                {
                    Config = ParseStandardConfig(skinPath, elem),
                    XName = ReadStringAttr(elem, "xname"),
                    YName = ReadStringAttr(elem, "yname"),
                    VisibilityName = ReadStringAttr(elem, "visname", false),
                    XRange = ReadUintAttr(elem, "xrange"),
                    OriginalXRange = ReadUintAttr(elem, "xrange"),
                    YRange = ReadUintAttr(elem, "yrange"),
                    OriginalYRange = ReadUintAttr(elem, "yrange"),
                    XReverse = ReadBoolAttr(elem, "xreverse"),
                    YReverse = ReadBoolAttr(elem, "yreverse")
                });
            }

            foreach (XElement elem in doc.Root.Elements("touchpad"))
            {
                _touchPads.Add(new TouchPad
                {
                    Config = ParseStandardConfig(skinPath, elem),
                    XName = ReadStringAttr(elem, "xname"),
                    YName = ReadStringAttr(elem, "yname"),
                    XRange = ReadUintAttr(elem, "xrange"),
                    OriginalXRange = ReadUintAttr(elem, "xrange"),
                    YRange = ReadUintAttr(elem, "yrange"),
                    OriginalYRange = ReadUintAttr(elem, "yrange"),
                });
            }

            foreach (XElement elem in doc.Root.Elements("analog"))
            {
                IEnumerable<XAttribute> directionAttrs = elem.Attributes("direction");
                if (directionAttrs.Count() < 1)
                {
                    throw new ConfigParseException("Element 'analog' needs attribute 'direction'.");
                }

                AnalogTrigger.DirectionValue dir;

                switch (directionAttrs.First().Value)
                {
                    case "up": dir = AnalogTrigger.DirectionValue.Up; break;
                    case "down": dir = AnalogTrigger.DirectionValue.Down; break;
                    case "left": dir = AnalogTrigger.DirectionValue.Left; break;
                    case "right": dir = AnalogTrigger.DirectionValue.Right; break;
                    case "fade": dir = AnalogTrigger.DirectionValue.Fade; break;
                    default: throw new ConfigParseException("Element 'analog' attribute 'direction' has illegal value. Valid values are 'up', 'down', 'left', 'right', 'fade'.");
                }

                _analogTriggers.Add(new AnalogTrigger
                {
                    Config = ParseStandardConfig(skinPath, elem),
                    Name = ReadStringAttr(elem, "name"),
                    Direction = dir,
                    IsReversed = ReadBoolAttr(elem, "reverse"),
                    UseNegative = ReadBoolAttr(elem, "usenegative")
                });
            }
        }

        private static string ReadStringAttr(XElement elem, string attrName, bool required = true)
        {
            IEnumerable<XAttribute> attrs = elem.Attributes(attrName);
            if (attrs.Count() == 0)
            {
                if (required)
                {
                    throw new ConfigParseException("Required attribute '" + attrName + "' not found on element '" + elem.Name + "'.");
                }
                else
                {
                    return "";
                }
            }
            return attrs.First().Value;
        }

        private static List<string> GetArrayAttr(XElement elem, string attrName, bool required = true)
        {
            IEnumerable<XAttribute> attrs = elem.Attributes(attrName);
            if (attrs.Count() == 0)
            {
                if (required)
                {
                    throw new ConfigParseException("Required attribute '" + attrName + "' not found on element '" + elem.Name + "'. You can use it with ';' for multiple values.");
                }
                else
                {
                    return new List<string>(0);
                }
            }
            return new List<string>(attrs.First().Value.Split(';'));
        }

        private static Color ReadColorAttr(XElement elem, string attrName, bool required)
        {
            Color result = new Color();

            IEnumerable<XAttribute> attrs = elem.Attributes(attrName);
            if (attrs.Count() == 0)
            {
                if (required)
                {
                    throw new ConfigParseException("Required attribute '" + attrName + "' not found on element '" + elem.Name + "'.");
                }
                else
                {
                    return result;
                }
            }
            object converted = ColorConverter.ConvertFromString(attrs.First().Value);
            if (result != null)
            {
                result = (Color)converted;
            }
            return result;
        }

        private static float ReadFloatConfig(XElement elem, string attrName)
        {
            if (!float.TryParse(ReadStringAttr(elem, attrName), out float ret))
            {
                throw new ConfigParseException("Failed to parse number for property '" + attrName + "' in element '" + elem.Name + "'.");
            }
            return ret;
        }

        private static uint ReadUintAttr(XElement elem, string attrName)
        {
            if (!uint.TryParse(ReadStringAttr(elem, attrName), out uint ret))
            {
                throw new ConfigParseException("Failed to parse number for property '" + attrName + "' in element '" + elem.Name + "'.");
            }
            return ret;
        }

        private static BitmapImage LoadImage(string skinPath, string fileName)
        {
            try
            {
                return new BitmapImage(new Uri(Path.Combine(skinPath, fileName)));
            }
            catch (Exception e)
            {
                throw new ConfigParseException("Could not load image '" + fileName + "'.", e);
            }
        }

        private static ElementConfig ParseStandardConfig(string skinPath, XElement elem)
        {
            IEnumerable<XAttribute> imageAttr = elem.Attributes("image");
            if (imageAttr.Count() == 0)
            {
                throw new ConfigParseException("Attribute 'image' missing for element '" + elem.Name + "'.");
            }

            BitmapImage image = LoadImage(skinPath, imageAttr.First().Value);

            uint width = (uint)image.PixelWidth;
            IEnumerable<XAttribute> widthAttr = elem.Attributes("width");
            if (widthAttr.Count() > 0)
            {
                width = uint.Parse(widthAttr.First().Value);
            }

            uint height = (uint)image.PixelHeight;
            IEnumerable<XAttribute> heightAttr = elem.Attributes("height");
            if (heightAttr.Count() > 0)
            {
                height = uint.Parse(heightAttr.First().Value);
            }

            uint x = ReadUintAttr(elem, "x");
            uint y = ReadUintAttr(elem, "y");

            List<string> targetBgs = GetArrayAttr(elem, "target", false);
            List<string> ignoreBgs = GetArrayAttr(elem, "ignore", false);

            return new ElementConfig
            {
                X = x,
                Y = y,
                OriginalX = x,
                OriginalY = y,
                Image = image,
                Width = width,
                OriginalWidth = width,
                Height = height,
                OriginalHeight = height,
                TargetBackgrounds = targetBgs,
                IgnoreBackgrounds = ignoreBgs
            };
        }

        private static bool ReadBoolAttr(XElement elem, string attrName, bool dfault = false)
        {
            IEnumerable<XAttribute> attrs = elem.Attributes(attrName);
            if (attrs.Count() == 0)
            {
                return dfault;
            }

            if (attrs.First().Value == "true")
            {
                return true;
            }

            if (attrs.First().Value == "false")
            {
                return false;
            }

            return dfault;
        }

        // ----------------------------------------------------------------------------------------------------------------

        public class LoadResults
        {
            public List<Skin> SkinsLoaded;
            public List<string> ParseErrors;
        }

        public static void LoadAllSkinsFromSubFolder(string path, List<Skin> skins, List<string> errs)
        {
            foreach (string skinDir in Directory.GetDirectories(path))
            {
                try
                {
                    List<Skin> generatedSkins = new List<Skin>();
                    Skin skin;
                    try
                    {
                        skin = new Skin(skinDir, generatedSkins);
                    }
                    catch (Exception e)
                    {
                        errs.Add(skinDir + " :: " + e.Message);
                        continue;
                    }
                    foreach (Skin generatedSkin in generatedSkins)
                    {
                        skins.Add(generatedSkin);
                    }
                }
                catch (ConfigParseException e)
                {
                    errs.Add(skinDir + " :: " + e.Message);
                }
                LoadAllSkinsFromSubFolder(skinDir, skins, errs);
            }
        }

        public static LoadResults LoadAllSkinsFromParentFolder(string path)
        {
            List<Skin> skins = new List<Skin>();
            List<string> errs = new List<string>();

            LoadAllSkinsFromSubFolder(path, skins, errs);

            return new LoadResults
            {
                SkinsLoaded = skins,
                ParseErrors = errs
            };
        }
    }
}