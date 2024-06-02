using HarmonyLib;
using System.Collections.Generic;
using Yarn.Unity;
using Yarn;
using CsvHelper;
using System.IO;
using Winch.Core;
using CsvHelper.Configuration.Attributes;
using CsvHelper.Configuration;

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

        public static void Load()
        {
            using (StringReader sr = new StringReader(Properties.Resources.Lines))
            {
                using (var csv = new CsvReader(sr))
                {
                    WinchCore.Log.Debug("Loading records!");
                    var records = csv.GetRecords<Line>();
                    foreach (var record in records)
                    {
                        lines.Add(record);
                    }
                }
            }

            using (StringReader sr = new StringReader(Properties.Resources.Metadata))
            {
                using (var csv = new CsvReader(sr))
                {
                    WinchCore.Log.Debug("Loading metadata!");
                    csv.Configuration.RegisterClassMap<LineMetadataMap>();
                    var records = csv.GetRecords<LineMetadata>();
                    foreach (var record in records)
                    {
                        metadata.Add(record);
                    }
                }
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

            Program modProgram = Program.Parser.ParseFrom(Properties.Resources.Program);
            var newProgram = new Program();

            Program oldProgram = Traverse.Create(runner.Dialogue).Field("program").GetValue<Program>();
            foreach (var nodeName in oldProgram.Nodes)
            {
                newProgram.Nodes[nodeName.Key] = nodeName.Value.Clone();
            }
            newProgram.InitialValues.Add(oldProgram.InitialValues);

            foreach (var nodeName in modProgram.Nodes)
            {
                newProgram.Nodes[nodeName.Key] = nodeName.Value.Clone();
            }
            newProgram.InitialValues.Add(modProgram.InitialValues);

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
