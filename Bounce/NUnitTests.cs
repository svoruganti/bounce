﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Bounce.Framework {
    public class NUnitTests : ITarget {
        public IEnumerable<IValue<string>> DllPaths;

        public IEnumerable<ITarget> Dependencies {
            get { return DllPaths.Cast<ITarget>(); }
        }

        public void BeforeBuild() {
        }

        public void Build() {
            IEnumerable<string> testDlls = DllPaths.Select(dll => dll.Value).Where(dll => dll.EndsWith("Tests.dll"));

            foreach (var dllPath in testDlls) {
                Console.WriteLine("running unit tests for: " + dllPath);
                new ShellCommandExecutor().ExecuteAndExpectSuccess("nunit-console", String.Format(@"""{0}""", dllPath));
            }
        }

        public void Clean() {
        }
    }
}