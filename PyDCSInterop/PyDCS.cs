using Python.Runtime;
using System.Diagnostics;

namespace PyDCSInterop {
    // Minimal, deterministic Python initialization for a known working venv and DLL.
    // Usage:
    //   using var py = new PyDCS(@"C:\full\path\to\.venv", @"C:\full\path\to\python312.dll", debug: true, explicitPath: @"E:\dev\airgoons_campaign_editor");
    // This intentionally removes probing/heuristics and only applies the explicit paths provided.
    public sealed class PyDCS : IDisposable {
        private PyObject? _dcs = null;
        private PyObject? _pydcs_extensions = null;

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
        public dynamic PydcsExtensions {
            get {
                if (_pydcs_extensions is null)
                    throw new InvalidOperationException("pydcs_extensions module not initialized. Ensure the PyDCS constructor completed successfully.");
                try {
                    using var _ = Py.GIL();
                }
                catch (Exception ex) {
                    throw new InvalidOperationException("Unable to acquire the Python GIL when accessing pydcs_extensions.", ex);
                }
                return _pydcs_extensions;
            }
        }

        private static readonly object _shutdownLock = new();
        private static bool _shutdownCalled = false;

        // Simplified constructor: accepts an explicit venv root and optional python DLL path.
        // This version will also include a common source location for the pydcs package:
        //   <venv>\src\pydcs
        // If that folder exists it will be added to PYTHONPATH so `import dcs` succeeds.
        // It also attempts to locate a repository root that contains a top-level `pydcs_extensions`
        // package (or a top-level __init__.py) and adds that folder to PYTHONPATH so the
        // package located at the repo root can be imported.
        // Set debug=true to print Python sys.path and exception details to stderr for troubleshooting.
        // explicitPath can be used to add a known folder (for example your repo root) to PYTHONPATH.
        public PyDCS(string pathToVirtualEnv, string? pythonDllPath = null, bool debug = false, string? explicitPath = null) {
            if (string.IsNullOrWhiteSpace(pathToVirtualEnv))
                throw new ArgumentException("pathToVirtualEnv must be a non-empty path.", nameof(pathToVirtualEnv));

            var resolvedVenv = Path.GetFullPath(pathToVirtualEnv);

            if (!Directory.Exists(resolvedVenv))
                throw new DirectoryNotFoundException($"Specified virtual environment directory not found: {resolvedVenv}");

            // If explicitPath provided, validate immediately to fail fast
            string? resolvedExplicitPath = null;
            if (!string.IsNullOrWhiteSpace(explicitPath)) {
                resolvedExplicitPath = Path.GetFullPath(explicitPath);
                if (!Directory.Exists(resolvedExplicitPath)) {
                    throw new DirectoryNotFoundException($"Explicit path provided but not found: {resolvedExplicitPath}");
                }
            }

            // Build venv paths
            var libPath = Path.Combine(resolvedVenv, "Lib");
            var sitePackages = Path.Combine(libPath, "site-packages");
            var dllsPath = Path.Combine(resolvedVenv, "DLLs");
            var scriptsPath = Path.Combine(resolvedVenv, "Scripts");
            var pydcsSrc = Path.Combine(resolvedVenv, "src", "pydcs");

            // Determine interpreterRoot from the venv when possible:
            // 1. If pyvenv.cfg contains a "home =" entry, prefer that (points to real Python install).
            // 2. If the venv itself contains Lib/encodings, the venv can be treated as interpreter root.
            // 3. If Scripts/python.exe exists, run it to obtain sys.base_prefix as a fallback.
            string? DetermineInterpreterRoot() {
                try {
                    var cfg = Path.Combine(resolvedVenv, "pyvenv.cfg");
                    if (File.Exists(cfg)) {
                        var lines = File.ReadAllLines(cfg);
                        foreach (var ln in lines) {
                            var l = ln.Trim();
                            if (l.StartsWith("home", StringComparison.OrdinalIgnoreCase)) {
                                var parts = l.Split('=', 2);
                                if (parts.Length == 2) {
                                    var home = parts[1].Trim();
                                    if (!string.IsNullOrEmpty(home) && Directory.Exists(home)) {
                                        if (debug) Console.Error.WriteLine($"[DEBUG] pyvenv.cfg home => {home}");
                                        return home;
                                    }
                                }
                            }
                        }
                        if (debug) Console.Error.WriteLine("[DEBUG] pyvenv.cfg present but no usable 'home' entry found.");
                    }

                    // If the venv contains the stdlib directly, use the venv as interpreter root.
                    var venvEnc = Path.Combine(resolvedVenv, "Lib", "encodings", "__init__.py");
                    if (File.Exists(venvEnc)) {
                        if (debug) Console.Error.WriteLine($"[DEBUG] venv contains stdlib encodings; using venv as interpreterRoot: {resolvedVenv}");
                        return resolvedVenv;
                    }

                    // If there is a python executable inside the venv, ask it for sys.base_prefix.
                    var venvPython = Path.Combine(resolvedVenv, "Scripts", "python.exe");
                    if (File.Exists(venvPython)) {
                        try {
                            var psi = new ProcessStartInfo(venvPython, "-c \"import sys;print(sys.base_prefix)\"") {
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            };
                            using var proc = Process.Start(psi);
                            if (proc != null) {
                                var outStr = proc.StandardOutput.ReadToEnd();
                                var errStr = proc.StandardError.ReadToEnd();
                                proc.WaitForExit(3000);
                                var candidate = outStr.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
                                if (!string.IsNullOrEmpty(candidate) && Directory.Exists(candidate)) {
                                    var candEnc = Path.Combine(candidate, "Lib", "encodings", "__init__.py");
                                    if (File.Exists(candEnc)) {
                                        if (debug) Console.Error.WriteLine($"[DEBUG] venv python sys.base_prefix => {candidate}");
                                        return candidate;
                                    }
                                }
                                if (debug && !string.IsNullOrEmpty(errStr)) Console.Error.WriteLine($"[DEBUG] venv python stderr: {errStr}");
                            }
                        } catch (Exception ex) {
                            if (debug) Console.Error.WriteLine($"[DEBUG] Failed to run venv python to determine base prefix: {ex.Message}");
                        }
                    }

                    if (debug) Console.Error.WriteLine("[DEBUG] Could not determine interpreterRoot from venv.");
                } catch (Exception ex) {
                    if (debug) Console.Error.WriteLine("[DEBUG] DetermineInterpreterRoot error: " + ex);
                }
                return null;
            }

            var interpreterRoot = DetermineInterpreterRoot();

            // prefer explicit dll if provided, otherwise pick interpreterRoot\python312.dll
            if (!string.IsNullOrWhiteSpace(pythonDllPath)) {
                var resolvedDll = Path.GetFullPath(pythonDllPath);
                if (!File.Exists(resolvedDll)) throw new FileNotFoundException("Specified Python DLL not found.", resolvedDll);
                Runtime.PythonDLL = resolvedDll;
            } else {
                var defaultDll = Path.Combine(interpreterRoot, "python312.dll");
                if (File.Exists(defaultDll)) Runtime.PythonDLL = defaultDll;
            }

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

            // Attempt to locate a repository root that contains a top-level pydcs_extensions package
            // or a top-level __init__.py (some repos use that). Walk upward from the current working directory.
            var repoRoot = FindRepoRootContaining("pydcs_extensions");

            // If the repo root itself contains a top-level __init__.py, adding the repo root
            // to PYTHONPATH can make the repo root be treated as a package root — in that case
            // Python needs the repoRoot's parent directory on sys.path so top-level imports
            // like "import pydcs_extensions" still work. Prefer adding the parent when repo root
            // contains an __init__.py.
            string? repoRootToAdd = null;
            if (repoRoot is not null) {
                var rootInit = Path.Combine(repoRoot, "__init__.py");
                if (File.Exists(rootInit)) {
                    var parent = Path.GetDirectoryName(repoRoot);
                    if (!string.IsNullOrEmpty(parent)) repoRootToAdd = parent;
                } else {
                    repoRootToAdd = repoRoot;
                }

                if (repoRootToAdd is not null && !pythonPathEntries.Contains(repoRootToAdd)) {
                    pythonPathEntries.Add(repoRootToAdd);
                }

                // Also add the explicit package folder if present (e.g. ...\repo\pydcs_extensions)
                var explicitPkg = Path.Combine(repoRoot, "pydcs_extensions");
                if (Directory.Exists(explicitPkg) && !pythonPathEntries.Contains(explicitPkg)) {
                    pythonPathEntries.Add(explicitPkg);
                }
            }

            // Add the user-provided explicit path last (so it can override/augment above)
            if (resolvedExplicitPath is not null && !pythonPathEntries.Contains(resolvedExplicitPath)) {
                pythonPathEntries.Add(resolvedExplicitPath);
            }

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

            if (debug) {
                Console.Error.WriteLine($"[DEBUG] Runtime.PythonDLL = {Runtime.PythonDLL ?? "(not set)"}");
                Console.Error.WriteLine($"[DEBUG] PYTHONHOME = {Environment.GetEnvironmentVariable("PYTHONHOME")}");
                Console.Error.WriteLine($"[DEBUG] PYTHONPATH = {Environment.GetEnvironmentVariable("PYTHONPATH")}");
                if (resolvedExplicitPath is not null) Console.Error.WriteLine($"[DEBUG] explicitPath resolved to: {resolvedExplicitPath}");
            }

            try {
                PythonEngine.Initialize();
            } catch (Exception ex) {
                throw new InvalidOperationException(
                    $"Failed to initialize PythonEngine. PYTHONHOME={Environment.GetEnvironmentVariable("PYTHONHOME")}; PYTHONPATH={Environment.GetEnvironmentVariable("PYTHONPATH")}; Runtime.PythonDLL={(Runtime.PythonDLL ?? "(not set)")} ",
                    ex);
            }

            // NEW: ensure Python's import search path contains the interpreter DLLs folder
            // (some embedded runs do not reflect PYTHONPATH into sys.path for extension lookups)
            try {
                using (Py.GIL()) {
                    dynamic sys = Py.Import("sys");
                    sys.path.insert(0, interpreterDlls); // interpreterDlls = Path.Combine(interpreterRoot, "DLLs")
                    if (repoRootToAdd is not null) {
                        try {
                            sys.path.insert(0, repoRootToAdd);
                        } catch {
                            // ignore if insertion fails
                        }
                    }
                    // Also ensure explicit package path is visible
                    if (repoRoot is not null) {
                        var explicitPkg = Path.Combine(repoRoot, "pydcs_extensions");
                        try { if (Directory.Exists(explicitPkg)) sys.path.insert(0, explicitPkg); } catch { }
                    }

                    // Insert the explicit path requested by the caller (highest priority)
                    if (resolvedExplicitPath is not null) {
                        try { sys.path.insert(0, resolvedExplicitPath); } catch { }
                    }

                    if (debug) {
                        try {
                            var list = ((IEnumerable<dynamic>)sys.path).Cast<object>().Select(o => o.ToString()).ToArray();
                            Console.Error.WriteLine($"[DEBUG] sys.path after inserts: {string.Join(";", list)}");
                        } catch { }
                    }
                }
            } catch (Exception ex) {
                if (debug) Console.Error.WriteLine($"[DEBUG] Warning: failed to insert interpreterDLLs into sys.path: {ex.Message}");
            }

            // IMPORT: show why import fails and try a safe fallback
            try {
                using var _ = Py.GIL();
                dynamic sys = Py.Import("sys");
                try {
                    _dcs = Py.Import("dcs");
                    _pydcs_extensions = Py.Import("pydcs_extensions");
                    if (debug) Console.Error.WriteLine("[DEBUG] Successfully imported dcs and pydcs_extensions.");
                } catch (PythonException ex) {
                    if (debug) {
                        Console.Error.WriteLine("[DEBUG] Python import error:");
                        Console.Error.WriteLine(ex.ToString());
                        try {
                            var list = ((IEnumerable<dynamic>)sys.path).Cast<object>().Select(o => o.ToString()).ToArray();
                            Console.Error.WriteLine($"[DEBUG] sys.path at import failure: {string.Join(";", list)}");
                        } catch { }
                    }

                    // Fallback: if pydcsSrc is "...\\src\\pydcs" maybe the package layout requires parent folder.
                    var pydcsParent = Path.GetDirectoryName(pydcsSrc);
                    if (!string.IsNullOrEmpty(pydcsParent) && Directory.Exists(pydcsParent) && !pythonPathEntries.Contains(pydcsParent)) {
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
                            if (debug) Console.Error.WriteLine("[DEBUG] Reimported dcs after pydcsParent fallback.");
                        } catch (PythonException ex2) {
                            if (debug) Console.Error.WriteLine("[DEBUG] Retry import 'dcs' after fallback failed: " + ex2);
                            throw new InvalidOperationException("Failed to import 'dcs' module after fallback failed. See inner exception for Python details.", ex2);
                        }
                    } else {
                        // As another fallback, attempt to insert repoRootToAdd if found
                        if (repoRootToAdd is not null && !pythonPathEntries.Contains(repoRootToAdd)) {
                            pythonPathEntries.Insert(0, repoRootToAdd);
                            pythonPathValue = string.Join(";", pythonPathEntries);
                            Environment.SetEnvironmentVariable("PYTHONPATH", pythonPathValue, EnvironmentVariableTarget.Process);
                            PythonEngine.PythonPath = pythonPathValue;
                            try {
                                sys.path.insert(0, repoRootToAdd);
                            } catch { }
                            try {
                                _dcs = Py.Import("dcs");
                                _pydcs_extensions = Py.Import("pydcs_extensions");
                                if (debug) Console.Error.WriteLine("[DEBUG] Reimported modules after repo-root fallback.");
                            } catch (PythonException ex2) {
                                if (debug) Console.Error.WriteLine("[DEBUG] Final import failure: " + ex2);
                                throw new InvalidOperationException("Failed to import 'dcs' or 'pydcs_extensions' after repo-root fallback. See inner exception for Python details.", ex2);
                            }
                        } else {
                            throw new InvalidOperationException("Failed to import 'dcs' module and no fallback available. See inner exception for Python details.", ex);
                        }
                    }
                }
            } catch (Exception finalEx) {
                // Re-throw to make failure visible to caller (and keep printed Python diagnostics above).
                throw new InvalidOperationException("Python import/initialization failed. See console output for Python details.", finalEx);
            }

            // Print interpreter view for verification
            using (Py.GIL()) {
                dynamic sys = Py.Import("sys");
                // Console.WriteLine($"sys.executable: {sys.executable}");
                // Console.WriteLine($"sys.prefix: {sys.prefix}");
                // Console.WriteLine($"sys.path: {string.Join(";", sys.path)}");
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
            if (_pydcs_extensions is not null) {
                try {
                    using var _ = Py.GIL();
                    _pydcs_extensions.Dispose();
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
                            // Console.WriteLine($"[WARN] Shutdown issue: {ex}");
                        }
                    }
                } finally {
                    _shutdownCalled = true;
                }
            }
        }

        // Walk upward from the current working directory to find a directory that contains
        // either a directory named `packageName` (e.g. pydcs_extensions) or a top-level __init__.py.
        // Returns the directory path if found, otherwise null.
        private static string FindRepoRootContaining(string packageName) {
            try {
                var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
                while (dir != null) {
                    // If there's a folder matching the package name, treat this as repo root candidate
                    var packageDir = Path.Combine(dir.FullName, packageName);
                    if (Directory.Exists(packageDir)) return dir.FullName;

                    // If there's a top-level __init__.py (repo root as a package), accept it
                    var initPy = Path.Combine(dir.FullName, "__init__.py");
                    if (File.Exists(initPy)) return dir.FullName;

                    // If there's a .git folder treat as repo root
                    var gitDir = Path.Combine(dir.FullName, ".git");
                    if (Directory.Exists(gitDir)) return dir.FullName;

                    dir = dir.Parent;
                }
            } catch {
                // best-effort; if anything fails just return null
            }
            return null;
        }

        public static PyObject? ResolvePathOnPyObject(PyObject root, string dottedPath) {
            var parts = dottedPath.Split('.');
            if (parts.Length == 0) throw new ArgumentException($"invalid dottedPath: {nameof(dottedPath)}");

            PyObject cur = root;
            for (int i = 1; i < parts.Length; ++i) {
                cur = cur.GetAttr(parts[i]);
            }
            return cur;
        }
    }
}
