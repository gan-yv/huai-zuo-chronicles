using System;

namespace Demo02.Core
{
    /// <summary>
    /// Single runtime boundary for process command-line arguments.
    /// Feature code consumes parsed options instead of reading the OS directly.
    /// </summary>
    public static class RuntimeLaunchOptions
    {
        private static readonly string[] arguments =
            Environment.GetCommandLineArgs();

        public static string[] Arguments => arguments;

        public static bool HasArgument(string argument)
        {
            return Array.IndexOf(arguments, argument) >= 0;
        }

        public static bool HasPrefix(string prefix)
        {
            foreach (string argument in arguments)
            {
                if (argument.StartsWith(
                        prefix,
                        StringComparison.Ordinal
                    ))
                {
                    return true;
                }
            }
            return false;
        }

        public static string GetValue(string prefix)
        {
            foreach (string argument in arguments)
            {
                if (argument.StartsWith(
                        prefix,
                        StringComparison.Ordinal
                    ))
                {
                    return argument.Substring(prefix.Length);
                }
            }
            return string.Empty;
        }

        public static bool TryGetFloat(
            string prefix,
            out float value)
        {
            return float.TryParse(
                GetValue(prefix),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out value
            );
        }
    }
}
