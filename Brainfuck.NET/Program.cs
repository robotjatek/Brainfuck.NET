using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Brainfuck.NET
{
    class Program
    {
        class Labels
        {
            public Label startOfBlock;
            public Label endOfBlock;
        }

        static void Main(string[] args)
        {
            var path = "mandelbrot.bf";
            if (args.Any())
                path = args[0];

            if (!File.Exists(path))
            {
                Console.WriteLine($"Error! Source file cannot be found: {path}");
                return;
            }

            var labels = new Stack<Labels>();

            var an = new AssemblyName("Brainfuck.NET");
            var dom = AppDomain.CurrentDomain;
            var builder = dom.DefineDynamicAssembly(an, AssemblyBuilderAccess.Save);
            var mb = builder.DefineDynamicModule(an.Name, "BF.exe", false);
            var tb = mb.DefineType("Brainfuck.Application", TypeAttributes.Public | TypeAttributes.Class);
            var methodBuilder = tb.DefineMethod("Main", MethodAttributes.Public | MethodAttributes.Static, typeof(int), new Type[] { });
            var gen = methodBuilder.GetILGenerator();

            // Create an optimised code from the original by merging instructions
            var precompiled = new Precompiler(path).Precompiled;

            var CellBuilder = gen.DeclareLocal(typeof(byte[]));
            gen.Emit(OpCodes.Ldc_I4, 30000);
            gen.Emit(OpCodes.Newarr, typeof(byte));
            gen.Emit(OpCodes.Stloc_S, CellBuilder);

            var DataPointerBuilder = gen.DeclareLocal(typeof(int));;
            gen.Emit(OpCodes.Ldc_I4_0);
            gen.Emit(OpCodes.Stloc, DataPointerBuilder);

            foreach (var symbol in precompiled)
            {
                switch (symbol.s)
                {
                    case Precompiler.States.DATA:
                        gen.Emit(OpCodes.Ldloc, CellBuilder);
                        gen.Emit(OpCodes.Ldloc, DataPointerBuilder);
                        gen.Emit(OpCodes.Ldloc, CellBuilder);
                        gen.Emit(OpCodes.Ldloc, DataPointerBuilder);
                        gen.Emit(OpCodes.Ldelem_I1);
                        gen.Emit(OpCodes.Ldc_I4, symbol.v);
                        gen.Emit(OpCodes.Add);
                        gen.Emit(OpCodes.Stelem_I1);
                        break;
                    case Precompiler.States.PTR:
                        gen.Emit(OpCodes.Ldloc, DataPointerBuilder);
                        gen.Emit(OpCodes.Ldc_I4, symbol.v);
                        gen.Emit(OpCodes.Add);
                        gen.Emit(OpCodes.Stloc, DataPointerBuilder);
                        break;
                    case Precompiler.States.OPEN_LOOP:
                        {
                            var blockBorders = new Labels
                            {
                                startOfBlock = gen.DefineLabel(),
                                endOfBlock = gen.DefineLabel()
                            };

                            gen.Emit(OpCodes.Ldloc, CellBuilder);
                            gen.Emit(OpCodes.Ldloc, DataPointerBuilder);
                            gen.Emit(OpCodes.Ldelem_I1);
                            gen.Emit(OpCodes.Brfalse, blockBorders.endOfBlock);
                            gen.MarkLabel(blockBorders.startOfBlock);
                            labels.Push(blockBorders);
                            break;
                        }
                    case Precompiler.States.CLOSE_LOOP:
                        {
                            var blockBorders = labels.Pop();
                            gen.Emit(OpCodes.Ldloc, CellBuilder);
                            gen.Emit(OpCodes.Ldloc, DataPointerBuilder);
                            gen.Emit(OpCodes.Ldelem_I1);
                            gen.Emit(OpCodes.Brtrue, blockBorders.startOfBlock);
                            gen.MarkLabel(blockBorders.endOfBlock);
                            break;
                        }
                    case Precompiler.States.PRINT:
                        gen.Emit(OpCodes.Ldloc, CellBuilder);
                        gen.Emit(OpCodes.Ldloc, DataPointerBuilder);
                        gen.Emit(OpCodes.Ldelem_I1);
                        gen.Emit(OpCodes.Call, typeof(Console).GetMethod("Write", new Type[] { typeof(char) }));
                        break;
                    case Precompiler.States.READ:
                        gen.Emit(OpCodes.Ldloc, CellBuilder);
                        gen.Emit(OpCodes.Ldloc, DataPointerBuilder);
                        gen.Emit(OpCodes.Call, typeof(Console).GetMethod("ReadKey", new Type[] { }));
                        gen.Emit(OpCodes.Stelem_I1);
                        break;
                    default:
                        //Skip symbol
                        break;
                }
            }
            gen.Emit(OpCodes.Call, typeof(Console).GetMethod("ReadKey", new Type[] { }));
            gen.Emit(OpCodes.Pop);

            gen.Emit(OpCodes.Ldc_I4_0);
            gen.Emit(OpCodes.Ret);

            tb.CreateType();
            builder.SetEntryPoint(methodBuilder, PEFileKinds.ConsoleApplication);
            builder.Save("BF.exe");

            Console.WriteLine("Compilation successful!");
            Console.WriteLine("The compiled executable can be found with the name: BF.exe");

            ConsoleKey key;
            do
            {
                Console.WriteLine("Press Enter to run the compiled executable or ESC to quit");
                key = Console.ReadKey().Key;
                if (key == ConsoleKey.Enter)
                    Process.Start("BF.exe");
            } while (key != ConsoleKey.Enter && key != ConsoleKey.Escape);
            
            Console.ReadKey();
        }
    }
}
