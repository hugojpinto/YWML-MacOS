using YWML.Src.Utils.Arc0Ex;
namespace YWML.Src.Loader
{
    public static class CLoader
    {
        /// <summary>
        /// Layers the given mods into the extension's patchable FA archive.
        /// </summary>
        /// <param name="modNamesMostImportantFirst">
        /// Mod names exactly as they appear in the loader list, ordered from most to least
        /// important (i.e. the same top-to-bottom order the UI shows).
        /// </param>
        /// <param name="modPaths">Mod name to mod folder on disk.</param>
        /// <param name="faToLoad">Path of the .fa archive to patch.</param>
        public static (CARC0Ex Archive, Dictionary<string, string> RawFiles) ModifyFA(
            IEnumerable<string> modNamesMostImportantFirst,
            Dictionary<string, string> modPaths,
            string faToLoad)
        {
            var fs = new FileStream(faToLoad, FileMode.Open, FileAccess.ReadWrite);
            CARC0Ex arcEx = new CARC0Ex(fs);

            // get the list of mod paths from least to most important
            var modPathsFromLeastImportant = modNamesMostImportantFirst
                .Select(name => modPaths[name].Replace("\\", "/"))
                .Reverse()
                .ToList();

            // store files for patching in the fa 
            var filesToPatch = new Dictionary<string, byte[]>();
            //Keep track of the base directory and the big file path so we can copy shit properly.
            var rawFiles = new Dictionary<string, string>();

            foreach (var modPath in modPathsFromLeastImportant)
            {
                // add all files except those in "include"
                foreach (var f in Directory.EnumerateFiles(modPath, "*", SearchOption.AllDirectories)
                                .Where(file => !IsInsideIncludeFolder(modPath, file)))
                {
                    rawFiles[f] = modPath;
                }

                // handle files inside the "include" folder
                var includePath = Path.Combine(modPath, "include");
                if (Directory.Exists(includePath))
                {
                    foreach (var file in Directory.EnumerateFiles(includePath, "*", SearchOption.AllDirectories))
                    {
                        // change the path to skip everything before "include"
                        var relativePath = Path.GetRelativePath(includePath, file).Replace("\\", "/");
                        filesToPatch[relativePath] = File.ReadAllBytes(file);
                    }
                }
                else
                {
                    throw new DirectoryNotFoundException("Make sure you have an include folder.");
                }
            }

            foreach (var file in filesToPatch)
            {
                arcEx.AddOrReplace(file.Key, file.Value);
            }
            return (arcEx, rawFiles
                .Where(x => !x.Key.Contains("ywml.json"))
                .ToDictionary(x => x.Key, x => x.Value));
        }

        /// <summary>
        /// Separator agnostic replacement for the original <c>file.Contains("include\\")</c> check:
        /// true when <paramref name="file"/> lives under the mod's top level "include" folder.
        /// </summary>
        private static bool IsInsideIncludeFolder(string modPath, string file)
        {
            var relative = Path.GetRelativePath(modPath, file);
            var segments = relative.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, '/', '\\' },
                                          StringSplitOptions.RemoveEmptyEntries);
            return segments.Length > 0 && string.Equals(segments[0], "include", StringComparison.Ordinal);
        }
    }
}
