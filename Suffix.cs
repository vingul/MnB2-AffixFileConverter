using System;
using System.Text.RegularExpressions;

namespace AffixFileConverter
{
    public class Suffix
    {
        private readonly string to;
        private readonly string fromm;
        private readonly string tags;
        private readonly Regex sub_from_sfx;

        public Suffix(string from_, string to_, string tags_)
        {
            fromm = Convert0(from_);
            to = Convert0(to_).Replace("\\", "$");
            tags = tags_;
            sub_from_sfx = new Regex(fromm + "$");
        }

        public string GetTags()
        {
            return tags;
        }

        public string GetFrom()
        {
            return fromm;
        }

        public string GetTo()
        {
            return to;
        }

        public string Apply(string word)
        {
            Match match = sub_from_sfx.Match(word);
            string replaced = sub_from_sfx.Replace(word, to);

            if (replaced == word && !fromm.Equals(to))
            {
                throw new ArgumentException($"Affix wasn't applied {fromm} -> {to} to {word}");
            }

            return replaced;
        }

        public override string ToString()
        {
            return $"Suffix [to={to}, fromm={fromm}, tags={tags}]";
        }

        private static string Convert0(string part) => part.Equals("0") ? "" : part;
    }
}
