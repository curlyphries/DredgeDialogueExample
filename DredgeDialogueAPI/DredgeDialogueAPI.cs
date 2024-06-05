using System;
using HarmonyLib;
using UnityEngine;
using Winch.Core;
using Yarn;

namespace DredgeDialogueAPI
{
	public class DredgeDialogueAPI : MonoBehaviour
	{   
		public void Awake()
		{
			WinchCore.Log.Debug($"{nameof(DredgeDialogueAPI)} has loaded!");
            try
            {
                DialogueLoader.LoadDialogues();
            } catch(Exception ex)
            {
                WinchCore.Log.Error(ex);
            }

            GameManager.Instance.OnGameStarted += GameStarted;

            WinchCore.Log.Debug("Success!");
		}

		public void GameStarted()
		{
			Inject();
		}

		public void Inject()
		{
			DialogueLoader.Inject();
            try
            {
                AddInstruction("TravellingMerchant_ChatOptions", 1, Instruction.Types.OpCode.AddOption, "line:01f8b99", "L84shortcutoption_TravellingMerchant_ChatOptions_6", 0, false);
                AddInstruction("TravellingMerchant_ChatOptions", 2, Instruction.Types.OpCode.AddOption, "line:01f8b99", "L84shortcutoption_TravellingMerchant_ChatOptions_6", 0, false);
            } catch (Exception e)
            {
                WinchCore.Log.Error(e);
            }
        }

        public static void AddInstruction(string nodeID, int index, Yarn.Instruction.Types.OpCode opCode, params object[] operands)
        {
            Yarn.Instruction instruction = new Yarn.Instruction();
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
    }
}
