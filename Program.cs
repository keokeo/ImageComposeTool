using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace ImageComposeTool
{
    class Program
    {
        private const int MaxValue = 16384;
        private const string OutputFolder = "Output";
        private static readonly string ImagesDirectory = Directory.GetCurrentDirectory();
        private const string ConfigFileName = "config.txt";

        private static void ReadConfig(out int row, out int column, out string name)
        {
            row = column = 0;
            name = string.Empty;
            string configFile = Path.Combine(ImagesDirectory, ConfigFileName);
            if (File.Exists(configFile))
            {
                // Read config
                string[] allLines = File.ReadAllLines(configFile);

                foreach (string line in allLines)
                {
                    if (line.ToLower().Contains("row"))
                    {
                        string temp = line.Substring(line.IndexOf(':') + 1);
                        int.TryParse(temp, out row);
                    }
                    else if (line.ToLower().Contains("column"))
                    {
                        string temp = line.Substring(line.IndexOf(':') + 1);
                        int.TryParse(temp, out column);
                    }
                    else if (line.ToLower().Contains("name"))
                    {
                        name = line.Substring(line.IndexOf(':') + 1).Trim();
                    }
                }
            }
        }

        private static void WriteConfig(int row, int column, int total, string name)
        {
            string[] allLines = new string[4];
            
            allLines[0] = string.Format("row: {0}", row);
            allLines[1] = string.Format("column: {0}", column);
            allLines[2] = string.Format("total: {0}", total);
            allLines[3] = string.Format("name: {0}", name);

            string configFile = Path.Combine(ImagesDirectory, ConfigFileName);
            File.WriteAllLines(configFile, allLines);            
        }

        private static void ComposeImages()
        {
            try
            {
                string[] paths = Directory.GetFiles(ImagesDirectory, "*.png");
                List<string> pathList = new List<string>(paths);
                for(int i=paths.Length-2;i>=1;i--)
                {
                    pathList.Add(paths[i]);
                }
                //SortedSet<string> pathList = new SortedSet<string>(paths);
                
                int count = pathList.Count;
                if (count > 0)
                {
                    ImageFormat format = null;
                    string name = string.Empty;
                    int width = 0;
                    int height = 0;
                    using (Image image = Image.FromFile(paths[0]))
                    {
                        format = image.RawFormat;
                        name = string.Empty;
                        width = image.Width;
                        height = image.Height;
                    }
                    int rowCount = 1;
                    int columnCount = 1;

                    ReadConfig(out rowCount, out columnCount, out name);
                    int tempCount = rowCount * columnCount;
                    if (tempCount < count || tempCount - count > Math.Min(rowCount, columnCount))
                    {
                        double temp = Math.Sqrt(count);
                        if (width < height)
                        {
                            rowCount = (int)Math.Round(temp);
                            columnCount = (int)Math.Ceiling(temp);

                            tempCount = rowCount * columnCount;
                            if (tempCount > count)
                            {
                                for (; rowCount > 0; --rowCount)
                                {
                                    if (count % rowCount == 0)
                                    {
                                        columnCount = count / rowCount;
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            columnCount = (int)Math.Round(temp);
                            rowCount = (int)Math.Ceiling(temp);

                            tempCount = columnCount * rowCount;
                            if (tempCount > count)
                            {
                                for (; columnCount > 0; --columnCount)
                                {
                                    if (count % columnCount == 0)
                                    {
                                        rowCount = count / columnCount;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(name))
                    {
                        name = Path.GetFileNameWithoutExtension(paths[0]);
                        name = name.Substring(0, name.LastIndexOf(' '));
                        name += Path.GetExtension(paths[0]);
                    }

                    int totalWidth = width * columnCount;
                    int totalHeight = height * rowCount;
                    float scale = 1.0f;
                    if (totalWidth * totalHeight > MaxValue * MaxValue)
                    {
                        scale = (float)MaxValue / Math.Max(totalWidth, totalHeight);
                        //Console.WriteLine("Width * Height is bigger than Max value!");
                        //return;
                        width = (int)(width * scale);
                        height = (int)(height * scale);
                        totalWidth = width * columnCount;
                        totalHeight = height * rowCount;
                    }

                    while (totalWidth > MaxValue || totalHeight > MaxValue)
                    {
                        if (totalWidth < totalHeight)
                        {
                            rowCount = count / ++columnCount + 1;
                        }
                        else
                        {
                            columnCount = count / ++rowCount + 1;
                        }

                        totalWidth = width * columnCount;
                        totalHeight = height * rowCount;
                    }

                    using (Bitmap bitmap = new Bitmap(totalWidth, totalHeight, PixelFormat.Format32bppArgb))
                    {
                        using (Graphics g = Graphics.FromImage(bitmap))
                        {
                            int i = 0;
                            foreach (string path in pathList)
                            {
                                Console.WriteLine(Path.GetFileName(path));

                                using (Image image = Image.FromFile(path))
                                {
                                   
                                    int columnIndex = i % columnCount;
                                    int rowIndex = i / columnCount;
                                    g.DrawImage(image,new RectangleF(width * columnIndex, height * rowIndex,width,height),new RectangleF(0,0,width,height) ,GraphicsUnit.Pixel);
                                }
                                i++;
                            }
                        }

                        if (!Directory.Exists(OutputFolder))
                        {
                            Directory.CreateDirectory(OutputFolder);
                        }
                        bitmap.Save($"{OutputFolder}/{name}", format);

                        WriteConfig(rowCount, columnCount, pathList.Count, name);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static void Main(string[] args)
        {
            ComposeImages();

            Console.WriteLine("Compose images finished.");
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}
