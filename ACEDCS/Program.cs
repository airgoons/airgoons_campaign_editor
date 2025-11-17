using MilitaryModel;
using TTSKML;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using PyDCSInterop;
using Microsoft.Extensions.Configuration;


namespace ACEDCS {
    public class Settings
    {
        public required string VenvPath { get; set; }
        public required string PyDllPath { get; set; }
        public required string PyDcsExtensionsPath { get; set; }
        public required string KmlPath { get; set; }
        public required string TemplateMizPath { get; set; }
        public required string OutputMizPath { get; set; }
        public required string FactionsPath { get; set; }
        public required string TagsPath { get; set; }
    }

    internal class Program {
        static void Main(string[] args) {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("acedcs_config.json", optional: false, reloadOnChange: false)
                .AddCommandLine(args)
                .Build();

            Settings? settingsConfig = config.GetRequiredSection("Settings").Get<Settings>();

            var topLevelUnits = KmlUnitImporter.Run(settingsConfig.KmlPath, settingsConfig.FactionsPath, settingsConfig.TagsPath);

            var fronts = MilitaryModel.DeploymentModel.KernelFlotGenerator.GenerateFronts(topLevelUnits, 500, 18_000, 1, 1, 0);

            try {
                var pydcs = new PyDCS(settingsConfig.VenvPath, settingsConfig.PyDllPath, false, settingsConfig.PyDcsExtensionsPath);
                SOTN.DCS.MissionPrep.GenerateMiz(pydcs, settingsConfig.TemplateMizPath, settingsConfig.OutputMizPath, topLevelUnits, fronts);
            }

            finally {
                PyDCS.ShutdownPythonRuntime();
            }
        }
    }
}
