using System;
using System.Collections.Generic;

using Godot;

namespace Game.Code.Helpers
{
    public static class PrefabHelper
    {
        // TODO: rewrite this using Godot collections

        public static Dictionary<string, PackedScene> LoadPrefabsDictionary(string path, string[] namesToExclude = null, bool caseSensitive = true)
        {
            var dict = new Dictionary<string, PackedScene>();
            var directory = new Directory();

            if (directory.Open(path) == Error.Ok)
            {
                directory.ListDirBegin();

                for (var filename = directory.GetNext(); !string.IsNullOrEmpty(filename); filename = directory.GetNext())
                {
                    if (directory.CurrentIsDir())
                    {
                        continue;
                    }

                    var filenameWOExtension = System.IO.Path.GetFileNameWithoutExtension(caseSensitive ? filename : filename.ToLowerInvariant());

                    if (namesToExclude != null && Array.IndexOf(namesToExclude, filenameWOExtension) > -1)
                    {
                        continue;
                    }

                    var scenePath = $"{path}/{filename}";
                    var scene = GD.Load<PackedScene>(scenePath);

                    if (scene == null)
                    {
                        GD.PushError($"Failed to load scene \"{scenePath}\"");
                        continue;
                    }

                    dict.Add(filenameWOExtension, scene);
                }
            }

            return dict;
        }

        public static List<PackedScene> LoadPrefabsList(string path)
        {
            var list = new List<PackedScene>();
            var dir = new Directory();

            if (dir.Open(path) == Error.Ok)
            {
                dir.ListDirBegin();

                for (var filename = dir.GetNext(); !string.IsNullOrEmpty(filename); filename = dir.GetNext())
                {
                    if (dir.CurrentIsDir())
                    {
                        continue;
                    }

                    var scene = GD.Load<PackedScene>($"{path}/{filename}");
                    list.Add(scene);
                }
            }

            return list;
        }
    }
}