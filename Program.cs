using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace AffixFileConverter
{
    class Program
    {
        static void PrintUsage()
        {
            Console.WriteLine($"Usage: {System.AppDomain.CurrentDomain.FriendlyName}.exe [-i affixes_folder_name] <output_file_name>[.afx]");
            Console.WriteLine("\t-i affixes_folder_name - optional argument. ");
        }
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            string affixFolderName = "affixes";
            string outName = "";
            int outNameIndex = 0;
            if (args.Length > 0)
            {
                if (args[0].ToLower() == "-i")
                {
                    if (args.Length < 3)
                    {
                        PrintUsage();
                        return;
                    }
                    affixFolderName = args[1];
                    outNameIndex = 2;
                }
                outName = args[outNameIndex];

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                Affix affix = new Affix();
                Dictionary<string, Tuple<Dictionary<string, SuffixGroup>, string>> affixes = affix.LoadAffixes(affixFolderName);

                stopwatch.Stop();
                Console.WriteLine($"Час зчитування файлів: {stopwatch.ElapsedMilliseconds}мс");

                if (!outName.EndsWith(".afx"))
                    outName += ".afx";

                try
                {
                    BinaryWriter writer = new BinaryWriter(File.Open(outName, FileMode.Create));

                    writer.Write(affixes.Count);
                    foreach (var affPair in affixes)
                    {
                        writer.Write(affPair.Key.Replace("_", "").Replace('.', '_')); // Назва правила
                        writer.Write(affPair.Value.Item1.Count); // Кількість регулярних виразів у правила
                        foreach(var suffPair in affPair.Value.Item1)
                        {
                            writer.Write(suffPair.Key); // Регулярний вираз правила для перевірки закінчення
                            var suffixes = suffPair.Value.GetAffixes();
                            writer.Write(suffixes.Count); // Кількість суфіксів для регулярного виразу в залежності від роду/множини і відмінку
                            foreach (var suff in suffixes)
                            {
                                writer.Write(suff.GetFrom());
                                writer.Write(suff.GetTo());

                                string tags = suff.GetTags();
                                int idx = tags.IndexOf(':');
                                if (idx >= 0)
                                {
                                    tags = tags.Remove(0, idx + 1);
                                }
                                string[] parts = tags.Split(new string[] { "//" }, StringSplitOptions.RemoveEmptyEntries); // Варіанти для роду/множини
                                writer.Write(parts.Length);
                                foreach (var part in parts)
                                {
                                    string[] arr_data = part.Split(':'); // Рід/множина і список відмінків
                                    if (arr_data.Length > 1)
                                    {
                                        string rid = arr_data[0];
                                        string[] vidms = arr_data[1].Split('/');
                                        writer.Write(vidms.Length);
                                        writer.Write(rid);
                                        foreach (var vidm in vidms)
                                            writer.Write(vidm);
                                    }
                                    else
                                    {
                                        writer.Write(0);
                                    }
                                }

                            }
                        }
                    }
                    writer.Close();
                }
                catch (IOException e)
                {
                    Console.WriteLine($"Помилка при записі файлу: {e.Message}");
                }

                try
                {
                    BinaryReader reader = new BinaryReader(File.Open(outName, FileMode.Open));
                    stopwatch.Reset();
                    stopwatch.Start();
                    int affixCount = reader.ReadInt32();
                    Dictionary<string, Dictionary<string, SuffixGroup>> readAffixes = new Dictionary<string, Dictionary<string, SuffixGroup>>();
                    for (int i = 0; i < affixCount; i++)
                    {
                        string rule = reader.ReadString();
                        int reCount = reader.ReadInt32();
                        Dictionary<string, SuffixGroup> ruleDict = new Dictionary<string, SuffixGroup>();
                        for (int j = 0; j < reCount; j++)
                        {
                            string re = reader.ReadString();
                            int endCount = reader.ReadInt32();
                            SuffixGroup sufGroup = new SuffixGroup(re);
                            for (int k = 0; k < endCount; k++)
                            {
                                string fromm = reader.ReadString();
                                string to = reader.ReadString();
                                Suffix suf = new Suffix(fromm, to, "");
                                sufGroup.AppendAffix(suf);
                                int len = reader.ReadInt32();
                                for (int l = 0; l < len; l++)
                                {
                                    int v_len = reader.ReadInt32();
                                    if (v_len > 0) {
                                        string rid = reader.ReadString();
                                        for (int m = 0; m < v_len; m++)
                                        {
                                            string vimd = reader.ReadString();
                                        }
                                    }
                                }
                            }
                            ruleDict[re] = sufGroup;
                        }
                        readAffixes[rule] = ruleDict;
                    }
                    stopwatch.Stop();
                    Console.WriteLine($"Час зчитування файлу: {stopwatch.ElapsedMilliseconds}мс");
                }
                catch (IOException e)
                {
                    Console.WriteLine($"Помилка при зчитуванні файлу: {e.Message}");
                }

            }
            else
            {
                PrintUsage();
                return;
            }
        }
    }
}
