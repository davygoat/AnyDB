/********************************************************************************************************************
 * 
 * Database_TypeInitializer.cs
 * 
 * A static constructor to make sure all the database providers I know about are readily available to .NET's assembly 
 * loader. The recommended way to use locally installed DbProvider DLLs is to use an app.config, or ASP.NET's equivalent, 
 * web.config. But that's easy to forget, especially for noddy apps. Much easier to just drop the necessary DLLs into 
 * the application directory and let AnyDB figure out how to load it.
 */

using System;
using System.Configuration;
using System.Diagnostics;
using System.Windows.Forms;

namespace AnyDB
{
    public partial class Database
    {
        /*===========================================================================================================
         * 
         * Static constructor
         * 
         * Make sure supported DLLs are available to DbProviderFactories.GetFactory().
         */

        static Database()
        {
            /*
             * Here's another bizarre scenario I've just run into:
             * 
             * 
             * "My .NET 2 DLL loads fine in a .NET 2 or 3.5 app. But not in .NET 4.0. But it's not blowing up in Visual 
             *  Studio, or popping up any "Unhandled Exception" message box. It just fails silently.
             *  
             * "The mighty Google reveals that mixed mode .NET 2 assemblies won't load into a .NET 4 app without special 
             *  configuration. But my DLL is emphatically NOT a mixed mode assembly. It's a plain ordinary managed one. 
             *  Something else must be going on. And in any case, it doesn't pop up a message box about 'special
             *  configuration.'
             * 
             * "I tried single stepping, but it just skipped over the constructor without any further ado. Just nothing. 
             *  Sticking in a breakpoint had no effect; it wasn't even going to go there. So I put a breakpoint in the 
             *  static constructor, and it wasn't going there either. It just didn't want to go there. Or maybe it did, 
             *  but didn't want me to single step. It was my old Visual Studio problem again, it was defaulting to x86, 
             *  with no way to set it to "Any CPU". After a bit of digging around the various properties pages, I finally 
             *  managed to get both my DLL and my test app to target "Any CPU".
             *  
             * "Now I could single step into the static constructor. It went in, but didn't get past ConfigurationManager
             *  .GetSection("system.data"). But it still wasn't popping up a message box, it just backed out and carried 
             *  on as if nothing happened. And nothing is precisely what happened.
             *  
             * "Right, wielding Occam's razor, I slashed out the static constructor, only for the same problem to resurface 
             *  in the instance constructor. This time it was backing out on the DbProviderFactory class. But again, no 
             *  message box. And still lots of nothing. So I put a try/catch block in, which showed that there *was* an 
             *  Exception there, but no message box. It was just disappearing into nowhere.
             *  
             * "The caught Exception finally informed me that Firebird was trying to install itself in the machine.config, 
             *  when it had already been installed in there, resulting in a constraint violation -- and well before I 
             *  would even try it myself. But there was no exception, just utter silence. Or rather, the exception was 
             *  there, but it just wouldn't show itself.
             *  
             * "I never had this problem before, because I was quite happily sticking to .NET 2 for minimal demand. Only 
             *  when I tried to use my DLL in a [by default] .NET 4.0 test app did the Firebird problem come to the 
             *  surface, or not! So Firebird had put itself into machine.config after all, but only for the latest .NET 
             *  version, not in the .NET 2.0 config.
             *  
             * "So what can I do about it defensively? Although I can catch the error, I can't throw it. Well, I can, 
             *  but you won't ever see it "'cause Microsoft ain't tellin' ya". The only thing I can do is add a reference 
             *  to System.Windows.Forms, and pop up a MessageBox myself. Now why can't Microsoft do that?"
             *  
             * 
             * The moral of this story:
             * 
             * A messed up machine.config will actually prevent .NET from telling you that it's messed up. That's 
             * seriously messed up. It certainly does have whiff of blast furnace slag about its presence.
             */

            try
            {
                ConfigurationManager.GetSection("system.data");
            }
            catch (Exception ex)
            {
                try
                {
                    MessageBox.Show(ex.Message);
                }
                catch { }
                Process.GetCurrentProcess().Kill();
            }

            /*
             * Now look for classes derived from DriverBase.
             */

            FindDriverClasses();
        }
    }
}