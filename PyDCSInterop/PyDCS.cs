using Python.Runtime;
using System;
using System.IO;

namespace PyDCSInterop {
    public class PyDCS : IDisposable {
        private static Py.GILState? _gilState;
        private static PyObject? _dcs;

        private static void InitVenv(string pathToVirtualEnv) {
            string cfgPath = Path.Combine(pathToVirtualEnv, "pyvenv.cfg");
            if (!File.Exists(cfgPath))
                throw new FileNotFoundException("pyvenv.cfg not found in virtual environment.");

            string? basePrefix = null;
            foreach (var line in File.ReadAllLines(cfgPath)) {
                if (line.StartsWith("home =")) {
                    basePrefix = line.Substring("home =".Length).Trim();
                    break;
                }
            }

            if (string.IsNullOrEmpty(basePrefix))
                throw new InvalidOperationException("Could not determine base interpreter from pyvenv.cfg.");

            // Attempt to discover the DLL
            string? dllPath = Directory.GetFiles(basePrefix, "python*.dll")
                                       .FirstOrDefault(f => Path.GetFileName(f).StartsWith("python3") && f.EndsWith(".dll"));

            if (dllPath == null)
                throw new FileNotFoundException($"No Python DLL found in base interpreter path: {basePrefix}");

            // Configure environment
            var existingPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            var path = existingPath.TrimEnd(';');
            path = string.IsNullOrEmpty(path) ? pathToVirtualEnv : $"{path};{pathToVirtualEnv}";

            Environment.SetEnvironmentVariable("PATH", path, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("PYTHONHOME", pathToVirtualEnv, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("PYTHONPATH", $"{pathToVirtualEnv}\\Lib\\site-packages;{pathToVirtualEnv}\\Lib", EnvironmentVariableTarget.Process);

            Runtime.PythonDLL = dllPath;
            PythonEngine.PythonHome = pathToVirtualEnv;
            PythonEngine.PythonPath = Environment.GetEnvironmentVariable("PYTHONPATH", EnvironmentVariableTarget.Process) ?? string.Empty;

            PythonEngine.Initialize();

            _gilState?.Dispose();
            _gilState = Py.GIL();
        }

        public PyDCS(string pathToVirtualEnv = @".venv") {
            InitVenv(pathToVirtualEnv);
            // _dcs = Py.Import("dcs");
        }

        public PyObject DCS => _dcs ?? throw new InvalidOperationException("DCS module not initialized.");

        public void Dispose() {
            _gilState?.Dispose();
        }
    }
}
