using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ts
{
    public class SearchFile
    {
        public string path_;
        public string[] lines_;
        public bool hadHit_ = false; // will be marked as true when we get a hit

        public SearchFile(string path)
        {
            path_ = path;
        }

        public string[] GetSearchText()
        {
            lines_ = System.IO.File.ReadAllLines(path_);
            for (int i = 0; i < lines_.Length; ++i)
                lines_[i] = lines_[i].Replace("\t", "    ");
            return lines_;
        }

        public string GetLine(int line, bool caseSensitive)
        {
            return caseSensitive ? lines_[line] : lines_[line].ToLowerInvariant();
        }

        public void Done()
        {
            lines_ = null;
        }
    }

    public class SearchDir
    {
        public string path_;
        public List<SearchDir> subDirs_ = new List<SearchDir>();
        public List<SearchFile> files_ = new List<SearchFile>();
        int index_ = 0;

        // create the root void dir
        public SearchDir()
        {

        }

        public int CountHits()
        {
            int hits = 0;
            foreach (var file in files_)
                hits += file.hadHit_ ? 1 : 0;
            foreach (var dir in subDirs_)
                hits += dir.CountHits();
            return hits;
        }

        public SearchDir(string path, SearchParams args)
        {
            path_ = path;
            var files = System.IO.Directory.EnumerateFiles(path_);
            if (files != null)
                foreach (var file in files)
                {
                    string ext = System.IO.Path.GetExtension(file);
                    if (!string.IsNullOrEmpty(ext))
                        ext = ext.Substring(1).ToLowerInvariant();
                    else if (args.OnlyExtensions.Count > 0)
                        continue;

                    if (args.OnlyExtensions.Count > 0)
                    {
                        if (!args.OnlyExtensions.Contains(ext))
                            continue;
                    }
                    if (args.ExcludedExtensions.Contains(ext))
                        continue;

                    System.IO.FileInfo fi = new System.IO.FileInfo(file);
                    if (fi.Length > args.MaxBytes)
                        continue;

                    files_.Add(new SearchFile(file));
                }

            if (args.Recurse)
            {
                //Console.WriteLine(string.Format("Recursing {0}", path_));

                var dirs = System.IO.Directory.EnumerateDirectories(path_);
                if (dirs != null)
                    foreach (var dir in dirs)
                        subDirs_.Add(new SearchDir(dir, args));
            }
        }

        public SearchFile GetNext()
        {
            if (index_ >= files_.Count)
            {
                if (index_ - files_.Count >= subDirs_.Count)
                    return null;
                else
                {
                    var ret = subDirs_[index_ - files_.Count].GetNext();
                    if (ret == null)
                    {
                        ++index_;
                        return GetNext();
                    }
                    else
                        return ret;
                }
            }
            else
            {
                var ret = files_[index_];
                ++index_;
                return ret;
            }
        }

        /// <summary>
        /// Advances the index so that the next call to `GetNext()` will return the first file of the next folder
        /// </summary>
        public void SkipCurrentDirectory()
        {
            if (index_ >= files_.Count)
                subDirs_[index_ - files_.Count].SkipCurrentDirectory();
            else
                index_ = files_.Count;
        }
    }
}
