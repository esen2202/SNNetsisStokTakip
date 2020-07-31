using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SNNetsisStokTakip.Classes
{
    public class ExceptionHelper
    {
        public static Exception CatchException(Action action)
        {
            try
            {
                action.Invoke();
                return null;
            }
            catch (Exception exception)
            {
                return exception;
            }
        }

    }
}
