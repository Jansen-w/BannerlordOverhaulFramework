using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace BOF.Core
{
    public static class BOFHelpers
    {
        /// <summary>
        /// Returns true if in a debug environment
        /// </summary>
        /// <returns></returns>
        public static bool IsDebug()
        {
            #if DEBUG
                return true;
            #else
                return false;
            #endif
        }

        /// <summary>
        /// Prints a white info string to the InformationManager in the bottom left of the screen.
        /// </summary>
        /// <param name="message"></param>
        public static void PrintInfoMessage(string messageString)
        {
            PrintColoredMessage(messageString, Color.White);
        }

        /// <summary>
        /// Prints a string of a specified color to the InformationManager in the bottom left of the screen.
        /// </summary>
        /// <param name="message"></param>
        public static void PrintColoredMessage(string messageString, Color color)
        {
            var message = new InformationMessage(messageString, color);
            InformationManager.DisplayMessage(message);
        }
    }
}
