
using System;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection.Emit;

namespace SDILReader
{
    public class ILInstruction
    {
        #region fields
        private OpCode code;
        private object operand;
        private byte[] operandData;
        private int offset;
        #endregion

        #region Properties
        public OpCode Code
        {
            get { return code; }
            set { code = value; }
        }

        public object Operand
        {
            get { return operand; }
            set { operand = value; }
        }

        public byte[] OperandData
        {
            get { return operandData; }
            set { operandData = value; }
        }

        public int Offset
        {
            get { return offset; }
            set { offset = value; }
        }

        #endregion

        /// <summary>
        /// Returns a friendly strign representation of this instruction
        /// </summary>
        /// <returns></returns>
        public string GetCode()
        {
            string result = "";
            result += GetExpandedOffset(offset) + " : " + code;
            if (operand != null)
            {
                switch (code.OperandType)
                {
                    case OperandType.InlineField:
                        System.Reflection.FieldInfo fOperand = ((System.Reflection.FieldInfo)operand);
                        result += " " + Globals.ProcessSpecialTypes(fOperand.FieldType.ToString()) + " " +
                            Globals.ProcessSpecialTypes(fOperand.ReflectedType.ToString()) +
                            "::" + fOperand.Name + "";
                        break;
                    case OperandType.InlineMethod:
                        try
                        {
                            System.Reflection.MethodInfo mOperand = (System.Reflection.MethodInfo)operand;
                            result += " ";
                            if (!mOperand.IsStatic) result += "instance ";
                            result += Globals.ProcessSpecialTypes(mOperand.ReturnType.ToString()) +
                                " " + Globals.ProcessSpecialTypes(mOperand.ReflectedType.ToString()) +
                                "::" + mOperand.Name + "()";
                        }
                        catch
                        {
                            try
                            {
                                System.Reflection.ConstructorInfo mOperand = (System.Reflection.ConstructorInfo)operand;
                                result += " ";
                                if (!mOperand.IsStatic) result += "instance ";
                                result += "void " +
                                    Globals.ProcessSpecialTypes(mOperand.ReflectedType.ToString()) +
                                    "::" + mOperand.Name + "()";
                            }
                            catch
                            {
                            }
                        }
                        break;
                    case OperandType.ShortInlineBrTarget:
                        result += " " + GetExpandedOffset((int)operand);
                        break;
                    case OperandType.InlineType:
                        result += " " + Globals.ProcessSpecialTypes(operand.ToString());
                        break;
                    case OperandType.InlineString:
                        if (operand.ToString() == "\r\n") result += " \"\\r\\n\"";
                        else result += " \"" + operand.ToString() + "\"";
                        break;
                    default: result += "not supported"; break;
                }
            }
            return result;

        }

        /// <summary>
        /// Add enough zeros to a number as to be represented on 4 characters
        /// </summary>
        /// <param name="offset">
        /// The number that must be represented on 4 characters
        /// </param>
        /// <returns>
        /// </returns>
        private string GetExpandedOffset(int offset)
        {
            string result = offset.ToString();
            for (int i = 0; result.Length < 4; i++)
            {
                result = "0" + result;
            }
            return result;
        }

        /// <summary>
        /// Normal constructor.
        /// </summary>
        public ILInstruction()
        {

        }
    }
}

namespace SDILReader
{
    public class MethodBodyReader
    {
        public List<SDILReader.ILInstruction> instructions = null;
        public byte[] il = null;


        #region il read methods
        private int ReadInt16(byte[] _il, ref int position)
        {
            return ((il[position++] | (il[position++] << 8)));
        }
        private ushort ReadUInt16(byte[] _il, ref int position)
        {
            return (ushort)((il[position++] | (il[position++] << 8)));
        }
        private int ReadInt32(byte[] _il, ref int position)
        {
            return (((il[position++] | (il[position++] << 8)) | (il[position++] << 0x10)) | (il[position++] << 0x18));
        }
        private ulong ReadInt64(byte[] _il, ref int position)
        {
            return (ulong)(((il[position++] | (il[position++] << 8)) | (il[position++] << 0x10)) | (il[position++] << 0x18) | (il[position++] << 0x20) | (il[position++] << 0x28) | (il[position++] << 0x30) | (il[position++] << 0x38));
        }
        private double ReadDouble(byte[] _il, ref int position)
        {
            return (((il[position++] | (il[position++] << 8)) | (il[position++] << 0x10)) | (il[position++] << 0x18) | (il[position++] << 0x20) | (il[position++] << 0x28) | (il[position++] << 0x30) | (il[position++] << 0x38));
        }
        private sbyte ReadSByte(byte[] _il, ref int position)
        {
            return (sbyte)il[position++];
        }
        private byte ReadByte(byte[] _il, ref int position)
        {
            return (byte)il[position++];
        }
        private Single ReadSingle(byte[] _il, ref int position)
        {
            return (Single)(((il[position++] | (il[position++] << 8)) | (il[position++] << 0x10)) | (il[position++] << 0x18));
        }
        #endregion

		static MethodBodyReader() {
			Globals.LoadOpCodes();
		}
        /// <summary>
        /// Constructs the array of ILInstructions according to the IL byte code.
        /// </summary>
        /// <param name="module"></param>
        private void ConstructInstructions(Module module)
        {
            byte[] il = this.il;
            int position = 0;
            instructions = new List<ILInstruction>();
            while (position < il.Length)
            {
                ILInstruction instruction = new ILInstruction();

                // get the operation code of the current instruction
                OpCode code = OpCodes.Nop;
                ushort value = il[position++];
                if (value != 0xfe)
                {
                    code = Globals.singleByteOpCodes[(int)value];
                }
                else
                {
                    value = il[position++];
                    code = Globals.multiByteOpCodes[(int)value];
                    value = (ushort)(value | 0xfe00);
                }
                instruction.Code = code;
                instruction.Offset = position - 1;
                int metadataToken = 0;
                // get the operand of the current operation
                switch (code.OperandType)
                {
                    case OperandType.InlineBrTarget:
                        metadataToken = ReadInt32(il, ref position);
                        metadataToken += position;
                        instruction.Operand = metadataToken;
                        break;
                    case OperandType.InlineField:
                        metadataToken = ReadInt32(il, ref position);
                        instruction.Operand = module.ResolveField(metadataToken);
                        break;
                    case OperandType.InlineMethod:
                        metadataToken = ReadInt32(il, ref position);
                        try
                        {
                            instruction.Operand = module.ResolveMethod(metadataToken);
                        }
                        catch
                        {
                            instruction.Operand = module.ResolveMember(metadataToken);
                        }
                        break;
                    case OperandType.InlineSig:
                        metadataToken = ReadInt32(il, ref position);
                        instruction.Operand = module.ResolveSignature(metadataToken);
                        break;
                    case OperandType.InlineTok:
                        metadataToken = ReadInt32(il, ref position);
                        // SSS : see what to do here
                        break;
                    case OperandType.InlineType:
                        metadataToken = ReadInt32(il, ref position);
                        instruction.Operand = module.ResolveType(metadataToken);
                        break;
                    case OperandType.InlineI:
                        {
                            instruction.Operand = ReadInt32(il, ref position);
                            break;
                        }
                    case OperandType.InlineI8:
                        {
                            instruction.Operand = ReadInt64(il, ref position);
                            break;
                        }
                    case OperandType.InlineNone:
                        {
                            instruction.Operand = null;
                            break;
                        }
                    case OperandType.InlineR:
                        {
                            instruction.Operand = ReadDouble(il, ref position);
                            break;
                        }
                    case OperandType.InlineString:
                        {
                            metadataToken = ReadInt32(il, ref position);
                            instruction.Operand = module.ResolveString(metadataToken);
                            break;
                        }
                    case OperandType.InlineSwitch:
                        {
                            int count = ReadInt32(il, ref position);
                            int[] casesAddresses = new int[count];
                            for (int i = 0; i < count; i++)
                            {
                                casesAddresses[i] = ReadInt32(il, ref position);
                            }
                            int[] cases = new int[count];
                            for (int i = 0; i < count; i++)
                            {
                                cases[i] = position + casesAddresses[i];
                            }
                            break;
                        }
                    case OperandType.InlineVar:
                        {
                            instruction.Operand = ReadUInt16(il, ref position);
                            break;
                        }
                    case OperandType.ShortInlineBrTarget:
                        {
                            instruction.Operand = ReadSByte(il, ref position) + position;
                            break;
                        }
                    case OperandType.ShortInlineI:
                        {
                            instruction.Operand = ReadSByte(il, ref position);
                            break;
                        }
                    case OperandType.ShortInlineR:
                        {
                            instruction.Operand = ReadSingle(il, ref position);
                            break;
                        }
                    case OperandType.ShortInlineVar:
                        {
                            instruction.Operand = ReadByte(il, ref position);
                            break;
                        }
                    default:
                        {
                            throw new Exception("Unknown operand type.");
                        }
                }
                instructions.Add(instruction);
            }
        }

        /// <summary>
        /// Gets the IL code of the method
        /// </summary>
        /// <returns></returns>
        public string GetBodyCode()
        {
            string result = "";
            if (instructions != null)
            {
                for (int i = 0; i < instructions.Count; i++)
                {
                    result += instructions[i].GetCode() + "\n";
                }
            }
            return result;

        }

        /// <summary>
        /// MethodBodyReader constructor
        /// </summary>
        /// <param name="mi">
        /// The System.Reflection defined MethodInfo
        /// </param>
        public MethodBodyReader(MethodInfo mi)
        {
            if (mi.GetMethodBody() != null)
            {
                il = mi.GetMethodBody().GetILAsByteArray();
                ConstructInstructions(mi.Module);
            }
        }
    }
}

namespace TestDependency
{
		
	[TestFixture]
	public class Test
	{
		
		[Test]
		public void TestCase()
		{
			var illegalNamespace ="Foo.Bar";
			var errors = from type in ReferencedAssemblies.Types 
				where type.ReferencesNameSpace(illegalNamespace)
				select type;
			Assert.IsTrue(errors.Any() == false, string.Format("{0} should not reference namespace {1}", errors, illegalNamespace)); 
			
		}
		
			[Test]
		public void IlFromMethod()
		{

			var mbr = new MethodbodyReader(GetType().GetMethod("IlFromMethod"));
		}
		
		public void AssemblyToIL(Assembly assembly) {
			var proc = new System.Diagnostics.Process();
			proc.EnableRaisingEvents=false;
            proc.StartInfo.FileName="/usr/bin/ilasm";
proc.Start();
			/*
Usage is: monodis [--output=filename] [--filter=filename] [--help] [--mscorlib]
[--assembly] [--assemblyref] [--classlayout] 
[--constant] [--customattr] [--declsec] [--event] [--exported] 
[--fields] [--file] [--genericpar] [--interface] [--manifest] 
[--marshal] [--memberref] [--method] [--methodimpl] [--methodsem] 
[--methodspec] [--moduleref] [--module] [--mresources] [--nested] 
[--param] [--parconst] [--property] [--propertymap] [--typedef] 
[--typeref] [--typespec] [--implmap] [--standalonesig] [--methodptr] 
[--fieldptr] [--paramptr] [--eventptr] [--propertyptr] [--blob] 
[--strings] [--userstrings] [--forward-decls] file ..
			 */
		}
	}
				
	
	public static class ReferencedAssemblies {
		
	 	public static ICollection<Type> Types {
		  	get {
				Console.WriteLine("entry " + Assembly.GetExecutingAssembly());
				var assemblies = Assembly.GetExecutingAssembly()
										 .GetReferencedAssemblies();
				return assemblies.Aggregate(new List<Type>(), (types, assembly) => {
					var a = Assembly.Load(assembly);
					types.AddRange(a.GetTypes().ToList());
					return types;
				});
			}
	  	}

		
		public static bool ReferencesNameSpace(this Type type, params string[] namespacename) {
			//check all method signatures
			var interfaces = type.GetInterfaces();
			var baseType = type.BaseType;
			
			if (type.IsInterface == false) {
				
			} 
			
			return true;
		}
	}
}