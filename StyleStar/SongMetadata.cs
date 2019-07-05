﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace StyleStar
{
    public class SongMetadata
    {
        public string FilePath { get; set; }
        public string ChartFullPath { get; set; }
        public bool IsMetadataFile { get; set; }
        public List<SongMetadata> ChildMetadata { get; private set; } = new List<SongMetadata>();
        public string SongFilename { get; set; }
        public Dictionary<int, double> BpmIndex = new Dictionary<int, double>();
        public List<BpmChangeEvent> BpmEvents = new List<BpmChangeEvent>();
        public double PlaybackOffset { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Designer { get; set; }
        public Texture2D AlbumImage { get; set; }
        public string Jacket { get; set; }
        public Difficulty Difficulty { get; set; }
        public int Level { get; set; }
        public Color ColorFore { get; set; }
        public Color ColorBack { get; set; }
        public Color ColorAccent { get; set; }

        public SongMetadata() { }

        public SongMetadata(string fileName)
        {
            Parse(fileName);
        }

        public SongMetadata(SongMetadata parent, string fileName)
        {
            SongFilename = parent.SongFilename;
            PlaybackOffset = parent.PlaybackOffset;
            Title = parent.Title;
            Artist = parent.Artist;
            Designer = parent.Designer;
            ColorFore = parent.ColorFore;
            ColorBack = parent.ColorBack;
            ColorAccent = parent.ColorAccent;
            BpmEvents.AddRange(parent.BpmEvents);

            Parse(fileName);
        }

        private void Parse(string fileName)
        {
            ChartFullPath = Path.GetFullPath(fileName);
            FilePath = Path.GetDirectoryName(fileName) + @"\";
            using (StreamReader sr = new StreamReader(fileName))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();

                    string parse;
                    if (StringExtensions.TrySearchTag(line, "WAVE", out parse))
                        SongFilename = parse;
                    if (StringExtensions.TrySearchTag(line, "WAVEOFFSET", out parse))
                        PlaybackOffset = Convert.ToDouble(parse);
                    if (StringExtensions.TrySearchTag(line, "TITLE", out parse))
                        Title = parse;
                    if (StringExtensions.TrySearchTag(line, "ARTIST", out parse))
                        Artist = parse;
                    if (StringExtensions.TrySearchTag(line, "DESIGNER", out parse))
                        Designer = parse;
                    if (StringExtensions.TrySearchTag(line, "DIFFICULTY", out parse))
                        Difficulty = (Difficulty)Convert.ToInt32(parse);
                    if (StringExtensions.TrySearchTag(line, "PLAYLEVEL", out parse))
                        Level = Convert.ToInt32(parse);
                    if (StringExtensions.TrySearchTag(line, "JACKET", out parse))
                        Jacket = parse;
                    if (StringExtensions.TrySearchTag(line, "COLORFORE", out parse))
                        ColorFore = Util.ParseFromHex(parse);
                    if (StringExtensions.TrySearchTag(line, "COLORBACK", out parse))
                        ColorBack = Util.ParseFromHex(parse);
                    if (StringExtensions.TrySearchTag(line, "COLORACCENT", out parse))
                        ColorAccent = Util.ParseFromHex(parse);

                    if (Regex.IsMatch(line, "(#BPM)"))
                    {
                        string[] bpmParse = line.Split(new string[] { "#BPM", ": " }, StringSplitOptions.RemoveEmptyEntries);
                        if (!BpmIndex.ContainsKey(Convert.ToInt32(bpmParse[0])))
                            BpmIndex.Add(Convert.ToInt32(bpmParse[0]), Convert.ToDouble(bpmParse[1]));
                        else
                            // Print an error here in some log somewhere
                            Console.WriteLine("When parsing " + fileName + ", multiple BPM definitions were found with the same ID number.");
                    }

                    if (StringExtensions.TrySearchTag(line, "CHART", out parse))
                    {
                        IsMetadataFile = true;
                        if (!parse.EndsWith(Defines.ChartExtension))
                            parse += Defines.ChartExtension;
                        ChildMetadata.Add(new SongMetadata(FilePath + parse));
                    }
                }
            }
            if (Jacket == "")
                AlbumImage = Globals.Textures["FallbackJacket"];
            else
            {
                using (FileStream fs = new FileStream(FilePath + Jacket, FileMode.Open))
                {
                    AlbumImage = Texture2D.FromStream(Globals.GraphicsManager.GraphicsDevice, fs);
                }
            }
        }
    }
}
