using System;

namespace at.D365.PowerCID.Portal.Helpers
{
    public static class VersionHelper{
        public static string GetNextBuildVersion(string previousVersion){
            var versionSplit = previousVersion.Split('.');
            var buildNumber = DateTime.Now.ToString("yyMM");
            var revisionNumber = DateTime.Now.ToString("ddHHmm");
            var newVersionNumber = $"{versionSplit[0]}.{versionSplit[1]}.{buildNumber}.{revisionNumber}";
            return newVersionNumber;
        }

        internal static string GetNextMinorVersion(string previousVersion)
        {
            var versionSplit = previousVersion.Split('.');
            var newVersionNumberOne = int.Parse(versionSplit[1]) + 1;
            var buildNumber = DateTime.Now.ToString("yyMM");
            var revisionNumber = DateTime.Now.ToString("ddHHmm");
            var newVersionNumber = $"{versionSplit[0]}.{newVersionNumberOne}.{buildNumber}.{revisionNumber}";
            return newVersionNumber;
        }
    }
}