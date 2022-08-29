using System;

namespace at.D365.PowerCID.Portal.Helpers
{
    public static class LoggingHelper
    {
        public static bool HasMethod(this object objectToCheck, string methodName)
        {
            var type = objectToCheck.GetType();
            return type.GetMethod(methodName) != null;
        }
    }
}


