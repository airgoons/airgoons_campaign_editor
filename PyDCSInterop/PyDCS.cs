using Python.Runtime;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace PyDCSInterop {
    // Minimal, deterministic Python initialization for a known working venv and DLL.
    // Usage:
    //   using var py = new PyDCS(@"C:\full\path\to\.venv", @"C:\full\path\to\python312.dll");
    // This intentionally removes probing/heuristics and only applies the explicit paths provided.
    public sealed class PyDCS : IDisposable {
        private PyObject? _dcs = null;

        public dynamic DCS {
            get {
                if (_dcs is null)
                    throw new InvalidOperationException("DCS module not initialized. Ensure the PyDCS constructor completed successfully.");
                try {
                    using var _ = Py.GIL();
                } catch (Exception ex) {
                    throw new InvalidOperationException("Unable to acquire the Python GIL when accessing DCS.", ex);
                }
                return _dcs;
            }
        }

        private static readonly object _shutdownLock = new();
        private static bool _shutdownCalled = false;

        // Simplified constructor: accepts an explicit venv root and optional python DLL path.
        // This version will also include a common source location for the pydcs package:
        //   <venv>\src\pydcs
        // If that folder exists it will be added to PYTHONPATH so `import dcs` succeeds.
        public PyDCS(string pathToVirtualEnv, string? pythonDllPath = null) {
            if (string.IsNullOrWhiteSpace(pathToVirtualEnv))
                throw new ArgumentException("pathToVirtualEnv must be a non-empty path.", nameof(pathToVirtualEnv));

            var resolvedVenv = Path.GetFullPath(pathToVirtualEnv);

            if (!Directory.Exists(resolvedVenv))
                throw new DirectoryNotFoundException($"Specified virtual environment directory not found: {resolvedVenv}");

            // Build venv paths
            var libPath = Path.Combine(resolvedVenv, "Lib");
            var sitePackages = Path.Combine(libPath, "site-packages");
            var dllsPath = Path.Combine(resolvedVenv, "DLLs");
            var scriptsPath = Path.Combine(resolvedVenv, "Scripts");
            var pydcsSrc = Path.Combine(resolvedVenv, "src", "pydcs");

            // interpreterRoot must point to the real Python install that contains the stdlib (Lib\encodings)
            var interpreterRoot = @"C:\Python312"; // <-- change if different on your machine

            // prefer explicit dll if provided, otherwise pick interpreterRoot\python312.dll
            if (!string.IsNullOrWhiteSpace(pythonDllPath)) {
                var resolvedDll = Path.GetFullPath(pythonDllPath);
                if (!File.Exists(resolvedDll)) throw new FileNotFoundException("Specified Python DLL not found.", resolvedDll);
                Runtime.PythonDLL = resolvedDll;
            } else {
                var defaultDll = Path.Combine(interpreterRoot, "python312.dll");
                if (File.Exists(defaultDll)) Runtime.PythonDLL = defaultDll;
            }

            // Diagnostics - show what files we are about to use
            Console.WriteLine($"Process is 64-bit: {Environment.Is64BitProcess}");
            Console.WriteLine($"Runtime.PythonDLL -> {Runtime.PythonDLL ?? "(not set)"}");
            Console.WriteLine($"interpreterRoot -> {interpreterRoot}");
            Console.WriteLine($"venv root -> {resolvedVenv}");

            // Ensure the interpreter root has the stdlib we expect.
            var interpreterLib = Path.Combine(interpreterRoot, "Lib");
            var interpreterDlls = Path.Combine(interpreterRoot, "DLLs"); // critical: add interpreter DLLs dir to python path
            var expectedEnc = Path.Combine(interpreterLib, "encodings", "__init__.py");
            if (!File.Exists(expectedEnc)) {
                throw new InvalidOperationException($"Interpreter root does not contain expected stdlib encodings: {expectedEnc}");
            }

            // Build PYTHONPATH to include the interpreter stdlib Lib as the first entry
            // then include the venv site-packages and your local src\pydcs so imports work.
            var pythonPathEntries = new List<string>();
            if (Directory.Exists(interpreterLib)) pythonPathEntries.Add(interpreterLib);            // real stdlib
            if (Directory.Exists(interpreterDlls)) pythonPathEntries.Add(interpreterDlls);          // real stdlib extensions (.pyd)
            if (Directory.Exists(libPath)) pythonPathEntries.Add(libPath);                         // venv\Lib
            if (Directory.Exists(sitePackages)) pythonPathEntries.Add(sitePackages);               // venv\Lib\site-packages
            if (Directory.Exists(dllsPath)) pythonPathEntries.Add(dllsPath);                       // venv\DLLs (if any)
            if (Directory.Exists(pydcsSrc)) pythonPathEntries.Add(pydcsSrc);                       // venv\src\pydcs (your package source)

            var pythonPathValue = string.Join(";", pythonPathEntries);

            // Set environment and pythonnet runtime values
            Environment.SetEnvironmentVariable("PYTHONHOME", interpreterRoot, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("PYTHONPATH", pythonPathValue, EnvironmentVariableTarget.Process);

            // ensure Windows loader can find Python DLL dependencies
            var path = Environment.GetEnvironmentVariable("PATH") ?? "";
            var pythonDllDirs = string.Join(";", new[] { interpreterRoot, interpreterDlls }.Where(s => !string.IsNullOrEmpty(s)));
            Environment.SetEnvironmentVariable("PATH", pythonDllDirs + ";" + path, EnvironmentVariableTarget.Process);

            PythonEngine.PythonHome = interpreterRoot;
            PythonEngine.PythonPath = pythonPathValue;

            Console.WriteLine($"Environment PYTHONHOME = {Environment.GetEnvironmentVariable("PYTHONHOME")}");
            Console.WriteLine($"Environment PYTHONPATH = {Environment.GetEnvironmentVariable("PYTHONPATH")}");

            try {
                PythonEngine.Initialize();
            } catch (Exception ex) {
                throw new InvalidOperationException(
                    $"Failed to initialize PythonEngine. PYTHONHOME={Environment.GetEnvironmentVariable("PYTHONHOME")}; PYTHONPATH={Environment.GetEnvironmentVariable("PYTHONPATH")}; Runtime.PythonDLL={(Runtime.PythonDLL ?? "(not set)")}",
                    ex);
            }

            // NEW: ensure Python's import search path contains the interpreter DLLs folder
            // (some embedded runs do not reflect PYTHONPATH into sys.path for extension lookups)
            try {
                using (Py.GIL()) {
                    dynamic sys = Py.Import("sys");
                    sys.path.insert(0, interpreterDlls); // interpreterDlls = Path.Combine(interpreterRoot, "DLLs")
                    Console.WriteLine($"Inserted interpreter DLLs into sys.path: {interpreterDlls}");
                }
            } catch (Exception ex) {
                Console.WriteLine($"Warning: failed to insert interpreterDLLs into sys.path: {ex.Message}");
            }

            // IMPORT: show why import fails and try a safe fallback
            try {
                using var _ = Py.GIL();
                dynamic sys = Py.Import("sys");
                try {
                    _dcs = Py.Import("dcs");
                } catch (PythonException ex) {
                    // Print the Python exception and sys.path to help debugging
                    Console.WriteLine("Python import 'dcs' failed:");
                    Console.WriteLine(ex.ToString());
                    Console.WriteLine("Python runtime sys.path:");
                    try {
                        Console.WriteLine(string.Join(";", sys.path));
                    } catch { /* ignore */ }

                    // Fallback: if pydcsSrc is "...\\src\\pydcs" maybe the package layout requires parent folder.
                    var pydcsParent = Path.GetDirectoryName(pydcsSrc);
                    if (!string.IsNullOrEmpty(pydcsParent) && Directory.Exists(pydcsParent) && !pythonPathEntries.Contains(pydcsParent)) {
                        Console.WriteLine($"Attempting fallback by prepending parent path: {pydcsParent}");
                        // update environment + pythonnet PythonPath
                        pythonPathEntries.Insert(0, pydcsParent);
                        pythonPathValue = string.Join(";", pythonPathEntries);
                        Environment.SetEnvironmentVariable("PYTHONPATH", pythonPathValue, EnvironmentVariableTarget.Process);
                        PythonEngine.PythonPath = pythonPathValue;

                        // Update running interpreter sys.path
                        try {
                            sys.path.insert(0, pydcsParent);
                        } catch {
                            // ignore if insertion fails, still try import
                        }

                        try {
                            _dcs = Py.Import("dcs");
                        } catch (PythonException ex2) {
                            Console.WriteLine("Retry import 'dcs' after fallback failed:");
                            Console.WriteLine(ex2.ToString());
                            throw new InvalidOperationException("Failed to import 'dcs' module after fallback. See inner exception for Python details.", ex2);
                        }
                    } else {
                        throw new InvalidOperationException("Failed to import 'dcs' module and no fallback available. See inner exception for Python details.", ex);
                    }
                }
            } catch (Exception finalEx) {
                // Re-throw to make failure visible to caller (and keep printed Python diagnostics above).
                throw new InvalidOperationException("Python import/initialization failed. See console output for Python details.", finalEx);
            }

            // Print interpreter view for verification
            using (Py.GIL()) {
                dynamic sys = Py.Import("sys");
                Console.WriteLine($"sys.executable: {sys.executable}");
                Console.WriteLine($"sys.prefix: {sys.prefix}");
                Console.WriteLine($"sys.path: {string.Join(";", sys.path)}");
            }
        }

        public void Dispose() {
            if (_dcs is not null) {
                try {
                    using var _ = Py.GIL();
                    _dcs.Dispose();
                } catch {
                    // Best-effort
                }
            }
        }

        public static void ShutdownPythonRuntime() {
            lock (_shutdownLock) {
                if (_shutdownCalled) return;
                try {
                    if (PythonEngine.IsInitialized) {
                        try {
                            PythonEngine.Shutdown();
                        } catch (PlatformNotSupportedException ex) {
                            Console.WriteLine($"[WARN] Shutdown issue: {ex}");
                        }
                    }
                } finally {
                    _shutdownCalled = true;
                }
            }
        }
    }
}
