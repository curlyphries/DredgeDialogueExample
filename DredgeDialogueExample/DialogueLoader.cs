using HarmonyLib;
using System.Collections.Generic;
using Yarn.Unity;
using Yarn;
using CsvHelper;
using System.IO;
using Winch.Core;
using CsvHelper.Configuration.Attributes;
using CsvHelper.Configuration;
using System;
using System.Linq;

namespace DredgeDialogueExample
{
    public class Line
    {
        [Index(0), Name("id")]
        public string Id { get; set; }

        [Index(1), Name("text")]
        public string Text { get; set; }

        [Index(2), Name("file")]
        public string File { get; set; }

        [Index(3), Name("node")]
        public string Node { get; set; }

        [Index(4), Name("lineNumber")]
        public int LineNumber { get; set; }
    }

    public class LineMetadata
    {
        [Index(0), Name("id")]
        public string Id { get; set; }

        [Index(1), Name("node")]
        public string Node { get; set; }

        [Index(2), Name("lineNumber")]
        public int LineNumber { get; set; }

        [Index(3), Name("tags")]
        public List<string> Tags { get; set; }
    }

    public sealed class LineMetadataMap : ClassMap<LineMetadata>
    {
        public LineMetadataMap()
        {
            Map(m => m.Id).Index(0);
            Map(m => m.Node).Index(1);
            Map(m => m.LineNumber).Index(2);
            Map(m => m.Tags).Index(3);
        }
    }

    public class DialogueLoader
    {
        public static List<Line> lines = new();
        public static List<LineMetadata> metadata = new();
        public static List<Program> programs = new();

        public static void LoadDialogues()
        {
            WinchCore.Log.Debug("Loading dialogues...");

            string[] modDirs = Directory.GetDirectories("Mods");    
            foreach (string modDir in modDirs)
            {
                string assetFolderPath = Path.Combine(modDir, "Assets");
                if (!Directory.Exists(assetFolderPath))
                    continue;
                string dialogueFolderPath = Path.Combine(assetFolderPath, "Dialogues");

                if (Directory.Exists(dialogueFolderPath)) LoadDialoguesFiles(dialogueFolderPath);
            }
        }

        private static void LoadDialoguesFiles(string dialogueFolderPath)
        {
            FileInfo[] fileInfos = new FileInfo[Directory.GetFiles(dialogueFolderPath).Where(f => f.EndsWith(".yarn")).Count()];
            int i = 0;

            foreach(string file in Directory.GetFiles(dialogueFolderPath))
            {
                if (!file.EndsWith(".yarn")) continue;

                fileInfos[i] = new FileInfo(file);
                i++;
            }
            try
            {
                YarnSpinnerConsole.CompileCommand.CompileFiles(fileInfos, new DirectoryInfo(dialogueFolderPath), "output", null, null, false);

            } catch (Exception ex)
            {
                WinchCore.Log.Error(ex);
            }

            WinchCore.Log.Info(Path.Combine(dialogueFolderPath, "output.yarn"));

            Load(dialogueFolderPath);
        }

        private static void Load(string path)
        {
            //StreamReader sr2 = new StreamReader("Mods\\DredgeDialogueExample\\Dialogue\\DialogueExample-Lines.csv");
            using (var csv = new CsvReader(new StreamReader(Path.Combine(path, "output-Lines.csv"))))
            {
                int n = 0;
                csv.Configuration.Delimiter = ",";
                var records = csv.GetRecords<Line>();
                foreach (var record in records)
                {
                    lines.Add(record);
                }
                WinchCore.Log.Debug("Loaded " + n + " records");
            }

            using (var csv = new CsvReader(new StreamReader(Path.Combine(path, "output-Metadata.csv"))))
            {
                int n = 0;
                csv.Configuration.Delimiter = ",";
                csv.Configuration.RegisterClassMap<LineMetadataMap>();
                var records = csv.GetRecords<LineMetadata>();
                foreach (var record in records)
                {
                    metadata.Add(record);
                    n++;
                }
                WinchCore.Log.Debug("Loaded " + n + " metadata");
            }
            using(var sr = new StreamReader(Path.Combine(path, "output.yarnc")))
            {
                var bytes = default(byte[]);
                using (var memstream = new MemoryStream())
                {
                    sr.BaseStream.CopyTo(memstream);
                    bytes = memstream.ToArray();
                }
                programs.Add(Program.Parser.ParseFrom(bytes));
            }
        }

        public static void Inject()
        {
            DredgeDialogueRunner runner = GameManager.Instance.DialogueRunner;
            DredgeLocalizedLineProvider lineProvider = runner.lineProvider as DredgeLocalizedLineProvider;

            foreach (var line in lines)
            {
                lineProvider.stringTable.AddEntry(line.Id, line.Text);
            }

            var newProgram = new Program();

            Program oldProgram = Traverse.Create(runner.Dialogue).Field("program").GetValue<Program>();
            foreach (var nodeName in oldProgram.Nodes)
            {
                newProgram.Nodes[nodeName.Key] = nodeName.Value.Clone();
            }
            newProgram.InitialValues.Add(oldProgram.InitialValues);

            foreach (var modProgram in programs)
            {
                foreach (var nodeName in modProgram.Nodes)
                {
                    newProgram.Nodes[nodeName.Key] = nodeName.Value.Clone();
                }
                newProgram.InitialValues.Add(modProgram.InitialValues);
            }

            runner.Dialogue.SetProgram(newProgram);

            WinchCore.Log.Debug("yippee");
            var _lineMetadata = Traverse.Create(runner.yarnProject.lineMetadata).Field("_lineMetadata").GetValue<SerializedDictionary<string, string>>();
            
            foreach (var metadataEntry in metadata)
            {
                _lineMetadata.Add(metadataEntry.Id, string.Join(" ", metadataEntry.Tags));
            }
        }
    }
}
