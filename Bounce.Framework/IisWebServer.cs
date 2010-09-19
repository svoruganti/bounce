using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;

namespace Bounce.Framework {
    class IisWebServer {
        private ManagementScope scope;

        public IisWebServer(string host) {
            var options = new ConnectionOptions();
            scope = new ManagementScope(String.Format(@"\\{0}\root\MicrosoftIISV2", host), options);
            scope.Connect();
        }

        public IisWebSite TryGetWebSiteByServerComment(string serverComment) {
            var query = new ManagementObjectSearcher(scope, new ObjectQuery(String.Format("select * from IIsWebServerSetting where ServerComment = '{0}'", serverComment)));
            ManagementObjectCollection websites = query.Get();

            foreach (var website in websites) {
                return new IisWebSite(scope, String.Format("IIsWebServer='{0}'", website["Name"]));
            }

            return null;
        }

        public IisWebSite CreateWebSite(string serverComment, IEnumerable<IisWebSiteBinding> bindings, string documentRoot) {
            var iis = new ManagementObject(scope, new ManagementPath("IIsWebService='W3SVC'"), null);

            ManagementBaseObject createNewSiteArgs = iis.GetMethodParameters("CreateNewSite");
            createNewSiteArgs["ServerComment"] = serverComment;
            createNewSiteArgs["ServerBindings"] = bindings.Select(b => CreateBinding(b)).ToArray();
            createNewSiteArgs["PathOfRootVirtualDir"] = documentRoot;

            var result = iis.InvokeMethod("CreateNewSite", createNewSiteArgs, null);

            var id = (string) result["ReturnValue"];
            return new IisWebSite(scope, id);
        }

        private ManagementObject CreateBinding(IisWebSiteBinding binding) {
            ManagementObject serverBinding = new ManagementClass(scope, new ManagementPath("ServerBinding"), null).CreateInstance();
            serverBinding["Port"] = binding.Port.ToString();

            if (binding.Hostname != null) {
                serverBinding["Hostname"] = binding.Hostname;
            }
            if (binding.IPAddress != null) {
                serverBinding["IP"] = binding.IPAddress.ToString();
            }

            return serverBinding;
        }
    }
}