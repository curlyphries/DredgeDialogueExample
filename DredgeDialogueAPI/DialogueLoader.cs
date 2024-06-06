using HarmonyLib;
using System.Collections.Generic;
using Yarn.Unity;
using Yarn;
using CsvHelper;
using System.IO;
using Winch.Core;
using CsvHelper.Configuration;
using System;
using System.Linq;
using Yarn.Compiler;
using System.Globalization;
using System.Text;

namespace DredgeDialogueAPI
{
    public class DialogueLoader
    {
        /// <summary>
        /// Unlocalized lines indexed by their IDs, loaded from mod .yarn files. Used as fallbacks. Loaded in LoadDialoguesFiles on startup.
        /// </summary>
        public static Dictionary<string, string> lines = new();

        /// <summary>
        /// Localized lines indexed by their IDs, loaded from mod CSV files. Loaded in Inject on game start (after main menu.)
        /// </summary>
        public static Dictionary<string, string> localizedLines = new();

        /// <summary>
        /// Hashtags (e.g. #exit #aah) for loaded dialogue lines.
        /// </summary>
        public static Dictionary<string, string[]> metadata = new();
        
        /// <summary>
        /// Programs from dialogue-providing mods.
        /// </summary>
        public static List<Program> programs = new();

        public static List<string> SearchForDialogueFolders()
        {
            string[] modDirs = Directory.GetDirectories("Mods");
            List<string> dialogueDirs = new();
            foreach (string modDir in modDirs)
            {
                string assetFolderPath = Path.Combine(modDir, "Assets");
                if (!Directory.Exists(assetFolderPath))
                    continue;
                string dialogueFolderPath = Path.Combine(assetFolderPath, "Dialogues");

                if (Directory.Exists(dialogueFolderPath)) dialogueDirs.Add(dialogueFolderPath);
            }

            return dialogueDirs;
        }

        public static void LoadDialogues()
        {
            WinchCore.Log.Debug("Loading dialogues...");

            foreach (string dialogueFolder in SearchForDialogueFolders())
            {
                LoadDialoguesFiles(dialogueFolder);
            }
        }

        private static void LoadDialoguesFiles(string dialogueFolderPath)
        {
            string[] yarnFiles = Directory.GetFiles(dialogueFolderPath).Where(f => f.EndsWith(".yarn")).ToArray();

            CompilationResult compilationResult = CompileProgram(yarnFiles);
            programs.Add(compilationResult.Program);

            // Load "fallback" lines from the Yarn program's string table.
            foreach (var stringEntry in compilationResult.StringTable)
            {
                lines[stringEntry.Key] = stringEntry.Value.text;

                metadata[stringEntry.Key] = stringEntry.Value.metadata.Where(x => !x.StartsWith("line:")).ToArray();
            }
        }

        private static CompilationResult CompileProgram(string[] inputs)
        {
            // The majority of this method is lifted from https://github.com/YarnSpinner-Tool/YarnSpinner-Console, which is licensed MIT.
            var compilationJob = CompilationJob.CreateFromFiles(inputs);

            // Declare the existence of 'visited' and 'visited_count'
            var visitedDecl = new DeclarationBuilder()
                .WithName("visited")
                .WithType(
                    new FunctionTypeBuilder()
                        .WithParameter(Yarn.BuiltinTypes.String)
                        .WithReturnType(Yarn.BuiltinTypes.Boolean)
                        .FunctionType)
                .Declaration;

            var visitedCountDecl = new DeclarationBuilder()
                .WithName("visited_count")
                .WithType(
                    new FunctionTypeBuilder()
                        .WithParameter(Yarn.BuiltinTypes.String)
                        .WithReturnType(Yarn.BuiltinTypes.Number)
                        .FunctionType)
                .Declaration;

            compilationJob.VariableDeclarations = (compilationJob.VariableDeclarations ?? Array.Empty<Declaration>()).Concat(new[] {
                visitedDecl,
                visitedCountDecl,
            });

            CompilationResult compilationResult;

            compilationResult = Compiler.Compile(compilationJob); // Exceptions will bubble through to Winch.

            return compilationResult;
        }

        public static void AddInstruction(string nodeID, int index, Yarn.Instruction.Types.OpCode opCode, params object[] operands)
        {
            Yarn.Instruction instruction = new Instruction();
            instruction.Opcode = opCode;

            foreach (var operand in operands)
            {
                if (operand is string)
                {
                    instruction.Operands.Add(new Operand((string)operand));
                }
                else if (operand is bool)
                {
                    instruction.Operands.Add(new Operand((bool)operand));
                }
                else if (operand is float || operand is int)
                {
                    instruction.Operands.Add(new Operand(Convert.ToSingle(operand)));
                }
            }

            DredgeDialogueRunner runner = GameManager.Instance.DialogueRunner;
            Program program = Traverse.Create(runner.Dialogue).Field("program").GetValue<Program>();

            program.Nodes[nodeID].Instructions.Insert(index, instruction);

            foreach (var label in program.Nodes[nodeID].Labels)
            {
                if (label.Value >= index)
                {
                    program.Nodes[nodeID].Labels[label.Key] += 1;
                }
            }
        }

        private static void LoadLocalizedLines()
        {
            // If the user reloads the game from the start menu after changing languages,
            // lines that were translated but that now aren't need to be set to fallbacks.
            localizedLines.Clear();
            foreach (string dialogueFolderPath in SearchForDialogueFolders())
            {
                // Language code (i.e. "en", "es", "pt-BR") from game settings.
                // The game's language can only be set from the main menu, before dialogue is loaded,
                // so in regard to dialogue, it's fine to only load once.
                string localeId = GameManager.Instance.SettingsSaveData.localeId;
                string linesPath = Path.Combine(dialogueFolderPath, $"lines-{localeId}.csv");
                if (File.Exists(linesPath))
                {
                    var config = new Configuration(CultureInfo.InvariantCulture)
                    {
                        PrepareHeaderForMatch = (header, index) => header.ToLowerInvariant(),
                    };

                    using (var reader = new StreamReader(linesPath, Encoding.UTF8))
                    using (var csv = new CsvReader(reader, config))
                    {
                        var records = csv.GetRecords<LinesCSVRecord>();

                        foreach (var record in records)
                        {
                            if (record.Id.Length == 0) continue;

                            localizedLines[record.Id] = record.Character.Length > 0 ? $"{record.Character}: {record.Text}" : record.Text;
                        }
                    }
                }
            }
        }
            
        public static void Inject()
        {
            LoadLocalizedLines();

            DredgeDialogueRunner runner = GameManager.Instance.DialogueRunner;
            DredgeLocalizedLineProvider lineProvider = runner.lineProvider as DredgeLocalizedLineProvider;

            foreach (var line in lines)
            {
                lineProvider.stringTable.AddEntry(line.Key, line.Value);
            }

            // Override localized lines.
            foreach (var localizedLine in localizedLines)
            {
                lineProvider.stringTable.AddEntry(localizedLine.Key, localizedLine.Value);
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

            var _lineMetadata = Traverse.Create(runner.yarnProject.lineMetadata).Field("_lineMetadata").GetValue<SerializedDictionary<string, string>>();

            foreach (var metadataEntry in metadata)
            {
                _lineMetadata.Add(metadataEntry.Key, string.Join(" ", metadataEntry.Value));
            }
        }
    }

    public class LinesCSVRecord
    {
        public string Character { get; set; }
        public string Text { get; set; }
        public string Id { get; set; }
    }
}
