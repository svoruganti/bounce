using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using NUnit.Framework;
using System.Net;

namespace Bounce.Tests {
    [TestFixture, Explicit]
    public class Iis6WebSiteTest {
        [Test, STAThread]
        public void Stuff() {
            string host = "localhost";
            var options = new ConnectionOptions();
            var scope = new ManagementScope(String.Format(@"\\{0}\root\MicrosoftIISV2", host), options);
            scope.Connect();

//            EnumerateWebsites(scope);
//            CreateSite(scope);
//            PrintSite(scope, "IIsWebServer='W3SVC/1180970907'");
            PrintSite(scope, "IIsWebServerSetting.Name='W3SVC/1180970907'");
//            FindSite(scope, "My New Site");
        }

        [Test]
        public void ShouldFindSiteByServerComment() {
            var server = new WebServer("localhost");
            WebSite site = server.TryGetWebSiteByServerComment("My New Site");
            Assert.That(site.ServerComment, Is.EqualTo("My New Site"));
            Assert.That(site.Bindings.Count(), Is.EqualTo(1));
            WebSiteBinding binding = site.Bindings.ElementAt(0);
            Assert.That(binding.Port, Is.EqualTo(6060));
            Assert.That(binding.Hostname, Is.Null);
            Assert.That(binding.IPAddress, Is.Null);
        }

        [Test]
        public void ShouldAddNewSite() {
            var server = new WebServer("localhost");
            WebSite site = server.CreateWebSite("A new site!", new [] {new WebSiteBinding {Port = 6020}}, @"c:\anewsite");
        }

        [Test]
        public void ShouldDelete() {
            var server = new WebServer("localhost");
            WebSite site = server.TryGetWebSiteByServerComment("A new site!");

            if (site != null) {
                Console.WriteLine("deleting");
                site.Delete();
            }
        }

        [Test]
        public void ShouldStart() {
            var server = new WebServer("localhost");
            WebSite site = server.TryGetWebSiteByServerComment("My New Site");
            site.Start();
        }

        [Test]
        public void ShouldStop() {
            var server = new WebServer("localhost");
            WebSite site = server.TryGetWebSiteByServerComment("My New Site");
            site.Stop();
        }

        [Test]
        public void ShouldGetState() {
            var server = new WebServer("localhost");
            WebSite site = server.TryGetWebSiteByServerComment("My New Site");
            Console.WriteLine(site.State);
        }


        private void FindSite(ManagementScope scope, string serverComment) {
            var query = new ManagementObjectSearcher(scope, new ObjectQuery(String.Format("select * from IIsWebServerSetting where ServerComment = '{0}'", serverComment)));
            ManagementObjectCollection websites = query.Get();

            foreach (var website in websites) {
                Console.WriteLine(website.Properties["Name"].Value + ":");
                foreach (var prop in website.Properties) {
                    Console.WriteLine("  {0}: {1}", prop.Name, prop.Value);
                }
                Console.WriteLine("system props:");
                foreach (var prop in website.SystemProperties) {
                    Console.WriteLine("  {0}: {1}", prop.Name, prop.Value);
                }
            }
        }

        private void PrintSite(ManagementScope scope, string path) {
            var site = new ManagementObject(scope, new ManagementPath(path), null);
            object desc = site["Description"];
            foreach (var rel in site.GetRelationships()) {
                Console.WriteLine("rel: " + rel);
            }
            foreach (var rel in site.GetRelated()) {
                Console.WriteLine("related: " + rel);
            }

            Console.WriteLine("props:");
            foreach (var prop in site.Properties) {
                Console.WriteLine("  {0}: {1}", prop.Name, prop.Value);
            }
            Console.WriteLine("system props:");
            foreach (var prop in site.SystemProperties) {
                Console.WriteLine("  {0}: {1}", prop.Name, prop.Value);
            }
        }

        private void EnumerateWebsites(ManagementScope scope) {
            var query = new ManagementObjectSearcher(scope, new ObjectQuery("select * from IIsWebVirtualDir"));
            ManagementObjectCollection websites = query.Get();

            foreach (var website in websites) {
                Console.WriteLine(website.Properties["Name"].Value + ":");
                foreach (var prop in website.Properties) {
                    Console.WriteLine("  {0}: {1}", prop.Name, prop.Value);
                }
                Console.WriteLine("system props:");
                foreach (var prop in website.SystemProperties) {
                    Console.WriteLine("  {0}: {1}", prop.Name, prop.Value);
                }
            }
        }

        private void CreateSite(ManagementScope scope) {
            var serverBinding = new ManagementClass(scope, new ManagementPath("ServerBinding"), null).CreateInstance();
            serverBinding["Port"] = "6060";

            var iis = new ManagementObject(scope, new ManagementPath("IIsWebService='W3SVC'"), null);
            ManagementBaseObject createNewSiteArgs = iis.GetMethodParameters("CreateNewSite");
            createNewSiteArgs["ServerComment"] = "My New Site";
            createNewSiteArgs["ServerBindings"] = new[] {serverBinding};
            createNewSiteArgs["PathOfRootVirtualDir"] = @"c:\somedirectory";

            var result = iis.InvokeMethod("CreateNewSite", createNewSiteArgs, null);
            Console.WriteLine(result["ReturnValue"]);
        }

        class WebSite {
            private readonly ManagementObject webSite;
            private readonly ManagementObject settings;

            public WebSite(ManagementScope scope, string path) {
                webSite = new ManagementObject(scope, new ManagementPath(path), null);
                settings = new ManagementObject(scope, new ManagementPath(String.Format("IIsWebServerSetting.Name='{0}'", webSite["Name"])), null);
            }

            public string ServerComment {
                get {
                    return (string) settings["ServerComment"];
                }
            }

            public IEnumerable<WebSiteBinding> Bindings {
                get {
                    var bindings = (ManagementBaseObject[]) settings["ServerBindings"];
                    return bindings.Select(b => CreateWebSiteBinding(b)).ToArray();
                }
            }

            public WebSiteState State {
                get {
                    var state = (int) webSite["ServerState"];
                    switch (state) {
                        case 1:
                            return WebSiteState.Starting;
                        case 2:
                            return WebSiteState.Started;
                        case 3:
                            return WebSiteState.Stopping;
                        case 4:
                            return WebSiteState.Stopped;
                        case 5:
                            return WebSiteState.Pausing;
                        case 6:
                            return WebSiteState.Paused;
                        case 7:
                            return WebSiteState.Continuing;
                        default:
                            throw new Exception(string.Format("didn't expect website serverstate of {0}", state));
                    }
                }
            }

            private static WebSiteBinding CreateWebSiteBinding(ManagementBaseObject binding) {
                var ip = (string) binding["IP"];
                var hostname = (string) binding["Hostname"];

                return new WebSiteBinding {
                                              Hostname = String.IsNullOrEmpty(hostname)? null: hostname,
                                              IPAddress = (String.IsNullOrEmpty(ip)? null: IPAddress.Parse(ip)),
                                              Port = int.Parse((string) binding["Port"])
                                          };
            }

            public void Delete() {
                webSite.Delete();
            }

            public void Start() {
                webSite.InvokeMethod("Start", new object[0]);
            }

            public void Stop() {
                webSite.InvokeMethod("Stop", new object[0]);
            }
        }

        class WebSiteBinding {
            public string Hostname;
            public int Port;
            public IPAddress IPAddress;
        }

        class WebServer {
            private ManagementScope scope;

            public WebServer(string host) {
                var options = new ConnectionOptions();
                scope = new ManagementScope(String.Format(@"\\{0}\root\MicrosoftIISV2", host), options);
                scope.Connect();
            }

            public WebSite TryGetWebSiteByServerComment(string serverComment) {
                var query = new ManagementObjectSearcher(scope, new ObjectQuery(String.Format("select * from IIsWebServerSetting where ServerComment = '{0}'", serverComment)));
                ManagementObjectCollection websites = query.Get();

                foreach (var website in websites) {
                    return new WebSite(scope, String.Format("IIsWebServer='{0}'", website["Name"]));
                }

                return null;
            }

            public WebSite CreateWebSite(string serverComment, IEnumerable<WebSiteBinding> bindings, string documentRoot) {
                var iis = new ManagementObject(scope, new ManagementPath("IIsWebService='W3SVC'"), null);

                ManagementBaseObject createNewSiteArgs = iis.GetMethodParameters("CreateNewSite");
                createNewSiteArgs["ServerComment"] = serverComment;
                createNewSiteArgs["ServerBindings"] = bindings.Select(b => CreateBinding(b)).ToArray();
                createNewSiteArgs["PathOfRootVirtualDir"] = documentRoot;

                var result = iis.InvokeMethod("CreateNewSite", createNewSiteArgs, null);

                var id = (string) result["ReturnValue"];
                return new WebSite(scope, id);
            }

            private ManagementObject CreateBinding(WebSiteBinding binding) {
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

    internal enum WebSiteState {
        Starting,
        Started,
        Stopping,
        Stopped,
        Pausing,
        Paused,
        Continuing
    }
}