using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AffixFileConverter
{
    public class Affix
    {
        private readonly Dictionary<string, Tuple<Dictionary<string, SuffixGroup>, string>> affixMap = new Dictionary<string, Tuple<Dictionary<string, SuffixGroup>, string>>();

        private static readonly Regex reWhitespace = new Regex("[ \t]+");

        public Dictionary<string, Tuple<Dictionary<string, SuffixGroup>, string>> GetAffixMap()
        {
            return affixMap;
        }

        public void LogUsage()
        {
            foreach (var affixItem in affixMap)
            {
                string affixFlag = affixItem.Key;
                Tuple<Dictionary<string, SuffixGroup>, string> affixGroups = affixItem.Value;
                Console.Error.WriteLine(affixFlag + " : " + affixGroups.Item1.Count);
                foreach (var e2 in affixGroups.Item1)
                {
                    Console.Error.WriteLine("\t" + e2.Key + ": " + e2.Value.Counter() + "\t\t(" + e2.Value.GetSize() + ")");
                }
            }
        }

        private static string End(string str, int chars)
        {
            return str.Substring(0, str.Length + chars);
        }

        public Dictionary<string, Tuple<Dictionary<string, SuffixGroup>, string>> LoadAffixFile(FileInfo affFile)
        {
            Dictionary<string, Tuple<Dictionary<string, SuffixGroup>, string>> localAffixMap = new Dictionary<string, Tuple<Dictionary<string, SuffixGroup>, string>>();

            SuffixGroup affixGroup = null;
            Dictionary<string, SuffixGroup> affixGroupMap = null;
            string affixFlag = null;

            List<string> readAllLines;

            try
            {
                readAllLines = File.ReadLines(affFile.FullName, Encoding.UTF8).ToList();
            }
            catch (IOException e)
            {
                throw new Exception("Error reading affix file " + affFile, e);
            }

            string prevComment = null;
            Regex commentCleaner = new Regex(@"^#\s*");
            foreach (string line in readAllLines)
            {
                string trimmedLine = line.Trim();

                if (string.IsNullOrEmpty(trimmedLine))
                    continue;
                if (trimmedLine.StartsWith("#"))
                {
                    prevComment = commentCleaner.Replace(trimmedLine, "");
                    continue;
                }

                if (trimmedLine.Contains("group "))
                {
                    affixFlag = trimmedLine.Split(' ')[1];
                    affixGroupMap = new Dictionary<string, SuffixGroup>();
                    localAffixMap[affixFlag] = new Tuple<Dictionary<string, SuffixGroup>, string>(affixGroupMap, prevComment);
                    continue;
                }

                prevComment = null;

                if (trimmedLine.EndsWith(":"))
                {
                    string match = End(trimmedLine, -1);

                    if (match.Contains(" -"))
                    {
                        string[] splits = match.Split(new[] { " -" }, StringSplitOptions.None);
                        affixGroup = new SuffixGroup(End(splits[0], -1), splits[1]);
                    }
                    else
                    {
                        affixGroup = new SuffixGroup(match);
                    }

                    if (affixGroupMap.ContainsKey(match))
                    {
                        if (affixFlag != "vr2" || match != "тися")
                        {
                            Console.Error.WriteLine("WARNING: overlapping match " + match + " in " + affixFlag + ":\n\t" + trimmedLine);
                        }
                        affixGroup = affixGroupMap[match];
                    }
                    else
                    {
                        affixGroupMap[match] = affixGroup;
                    }

                    continue;
                }

                string[] halfs = trimmedLine.Split('@');
                string affixes = halfs[0].Trim();

                if (affixes.Contains("#"))
                {
                    affixes = affixes.Split('#')[0].Trim();
                }

                string[] parts = reWhitespace.Split(affixes);

                if (parts.Length > 2)
                {
                    string match = parts[2];
                    if (!affixGroupMap.ContainsKey(match))
                    {
                        affixGroup = new SuffixGroup(match);
                        affixGroupMap[match] = affixGroup;
                    }
                    else
                    {
                        affixGroup = affixGroupMap[match];
                    }
                }

                if (parts.Length > 3)
                {
                    Console.Error.WriteLine("WARNING: extra fields in suffix description " + affixes);
                }

                string tags;
                if (halfs.Length > 1)
                {
                    tags = halfs[1].Trim();
                }
                else
                {
                    tags = "";
                    Console.Error.WriteLine("Empty tags for " + trimmedLine);
                }

                string fromm = parts[0];
                string to = parts[1];

                Suffix affixObj = new Suffix(fromm, to, tags);

                affixGroup.AppendAffix(affixObj);
            }

            foreach (var kvp in localAffixMap)
            {
                affixMap[kvp.Key] = kvp.Value;
            }
            return localAffixMap;
        }

        public Dictionary<string, Tuple<Dictionary<string, SuffixGroup>, string>> LoadAffixes(string filename)
        {
            DirectoryInfo dir = new DirectoryInfo(filename);
            if (!dir.Exists)
                throw new ArgumentException(filename + " is not a directory: " + dir.FullName);

            Console.Error.WriteLine("Loading affixes from directory " + filename);

            var affixMap = dir.GetFiles("*.aff")
                .Select(f => LoadAffixFile(f))
                .SelectMany(dict => dict)
                .ToDictionary(entry => entry.Key, entry => entry.Value);

            if (affixMap.Count == 0)
            {
                throw new Exception("ERROR: Failed to load affixes from " + filename);
            }

            Console.WriteLine("Loaded: " + string.Join(", ", affixMap.Keys));

            return affixMap;
        }
    }
}
