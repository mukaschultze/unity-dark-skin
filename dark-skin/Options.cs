using CommandLine;

namespace DarkSkin {

    [Verb("enable", HelpText = "Unlock the dark skin.")]
    class EnableOptions : BaseOption { }

    [Verb("disable", HelpText = "Revert the skin to the original.")]
    class DisableOptions : BaseOption { }

    [Verb("findhex", HelpText = "Find and list the addresses and hexes of the GetSkinIdx methods")]
    class FindHexOptions : BaseOption { }

    class BaseOption {
        [Value(0, MetaName = "unityExe", Default = ".")]
        public string InputFile { get; set; }

        [Option('f', "fast-enumerator", Default = false, HelpText = "Use fast file enumeration to search for executables, otherwise use recursive enumeration.")]
        public bool FastEnumerator { get; set; }
    }

}