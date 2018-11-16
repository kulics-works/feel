﻿using Antlr4.Runtime.Misc;
using System;
using System.Collections.Generic;
using System.Text;
using static Compiler.XsParser;

namespace Compiler
{
    internal partial class Visitor
    {
        public override object VisitStatement([NotNull] StatementContext context)
        {
            var obj = "";
            var ns = (Namespace)Visit(context.exportStatement());

            obj += $"namespace {ns.name + Wrap + BlockLeft + Wrap}";
            obj += ns.imports + Wrap;

            var unStatic = "";
            var Static = "";
            var hasStatic = false;
            foreach (var item in context.namespaceSupportStatement())
            {
                switch (item.GetChild(0))
                {
                    case NamespaceFunctionStatementContext _:
                    case NamespaceVariableStatementContext _:
                    case NamespaceInvariableStatementContext _:
                    case NamespaceConstantStatementContext _:
                        Static += Visit(item);
                        hasStatic = true;
                        break;
                    default:
                        unStatic += Visit(item);
                        break;
                }
            }
            if (hasStatic)
            {
                obj += $"using static {ns.staticName + Terminate + Wrap}";
                obj += $"public static partial class {ns.staticName} {BlockLeft + Wrap}";
                obj += Static;
                obj += BlockRight + Terminate + Wrap;
            }
            obj += unStatic;
            obj += BlockRight + Wrap;
            return obj;
        }

        class Namespace
        {
            public string name;
            public string staticName;
            public string imports;
        }

        public override object VisitExportStatement([NotNull] ExportStatementContext context)
        {
            var obj = new Namespace();
            obj.name = (string)Visit(context.nameSpace());

            obj.staticName = FileName;
            if (context.id() != null)
            {
                obj.staticName = (Visit(context.id()) as Result).text;
            }
            foreach (var item in context.importStatement())
            {
                obj.imports += (string)Visit(item);
            }
            return obj;
        }

        public override object VisitImportStatement([NotNull] ImportStatementContext context)
        {
            var obj = "";
            if (context.annotation() != null)
            {
                obj += Visit(context.annotation());
            }
            if (context.id() != null)
            {
                var ns = (string)Visit(context.nameSpace());
                obj += "using static " + ns;
                if (context.id() != null)
                {
                    var r = (Result)Visit(context.id());

                    obj += "." + r.text;
                }

                obj += Terminate;
            }
            else
            {
                obj += "using " + Visit(context.nameSpace()) + Terminate;
            }
            obj += Wrap;
            return obj;
        }

        public override object VisitNameSpace([NotNull] NameSpaceContext context)
        {
            var obj = "";
            for (int i = 0; i < context.id().Length; i++)
            {
                var id = (Result)Visit(context.id(i));
                if (i == 0)
                {
                    obj += "" + id.text;
                }
                else
                {
                    obj += "." + id.text;
                }
            }
            return obj;
        }

        public override object VisitNameSpaceItem([NotNull] NameSpaceItemContext context)
        {
            var obj = "";
            for (int i = 0; i < context.id().Length; i++)
            {
                var id = (Result)Visit(context.id(i));
                if (i == 0)
                {
                    obj += "" + id.text;
                }
                else
                {
                    obj += "." + id.text;
                }
            }
            return obj;
        }

        public override object VisitName([NotNull] NameContext context)
        {
            var obj = "";
            for (int i = 0; i < context.id().Length; i++)
            {
                var id = (Result)Visit(context.id(i));
                if (i == 0)
                {
                    obj += "" + id.text;
                }
                else
                {
                    obj += "." + id.text;
                }
            }
            return obj;
        }

        public override object VisitEnumStatement([NotNull] EnumStatementContext context)
        {
            var obj = "";
            var id = (Result)Visit(context.id());
            var header = "";
            if (context.annotation() != null)
            {
                header += Visit(context.annotation());
            }
            header += id.permission + " enum " + id.text;
            header += Wrap + BlockLeft + Wrap;
            for (int i = 0; i < context.enumSupportStatement().Length; i++)
            {
                obj += Visit(context.enumSupportStatement(i));
            }
            obj += BlockRight + Terminate + Wrap;
            obj = header + obj;
            return obj;
        }

        public override object VisitEnumSupportStatement([NotNull] EnumSupportStatementContext context)
        {
            var id = (Result)Visit(context.id());
            if (context.Integer() != null)
            {
                var op = "";
                if (context.add() != null)
                {
                    op = (string)Visit(context.add());
                }
                id.text += " = " + op + context.Integer().GetText();
            }
            return id.text + ",";
        }

        public override object VisitNamespaceFunctionStatement([NotNull] NamespaceFunctionStatementContext context)
        {
            var id = (Result)Visit(context.id());
            var obj = "";
            if (context.annotation() != null)
            {
                obj += Visit(context.annotation());
            }
            // 异步
            if (context.t.Type == FlowRight)
            {
                var pout = (string)Visit(context.parameterClauseOut());
                if (pout != "void")
                {
                    pout = $"{Task}<{pout}>";
                }
                else
                {
                    pout = Task;
                }
                obj += $"{id.permission} async static {pout} {id.text}";
            }
            else
            {
                obj += $"{id.permission} static {Visit(context.parameterClauseOut())} {id.text}";
            }

            // 泛型
            if (context.templateDefine() != null)
            {
                obj += Visit(context.templateDefine());
            }
            obj += Visit(context.parameterClauseIn()) + Wrap + BlockLeft + Wrap;
            obj += ProcessFunctionSupport(context.functionSupportStatement());
            obj += BlockRight + Wrap;
            return obj;
        }

        public override object VisitNamespaceInvariableStatement([NotNull] NamespaceInvariableStatementContext context)
        {
            var r1 = (Result)Visit(context.expression(0));
            var r2 = (Result)Visit(context.expression(1));
            var typ = "";
            if (context.type() != null)
            {
                typ = (string)Visit(context.type());
            }
            else
            {
                typ = (string)r2.data;
            }

            var obj = "";
            if (context.annotation() != null)
            {
                obj += Visit(context.annotation());
            }
            obj += $"{r1.permission} readonly static {typ} {r1.text} = {r2.text} {Terminate} {Wrap}";

            return obj;
        }

        public override object VisitNamespaceConstantStatement([NotNull] NamespaceConstantStatementContext context)
        {
            var id = (Result)Visit(context.id());
            var expr = (Result)Visit(context.expression());
            var typ = "";
            if (context.type() != null)
            {
                typ = (string)Visit(context.type());
            }
            else
            {
                typ = (string)expr.data;
            }

            var obj = "";
            if (context.annotation() != null)
            {
                obj += Visit(context.annotation());
            }
            switch (typ)
            {
                case i8:
                    typ = "ubyte";
                    break;
                case i16:
                    typ = "short";
                    break;
                case i32:
                    typ = "int";
                    break;
                case i64:
                    typ = "long";
                    break;

                case u8:
                    typ = "byte";
                    break;
                case u16:
                    typ = "ushort";
                    break;
                case u32:
                    typ = "uint";
                    break;
                case u64:
                    typ = "ulong";
                    break;

                case f32:
                    typ = "float";
                    break;
                case f64:
                    typ = "double";
                    break;

                case str:
                    typ = "string";
                    break;
                default:
                    break;
            }
            obj += $"{id.permission} const {typ} {id.text} = {expr.text} {Terminate} {Wrap}";
            return obj;
        }

        public override object VisitNamespaceVariableStatement([NotNull] NamespaceVariableStatementContext context)
        {
            var r1 = (Result)Visit(context.expression(0));
            var typ = "";
            if (context.type() != null)
            {
                typ = (string)Visit(context.type());
            }
            else if (context.expression().Length == 2)
            {
                var r2 = (Result)Visit(context.expression(1));
                typ = (string)r2.data;
            }
            var obj = "";
            if (context.annotation() != null)
            {
                obj += Visit(context.annotation());
            }
            if (context.namespaceControlSubStatement().Length > 0)
            {
                obj += $"{r1.permission} static {typ} {r1.text} {{";
                foreach (var item in context.namespaceControlSubStatement())
                {
                    obj += Visit(item);
                }
                obj += $"}} {Wrap}";
            }
            else
            {
                obj += $"{r1.permission} static {typ} {r1.text} {{ get;set; }} {Wrap}";
            }
            if (context.expression().Length == 2)
            {
                var r2 = (Result)Visit(context.expression(1));
                obj += $" = {r2.text} {Terminate} {Wrap}";
            }
            return obj;
        }

        public override object VisitNamespaceControlSubStatement([NotNull] NamespaceControlSubStatementContext context)
        {
            var obj = "";
            var id = "";
            id = GetControlSub(context.id().GetText());
            if (context.functionSupportStatement().Length > 0)
            {
                obj += id + BlockLeft;
                foreach (var item in context.functionSupportStatement())
                {
                    obj += Visit(item);
                }
                obj += BlockRight + Wrap;
            }
            else
            {
                obj += id + ";";
            }

            return obj;
        }

        public string GetControlSub(string id)
        {
            switch (id)
            {
                case "get":
                    id = " get ";
                    break;
                case "set":
                    id = " set ";
                    break;
                case "_get":
                    id = " private get ";
                    break;
                case "_set":
                    id = " private set ";
                    break;
                case "add":
                    id = " add ";
                    break;
                case "remove":
                    id = " remove ";
                    break;
                default:
                    break;
            }
            return id;
        }
    }
}