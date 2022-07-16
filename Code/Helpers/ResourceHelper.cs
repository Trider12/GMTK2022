using System;
using System.Collections.Generic;

using Godot;

namespace Game.Code.Helpers
{
    public static class ResourceHelper
    {
        // TODO: rewrite this using Godot collections

        public static Dictionary<string, T> LoadResourcesDictionary<T>(string path, string[] namesToExclude = null, bool caseSensitive = true) where T : Resource
        {
            var dict = new Dictionary<string, T>();
            var directory = new Directory();

            if (directory.Open(path) == Error.Ok)
            {
                directory.ListDirBegin();

                for (var filename = directory.GetNext(); !string.IsNullOrEmpty(filename); filename = directory.GetNext())
                {
                    if (directory.CurrentIsDir() || filename.EndsWith(".import"))
                    {
                        continue;
                    }

                    var filenameWOExtension = System.IO.Path.GetFileNameWithoutExtension(caseSensitive ? filename : filename.ToLowerInvariant());

                    if (namesToExclude != null && Array.IndexOf(namesToExclude, filenameWOExtension) > -1)
                    {
                        continue;
                    }

                    var resourcePath = $"{path}/{filename}";
                    var resource = GD.Load<T>(resourcePath);

                    if (resource == null)
                    {
                        GD.PushError($"Failed to load resource of type {typeof(T).Name} \"{resourcePath}\"");
                        continue;
                    }

                    dict.Add(filenameWOExtension, resource);
                }
            }

            return dict;
        }

        public static IList<T> LoadPrefabsList<T>(string path) where T : Resource
        {
            var list = new List<T>();
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

                    var resourcePath = $"{path}/{filename}";
                    var resource = GD.Load<T>(resourcePath);

                    if (resource == null)
                    {
                        GD.PushError($"Failed to load resource of type {typeof(T).Name} \"{resourcePath}\"");
                        continue;
                    }

                    list.Add(resource);
                }
            }

            return list;
        }
    }
}