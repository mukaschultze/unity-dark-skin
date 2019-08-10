using CommandLine;

namespace DarkSkin {

    [Verb("enable", HelpText = "Unlock the dark skin.")]
    class EnableOptions : BaseOption { }

    [Verb("disable", HelpText = "Revert the skin to the original.")]
    class DisableOptions : BaseOption { }

    [Verb("findhex", HelpText = "Find the address of the GetSkinIdx method for a particular Unity version")]
    class FindHexOptions : BaseOption { }

    class BaseOption {
        [Option('i', "input", Default = "**/Unity.exe", HelpText = "Unity executable path.")]
        public string InputFile { get; set; }

        [Option('f', "fast-enumerator", HelpText = "Use fast file enumeration to search for executables, otherwise use recursive enumeration.")]
        public string FastEnumerator { get; set; }
    }

}