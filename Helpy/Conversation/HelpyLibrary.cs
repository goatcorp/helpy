using System.Text.RegularExpressions;
using Yarn;

namespace Helpy.Conversation
{
    public class HelpyLibrary : Library
    {
        private TroubleContext trouble;

        public HelpyLibrary(TroubleContext trouble)
        {
            this.trouble = trouble;

            RegisterFunction("GetPlatform", FunGetPlatform);
            RegisterFunction("GetIndexMismatchKind", FunGetIndexMismatchKind);
            RegisterFunction("HasVerBckMismatch", FunHasVerBckMismatch);
            RegisterFunction("HasAutoLogin", FunHasAutoLogin);
            RegisterFunction("HasThirdPlugins", FunHasThirdPlugins);
            RegisterFunction("HasSpecificPlugin", FunHasSpecificPlugin);
            RegisterFunction("RegexLastExceptionDalamud", FunRegexLastExceptionDalamud);
            RegisterFunction("RegexLastExceptionXL", FunRegexLastExceptionXL);
            RegisterFunction("HasRecentExceptionDalamud", FunHasRecentExceptionDalamud);
            RegisterFunction("HasRecentExceptionXL", FunHasRecentExceptionXL);
            RegisterFunction("HasRecentException", FunHasRecentException);
        }

        private void CheckPrecondition()
        {
            if (!trouble.IsPackLoaded)
                throw new Exception("Trouble function called, but no pack was loaded. Something is wrong with the flow.");
        }

        private int FunGetPlatform()
        {
            CheckPrecondition();

            return trouble.PayloadXL!.Platform;
        }

        private int FunGetIndexMismatchKind()
        {
            CheckPrecondition();

            return (int) trouble.PayloadXL!.IndexIntegrity;
        }

        private bool FunHasVerBckMismatch()
        {
            CheckPrecondition();

            return !trouble.PayloadXL!.BckMatch;
        }

        private bool FunHasAutoLogin()
        {
            CheckPrecondition();

            return trouble.PayloadXL!.IsAutoLogin;
        }

        private bool FunHasThirdPlugins()
        {
            CheckPrecondition();

            return trouble.PayloadDalamud?.HasThirdRepo ?? false;
        }

        private bool FunHasSpecificPlugin(string name)
        {
            CheckPrecondition();

            return trouble.PayloadDalamud?.LoadedPlugins.Any(x => x.InternalName == name) ?? false;
        }

        private bool FunRegexLastExceptionDalamud(string regexStr)
        {
            CheckPrecondition();

            if (trouble.LastExceptionDalamud == null)
                return false;

            var regex = new Regex(regexStr);
            return regex.IsMatch(trouble.LastExceptionDalamud.Info);
        }

        private bool FunRegexLastExceptionXL(string regexStr)
        {
            CheckPrecondition();

            if (trouble.LastExceptionXL == null)
                return false;

            var regex = new Regex(regexStr);
            return regex.IsMatch(trouble.LastExceptionXL.Info);
        }

        const int ExceptionDaysLimit = 2;

        private bool FunHasRecentExceptionDalamud()
        {
            CheckPrecondition();

            if (trouble.LastExceptionDalamud == null)
                return false;

            var timePassed = DateTime.Now - trouble.LastExceptionDalamud.When;
            return timePassed.Days <= ExceptionDaysLimit;
        }

        private bool FunHasRecentExceptionXL()
        {
            CheckPrecondition();

            if (trouble.LastExceptionXL == null)
                return false;

            var timePassed = DateTime.Now - trouble.LastExceptionXL.When;
            return timePassed.Days <= ExceptionDaysLimit;
        }

        private bool FunHasRecentException()
        {
            CheckPrecondition();

            return FunHasRecentExceptionDalamud() || FunHasRecentExceptionXL();
        }
    }
}
