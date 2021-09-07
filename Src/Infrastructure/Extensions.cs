using Desktop.Model;
using log4net;
using Prism.Services.Dialogs;
using System.Runtime.CompilerServices;

namespace Desktop.Infrastructure
{
    public static class Extensions
    {
        public static void LogDialogOpened(this ILog logger, IDialogParameters parameters, [CallerMemberName] string method = null, [CallerFilePath] string fileName = null)
        {
            logger.LogInfo($" Open dialog with parameters {string.Join(", ", parameters.Keys)}",method,fileName);
        }
    
    }
}
