using System;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

internal static class Inspect
{
    private static void Main(string[] args)
    {
        using (var module = ModuleDefMD.Load(args[0]))
        {
            Console.WriteLine("Runtime: " + module.RuntimeVersion);
            Console.WriteLine("Assembly: " + module.Assembly.FullName);
            Console.WriteLine("MVID: " + module.Mvid);
            if (args.Length > 1 && args[1] == "types")
            {
                foreach (var type in module.GetTypes())
                {
                    Console.WriteLine("TYPE " + type.MDToken + " " + type.FullName + " : " + type.BaseType);
                    foreach (var method in type.Methods)
                        Console.WriteLine("  " + method.MDToken + " " + method.FullName);
                    foreach (var field in type.Fields)
                        Console.WriteLine("  FIELD " + field.MDToken + " " + field.FullName);
                }
                return;
            }
            foreach (var type in module.GetTypes())
            foreach (var method in type.Methods)
            {
                if (!method.HasBody) continue;
                if (args.Length > 1 && args[1].StartsWith("refs:"))
                {
                    uint token = Convert.ToUInt32(args[1].Substring(5), 16);
                    foreach (var instruction in method.Body.Instructions)
                    {
                        var operand = instruction.Operand as IMDTokenProvider;
                        if (operand != null && operand.MDToken.Raw == token)
                            Console.WriteLine(method.MDToken + " " + method.FullName + " " + instruction);
                    }
                    continue;
                }
                if (args.Length > 1 && args[1] == "keysummary")
                {
                    if (method.FullName.Contains("Input.Keys") && method.Body.Instructions.Count > 100)
                        Console.WriteLine(method.MDToken + " " + method.Body.Instructions.Count + " " + method.FullName);
                    continue;
                }
                bool match = false;
                if (args.Length > 1 && args[1] == "keys")
                {
                    if (method.FullName.Contains("Input.Keys"))
                        foreach (var instruction in method.Body.Instructions)
                            if (instruction.IsLdcI4() && instruction.GetLdcI4Value() == 76) match = true;
                }
                else if (args.Length > 1)
                    match = method.MDToken.Raw == Convert.ToUInt32(args[1], 16);
                if (args.Length == 1)
                    foreach (var instruction in method.Body.Instructions)
                    {
                        if (instruction.OpCode != OpCodes.Ldstr) continue;
                        string value = (string)instruction.Operand;
                        if (value.IndexOf("reload", StringComparison.OrdinalIgnoreCase) >= 0
                            || value.IndexOf("revert", StringComparison.OrdinalIgnoreCase) >= 0
                            || value.IndexOf("load beatmap", StringComparison.OrdinalIgnoreCase) >= 0)
                            match = true;
                    }

                if (!match) continue;
                Console.WriteLine("\nTOKEN " + method.MDToken + " " + method.FullName);
                foreach (var instruction in method.Body.Instructions)
                    Console.WriteLine(instruction);
            }
        }
    }
}
