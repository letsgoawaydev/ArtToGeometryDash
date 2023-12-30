using GeometryDashAPI.Attributes;
using GeometryDashAPI.Data;
using GeometryDashAPI.Levels;
using GeometryDashAPI.Levels.GameObjects.Default;
using GeometryDashAPI.Levels.GameObjects.Triggers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;

namespace ArtToGeometryDash
{
    public class Program
    {
        private static string levelName;
        private static int levelRevision;
        private static string fileArt;
        private static int artWidth, artHeight;
        private static Bitmap art;
        private static byte[] filterColor;
        private static bool useAlphaChannel;
        private static byte alphaChannel;
        private static int colorStart;
        private static float scale;
        private static int EditorLayer;
        private static int blockStart;
        private static double userDistance;
        private static bool addTrigger;
        private static float coordX, coordY;
        private static LocalLevels localLevels;
        private static Dictionary<int, int> palette = new Dictionary<int, int>();
        private static Level level;
        private static bool filtering;
        private static string mode;
        private static int HSVChannel;

        const string consoleLine = "________________";

        private static int BytesToInt(byte r, byte g, byte b)
        {
            return r << 16 | g << 8 | b;
        }

        private static int? FindSimilar(byte r, byte g, byte b)
        {
            foreach (var element in palette)
            {
                if (element.Key < 1 && element.Key > 999)
                    continue;
                byte cr = level.Colors[(short)element.Value].Red;
                byte cg = level.Colors[(short)element.Value].Green;
                byte cb = level.Colors[(short)element.Value].Blue;

                double distance = Math.Sqrt(Math.Pow(r - cr, 2) + Math.Pow(g - cg, 2) + Math.Pow(b - cb, 2));
                if (distance < userDistance)
                    return element.Key;
            }
            return null;
        }

        private static bool FindSimilar(byte r, byte g, byte b, byte cr, byte cg, byte cb)
        {
            double distance = Math.Sqrt(Math.Pow(r - cr, 2) + Math.Pow(g - cg, 2) + Math.Pow(b - cb, 2));
            if (distance < userDistance)
                return true;
            return false;
        }

        private static Bitmap OpenResizeArt(string path, int width, int height)
        {
            var img = new Bitmap(path);
            if (artHeight == 0 && artWidth == 0)
            {
                artWidth = img.Width;
                artHeight = img.Height;
                return img;
            }
            else
            {
                var res = new Bitmap(width, height);
                var drawImage = Graphics.FromImage(res);

                drawImage.InterpolationMode = InterpolationMode.Bilinear;
                var destination = new Rectangle(0, 0, width, height);
                var source = new Rectangle(0, 0, img.Width, img.Height);
                drawImage.DrawImage(img, destination, source, GraphicsUnit.Pixel);

                return res;
            }
        }

        private static string RGB2HSVString(byte ri, byte gi, byte bi)
        {
            int h;
            float s, v;
            float max, min;

            float r = (float)ri / 255;
            float g = (float)gi / 255;
            float b = (float)bi / 255;

            max = (float)Math.Max(Math.Max(ri, gi), bi) / 255;
            min = (float)Math.Min(Math.Min(ri, gi), bi) / 255;

            if (max == r && g >= b)
                h = (int)(60 * (g - b) / (max - min));
            else if (max == r && g < b)
                h = (int)(60 * (g - b) / (max - min) + 360);
            else if (max == g)
                h = (int)(60 * (b - r) / (max - min) + 120);
            else
                h = (int)(60 * (r - g) / (max - min) + 240);

            if (h > 180)
                h -= 360;

            if (max == 0)
                s = 0;
            else
                s = 1 - min / max;

            v = max;

            return string.Format("{0}a{1}a{2}a0a0", h, Math.Round(s, 2), Math.Round(v, 2)).Replace(",", ".");
        }

        public static void Main(string[] args)
        {
            Console.Title = "Art To Geometry Dash";
            Welcome();

            localLevels = LocalLevels.LoadFile();
            InputSettings(localLevels);

            Console.Write("How to add image, HSV or RGB (HSV uses only 1 color channel, RGB is only recommended for pixel art)\nWrite 'h' or 'r'\n> ");
            mode = Console.ReadLine().ToLower();
            Console.Clear();
            if (mode == "h")
                InputHSVSettings();
            else
                InputRGBSettings();
            Console.Clear();

            Stopwatch sw = new Stopwatch();
            sw.Restart();

            Console.WriteLine("Loading image ...");
            art = OpenResizeArt(fileArt, artWidth, artHeight);

            Console.WriteLine("Loading level ...");
            if (levelRevision == -1)
                level = new Level(localLevels.GetLevel(levelName));
            else
                level = new Level(localLevels.GetLevel(levelName, levelRevision));
            blockStart = level.CountBlock;

            Console.WriteLine("Creating art...");
            if (mode == "h")
                CreateHSVart();
            else
                CreateRGBart();

            Console.WriteLine("Save...");
            localLevels.GetLevel(levelName).SaveLevel(level);
            localLevels.Save();

            sw.Stop();

            Console.Clear();
            if (mode == "h")
                Console.WriteLine($"Completed!\nUsed blocks: {level.CountBlock - blockStart}\nElapsed time: {sw.ElapsedTicks} ticks ({sw.ElapsedMilliseconds} milliseconds)");
            else
                Console.WriteLine($"Completed!\nUsed colors: {palette.Count}\nUsed blocks: {level.CountBlock - blockStart}\nElapsed time: {sw.ElapsedTicks} ticks ({sw.ElapsedMilliseconds} milliseconds)");
            Console.WriteLine("Press any button to continue.");
            Console.ReadKey();
        }

        private static void Welcome()
        {
            Console.WriteLine("Art To Geometry Dash 2.2");
            Console.WriteLine("The program for the transfer of art and other images in Geometry Dash.");
            Console.WriteLine("Version: 1.0");
            Console.WriteLine($"{consoleLine}\n\nOriginal Authors: Folleach and Relayx");
            Console.WriteLine($"{consoleLine}\n\nFork Author: LetsGoAway");
            Console.WriteLine($"{consoleLine}\n\nContributors: Nodus Lorden (HSV and Resize mode)");
            Console.WriteLine($"{consoleLine}\n\nFor its work, the program uses the library GeometryDashAPI\nhttps://github.com/Folleach/GeometryDashAPI");
            Console.WriteLine($"{consoleLine}\n");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("You must close all instances of the game before making a image, so it\nis recommended to make an empty level before continuing!");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("Press any button to continue.");
            Console.ReadKey();
            Console.Clear();
        }

        private static void InputSettings(LocalLevels levels)
        {

            while (true)
            {
                Console.Write("Image path.\nExample: C:\\images\\my.png\n> ");
                fileArt = Console.ReadLine().Replace("\"", "");
                if (File.Exists(fileArt))
                    break;
                Console.WriteLine($"File '{fileArt}' doesn't exist\n");
            }

            Console.Clear();

            Console.Write("Art Width and height. Type nothing for the actual image's dimensions, otherwise it will\n either be upscaled or downscaled.\nExample: 32 32\n> ");


            string artsize = Console.ReadLine();
            if (artsize == "")
            {
                artWidth = 0;
                artHeight = 0;
            }
            else
            {
                artWidth = int.Parse(artsize.Split(' ')[0]);
                artHeight = int.Parse(artsize.Split(' ')[1]);
            }
            Console.Clear();

            scale = 5;
            /*
                        Console.Write("Alpha color.\nExample 1: 255 255 255 (Cut close to white color)\nExample 2: A 245 (Cuts all pixels with little alpha.)\nExample 3: Nothing typed (for skip) \n> ");
                        string[] al = Console.ReadLine().Split(' ');
                        // Eng a and Rus а
                        if (al.Length > 1)
                        {
                            if (al[0].ToLower() == "a" || al[0].ToLower() == "а")
                            {
                                useAlphaChannel = true;
                                alphaChannel = byte.Parse(al[1]);
                            }
                            else
                            {
                                filterColor = new byte[3];
                                filterColor[0] = byte.Parse(al[0]);
                                filterColor[1] = byte.Parse(al[1]);
                                filterColor[2] = byte.Parse(al[2]);
                            }
                            filtering = true;
                        }
                        else
                            filtering = false;*/
            Console.Clear();

            Console.Write("Editor Layer for art.\n> ");
            EditorLayer = int.Parse(Console.ReadLine());
            Console.Clear();

            Console.Write("Image position in level.\nExample for position in cells: 10 5\nExample for position in coordinates: C 600 300\nUse a position far from 0 when adding a large image, otherwise the level may not open.\n> ");
            string[] coords = Console.ReadLine().Split(' ');
            if (coords[0].ToLower() == "c" || coords[0].ToLower() == "с")
            {
                coordX = float.Parse(coords[1]);
                coordY = float.Parse(coords[2]);
            }
            else
            {
                coordX = float.Parse(coords[0]) * 60 - 15;
                coordY = float.Parse(coords[1]) * 60;
            }
            Console.Clear();

            while (true)
            {
                Console.Write("The name of the level in which the art will be placed.\nRecomended use empty level.\nWARNING: Possible level corruption!\n> ");
                levelName = Console.ReadLine();
                Console.Write("Level revision. Enter nothing if it isnt a revised level.\n> ");
                var rev = Console.ReadLine();
                levelRevision = int.TryParse(rev, out var num) ? num : -1;
                try
                {
                    levels.GetLevel(levelName, levelRevision == -1 ? 0 : levelRevision);
                }
                catch
                {
                    Console.WriteLine($"Level '{levelName}' with revision doesn't exist\n");
                    continue;
                }
                break;
            }

            Console.Clear();
        }

        private static void InputRGBSettings()
        {
            Console.Write("Color similarity ratio.\n0 - Exact color match\nThe higher the value, the less color will be used in the level.\nRecommended value: 10\n> ");
            userDistance = double.Parse(Console.ReadLine(), CultureInfo.InvariantCulture);
            Console.Clear();

            Console.Write("What ID to start creating the palette\n> ");
            colorStart = int.Parse(Console.ReadLine());
            Console.Clear();

            Console.Write("Add a color changing trigger?\nWrite Y or N\n> ");
            string addtrgstr = Console.ReadLine();
            addTrigger = addtrgstr.ToLower() == "yes" || addtrgstr.ToLower() == "y" ? true : false;
            Console.Clear();
        }

        private static void InputHSVSettings()
        {
            if (filtering)
            {
                Console.Write("Color similarity ratio (for deleting hsv colors).\n0 - Exact color match\nThe higher the value, the less color will be used in the level.\nRecommended value: 10\n> ");
                userDistance = double.Parse(Console.ReadLine(), CultureInfo.InvariantCulture);
                Console.Clear();
            }
            Console.Write("One color slot is required, however the actual color will be changed using HSV\n values which dont require any color slots\n (Enter ID for base HSV color)\n> ");
            HSVChannel = int.Parse(Console.ReadLine());
            Console.Clear();
        }

        private static void CreateRGBart()
        {
            int currentColorID = colorStart;

            for (int x = 0; x < art.Width; x += 1)
            {
                for (int y = 0; y < art.Height; y += 1)
                {
                    System.Drawing.Color color = art.GetPixel(x, y);
                    int colorInt = BytesToInt(color.R, color.G, color.B);
                    if (filtering)
                    {
                        if (useAlphaChannel)
                        {
                            if (color.A < alphaChannel)
                                continue;
                        }
                        else
                        {
                            if (FindSimilar(color.R, color.G, color.B, filterColor[0], filterColor[1], filterColor[2]))
                                continue;
                        }
                    }
                    if (!palette.ContainsKey(colorInt))
                    {
                        int? sim = FindSimilar(color.R, color.G, color.B);
                        if (sim == null)
                        {
                            GeometryDashAPI.Levels.Color col = new GeometryDashAPI.Levels.Color(currentColorID);
                            col.Id = currentColorID;
                            col.SetColor(color.R, color.G, color.B);
                            level.AddColor(col);
                            palette.Add(colorInt, currentColorID);
                            if (addTrigger)
                            {
                                level.AddBlock(new PulseTrigger()
                                {
                                    TargetGroupId = currentColorID,
                                    Red = color.R,
                                    Green = color.G,
                                    Blue = color.B,
                                    Hold = 10f,
                                    PositionX = +coordX,
                                    PositionY = 0 + coordY,
                                    EditorL = (short)EditorLayer
                                });
                            }
                            currentColorID++;
                        }
                        else
                        {
                            colorInt = (int)sim;
                        }
                    }
                    ColorBlock block = new ColorBlock(3097);
                    block.WithoutLoaded.Add("21," + palette[colorInt].ToString());
                    block.Scale = scale;
                    block.EditorL = (short)EditorLayer;
                    block.PositionX = (x * 1f * scale) + coordX;
                    block.PositionY = ((art.Height - y) * 1f * scale) + coordY;
                    level.AddBlock(block);
                }
            }
        }

        private static void CreateHSVart()
        {
            GeometryDashAPI.Levels.Color hsv = new GeometryDashAPI.Levels.Color(HSVChannel);
            hsv.Id = HSVChannel;

            hsv.SetColor(byte.MaxValue, byte.MinValue, byte.MinValue);
            level.AddColor(hsv);

            for (int x = 0; x < art.Width; x += 1)
            {
                for (int y = 0; y < art.Height; y += 1)
                {
                    System.Drawing.Color color = art.GetPixel(x, y);
                    if (filtering)
                    {
                        if (useAlphaChannel)
                        {
                            if (color.A < alphaChannel)
                                continue;
                        }
                        else
                        {
                            if (FindSimilar(color.R, color.G, color.B, filterColor[0], filterColor[1], filterColor[2]))
                                continue;
                        }
                    }
                    if (color.A == byte.MinValue)
                    {
                        continue;
                    }
                    ColorBlock block = new ColorBlock(3097);
                    block.WithoutLoaded.Add("21," + HSVChannel.ToString());
                    block.Scale = scale;
                    block.EditorL = (short)EditorLayer;
                    block.PositionX = (x * 1f * scale) + coordX;
                    block.PositionY = ((art.Height - y) * 1f * scale) + coordY;
                    block.WithoutLoaded.Add("41,1");

                    block.WithoutLoaded.Add("43," + RGB2HSVString(color.R, color.G, color.B));
                    level.AddBlock(block);
                }
            }
        }
    }
}
