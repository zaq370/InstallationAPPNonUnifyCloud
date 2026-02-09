using InstallationAPPNonUnify.Modules;
using System.Threading;

namespace InstallationAPPNonUnify.App_Start
{
    public static class CustomeServices
    {
        public static void ServiceInitiate() {

            //要在呼叫CRMService 之前做，載入設定檔
            new BackendInfo().UpdateFileContent();

            //載入 CRMService
            Thread CrmService = new Thread(new CRMServiceConnection().UpdateConnectionSetting);
            CrmService.Start();

            //重新載入文字檔
            new LanguagePackage().LoadLanguagePackage();
            new WhiteListInfo().UpdateList();
        }
    }
}