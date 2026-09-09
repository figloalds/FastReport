using System.Threading.Tasks;

namespace FastReport.Engine
{
    public partial class ReportEngine
    {
        #region Private Methods


        private bool RunDialogs()
        {
            return true;
        }

        private Task<bool> RunDialogsAsync()
        {
            return Task.FromResult(true);
        }

        #endregion Private Methods
    }
}
