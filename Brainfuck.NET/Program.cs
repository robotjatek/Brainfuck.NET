using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Reflection.Emit;
using System.IO;

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
            var labels = new Stack<Labels>();

            var an = new AssemblyName("Brainfuck.NET");
            var dom = AppDomain.CurrentDomain;
            var builder = dom.DefineDynamicAssembly(an, AssemblyBuilderAccess.Save);
            var mb = builder.DefineDynamicModule(an.Name, "BF.exe", false);
            var tb = mb.DefineType("Brainfuck.Application", TypeAttributes.Public | TypeAttributes.Class);
            var methodBuilder = tb.DefineMethod("Main", MethodAttributes.Public | MethodAttributes.Static, typeof(int), new Type[] { });
            var gen = methodBuilder.GetILGenerator();

            var dataMethod = tb.DefineMethod("AddToCell", MethodAttributes.Private | MethodAttributes.Static, typeof(void), new Type[] { });

            /*gen.Emit(OpCodes.Ldstr, "Hello world");
            gen.Emit(OpCodes.Call, typeof(Console).GetMethod("WriteLine", new Type[] { typeof(string) }));
            gen.Emit(OpCodes.Call, typeof(Console).GetMethod("ReadKey", new Type[] { }));
            gen.Emit(OpCodes.Pop);
            gen.Emit(OpCodes.Ldc_I4_0);
            gen.Emit(OpCodes.Ret);*/

            var p = new Precompiler("mandelbrot.bf");
            var precompiled = p.Precompiled;

            var CellBuilder = gen.DeclareLocal(typeof(byte[]));
            //CellBuilder.SetLocalSymInfo("Cells");
            gen.Emit(OpCodes.Ldc_I4, 30000);
            gen.Emit(OpCodes.Newarr, typeof(byte));
            gen.Emit(OpCodes.Stloc_S, CellBuilder);

            var DataPointerBuilder = gen.DeclareLocal(typeof(int));
            //DataPointerBuilder.SetLocalSymInfo("DataPointer");
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


            var t = tb.CreateType();
            builder.SetEntryPoint(methodBuilder, PEFileKinds.ConsoleApplication);
            builder.Save("BF.exe");
        }
    }
}
